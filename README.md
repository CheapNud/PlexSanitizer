# PlexSanitizer

A cross-platform desktop application for cleaning up media files and folders for Plex Media Server.

Built with **Blazor Server**, **MudBlazor**, **Avalonia**, and **Photino**.

## ⚠️ Warning

**This software is experimental.** Always test on a small subset of files first. The author is not responsible for any data loss or damage that may occur. Use at your own risk.

**Backup your media files before use.**

![Screenshot 2025-05-18 155549](https://github.com/user-attachments/assets/1e519c87-1695-441d-b422-2cbfca2f9ea8)

## Features

- **Folder Sanitizer**: Clean folder names with configurable rules
- **File Renamer**: Parse and rename media files to Plex standards
- **Preview Changes**: See what will be renamed before applying
- **Cross-Platform**: Windows, macOS, and Linux support

## Usage

### Requirements
- .NET 8.0 or later

### Running
```bash
dotnet run
```

### Folder Sanitizer
1. Enter folder path (use `\\server\share` format for network paths, not `Z:\`)
2. Click "Scan Folders"
3. Toggle rules as needed
4. Preview and apply changes

### File Renamer
1. Enter folder with media files
2. Click "Analyze Files" 
3. Review detected titles/seasons/episodes
4. Apply renaming or organize into Plex structure

## Supported Formats
- Video: .mp4, .mkv, .avi, .mov, .wmv, .flv, .mpg, .mpeg, .m4v
- Paths: Local paths and UNC network paths (mapped drives not supported)

## License

MIT License - see [LICENSE](LICENSE) file.

## Disclaimer

Use this software at your own risk. Always backup your files before processing.
