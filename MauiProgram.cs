using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using PlexSanitizer.Services;

namespace PlexSanitizer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });


            builder.Services.AddMauiBlazorWebView();


            // Register MudBlazor services
            builder.Services.AddMudServices();

            // Register application services
            builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
            builder.Services.AddSingleton<IMediaInfoService, MediaInfoService>();
            builder.Services.AddSingleton<IRenameService, RenameService>();
            builder.Services.AddHttpClient();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
