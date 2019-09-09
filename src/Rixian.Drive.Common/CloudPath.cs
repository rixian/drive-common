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
    public sealed class CloudPath : IEquatable<CloudPath>, IEquatable<string>
    {
        /// <summary>
        /// The default root path object. I.e. '/'.
        /// </summary>
        public static readonly CloudPath Root = new CloudPath(CloudPathType.None, string.Empty, "/", string.Empty);

        /// <summary>
        /// Root path without partition information.
        /// </summary>
        public static readonly string RelativeRoot = "/";

        /// <summary>
        /// Prefix used for shre paths.
        /// </summary>
        public static readonly string SharePrefix = "//";

        /// <summary>
        /// Directory seperator used in the path.
        /// </summary>
        public static readonly char DirectorySeparator = '/';

        /// <summary>
        /// Partition seperator used in path.
        /// </summary>
        public static readonly char PartitionSeparator = ':';

        /// <summary>
        /// Stream seperator used in path.
        /// </summary>
        public static readonly char StreamSeparator = ':';

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
        private static readonly string CloudPathRegexString = $@"(?:^(?:{RegexDirectorySeparator}{RegexDirectorySeparator}(?<{ShareLabelName}>[^{RegexDirectorySeparator}{InvalidRegexCharacters}]+))|^(?:(?<{PartitionLabelName}>[^{RegexDirectorySeparator}{InvalidRegexCharacters}]+){RegexPartitionSeparator}(?:$|[\/])))?(?<{PathName}>[^{InvalidRegexCharacters}]*)(?:\:(?<{StreamName}>[^{InvalidRegexCharacters}]+))?$";
        private static readonly Regex InternalCloudPathRegex = new Regex(CloudPathRegexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            this.Label = label?.Trim() ?? string.Empty;

            switch (this.Type)
            {
                case CloudPathType.Share:
                    this.FormattedLabel = FormatShare(this.Label);
                    break;
                case CloudPathType.Partition:
                    this.FormattedLabel = FormatPartition(this.Label);
                    break;
                case CloudPathType.None:
                default:
                    this.FormattedLabel = this.Label;
                    break;
            }

            path = path?.Trim() ?? string.Empty;
            if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                path = $"/{path}";
            }

            path = NormalizePath(path);

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
        /// Gets a value indicating whether this path represents a file or not.
        /// </summary>
        public bool IsFilePath
        {
            get
            {
                return !this.IsDirectoryPath;
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
        /// Gets the Label formatted with the partition or share charaters.
        /// </summary>
        public string FormattedLabel { get; }

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

            return TryParse(input);
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

            return TryParse(input);
        }

        /// <summary>
        /// Parses the provided path as an instance of a <see cref="CloudPath"/>.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <returns>The parsed <see cref="CloudPath"/>.</returns>
        public static CloudPath TryParse(string path)
        {
            var trimmedPath = path?.Trim()?.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(trimmedPath))
            {
                return null;
            }

            Match match = InternalCloudPathRegex.Match(trimmedPath);

            var matchedPath = match.Groups[PathName]?.Value;
            if (matchedPath == null || string.IsNullOrWhiteSpace(matchedPath))
            {
                matchedPath = "/";
            }

            var matchedStream = match.Groups[StreamName]?.Value;
            if (matchedStream == null || string.IsNullOrWhiteSpace(matchedStream))
            {
                matchedStream = string.Empty;
            }

            var matchedLabel = match.Groups[PartitionLabelName]?.Value;
            if (matchedLabel == null || string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[ShareLabelName]?.Value;
                if (matchedLabel == null || string.IsNullOrWhiteSpace(matchedLabel))
                {
                    return new CloudPath(CloudPathType.None, string.Empty, matchedPath, matchedStream);
                }

                return new CloudPath(CloudPathType.Share, matchedLabel, matchedPath, matchedStream);
            }
            else
            {
                return new CloudPath(CloudPathType.Partition, matchedLabel, matchedPath, matchedStream);
            }
        }

        /// <summary>
        /// Combines multiple path segments into a single path.
        /// </summary>
        /// <param name="paths">The path segments.</param>
        /// <returns>The full path.</returns>
        public static CloudPath Combine(params string[] paths)
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
        /// Combines multiple path segments into a single path.
        /// </summary>
        /// <param name="paths">The path segments.</param>
        /// <returns>The full path.</returns>
        public static CloudPath Combine(params CloudPath[] paths)
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

            CloudPath firstPath = paths[0];
            CloudPath lastPath = paths[paths.Length - 1];
            CloudPath combinedPath = Combine(paths.Select(p => p.Path).ToArray());
            if (combinedPath == null)
            {
                return null;
            }

            CloudPath outputPath = combinedPath;
            if (firstPath.Type == CloudPathType.Partition)
            {
                outputPath = outputPath.WithPartition(firstPath.Label);
            }
            else if (firstPath.Type == CloudPathType.Share)
            {
                outputPath = outputPath.WithShare(firstPath.Label);
            }

            if (!string.IsNullOrWhiteSpace(lastPath.Stream))
            {
                outputPath = outputPath.WithStream(lastPath.Stream);
            }

            return outputPath;
        }

        /// <summary>
        /// Normalizes the path, including hierarchies and relative paths.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string NormalizePath(string path)
        {
            if (path == null)
            {
                return null;
            }

            var segments = GetPathSegments(path).Where(s => s != ".").ToList();

            IEnumerable<(string s, int i)> indexedBacktrackSegments = segments.Select((s, i) => (s, i)).Where(t => t.s == "..").ToList();

            var backtrackOffset = 0;
            foreach ((var s, var i) in indexedBacktrackSegments)
            {
                segments.RemoveAt(i - backtrackOffset);
                segments.RemoveAt(i - 1 - backtrackOffset);
                backtrackOffset = 2;
            }

            var normalizedPath = segments.Aggregate(string.Empty, (p, s) => p + CloudPath.DirectorySeparator + s);

            if (IsFormattedAsDirectory(path))
            {
                normalizedPath += CloudPath.DirectorySeparator;
            }

            return normalizedPath;
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

            if (path.LastIndexOf(CloudPath.DirectorySeparator) == path.Length - 1)
            {
                return true;
            }

            return false;
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

            return path.Split(new[] { CloudPath.DirectorySeparator }, StringSplitOptions.RemoveEmptyEntries);
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
            var stream = includeStream && !string.IsNullOrWhiteSpace(this.Stream) ? $"{StreamSeparator}{this.Stream}" : null;
            switch (this.Type)
            {
                case CloudPathType.Share:
                    return $"{SharePrefix}{this.Label}{this.Path}{stream}";
                case CloudPathType.Partition:
                    return $"{this.Label}{PartitionSeparator}{this.Path}{stream}";
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

        /// <inheritdoc/>
        public bool Equals(CloudPath other)
        {
            if (other == null)
            {
                return false;
            }

            return
                this.Type == other.Type &&
                string.Equals(this.Path, other.Path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Stream, other.Stream, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Label, other.Label, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public bool Equals(string other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Equals((CloudPath)other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is string)
            {
                return this.Equals(obj as string);
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals(obj as CloudPath);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;

                hash = (hash * 7) + this.Type.GetHashCode();
                hash = (hash * 7) + this.Label?.GetHashCode() ?? 0;
                hash = (hash * 7) + this.Path?.GetHashCode() ?? 0;
                hash = (hash * 7) + this.Stream?.GetHashCode() ?? 0;

                return hash;
            }
        }

        private static string FormatShare(string label) => $"{SharePrefix}{label}";

        private static string FormatPartition(string label) => $"{label}{PartitionSeparator}";
    }
}
