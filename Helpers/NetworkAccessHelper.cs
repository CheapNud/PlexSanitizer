using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PlexSanitizer.Services
{
    public static class NetworkAccessHelper
    {
        // Windows-specific P/Invoke declarations for network access
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        [DllImport("Mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(
            ref NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUsername,
            uint dwFlags);

        // Add this for local drive to UNC path resolution
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetGetConnection(
            string lpLocalName,
            [Out] char[] lpRemoteName,
            ref int lpnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }

        private const int RESOURCETYPE_DISK = 0x00000001;
        private const uint CONNECT_TEMPORARY = 0x00000004;
        private const int NO_ERROR = 0;

        /// <summary>
        /// Resolves a mapped drive letter to its UNC path
        /// </summary>
        /// <param name="driveLetter">Drive letter (e.g., "Z:", "Z:\", etc.)</param>
        /// <returns>UNC path or null if not a mapped drive</returns>
        public static string GetUNCPath(string driveLetter)
        {
            // Only works on Windows
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                // Normalize the drive letter format to "X:"
                if (driveLetter.Length < 2 || driveLetter[1] != ':')
                    return null;

                string normalizedDrive = driveLetter.Substring(0, 2);

                // Prepare a buffer for the UNC path
                int length = 512;
                char[] buffer = new char[length];

                // Get the UNC path
                int result = WNetGetConnection(normalizedDrive, buffer, ref length);

                if (result == NO_ERROR)
                {
                    // Convert buffer to string and remove any trailing nulls
                    string uncPath = new string(buffer, 0, length).TrimEnd('\0');

                    Debug.WriteLine($"Resolved drive {driveLetter} to UNC path: {uncPath}");

                    // If there's additional path info beyond the drive letter, append it
                    if (driveLetter.Length > 2)
                    {
                        string relativePath = driveLetter.Substring(2);
                        // Make sure the paths join correctly
                        if (!relativePath.StartsWith("\\"))
                            relativePath = "\\" + relativePath;

                        uncPath += relativePath;
                    }

                    return uncPath;
                }

                Debug.WriteLine($"Failed to resolve drive {driveLetter} to UNC path. Error code: {result}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resolving UNC path for {driveLetter}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a path appears to be a mapped network drive
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the path appears to be a mapped drive</returns>
        public static bool IsMappedDrive(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 2 || path[1] != ':')
                return false;

            char driveLetter = char.ToUpper(path[0]);

            // Check if it's a typical network drive letter (usually towards the end of the alphabet)
            // Common network drives: T, U, V, W, X, Y, Z
            if (driveLetter >= 'T' && driveLetter <= 'Z')
            {
                // Try to resolve it - if it fails, it's likely a mapped drive that's not accessible
                try
                {
                    string uncPath = GetUNCPath(path.Substring(0, 2));
                    if (uncPath == null)
                    {
                        // Could be a mapped drive that's not accessible
                        // Check if it's not a standard Windows drive
                        return !IsSystemDrive(driveLetter);
                    }
                    // Successfully resolved, so it's a mapped drive
                    return true;
                }
                catch
                {
                    // If we can't resolve it, assume it's a mapped drive
                    return !IsSystemDrive(driveLetter);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a drive letter is typically a system drive (C:) or common local drives
        /// </summary>
        /// <param name="driveLetter">Drive letter to check</param>
        /// <returns>True if it's a system drive</returns>
        private static bool IsSystemDrive(char driveLetter)
        {
            // C: is always system drive
            // A:, B: are floppy drives (rarely used)
            // D: often CD/DVD
            return driveLetter == 'C' || driveLetter == 'A' || driveLetter == 'B';
        }

        /// <summary>
        /// Checks if a network path is accessible
        /// </summary>
        public static bool IsNetworkPathAccessible(string path)
        {
            try
            {
                // For network drives (\\server\share or Z:\), check using WinAPI if on Windows
                if (OperatingSystem.IsWindows() &&
                    (path.StartsWith("\\\\") || (path.Length >= 2 && path[1] == ':')))
                {
                    // If it's a drive letter, try to resolve to UNC path first
                    if (path.Length >= 2 && path[1] == ':')
                    {
                        string uncPath = GetUNCPath(path);
                        if (!string.IsNullOrEmpty(uncPath))
                        {
                            Debug.WriteLine($"Using resolved UNC path for accessibility check: {uncPath}");

                            // Try with the UNC path
                            ulong uncFreeBytesAvailable;
                            ulong uncTotalNumberOfBytes;
                            ulong uncTotalNumberOfFreeBytes;

                            bool uncSuccess = GetDiskFreeSpaceEx(
                                uncPath,
                                out uncFreeBytesAvailable,
                                out uncTotalNumberOfBytes,
                                out uncTotalNumberOfFreeBytes);

                            if (uncSuccess)
                            {
                                Debug.WriteLine("Successfully accessed resolved UNC path");
                                return true;
                            }

                            Debug.WriteLine("Failed to access resolved UNC path, trying original path");
                        }
                    }

                    // Try with original path
                    ulong freeBytesAvailable;
                    ulong totalNumberOfBytes;
                    ulong totalNumberOfFreeBytes;

                    bool success = GetDiskFreeSpaceEx(
                        path,
                        out freeBytesAvailable,
                        out totalNumberOfBytes,
                        out totalNumberOfFreeBytes);

                    return success;
                }

                // Standard check - try to get directory info
                return Directory.Exists(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking network path: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to connect to a network share
        /// </summary>
        public static bool ConnectToNetworkShare(string uncPath, string username = null, string password = null)
        {
            if (!OperatingSystem.IsWindows())
            {
                // Not supported on non-Windows platforms
                return false;
            }

            try
            {
                if (!uncPath.StartsWith("\\\\"))
                {
                    // Not a UNC path, can't connect
                    return false;
                }

                var netResource = new NETRESOURCE
                {
                    dwType = RESOURCETYPE_DISK,
                    lpRemoteName = uncPath
                };

                int result = WNetAddConnection2(
                    ref netResource,
                    password,
                    username,
                    CONNECT_TEMPORARY);

                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to network share: {ex.Message}");
                return false;
            }
        }
    }
}