// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENSE file in the project root for full license information.

namespace Rixian.Drive.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public enum CloudPathType
    {
        None = 0,
        Share,
        Partition
    }

    public sealed class CloudPath
    {
        public static readonly string RelativeRoot = "/";

        public static readonly char DirectorySeparator = '/';
        private static readonly string regexDirectorySeparator = $"\\{DirectorySeparator}";

        internal static readonly char PartitionSeparator = ':';
        private static readonly string regexPartitionSeparator = $"\\{PartitionSeparator}";

        public static readonly char[] InvalidCharacters = new[] { '\\', ':', '?', '*', '[', ']' };
        private static readonly string invalidRegexCharacters = InvalidCharacters.Select(c => $"\\{c}").Aggregate((l, r) => $"{l}{r}");

        private static readonly string shareLabelName = "shareLabel";
        private static readonly string partitionLabelName = "partitionLabel";
        private static readonly string pathName = "path";
        private static readonly string streamName = "stream";

        // (?:^(?:\/\/(?<shareLabel>[^\/\\:\*\?\[\]]+))|^(?:(?<partitionLabel>[^\/\\:\*\?\[\]]+):))?(?<partitionPath>[^\\:\*\?\[\]]*)$
        public static readonly string CloudUriRegex = $@"(?:^(?:{regexDirectorySeparator}{regexDirectorySeparator}(?<{shareLabelName}>[^{regexDirectorySeparator}{invalidRegexCharacters}]+))|^(?:(?<{partitionLabelName}>[^{regexDirectorySeparator}{invalidRegexCharacters}]+){regexPartitionSeparator}))?(?<{pathName}>[^{invalidRegexCharacters}]*)(?:\:(?<{streamName}>[^{invalidRegexCharacters}]+))?$";
        public static readonly CloudPath Root = new CloudPath("/");

        public string Stream { get; }

        public string Path { get; }

        public string Label { get; }

        public CloudPathType Type { get; }

        public CloudPath(string path)
        {
            (CloudPathType type, string label, string path, string stream) result = ParseInternal(path);


            if (string.IsNullOrWhiteSpace(result.label))
            {
                result.type = CloudPathType.None;
            }

            if (string.IsNullOrWhiteSpace(result.path))
            {
                throw new ArgumentException("Parameter must have a path", nameof(path));
            }

            this.Type = result.type;
            this.Label = result.label?.Trim();

            result.path = result.path?.Trim();
            if (!result.path.StartsWith("/"))
            {
                result.path = $"/{result.path}";
            }

            this.Path = result.path;
            this.Stream = result.stream;
        }

        public CloudPath(CloudPathType type, string label, string path, string stream)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                type = CloudPathType.None;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Parameter must have a value", nameof(path));
            }

            this.Type = type;
            this.Label = label?.Trim();

            path = path?.Trim();
            if (!path.StartsWith("/"))
            {
                path = $"/{path}";
            }

            this.Path = path;
            this.Stream = stream;
        }

        public override string ToString()
        {
            switch (this.Type)
            {
                case CloudPathType.Share:
                    return $"//{this.Label}{this.Path}";
                case CloudPathType.Partition:
                    return $"{this.Label}:{this.Path}";
                default:
                    return this.Path;
            }
        }

        private static (CloudPathType type, string label, string path, string stream) ParseInternal(string path)
        {
            path = path?.Trim()?.Replace('\\', '/');

            Match match = Regex.Match(path, CloudUriRegex);

            var matchedPath = match.Groups[pathName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedPath))
            {
                matchedPath = "/";
            }

            var matchedStream = match.Groups[streamName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedStream))
            {
                matchedStream = null;
            }

            var matchedLabel = match.Groups[partitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[shareLabelName]?.Value;

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

        public static CloudPath Parse(string path)
        {
            path = path?.Trim()?.Replace('\\', '/');

            Match match = Regex.Match(path, CloudUriRegex);

            var matchedPath = match.Groups[pathName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedPath))
            {
                matchedPath = "/";
            }

            var matchedStream = match.Groups[streamName]?.Value;
            if (string.IsNullOrWhiteSpace(matchedStream))
            {
                matchedStream = null;
            }

            var matchedLabel = match.Groups[partitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[shareLabelName]?.Value;
                return new CloudPath(CloudPathType.Share, matchedLabel, matchedPath, matchedStream);
            }
            else
            {
                return new CloudPath(CloudPathType.Partition, matchedLabel, matchedPath, matchedStream);
            }
        }

        public static string NormalizePath(string path)
        {
            (CloudPathType type, string label, string path, string stream) parsed = ParseInternal(path);

            List<string> segments = GetPathSegments(parsed.path).Where(s => s != ".").ToList();


            IEnumerable<(string s, int i)> indexedBacktrackSegments = segments.Select((s, i) => (s, i)).Where(t => t.s == "..").ToList();

            int backtrackOffset = 0;
            foreach ((string s, int i) segment in indexedBacktrackSegments)
            {
                segments.RemoveAt(segment.i - backtrackOffset);
                segments.RemoveAt(segment.i - 1 - backtrackOffset);
                backtrackOffset = 2;
            }


            var normalizedPath = segments.Aggregate(string.Empty, (p, s) => p + DirectorySeparator + s);

            if (IsFormattedAsDirectory(parsed.path))
            {
                normalizedPath += DirectorySeparator;
            }

            return new CloudPath(parsed.type, parsed.label, normalizedPath, parsed.stream).ToString();
        }


        public static string GetParent(string parentPath)
        {
            if (parentPath == "/")
            {
                throw new InvalidOperationException("There is no parent of root.");
            }

            parentPath = parentPath.TrimEnd('/');
            int lookaheadCount = parentPath.Length;

            int index = parentPath.LastIndexOf(DirectorySeparator, lookaheadCount - 1, lookaheadCount);
            //System.Diagnostics.Debug.Assert(index >= 0);
            parentPath = parentPath.Remove(index + 1);
            return parentPath;
        }

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

        private static (string root, string body)? GetPathInfo(string path)
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

            Match match = Regex.Match(path, CloudUriRegex);

            var matchedPath = match.Groups[pathName]?.Value;
            var matchedLabel = match.Groups[partitionLabelName]?.Value;

            if (string.IsNullOrWhiteSpace(matchedLabel))
            {
                matchedLabel = match.Groups[shareLabelName]?.Value;
                if (string.IsNullOrWhiteSpace(matchedLabel))
                {
                    return (string.Empty, matchedPath);
                }

                return (FormatShare(matchedLabel), matchedPath);
            }
            else
            {
                return (FormatPartition(matchedLabel), matchedPath);
            }
        }

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

            (string root, string body)? pathInfo = GetPathInfo(path);

            return pathInfo?.root;
        }

        public static string GetPathBody(string path)
        {
            if (path == null)
            {
                return null;
            }

            (string root, string body)? pathInfo = GetPathInfo(path);

            return pathInfo?.body;

        }

        public static IReadOnlyList<string> GetPathSegments(string path)
        {
            if (path == null)
            {
                return null;
            }

            return path.Split(new[] { DirectorySeparator }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string FormatShare(string label) => $"//{label}";

        private static string FormatPartition(string label) => $"{label}:";

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

        public static bool IsPathRooted(string path)
        {
            // starts with partirion, share, or slash == true

            /*
             * The IsPathRooted method returns true if the first character is a directory separator character such as "\", or if the path starts with a drive letter and colon (:). 
             * For example, it returns true for path strings such as "\\MyDir\\MyFile.txt", "C:\\MyDir", or "C:MyDir". It returns false for path strings such as "MyDir". 
             */
            throw new NotImplementedException();
        }

        public static bool IsRoot(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            (string root, string body)? pathInfo = GetPathInfo(path);

            var body = pathInfo?.body;

            // The commented-out section is if labels are required
            if (string.IsNullOrWhiteSpace(body) || body == "/")
            {
                return true;
            }

            return false;
        }
    }
}
