using CheapPlexSanitizer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheapPlexSanitizer.Services
{
    public class RenameService : IRenameService
    {
        private readonly IFileSystemService _fileSystemService;
        private readonly IMediaInfoService _mediaInfoService;

        public RenameService(IFileSystemService fileSystemService, IMediaInfoService mediaInfoService)
        {
            _fileSystemService = fileSystemService;
            _mediaInfoService = mediaInfoService;
        }

        public async Task<IEnumerable<FileItem>> AnalyzeFilesAsync(string folderPath)
        {
            try
            {
                var files = await _fileSystemService.GetFilesAsync(folderPath);
                var analyzedFiles = new List<FileItem>();

                foreach (var file in files)
                {
                    var analyzedFile = await GenerateNewNameAsync(file);
                    analyzedFiles.Add(analyzedFile);
                }

                return analyzedFiles;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error analyzing files: {ex.Message}");
                throw;
            }
        }

        public async Task<FileItem> GenerateNewNameAsync(FileItem fileItem)
        {
            try
            {
                // Detect media type and parse file name
                fileItem.MediaType = await _mediaInfoService.DetectMediaTypeAsync(fileItem.Name);
                fileItem.MediaInfo = await _mediaInfoService.ParseFileNameAsync(fileItem.Name);

                // Generate new Plex-compatible file name
                string newFileName = _mediaInfoService.GeneratePlexFileName(fileItem.MediaInfo, fileItem.MediaType);

                // Add original extension
                newFileName += fileItem.Extension;

                // Update file item with new name and path
                fileItem.NewName = newFileName;
                fileItem.NewPath = Path.Combine(fileItem.Directory, newFileName);

                return fileItem;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating new name: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ApplyRenamingAsync(IEnumerable<FileItem> filesToRename)
        {
            try
            {
                bool allSuccessful = true;

                foreach (var file in filesToRename.Where(f => f.IsSelected && f.HasChanges))
                {
                    bool success = await _fileSystemService.RenameFileAsync(file.FullPath, file.NewPath);
                    if (!success)
                    {
                        Debug.WriteLine($"Failed to rename file: {file.FullPath}");
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying renaming: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> OrganizeFoldersAsync(IEnumerable<FileItem> filesToOrganize, string targetBasePath)
        {
            try
            {
                bool allSuccessful = true;

                // Create folders and move files
                foreach (var file in filesToOrganize.Where(f => f.IsSelected))
                {
                    string destinationFolder = string.Empty;
                    string destinationPath = string.Empty;

                    switch (file.MediaType)
                    {
                        case MediaType.Movie:
                            // Movies/Movie Title (Year)/Movie Title (Year).extension
                            string movieTitle = file.MediaInfo.Title;
                            if (file.MediaInfo.Year.HasValue)
                            {
                                movieTitle += $" ({file.MediaInfo.Year})";
                            }

                            destinationFolder = Path.Combine(targetBasePath, "Movies", movieTitle);
                            destinationPath = Path.Combine(destinationFolder, file.NewName ?? file.Name);
                            break;

                        case MediaType.TvShow:
                            // TV Shows/Series Title/Season XX/Series Title - SXXEXX - Episode Title.extension
                            string showTitle = file.MediaInfo.Title;
                            string seasonFolder = file.MediaInfo.Season.HasValue
                                ? $"Season {file.MediaInfo.Season:D2}"
                                : "Season 01";

                            destinationFolder = Path.Combine(targetBasePath, "TV Shows", showTitle, seasonFolder);
                            destinationPath = Path.Combine(destinationFolder, file.NewName ?? file.Name);
                            break;

                        default:
                            // Unknown media type, just place in the target folder without organizing
                            destinationFolder = Path.Combine(targetBasePath, "Other");
                            destinationPath = Path.Combine(destinationFolder, file.NewName ?? file.Name);
                            break;
                    }

                    // Create folder structure if it doesn't exist
                    await _fileSystemService.CreateFolderAsync(destinationFolder);

                    // Move and rename the file
                    bool success = await _fileSystemService.RenameFileAsync(file.FullPath, destinationPath);
                    if (!success)
                    {
                        Debug.WriteLine($"Failed to organize file: {file.FullPath}");
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error organizing folders: {ex.Message}");
                return false;
            }
        }
    }
}
