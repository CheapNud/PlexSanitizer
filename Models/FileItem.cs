using System;

namespace CheapPlexSanitizer.Models;

public class FileItem
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public string Directory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string NewName { get; set; }
    public string NewPath { get; set; }
    public bool IsSelected { get; set; }
    public bool HasChanges => !string.IsNullOrEmpty(NewName) && Name != NewName;
    public MediaType MediaType { get; set; } = MediaType.Unknown;
    public MediaInfo MediaInfo { get; set; }
}