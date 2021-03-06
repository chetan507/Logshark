﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Logshark.Common.Helpers
{
    /// <summary>
    /// Helper methods for working with directories.
    /// </summary>
    public static class DirectoryHelper
    {
        /// <summary>
        /// Retrieves a list of FileInfo objects for all files within a given directory and any nested subdirectories.
        /// </summary>
        /// <param name="path">Path to a directory to traverse.</param>
        /// <returns>Collection of FileInfo objects for all files within the given directory.</returns>
        public static IEnumerable<FileInfo> GetAllFiles(string path)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(path);
            List<FileInfo> files = fileEntries.Select(fileName => new FileInfo(fileName)).ToList();

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectoryEntries)
            {
                files.AddRange(GetAllFiles(subdirectory));
            }

            return files;
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <param name="isRecursive">Specifies whether subdirectories should be recursively deleted.</param>
        public static void DeleteDirectory(string path, bool isRecursive = true)
        {
            // Bail out if location doesn't exist.
            if (!Directory.Exists(path))
            {
                return;
            }

            // Delete contents of location.
            var directory = new DirectoryInfo(path);
            directory.Delete(recursive: true);
        }

        /// <summary>
        /// Retrieves the volume name for a given path.
        /// </summary>
        /// <param name="directory">The path to a directory.</param>
        /// <returns>The volume name for the path.</returns>
        public static string GetVolumeName(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            var volumeName = directoryInfo.Root.FullName;

            return volumeName;
        }
    }
}