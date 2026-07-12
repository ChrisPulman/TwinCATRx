// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace CP.TwinCatRx.Core.Reactive;
#else
namespace CP.TwinCatRx.Core;
#endif

/// <summary>Provides filtered file enumeration extensions for <see cref="DirectoryInfo"/>.</summary>
public static class DirectoryInfoExtensions
{
    /// <summary>Extends directory instances with filtered file enumeration helpers.</summary>
    /// <param name="directory">The directory to search.</param>
    extension(DirectoryInfo directory)
    {
        /// <summary>Gets files from the directory that satisfy the supplied predicate.</summary>
        /// <param name="predicate">The file predicate.</param>
        /// <returns>The matching files.</returns>
        public FileInfo[] GetFilesWhere(Func<FileInfo, bool> predicate) =>
            GetFilesWhereCore(directory, "*", SearchOption.TopDirectoryOnly, predicate);

        /// <summary>Gets files matching the search pattern that satisfy the supplied predicate.</summary>
        /// <param name="searchPattern">The file search pattern.</param>
        /// <param name="predicate">The file predicate.</param>
        /// <returns>The matching files.</returns>
        public FileInfo[] GetFilesWhere(string searchPattern, Func<FileInfo, bool> predicate) =>
            GetFilesWhereCore(directory, searchPattern, SearchOption.TopDirectoryOnly, predicate);

        /// <summary>Gets files matching the search pattern and search option that satisfy the supplied predicate.</summary>
        /// <param name="searchPattern">The file search pattern.</param>
        /// <param name="searchOption">The search option.</param>
        /// <param name="predicate">The file predicate.</param>
        /// <returns>The matching files.</returns>
        public FileInfo[] GetFilesWhere(string searchPattern, SearchOption searchOption, Func<FileInfo, bool> predicate) =>
            GetFilesWhereCore(directory, searchPattern, searchOption, predicate);

        /// <summary>Gets files matching any search pattern that satisfy the supplied predicate.</summary>
        /// <param name="searchPatterns">The file search patterns.</param>
        /// <param name="predicate">The file predicate.</param>
        /// <returns>The matching files.</returns>
        public FileInfo[] GetFilesWhere(string[] searchPatterns, Func<FileInfo, bool> predicate) =>
            GetFilesWhereCore(directory, searchPatterns, SearchOption.TopDirectoryOnly, predicate);

        /// <summary>Gets files matching any search pattern and search option that satisfy the supplied predicate.</summary>
        /// <param name="searchPatterns">The file search patterns.</param>
        /// <param name="searchOption">The search option.</param>
        /// <param name="predicate">The file predicate.</param>
        /// <returns>The matching files.</returns>
        public FileInfo[] GetFilesWhere(string[] searchPatterns, SearchOption searchOption, Func<FileInfo, bool> predicate) =>
            GetFilesWhereCore(directory, searchPatterns, searchOption, predicate);
    }

    /// <summary>Gets filtered files for one search pattern.</summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <param name="searchOption">The search option.</param>
    /// <param name="predicate">The file predicate.</param>
    /// <returns>The matching files.</returns>
    private static FileInfo[] GetFilesWhereCore(DirectoryInfo directory, string searchPattern, SearchOption searchOption, Func<FileInfo, bool> predicate)
    {
        var checkedDirectory = Require(directory, nameof(directory));
        var checkedSearchPattern = Require(searchPattern, nameof(searchPattern));
        var checkedPredicate = Require(predicate, nameof(predicate));

        var matches = new List<FileInfo>();
        foreach (var path in Directory.EnumerateFiles(checkedDirectory.FullName, checkedSearchPattern, searchOption))
        {
            var file = new FileInfo(path);
            if (checkedPredicate(file))
            {
                matches.Add(file);
            }
        }

        return [.. matches];
    }

    /// <summary>Gets filtered files for multiple search patterns.</summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="searchPatterns">The file search patterns.</param>
    /// <param name="searchOption">The search option.</param>
    /// <param name="predicate">The file predicate.</param>
    /// <returns>The matching files.</returns>
    private static FileInfo[] GetFilesWhereCore(DirectoryInfo directory, string[] searchPatterns, SearchOption searchOption, Func<FileInfo, bool> predicate)
    {
        var checkedDirectory = Require(directory, nameof(directory));
        var checkedSearchPatterns = Require(searchPatterns, nameof(searchPatterns));
        var checkedPredicate = Require(predicate, nameof(predicate));

        var matches = new List<FileInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var searchPattern in checkedSearchPatterns)
        {
            foreach (var path in Directory.EnumerateFiles(checkedDirectory.FullName, searchPattern, searchOption))
            {
                if (!seenPaths.Add(path))
                {
                    continue;
                }

                var file = new FileInfo(path);
                if (checkedPredicate(file))
                {
                    matches.Add(file);
                }
            }
        }

        return [.. matches];
    }

    /// <summary>Returns a value or throws when it is null.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>The non-null value.</returns>
    private static T Require<T>(T? value, string parameterName)
        where T : class =>
        value ?? throw new ArgumentNullException(parameterName);
}
