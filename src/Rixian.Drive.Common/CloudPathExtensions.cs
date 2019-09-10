// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Drive.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Additional extensions for the CloudPath class.
    /// </summary>
    public static class CloudPathExtensions
    {
        /// <summary>
        /// Returns the path to the parent directory.
        /// </summary>
        /// <param name="cloudPath">The path to navigate.</param>
        /// <returns>The path to the parent directory.</returns>
        public static CloudPath GetParent(this CloudPath cloudPath)
        {
            var path = cloudPath?.ToString();
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

            var index = path.LastIndexOf(CloudPath.DirectorySeparator, lookaheadCount - 1, lookaheadCount);

            path = path.Remove(index + 1);
            return path;
        }

        /// <summary>
        /// Normalizes the path to refer to a directory.
        /// </summary>
        /// <param name="cloudPath">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static CloudPath NormalizeToDirectory(this CloudPath cloudPath)
        {
            if (cloudPath == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(cloudPath.Stream))
            {
                throw new InvalidOperationException(Properties.Resources.PathNoramizedWithStreamExceptionMessage);
            }

            var path = cloudPath?.ToString();
            if (path == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            if (path.LastIndexOf(CloudPath.DirectorySeparator) == path.Length - 1)
            {
                return path;
            }

            return $"{path}{CloudPath.DirectorySeparator}";
        }

        /// <summary>
        /// Normalizes the path to refer to a file.
        /// </summary>
        /// <param name="cloudPath">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static CloudPath NormalizeToFile(this CloudPath cloudPath)
        {
            if (cloudPath == null)
            {
                return null;
            }

            var path = cloudPath?.ToString();
            if (path == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.TrimEnd(CloudPath.DirectorySeparator);
        }

        /// <summary>
        /// Checks if the path if formatted as a directory path (trailing '/').
        /// </summary>
        /// <param name="cloudPath">The path to check.</param>
        /// <returns>True if the path represents a directory, otherwise false.</returns>
        public static bool IsFormattedAsDirectory(this CloudPath cloudPath)
        {
            var path = cloudPath?.ToString();
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
        /// Changes the extension of the file on the path.
        /// </summary>
        /// <param name="cloudPath">The path to modify.</param>
        /// <param name="extension">The new extension.</param>
        /// <returns>The modified path with the new extension.</returns>
        public static CloudPath ChangeExtension(this CloudPath cloudPath, string extension)
        {
            /*
             * On Windows-based desktop platforms, if path is null or an empty string (""), the path information is returned unmodified.
             * If extension is null, the returned string contains the specified path with its extension removed.
             * If path has no extension, and extension is not null, the returned path string contains extension appended to the end of path.
             */

            var path = cloudPath?.ToString();
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
        /// Gets the path for the directory.
        /// </summary>
        /// <param name="cloudPath">The input path.</param>
        /// <returns>The path to the directory.</returns>
        public static string GetDirectoryName(this CloudPath cloudPath)
        {
            string path = cloudPath?.ToString();

            if (path == null)
            {
                return null;
            }

            if (path == "/")
            {
                throw new InvalidOperationException(Properties.Resources.NoParentOfRootExceptionMessage);
            }

            var index = path.LastIndexOf(CloudPath.DirectorySeparator);

            path = path.Remove(index);

            var dirName = GetFileNameInternal(path);
            return dirName;
        }

        /// <summary>
        /// Gets the extension of the file this path represents.
        /// </summary>
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>The file extension.</returns>
        public static string GetExtension(this CloudPath cloudPath)
        {
            /*
             * The extension of the specified path (including the period "."), or null, or Empty. If path is null, GetExtension(String) returns null. If path does not have extension information, GetExtension(String) returns Empty.
             *
             * The extension of path is obtained by searching path for a period (.), starting with the last character in path and continuing toward the start of path.
             * If a period is found before a DirectorySeparatorChar or AltDirectorySeparatorChar character, the returned string contains the period and the characters after it; otherwise, Empty is returned.
             */

            if (cloudPath == null)
            {
                return null;
            }

            var path = cloudPath.Path;

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
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>The file name and extension.</returns>
        public static string GetFileName(this CloudPath cloudPath)
        {
            /*
            The characters after the last directory character in path. If the last character of path is a directory or volume separator character, this method returns Empty. If path is null, this method returns null.
            */

            if (cloudPath == null)
            {
                return null;
            }

            return GetFileNameInternal(cloudPath.Path);
        }

        /// <summary>
        /// Gets the name of the file this path represents.
        /// </summary>
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>The file name.</returns>
        public static string GetFileNameWithoutExtension(this CloudPath cloudPath)
        {
            // The string returned by GetFileName(String), minus the last period(.) and all characters following it.
            var fileName = GetFileName(cloudPath);
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
        /// Gets the path segments, determined by the directory seperator character.
        /// </summary>
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>The path segments.</returns>
        public static IReadOnlyList<string> GetPathSegments(this CloudPath cloudPath)
        {
            return CloudPath.GetPathSegments(cloudPath?.Path);
        }

        /// <summary>
        /// Determines if the path has an extension.
        /// </summary>
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>True if the path has an extension, otherwise false.</returns>
        public static bool HasExtension(this CloudPath cloudPath)
        {
            /*
             * true if the characters that follow the last directory separator (\\ or /) or volume separator (:) in the path include a period (.) followed by one or more characters; otherwise, false.
             *
             * Starting from the end of path, this method searches for a period (.) followed by at least one character. If this pattern is found before a DirectorySeparatorChar,
             * AltDirectorySeparatorChar, or VolumeSeparatorChar character is encountered, this method returns true.
             */

            var path = cloudPath?.ToString();

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

            var separatorIndex = path.LastIndexOf(CloudPath.DirectorySeparator);
            if (separatorIndex > periodIndex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the path starts at a root location. Either  '/' or a partition label.
        /// </summary>
        /// <param name="cloudPath">The path to inspect.</param>
        /// <returns>True if the path is rooted, otherwise false.</returns>
        public static bool IsPathRooted(this CloudPath cloudPath)
        {
            if (cloudPath == null)
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
        /// Creates a new instance of a CloudPath that contains a stream name.
        /// </summary>
        /// <param name="cloudPath">The path to modify.</param>
        /// <param name="stream">The stream name.</param>
        /// <returns>The existing cloud path with the stream name.</returns>
        public static CloudPath WithStream(this CloudPath cloudPath, string stream)
        {
            if (cloudPath is null)
            {
                throw new ArgumentNullException(nameof(cloudPath));
            }

            if (!string.IsNullOrWhiteSpace(cloudPath.Stream))
            {
                throw new InvalidOperationException(Properties.Resources.PathAlreadyConatinsStreamExceptionMessage);
            }

            return new CloudPath(cloudPath.Type, cloudPath.Label, cloudPath.Path, stream);
        }

        /// <summary>
        /// Creates a new instance of a CloudPath that contains a partition name.
        /// </summary>
        /// <param name="cloudPath">The path to modify.</param>
        /// <param name="label">The label name.</param>
        /// <returns>The existing cloud path with the stream name.</returns>
        public static CloudPath WithPartition(this CloudPath cloudPath, string label)
        {
            if (cloudPath is null)
            {
                throw new ArgumentNullException(nameof(cloudPath));
            }

            if (!string.IsNullOrWhiteSpace(cloudPath.Label))
            {
                throw new InvalidOperationException(Properties.Resources.PathAlreadyConatinsStreamExceptionMessage);
            }

            return new CloudPath(CloudPathType.Partition, label, cloudPath.Path, cloudPath.Stream);
        }

        /// <summary>
        /// Creates a new instance of a CloudPath that contains a share name.
        /// </summary>
        /// <param name="cloudPath">The path to modify.</param>
        /// <param name="label">The label name.</param>
        /// <returns>The existing cloud path with the stream name.</returns>
        public static CloudPath WithShare(this CloudPath cloudPath, string label)
        {
            if (cloudPath is null)
            {
                throw new ArgumentNullException(nameof(cloudPath));
            }

            if (!string.IsNullOrWhiteSpace(cloudPath.Label))
            {
                throw new InvalidOperationException(Properties.Resources.PathAlreadyConatinsStreamExceptionMessage);
            }

            return new CloudPath(CloudPathType.Share, label, cloudPath.Path, cloudPath.Stream);
        }

        private static string GetFileNameInternal(string path)
        {
            /*
            The characters after the last directory character in path. If the last character of path is a directory or volume separator character, this method returns Empty. If path is null, this method returns null.
            */

            var separatorIndex = path.LastIndexOf(CloudPath.DirectorySeparator);
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
    }
}
