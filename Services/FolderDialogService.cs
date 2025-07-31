using MudBlazor;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IFolderDialogService
    {
        Task<string> GetFolderPathAsync(IDialogService dialogService);
    }

    public class FolderDialogService : IFolderDialogService
    {
        public async Task<string> GetFolderPathAsync(IDialogService dialogService)
        {
            try
            {
                // Use MudBlazor's dialog system to get a folder path from the user
                var parameters = new DialogParameters();
                parameters.Add("ContentText", "Enter folder path (e.g., Z:\\New):");
                parameters.Add("ButtonText", "Select");
                parameters.Add("Color", MudBlazor.Color.Primary);

                var dialog = await dialogService.ShowAsync<Components.Shared.DialogFolderPicker>("Select Folder", parameters);
                var result = await dialog.Result;

                if (!result.Canceled && result.Data is string folderPath)
                {
                    return folderPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting folder path: {ex.Message}");
                return null;
            }
        }
    }
}