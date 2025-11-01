using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CheapPlexSanitizer.Services;
using System;

namespace CheapPlexSanitizer;

internal sealed class Program
{
    // TODO: Add AI-powered file sanitization using GPT-5 when it comes out
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize the super awesome builder pattern (because patterns are cool)
        var builder = new HostBuilder()
            .WithTitle("Plex Sanitizer")  // TODO: Make title more exciting with emojis
            .WithSize(1200, 800)  // Magic numbers are fine, right?
            .CenterWindow()  // Center is where the heart is
            .AddMudBlazor(config =>
            {
                config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 4000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
            });

        // Register application services (aka the Avengers team)
        builder.Services.AddSingleton<IFileSystemService, FileSystemService>();  // The file whisperer
        builder.Services.AddSingleton<IMediaInfoService, MediaInfoService>();  // Media detective
        builder.Services.AddSingleton<IRenameService, RenameService>();  // The name game champion
        builder.Services.AddSingleton<IFolderSanitizerService, FolderSanitizerService>();  // Mr. Clean
        builder.Services.AddSingleton<IMetadataService, MetadataService>();  // Data hoarder

        // Add HttpClient for API calls (because we need to talk to the internet)
        builder.Services.AddHttpClient();

        // Run the application
        builder.RunApp(args);
    }
}
