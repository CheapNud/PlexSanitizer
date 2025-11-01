using System;

namespace CheapPlexSanitizer.Models
{
    public class FolderItem
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string NewName { get; set; }
        public bool IsSelected { get; set; } = true;
        public bool HasChanges => !string.IsNullOrEmpty(NewName) && Name != NewName;
        public DateTime LastModified { get; set; }
        public string ParentPath { get; set; }
    }
}