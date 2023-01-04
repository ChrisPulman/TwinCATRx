using System;
using System.IO;
using System.Linq;

namespace CP.TwinCatRx.Core
{
    /// <summary>
    /// Directory Info Extension.
    /// </summary>
    public static partial class DirectoryInfoExtension
    {
        /// <summary>Returns an enumerable collection of file names in a specified @this.</summary>
        /// <returns>
        ///     An enumerable collection of the full names (including paths) for the files in the directory specified by
        ///     <paramref
        ///         name="this" />
        ///     .
        /// </returns>
        /// <param name="this">The directory to search. </param>
        /// <param name="predicate">The function. </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="this " />is a zero-length string, contains only white space, or contains invalid characters as defined by
        ///     <see
        ///         cref="Path.GetInvalidPathChars" />
        ///     .
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="this" /> is null.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="this" /> is invalid, such as referring to an unmapped drive.
        /// </exception>
        /// <exception cref="IOException">
        ///     <paramref name="this" /> is a file name.
        /// </exception>
        /// <exception cref="PathTooLongException">The specified @this, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="UnauthorizedAccessException">The caller is Unauthorized.</exception>
        /// <example>
        ///     <code>
        ///           using System;
        ///           using System.IO;
        ///           using Microsoft.VisualStudio.TestTools.UnitTesting;
        ///
        ///
        ///           namespace ExtensionMethods.Examples
        ///           {
        ///               [TestClass]
        ///               public class System_IO_DirectoryInfo_GetFilesWhere
        ///               {
        ///                   [TestMethod]
        ///                   public void GetFilesWhere()
        ///                   {
        ///                       // Type
        ///                       var root = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, &quot;System_IO_DirectoryInfo_GetFilesWhere&quot;));
        ///                       Directory.CreateDirectory(root.FullName);
        ///
        ///                       var file1 = new FileInfo(Path.Combine(root.FullName, &quot;test.txt&quot;));
        ///                       var file2 = new FileInfo(Path.Combine(root.FullName, &quot;test.cs&quot;));
        ///                       var file3 = new FileInfo(Path.Combine(root.FullName, &quot;test.asp&quot;));
        ///                       file1.Create();
        ///                       file2.Create();
        ///                       file3.Create();
        ///
        ///                       // Exemples
        ///                       FileInfo[] result = root.GetFilesWhere(x =&gt; x.Extension == &quot;.txt&quot; || x.Extension == &quot;.cs&quot;);
        ///
        ///                       // Unit Test
        ///                       Assert.AreEqual(2, result.Length);
        ///                   }
        ///               }
        ///           }
        ///     </code>
        /// </example>
        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, Func<FileInfo, bool> predicate) => Directory.EnumerateFiles(@this.FullName).Select(x => new FileInfo(x)).Where(predicate).ToArray();

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified @this.
        /// </summary>
        /// <param name="this">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="this" />.</param>
        /// <param name="predicate">The function.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by
        /// <paramref name="this" />
        /// and that match the specified search pattern.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="this " />is a zero-length string, contains only white space, or contains invalid characters as defined by
        /// <see cref="Path.GetInvalidPathChars" />
        /// .- or -<paramref name="searchPattern" /> does not contain a valid pattern.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="this" /> is null.-or-<paramref name="searchPattern" /> is null.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="this" /> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="this" /> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified @this, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is Unauthorized.</exception>
        /// <example>
        ///   <code>
        /// using System;
        /// using System.IO;
        /// using Microsoft.VisualStudio.TestTools.UnitTesting;
        /// namespace ExtensionMethods.Examples
        /// {
        /// [TestClass]
        /// public class System_IO_DirectoryInfo_GetFilesWhere
        /// {
        /// [TestMethod]
        /// public void GetFilesWhere()
        /// {
        /// // Type
        /// var root = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System_IO_DirectoryInfo_GetFilesWhere"));
        /// Directory.CreateDirectory(root.FullName);
        /// var file1 = new FileInfo(Path.Combine(root.FullName, "test.txt"));
        /// var file2 = new FileInfo(Path.Combine(root.FullName, "test.cs"));
        /// var file3 = new FileInfo(Path.Combine(root.FullName, "test.asp"));
        /// file1.Create();
        /// file2.Create();
        /// file3.Create();
        /// // Exemples
        /// FileInfo[] result = root.GetFilesWhere(x =&gt; x.Extension == ".txt" || x.Extension == ".cs");
        /// // Unit Test
        /// Assert.AreEqual(2, result.Length);
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, string searchPattern, Func<FileInfo, bool> predicate) => Directory.EnumerateFiles(@this.FullName, searchPattern).Select(x => new FileInfo(x)).Where(predicate).ToArray();

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified @this, and optionally searches subdirectories.
        /// </summary>
        /// <param name="this">The directory to search.</param>
        /// <param name="searchPattern">The search string to match against the names of directories in <paramref name="this" />.</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.The default value is
        /// <see cref="SearchOption.TopDirectoryOnly" />
        /// .</param>
        /// <param name="predicate">The function.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by
        /// <paramref name="this" />
        /// and that match the specified search pattern and option.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="this " />is a zero-length string, contains only white space, or contains invalid characters as defined by
        /// <see cref="Path.GetInvalidPathChars" />
        /// .- or -<paramref name="searchPattern" /> does not contain a valid pattern.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="this" /> is null.-or-<paramref name="searchPattern" /> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption" /> is not a valid <see cref="SearchOption" /> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="this" /> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="this" /> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified @this, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is Unauthorized.</exception>
        /// <example>
        ///   <code>
        /// using System;
        /// using System.IO;
        /// using Microsoft.VisualStudio.TestTools.UnitTesting;
        /// namespace ExtensionMethods.Examples
        /// {
        /// [TestClass]
        /// public class System_IO_DirectoryInfo_GetFilesWhere
        /// {
        /// [TestMethod]
        /// public void GetFilesWhere()
        /// {
        /// // Type
        /// var root = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System_IO_DirectoryInfo_GetFilesWhere"));
        /// Directory.CreateDirectory(root.FullName);
        /// var file1 = new FileInfo(Path.Combine(root.FullName, "test.txt"));
        /// var file2 = new FileInfo(Path.Combine(root.FullName, "test.cs"));
        /// var file3 = new FileInfo(Path.Combine(root.FullName, "test.asp"));
        /// file1.Create();
        /// file2.Create();
        /// file3.Create();
        /// // Exemples
        /// FileInfo[] result = root.GetFilesWhere(x =&gt; x.Extension == ".txt" || x.Extension == ".cs");
        /// // Unit Test
        /// Assert.AreEqual(2, result.Length);
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, string searchPattern, SearchOption searchOption, Func<FileInfo, bool> predicate) => Directory.EnumerateFiles(@this.FullName, searchPattern, searchOption).Select(x => new FileInfo(x)).Where(predicate).ToArray();

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified @this.
        /// </summary>
        /// <param name="this">The directory to search.</param>
        /// <param name="searchPatterns">The search string to match against the names of directories in <paramref name="this" />.</param>
        /// <param name="predicate">The function.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by
        /// <paramref name="this" />
        /// and that match the specified search pattern.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="this " />is a zero-length string, contains only white space, or contains invalid characters as defined by
        /// <see cref="Path.GetInvalidPathChars" />
        /// .- or -<paramref name="searchPatterns" /> does not contain a valid pattern.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="this" /> is null.-or-<paramref name="searchPatterns" /> is null.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="this" /> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="this" /> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified @this, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is Unauthorized.</exception>
        /// <example>
        ///   <code>
        /// using System;
        /// using System.IO;
        /// using Microsoft.VisualStudio.TestTools.UnitTesting;
        /// namespace ExtensionMethods.Examples
        /// {
        /// [TestClass]
        /// public class System_IO_DirectoryInfo_GetFilesWhere
        /// {
        /// [TestMethod]
        /// public void GetFilesWhere()
        /// {
        /// // Type
        /// var root = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System_IO_DirectoryInfo_GetFilesWhere"));
        /// Directory.CreateDirectory(root.FullName);
        /// var file1 = new FileInfo(Path.Combine(root.FullName, "test.txt"));
        /// var file2 = new FileInfo(Path.Combine(root.FullName, "test.cs"));
        /// var file3 = new FileInfo(Path.Combine(root.FullName, "test.asp"));
        /// file1.Create();
        /// file2.Create();
        /// file3.Create();
        /// // Exemples
        /// FileInfo[] result = root.GetFilesWhere(x =&gt; x.Extension == ".txt" || x.Extension == ".cs");
        /// // Unit Test
        /// Assert.AreEqual(2, result.Length);
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, string[] searchPatterns, Func<FileInfo, bool> predicate) => searchPatterns.SelectMany(@this.GetFiles).Distinct().Where(predicate).ToArray();

        /// <summary>
        /// Returns an enumerable collection of file names that match a search pattern in a specified @this, and optionally searches subdirectories.
        /// </summary>
        /// <param name="this">The directory to search.</param>
        /// <param name="searchPatterns">The search string to match against the names of directories in <paramref name="this" />.</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.The default value is
        /// <see cref="SearchOption.TopDirectoryOnly" />
        /// .</param>
        /// <param name="predicate">The function.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory specified by
        /// <paramref name="this" />
        /// and that match the specified search pattern and option.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="this " />is a zero-length string, contains only white space, or contains invalid characters as defined by
        /// <see cref="Path.GetInvalidPathChars" />
        /// .- or -<paramref name="searchPatterns" /> does not contain a valid pattern.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="this" /> is null.-or-<paramref name="searchPatterns" /> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption" /> is not a valid <see cref="SearchOption" /> value.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="this" /> is invalid, such as referring to an unmapped drive.</exception>
        /// <exception cref="IOException"><paramref name="this" /> is a file name.</exception>
        /// <exception cref="PathTooLongException">The specified @this, file name, or combined exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller is Unauthorized.</exception>
        /// <example>
        ///   <code>
        /// using System;
        /// using System.IO;
        /// using Microsoft.VisualStudio.TestTools.UnitTesting;
        /// namespace ExtensionMethods.Examples
        /// {
        /// [TestClass]
        /// public class System_IO_DirectoryInfo_GetFilesWhere
        /// {
        /// [TestMethod]
        /// public void GetFilesWhere()
        /// {
        /// // Type
        /// var root = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System_IO_DirectoryInfo_GetFilesWhere"));
        /// Directory.CreateDirectory(root.FullName);
        /// var file1 = new FileInfo(Path.Combine(root.FullName, "test.txt"));
        /// var file2 = new FileInfo(Path.Combine(root.FullName, "test.cs"));
        /// var file3 = new FileInfo(Path.Combine(root.FullName, "test.asp"));
        /// file1.Create();
        /// file2.Create();
        /// file3.Create();
        /// // Exemples
        /// FileInfo[] result = root.GetFilesWhere(x =&gt; x.Extension == ".txt" || x.Extension == ".cs");
        /// // Unit Test
        /// Assert.AreEqual(2, result.Length);
        /// }
        /// }
        /// }
        /// </code>
        /// </example>
        public static FileInfo[] GetFilesWhere(this DirectoryInfo @this, string[] searchPatterns, SearchOption searchOption, Func<FileInfo, bool> predicate) => searchPatterns.SelectMany(x => @this.GetFiles(x, searchOption)).Distinct().Where(predicate).ToArray();
    }
}
