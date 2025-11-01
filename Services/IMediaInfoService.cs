using CheapPlexSanitizer.Models;
using System.Threading.Tasks;

namespace CheapPlexSanitizer.Services
{
    public interface IMediaInfoService
    {
        Task<MediaInfo> ParseFileNameAsync(string fileName);
        Task<MediaType> DetectMediaTypeAsync(string fileName);
        string GeneratePlexFileName(MediaInfo mediaInfo, MediaType mediaType);
    }
}
