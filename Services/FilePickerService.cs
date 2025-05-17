using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace PlexSanitizer.Services
{

    public class FilePickerService : IFilePickerService
    {
        public async Task<string> PickFolderAsync()
        {
            try
            {
                // Since MAUI doesn't have a built-in folder picker, we'll use a simple input dialog
                string result = await Application.Current.Windows[0].Page.DisplayPromptAsync(
                    "Select Folder",
                    "Enter the full path to the folder:",
                    "OK",
                    "Cancel",
                    "e.g., C:\\Users\\Username\\Documents or Z:\\Network\\Path",
                    -1,
                    Keyboard.Text);

                if (!string.IsNullOrEmpty(result))
                {
                    // Return the entered path
                    return result.Trim();
                }

                return null; // User cancelled
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error with folder input dialog: {ex.Message}");
                return null;
            }
        }
    }
}