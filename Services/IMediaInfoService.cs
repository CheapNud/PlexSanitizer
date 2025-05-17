using PlexSanitizer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IMediaInfoService
    {
        Task<MediaInfo> ParseFileNameAsync(string fileName);
        Task<MediaType> DetectMediaTypeAsync(string fileName);
        string GeneratePlexFileName(MediaInfo mediaInfo, MediaType mediaType);
    }
}
