using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace PlexSanitizer.Services
{
    public class FilePickerService : IFilePickerService
    {
        public async Task<string> PickFolderAsync()
        {
            try
            {
                // Get the main window
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.StorageProvider is { } storageProvider)
                {
                    var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = "Select Folder",
                        AllowMultiple = false
                    });

                    if (result.Count > 0)
                    {
                        return result[0].Path.LocalPath;
                    }
                }

                return null; // User cancelled
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with folder picker: {ex.Message}");
                return null;
            }
        }
    }
}