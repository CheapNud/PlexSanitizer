using PlexSanitizer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlexSanitizer.Services;

public interface IFileSystemService
{
    Task<IEnumerable<FileItem>> GetFilesAsync(string folderPath);
    Task<bool> RenameFileAsync(string oldPath, string newPath);
    Task<IEnumerable<string>> GetFoldersAsync(string parentPath);
    Task<bool> CreateFolderAsync(string path);
    Task<bool> FolderExistsAsync(string path);
    Task<bool> FileExistsAsync(string path);
}
