using PlexSanitizer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IRenameService
    {
        Task<IEnumerable<FileItem>> AnalyzeFilesAsync(string folderPath);
        Task<bool> ApplyRenamingAsync(IEnumerable<FileItem> filesToRename);
        Task<FileItem> GenerateNewNameAsync(FileItem fileItem);
        Task<bool> OrganizeFoldersAsync(IEnumerable<FileItem> filesToOrganize, string targetBasePath);
    }
}
