using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public interface IFilePickerService
    {
        Task<string> PickFolderAsync();
    }
}
