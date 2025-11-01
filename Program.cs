using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PlexSanitizer.Services;
using System;

namespace PlexSanitizer;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new HostBuilder()
            .WithTitle("Plex Sanitizer")
            .WithSize(1200, 800)
            .CenterWindow()
            .AddMudBlazor(config =>
            {
                config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 4000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
            });

        // Register application services
        builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
        builder.Services.AddSingleton<IMediaInfoService, MediaInfoService>();
        builder.Services.AddSingleton<IRenameService, RenameService>();
        builder.Services.AddSingleton<IFolderSanitizerService, FolderSanitizerService>();
        builder.Services.AddSingleton<IMetadataService, MetadataService>();

        // Add HttpClient for any API calls
        builder.Services.AddHttpClient();

        // Run the application
        builder.RunApp(args);
    }
}
