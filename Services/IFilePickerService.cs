using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IFilePickerService
    {
        Task<string> PickFolderAsync();
    }
}
