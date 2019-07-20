// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Drive.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Manipulates strings that contain directory or file paths.
    /// </summary>
    public sealed class CloudPath
    {
        /// <summary>
        /// Root path without partition information.
        /// </summary>
        public static readonly string RelativeRoot = "/";

        /// <summary>
        /// Directory seperator used in the path.
        /// </summary>
        public static readonly char DirectorySeparator = '/';

        /// <summary>
        /// Partition seperator used in path.
        /// </summary>
        public static readonly char PartitionSeparator = ':';

        /// <summary>
        /// Characters that are not valid in the path.
        /// </summary>
        public static readonly char[] InvalidCharacters = new[] { '\\', ':', '?', '*', '[', ']' };

        private const string ShareLabelName = "shareLabel";
        private const string PartitionLabelName = "partitionLabel";
        private const string PathName = "path";
        private const string StreamName = "stream";

        private static readonly string RegexDirectorySeparator = $"\\{DirectorySeparator}";
        private static readonly string RegexPartitionSeparator = $"\\{PartitionSeparator}";
        private static readonly string InvalidRegexCharacters = InvalidCharacters.Select(c => $"\\{c}").Aggregate((l, r) => $"{l}{r}");
        private static readonly string CloudPathRegexString = $@"(?:^(?:{RegexDirectorySeparator}{RegexDirectorySeparator}(?<{ShareLabelName}>[^{RegexDirectorySeparator}{InvalidRegexCharacters}]+))|^(?:(?<{PartitionLabelName}>[^{RegexDirectorySeparator}{InvalidRegexCharacters}]+){RegexPartitionSeparator}))?(?<{PathName}>[^{InvalidRegexCharacters}]*)(?:\:(?<{StreamName}>[^{InvalidRegexCharacters}]+))?$";

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudPath"/> class.
        /// </summary>
        /// <param name="path">The full or ralative path.</param>
        public CloudPath(string path)
        {
            (CloudPathType type, string label, string path, string stream) result = ParseInternal(path);

            if (string.IsNullOrWhiteSpace(result.label))
            {
                result.type = CloudPathType.None;
            }

            if (string.IsNullOrWhiteSpace(result.path))
            {
                throw new ArgumentException(Properties.Resources.PathNullExceptionMessage, nameof(path));
            }

            this.Type = result.type;
            this.Label = result.label?.Trim();

            result.path = result.path?.Trim();
            if (!result.path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                result.path = $"/{result.path}";
            }

            this.Path = result.path;
            this.Stream = result.stream;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudPath"/> class.
        /// </summary>
        /// <param name="type">The type of path.</param>
        /// <param name="label">The name of the partition label.</param>
        /// <param name="path">The path relative to the partition.</param>
        /// <param name="stream">Optional. The name of the alternate stream.</param>
        public CloudPath(CloudPathType type, string label, string path, string stream)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                type = CloudPathType.None;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Properties.Resources.PathNullExceptionMessage, nameof(path));
            }

            this.Type = type;
            this.Label = label?.Trim();

            path = path?.Trim();
            if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path = $"/{path}";
            }

            this.Path = path;
            this.Stream = stream;
        }

        /// <summary>
        /// Gets the regex used for validating and parsing paths.
        /// </summary>
        public static string CloudPathRegex
        {
            get
            {
                return CloudPathRegexString;
            }
        }

        /// <summary>
        /// Gets the default root path object.
        /// </summary>
        public static CloudPath Root
        {
            get
            {
                return new CloudPath("/");
            }
        }

        /// <summary>
        /// Gets the stream part of the path.
        /// </summary>
        public string Stream { get; }

        /// <summary>
        /// Gets the relative path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the partition label part of the path.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets the path type.
        /// </summary>
        public CloudPathType Type { get; }

        /// <summary>
        /// Gets a value indicating whether this path represents a directory or not.
        /// </summary>
        public bool IsDirectoryPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Path))
                {
                    return false;
                }

                if (this.Path.LastIndexOf(DirectorySeparator) == this.Path.Length - 1)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this path is the root or not, i.e. the '/' character.
        /// </summary>
        public bool IsRootPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Path) || this.Path == "/")
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Converts a <see cref="CloudPath"/> to a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        public static implicit operator CloudPath(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new CloudPath(input);
        }

        /// <summary>
        /// Converts a <see cref="CloudPath"/> to a string.
        /// </summary>
        /// <param name="input">The input <see cref="CloudPath"/>.</param>
        public static implicit operator string(CloudPath input)
        {
            return input?.ToString();
        }

        /// <summary>
        /// Converts a <see cref="CloudPath"/> to a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The <see cref="CloudPath"/> or null.</returns>
        public static CloudPath FromString(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new CloudPath(input);
        }

        /// <summary>
        /// Parses the provided path as an instance of a <see cref="CloudPath"/>.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <returns>The parsed <see cref="CloudPath"/>.</returns>
        public static CloudPath Parse(string path)
        {
            path = path?.Trim()?.Replace('\\', '/');

            Match match = Regex.Match(path, CloudPathRegex);

            var matchedPath = match.Groups[PathName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedPath))
            {
                matchedPath = "/";
            }

            var matchedStream = match.Groups[StreamName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedStream))
            {
                matchedStream = null;
            }

            var matchedLabel = match.Groups[PartitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[ShareLabelName]?.Value;
                return new CloudPath(CloudPathType.Share, matchedLabel, matchedPath, matchedStream);
            }
            else
            {
                return new CloudPath(CloudPathType.Partition, matchedLabel, matchedPath, matchedStream);
            }
        }

        /// <summary>
        /// Normalizes the path, including hierarchies and relative paths.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string NormalizePath(string path)
        {
            (CloudPathType type, string label, string path, string stream) parsed = ParseInternal(path);

            var normalizedPath = NormalizePathInternal(parsed.path);

            return new CloudPath(parsed.type, parsed.label, normalizedPath, parsed.stream).ToString();
        }

        /// <summary>
        /// Returns the path to the parent directory.
        /// </summary>
        /// <param name="path">The path to navigate.</param>
        /// <returns>The path to the parent directory.</returns>
        public static string GetParent(string path)
        {
            if (path == null)
            {
                return null;
            }

            if (path == "/")
            {
                throw new InvalidOperationException(Properties.Resources.NoParentOfRootExceptionMessage);
            }

            path = path.TrimEnd('/');
            var lookaheadCount = path.Length;

            var index = path.LastIndexOf(DirectorySeparator, lookaheadCount - 1, lookaheadCount);

            path = path.Remove(index + 1);
            return path;
        }

        /// <summary>
        /// Normalizes the path to refer to a directory.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string NormalizeToDirectory(string path)
        {
            if (path == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            if (path.LastIndexOf(DirectorySeparator) == path.Length - 1)
            {
                return path;
            }

            return $"{path}{DirectorySeparator}";
        }

        /// <summary>
        /// Normalizes the path to refer to a file.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string NormalizeToFile(string path)
        {
            if (path == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.TrimEnd(DirectorySeparator);
        }

        /// <summary>
        /// Checks if the path if formatted as a directory path (trailing '/').
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path represents a directory, otherwise false.</returns>
        public static bool IsFormattedAsDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.LastIndexOf(DirectorySeparator) == path.Length - 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the extension of the file on the path.
        /// </summary>
        /// <param name="path">The path to modify.</param>
        /// <param name="extension">The new extension.</param>
        /// <returns>The modified path with the new extension.</returns>
        public static string ChangeExtension(string path, string extension)
        {
            /*
             * On Windows-based desktop platforms, if path is null or an empty string (""), the path information is returned unmodified.
             * If extension is null, the returned string contains the specified path with its extension removed.
             * If path has no extension, and extension is not null, the returned path string contains extension appended to the end of path.
             */

            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            extension = extension?.TrimStart('.');
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = $".{extension}";
            }

            var periodIndex = path.LastIndexOf('.');
            if (periodIndex < 0)
            {
                return $"{path}{extension}";
            }

            if (periodIndex == path.Length - 1)
            {
                return $"{path.TrimEnd('.')}{extension}";
            }

            var pathWithoutExtension = path.Substring(0, periodIndex);
            return $"{pathWithoutExtension}{extension}";
        }

        /// <summary>
        /// Combines multiple path segments into a single path.
        /// </summary>
        /// <param name="paths">The path segments.</param>
        /// <returns>The full path.</returns>
        public static string Combine(params string[] paths)
        {
            /*
             * paths should be an array of the parts of the path to combine. If the one of the subsequent paths is an absolute path, then the combine operation resets starting with that absolute path, discarding all previous combined paths.
             *
             * Zero-length strings are omitted from the combined path.
             *
             * The parameters are not parsed if they have white space.
             *
             * Not all invalid characters for directory and file names are interpreted as unacceptable by the Combine method, because you can use these characters for search wildcard characters. For example, while Path.Combine("c:\\", "*.txt") might be invalid if you were to create a file from it, it is valid as a search string. It is therefore successfully interpreted by the Combine method.
             */

            if (paths == null || paths.Length == 0)
            {
                return null;
            }

            if (paths.Length == 1)
            {
                return paths[0];
            }

            return paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Aggregate((l, r) => $"{l.TrimEnd(DirectorySeparator)}{DirectorySeparator}{r.TrimStart(DirectorySeparator)}");
        }

        /// <summary>
        /// Gets the path for the directory.
        /// </summary>
        /// <param name="path">The input path.</param>
        /// <returns>The path to the directory.</returns>
        public static string GetDirectoryName(string path)
        {
            /*
             * Directory information for path, or null if path denotes a root directory or is null. Returns Empty if path does not contain directory information.
             *
             * In most cases, the string returned by this method consists of all characters in the path up to but not including the last DirectorySeparatorChar or AltDirectorySeparatorChar.
             * If the path consists of a root directory, such as "c:\", null is returned. Note that this method does not support paths using "file:".
             * Because the returned path does not include the DirectorySeparatorChar or AltDirectorySeparatorChar, passing the returned path back into the GetDirectoryName method will result
             * in the truncation of one folder level per subsequent call on the result string. For example, passing the path "C:\Directory\SubDirectory\test.txt" into the GetDirectoryName
             * method will return "C:\Directory\SubDirectory". Passing that string, "C:\Directory\SubDirectory", into GetDirectoryName will result in "C:\Directory".
             */

            if (path == null)
            {
                return null;
            }

            var separatorIndex = path.LastIndexOf(DirectorySeparator);
            if (separatorIndex <= 0)
            {
                return null;
            }

            return path.Substring(0, separatorIndex);
        }

        /// <summary>
        /// Gets the extension of the file this path represents.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The file extension.</returns>
        public static string GetExtension(string path)
        {
            /*
             * The extension of the specified path (including the period "."), or null, or Empty. If path is null, GetExtension(String) returns null. If path does not have extension information, GetExtension(String) returns Empty.
             *
             * The extension of path is obtained by searching path for a period (.), starting with the last character in path and continuing toward the start of path.
             * If a period is found before a DirectorySeparatorChar or AltDirectorySeparatorChar character, the returned string contains the period and the characters after it; otherwise, Empty is returned.
             */

            if (path == null)
            {
                return null;
            }

            var periodIndex = path.LastIndexOf('.');
            if (periodIndex == -1)
            {
                return string.Empty;
            }

            if (periodIndex == path.Length - 1)
            {
                return string.Empty;
            }

            return path.Substring(periodIndex, path.Length - periodIndex);
        }

        /// <summary>
        /// Gets the name of the file and extension this path represents.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The file name and extension.</returns>
        public static string GetFileName(string path)
        {
            /*
            The characters after the last directory character in path. If the last character of path is a directory or volume separator character, this method returns Empty. If path is null, this method returns null.
            */

            if (path == null)
            {
                return null;
            }

            var separatorIndex = path.LastIndexOf(DirectorySeparator);
            if (separatorIndex < 0)
            {
                return path;
            }

            if (separatorIndex == path.Length - 1)
            {
                return string.Empty;
            }

            return path.Substring(separatorIndex + 1, path.Length - separatorIndex - 1);
        }

        /// <summary>
        /// Gets the name of the file this path represents.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The file name.</returns>
        public static string GetFileNameWithoutExtension(string path)
        {
            // The string returned by GetFileName(String), minus the last period(.) and all characters following it.
            var fileName = GetFileName(path);
            if (fileName == null)
            {
                return null;
            }

            var periodIndex = fileName.LastIndexOf('.');
            if (periodIndex == -1)
            {
                return fileName;
            }

            if (periodIndex == fileName.Length - 1)
            {
                return string.Empty;
            }

            return fileName.Substring(0, periodIndex);
        }

        /// <summary>
        /// Gets the partition or share part of the path.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The path root.</returns>
        public static string GetPathRoot(string path)
        {
            /*
             * The root directory of path, or null if path is null, or an empty string if path does not contain root directory information.
             *
             *  Possible patterns for the string returned by this method are as follows:

                An empty string (path specified a relative path on the current drive or volume).

                "" (path specified an absolute path on the current drive).

                "X:" (path specified a relative path on a drive, where X represents a drive or volume letter).

                "X:" (path specified an absolute path on a given drive).

                "\\ComputerName\SharedFolder" (a UNC path).

                "\?\C:" (a DOS device path, supported in .NET Core 1.1 and later versions and in .NET Framework 4.6.2 and later versions)

             */

            if (path == null)
            {
                return null;
            }

            (string label, string root, string body)? pathInfo = GetPathInfo(path);

            return pathInfo?.root;
        }

        /// <summary>
        /// Gets the body part of the path.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The path body.</returns>
        public static string GetPathBody(string path)
        {
            if (path == null)
            {
                return null;
            }

            (string label, string root, string body)? pathInfo = GetPathInfo(path);

            return pathInfo?.body;
        }

        /// <summary>
        /// Gets the label part of the path.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The path label.</returns>
        public static string GetPathLabel(string path)
        {
            if (path == null)
            {
                return null;
            }

            (string label, string root, string body)? pathInfo = GetPathInfo(path);

            return pathInfo?.label;
        }

        /// <summary>
        /// Gets the path segments, determined by the directory seperator character.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>The path segments.</returns>
        public static IReadOnlyList<string> GetPathSegments(string path)
        {
            if (path == null)
            {
                return null;
            }

            return path.Split(new[] { DirectorySeparator }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Determines if the path has an extension.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>True if the path has an extension, otherwise false.</returns>
        public static bool HasExtension(string path)
        {
            /*
             * true if the characters that follow the last directory separator (\\ or /) or volume separator (:) in the path include a period (.) followed by one or more characters; otherwise, false.
             *
             * Starting from the end of path, this method searches for a period (.) followed by at least one character. If this pattern is found before a DirectorySeparatorChar,
             * AltDirectorySeparatorChar, or VolumeSeparatorChar character is encountered, this method returns true.
             */

            if (path == null)
            {
                return false;
            }

            var periodIndex = path.LastIndexOf('.');
            if (periodIndex == -1)
            {
                return false;
            }

            if (periodIndex == path.Length - 1)
            {
                return false;
            }

            var separatorIndex = path.LastIndexOf(DirectorySeparator);
            if (separatorIndex > periodIndex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the path starts at a root location. Either  '/' or a partition label.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>True if the path is rooted, otherwise false.</returns>
        public static bool IsPathRooted(string path)
        {
            if (path == null)
            {
                return false;
            }

            // starts with partition, share, or slash == true

            /*
             * The IsPathRooted method returns true if the first character is a directory separator character such as "\", or if the path starts with a drive letter and colon (:).
             * For example, it returns true for path strings such as "\\MyDir\\MyFile.txt", "C:\\MyDir", or "C:MyDir". It returns false for path strings such as "MyDir".
             */
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines if the path is the root, i.e. the '/' character.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns>True if the path is root, otherwise false.</returns>
        public static bool IsRoot(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            (string label, string root, string body)? pathInfo = GetPathInfo(path);

            var body = pathInfo?.body;

            // The commented-out section is if labels are required
            if (string.IsNullOrWhiteSpace(body) || body == "/")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the full path as a string.
        /// </summary>
        /// <returns>The full path.</returns>
        public override string ToString()
        {
            return this.ToString(true);
        }

        /// <summary>
        /// Returns the full path as a string, optionally including the stream name.
        /// </summary>
        /// <param name="includeStream">True to include the stream name, otherwise false.</param>
        /// <returns>The path string.</returns>
        public string ToString(bool includeStream)
        {
            var stream = includeStream && !string.IsNullOrWhiteSpace(this.Stream) ? $":{this.Stream}" : null;
            switch (this.Type)
            {
                case CloudPathType.Share:
                    return $"//{this.Label}{this.Path}{stream}";
                case CloudPathType.Partition:
                    return $"{this.Label}:{this.Path}{stream}";
                default:
                    return $"{this.Path}{stream}";
            }
        }

        /// <summary>
        /// Returns a new <see cref="CloudPath"/> formatted as a directory.
        /// </summary>
        /// <returns>A <see cref="CloudPath"/> formatted as a directory.</returns>
        public CloudPath FormatAsDirectory()
        {
            if (this.IsDirectoryPath)
            {
                return this;
            }
            else if (!string.IsNullOrWhiteSpace(this.Stream))
            {
                throw new InvalidOperationException(Properties.Resources.FilePathWithStreamToDirectoryPathConversionExceptionMessage);
            }
            else
            {
                return new CloudPath(this.Type, this.Label, this.Path + DirectorySeparator, this.Stream);
            }
        }

        /// <summary>
        /// Returns a new <see cref="CloudPath"/> formatted as a directory.
        /// </summary>
        /// <returns>A <see cref="CloudPath"/> formatted as a directory.</returns>
        public CloudPath FormatAsFile()
        {
            if (!this.IsDirectoryPath)
            {
                return this;
            }
            else if (this.IsRootPath)
            {
                throw new InvalidOperationException(Properties.Resources.RootDirectoryPathToFilePathConversionExceptionMessage);
            }
            else
            {
                return new CloudPath(this.Type, this.Label, this.Path.TrimEnd(DirectorySeparator), this.Stream);
            }
        }

        private static (CloudPathType type, string label, string path, string stream) ParseInternal(string path)
        {
            path = path?.Trim()?.Replace('\\', '/');

            Match match = Regex.Match(path, CloudPathRegex);

            var matchedPath = match.Groups[PathName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedPath))
            {
                matchedPath = "/";
            }

            matchedPath = NormalizePathInternal(matchedPath);

            var matchedStream = match.Groups[StreamName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedStream))
            {
                matchedStream = null;
            }

            var matchedLabel = match.Groups[PartitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[ShareLabelName]?.Value;

                if (string.IsNullOrWhiteSpace(matchedLabel))
                {
                    return (CloudPathType.None, null, matchedPath, matchedStream);
                }

                return (CloudPathType.Share, matchedLabel, matchedPath, matchedStream);
            }
            else
            {
                return (CloudPathType.Partition, matchedLabel, matchedPath, matchedStream);
            }
        }

        private static (string label, string root, string body)? GetPathInfo(string path)
        {
            /*
             * The root directory of path, or null if path is null, or an empty string if path does not contain root directory information.
             *
             *  Possible patterns for the string returned by this method are as follows:

                An empty string (path specified a relative path on the current drive or volume).

                "" (path specified an absolute path on the current drive).

                "X:" (path specified a relative path on a drive, where X represents a drive or volume letter).

                "X:" (path specified an absolute path on a given drive).

                "\\ComputerName\SharedFolder" (a UNC path).

                "\?\C:" (a DOS device path, supported in .NET Core 1.1 and later versions and in .NET Framework 4.6.2 and later versions)

             */

            if (path == null)
            {
                return null;
            }

            path = path.Trim().Replace('\\', '/');

            Match match = Regex.Match(path, CloudPathRegex);

            var matchedPath = match.Groups[PathName]?.Value;
            var matchedLabel = match.Groups[PartitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[ShareLabelName]?.Value;
                if (string.IsNullOrWhiteSpace(matchedLabel))
                {
                    return (string.Empty, string.Empty, matchedPath);
                }

                return (matchedLabel, FormatShare(matchedLabel), matchedPath);
            }
            else
            {
                return (matchedLabel, FormatPartition(matchedLabel), matchedPath);
            }
        }

        private static string NormalizePathInternal(string path)
        {
            var segments = GetPathSegments(path).Where(s => s != ".").ToList();

            IEnumerable<(string s, int i)> indexedBacktrackSegments = segments.Select((s, i) => (s, i)).Where(t => t.s == "..").ToList();

            var backtrackOffset = 0;
            foreach ((var s, var i) in indexedBacktrackSegments)
            {
                segments.RemoveAt(i - backtrackOffset);
                segments.RemoveAt(i - 1 - backtrackOffset);
                backtrackOffset = 2;
            }

            var normalizedPath = segments.Aggregate(string.Empty, (p, s) => p + DirectorySeparator + s);

            if (IsFormattedAsDirectory(path))
            {
                normalizedPath += DirectorySeparator;
            }

            return normalizedPath;
        }

        private static string FormatShare(string label) => $"//{label}";

        private static string FormatPartition(string label) => $"{label}:";
    }
}
