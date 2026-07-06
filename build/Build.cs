// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

//using CP.BuildTools;
using System.Linq;
using CP.BuildTools;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Tools.PowerShell;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace TwinCATRx.Build;

sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [GitRepository] readonly GitRepository Repository = null!;
    [Solution(GenerateProjects = true)] readonly Solution Solution = null!;
    [NerdbankGitVersioning] readonly NerdbankGitVersioning NerdbankVersioning = null!;
    [Parameter][Secret] readonly string NuGetApiKey = null!;
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    static AbsolutePath PackagesDirectory => RootDirectory / "output";

    Target Print => _ => _
        .Executes(() => Log.Information("NerdbankVersioning = {Value}", NerdbankVersioning.NuGetPackageVersion));

    Target Clean => _ => _
        .Before(Restore)
        .Executes(async () =>
        {
            if (IsLocalBuild)
            {
                return;
            }

            PackagesDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore, Print)
        .Executes(() => DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()));

    Target Pack => _ => _
    .After(Compile)
    .Produces(PackagesDirectory / "*.nupkg")
    .Executes(() =>
    {
        if (Repository.IsOnMainOrMasterBranch())
        {
            var packableProjects = Solution.GetPackableProjects();

            DotNetPack(settings => settings
                .SetConfiguration(Configuration)
                .SetVersion(NerdbankVersioning.SimpleVersion)
                .SetOutputDirectory(PackagesDirectory)
                .CombineWith(packableProjects, (packSettings, project) =>
                    packSettings.SetProject(project)));
        }
    });

    Target Deploy => _ => _
    .DependsOn(Pack)
    .Requires(() => NuGetApiKey)
    .Executes(() =>
    {
        if (Repository.IsOnMainOrMasterBranch())
        {
            DotNetNuGetPush(settings => settings
                        .SetSource(this.PublicNuGetSource())
                        .SetSkipDuplicate(true)
                        .SetApiKey(NuGetApiKey)
                        .CombineWith(PackagesDirectory.GlobFiles("*.nupkg"), (s, v) => s.SetTargetPath(v)),
                    degreeOfParallelism: 5, completeOnFailure: true);
        }
    });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = Solution.AllProjects.Where(p => p.Name.EndsWith("Tests")).ToList();
            if (testProjects is null || testProjects.Count == 0)
            {
                Log.Warning("No test projects found.");
                return;
            }
            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .SetNoBuild(true)
                .CombineWith(testProjects, (testSettings, project) =>
                    testSettings.SetProjectFile(project)));
        });
}
