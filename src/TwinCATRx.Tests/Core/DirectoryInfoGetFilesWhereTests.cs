// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using CP.TwinCatRx.Core;

namespace TwinCATRx.Tests.Core;

/// <summary>
/// Tests for DirectoryInfoGetFilesWhere extension methods.
/// </summary>
public class DirectoryInfoGetFilesWhereTests
{
    /// <summary>
    /// Basic filter should return matching files.
    /// </summary>
    [Fact]
    public void GetFilesWhere_Basic_Filter_Works()
    {
        var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "GetFilesWhere_" + Guid.NewGuid()));
        dir.Create();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "a.txt"), string.Empty);
            File.WriteAllText(Path.Combine(dir.FullName, "b.cs"), string.Empty);
            File.WriteAllText(Path.Combine(dir.FullName, "c.asp"), string.Empty);

            var files = dir.GetFilesWhere(f => f.Extension == ".txt" || f.Extension == ".cs");
            files.Should().HaveCount(2);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Search pattern overload.
    /// </summary>
    [Fact]
    public void GetFilesWhere_With_SearchPattern_Works()
    {
        var dir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "GetFilesWhere2_" + Guid.NewGuid()));
        dir.Create();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "a.txt"), string.Empty);
            File.WriteAllText(Path.Combine(dir.FullName, "b.cs"), string.Empty);
            File.WriteAllText(Path.Combine(dir.FullName, "c.asp"), string.Empty);

            var files = dir.GetFilesWhere("*.cs", f => true);
            files.Should().HaveCount(1);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
