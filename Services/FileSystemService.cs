using PlexSanitizer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PlexSanitizer.Services;

public class FileSystemService : IFileSystemService
{
    public async Task<IEnumerable<FileItem>> GetFilesAsync(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine($"Directory does not exist: {folderPath}");
                return new List<FileItem>();
            }

            // Get all files in the directory
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => IsMediaFile(file))
                .Select(path => new FileItem
                {
                    FullPath = path,
                    Name = Path.GetFileName(path),
                    Extension = Path.GetExtension(path),
                    Directory = Path.GetDirectoryName(path),
                    Size = new FileInfo(path).Length,
                    LastModified = File.GetLastWriteTime(path)
                })
                .ToList();

            return await Task.FromResult(files);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting files: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> RenameFileAsync(string oldPath, string newPath)
    {
        try
        {
            if (!File.Exists(oldPath))
            {
                Debug.WriteLine($"File does not exist: {oldPath}");
                return false;
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Rename file
            File.Move(oldPath, newPath);
            Debug.WriteLine($"Renamed file from {oldPath} to {newPath}");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error renaming file: {ex.Message}");
            return await Task.FromResult(false);
        }
    }

    public async Task<IEnumerable<string>> GetFoldersAsync(string parentPath)
    {
        try
        {
            if (!Directory.Exists(parentPath))
            {
                Debug.WriteLine($"Directory does not exist: {parentPath}");
                return new List<string>();
            }

            var folders = Directory.GetDirectories(parentPath);
            return await Task.FromResult(folders);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting folders: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> CreateFolderAsync(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                return await Task.FromResult(true);
            }

            Directory.CreateDirectory(path);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating folder: {ex.Message}");
            return await Task.FromResult(false);
        }
    }

    public async Task<bool> FolderExistsAsync(string path)
    {
        return await Task.FromResult(Directory.Exists(path));
    }

    public async Task<bool> FileExistsAsync(string path)
    {
        return await Task.FromResult(File.Exists(path));
    }

    private bool IsMediaFile(string filePath)
    {
        // Common video file extensions
        string[] mediaExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".mpg", ".mpeg", ".m4v" };
        return mediaExtensions.Contains(Path.GetExtension(filePath).ToLower());
    }
}