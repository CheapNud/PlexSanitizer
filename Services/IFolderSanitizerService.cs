using PlexSanitizer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IFolderSanitizerService
    {
        /// <summary>
        /// Gets folders from the specified path
        /// </summary>
        /// <param name="rootPath">The root path to scan for folders</param>
        /// <returns>List of folder items</returns>
        Task<List<FolderItem>> GetFoldersAsync(string rootPath);

        /// <summary>
        /// Previews sanitization changes without applying them
        /// </summary>
        /// <param name="folders">List of folders to sanitize</param>
        /// <returns>Updated list with preview changes</returns>
        Task<List<FolderItem>> PreviewSanitizationAsync(List<FolderItem> folders);

        /// <summary>
        /// Applies sanitization changes to the folders
        /// </summary>
        /// <param name="folders">List of folders to sanitize</param>
        /// <returns>True if all operations were successful</returns>
        Task<bool> ApplySanitizationAsync(List<FolderItem> folders);

        /// <summary>
        /// Gets the list of available sanitization rules
        /// </summary>
        /// <returns>List of sanitization rules</returns>
        List<FolderSanitizationRule> GetRules();

        /// <summary>
        /// Toggles a rule on or off
        /// </summary>
        /// <param name="index">Index of the rule</param>
        /// <param name="isActive">Whether the rule should be active</param>
        void ToggleRule(int index, bool isActive);

        /// <summary>
        /// Checks if a path exists
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if the path exists</returns>
        Task<bool> PathExistsAsync(string path);
    }
}