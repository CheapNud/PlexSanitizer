using PlexSanitizer.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public class MediaInfoService : IMediaInfoService
    {
        private readonly Regex _moviePattern = new(@"^(?<title>.+?)(?:\W|_)*(?<year>19\d{2}|20\d{2})(?:\W|_)*(?<resolution>(?:480|720|1080|2160)[pi])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _tvShowPattern = new(@"^(?<title>.+?)(?:\W|_)*[Ss](?<season>\d{1,2})(?:\W|_)*[Ee](?<episode>\d{1,2})(?:\W|_)*(?<episodetitle>.+?)?(?:\W|_)*(?<resolution>(?:480|720|1080|2160)[pi])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _cleanupPattern = new(@"[\._]", RegexOptions.Compiled);

        public async Task<MediaInfo> ParseFileNameAsync(string fileName)
        {
            try
            {
                MediaType mediaType = await DetectMediaTypeAsync(fileName);
                MediaInfo mediaInfo = new MediaInfo();

                switch (mediaType)
                {
                    case MediaType.Movie:
                        ParseMovieFileName(fileName, mediaInfo);
                        break;
                    case MediaType.TvShow:
                        ParseTvShowFileName(fileName, mediaInfo);
                        break;
                    default:
                        mediaInfo.Title = fileName;
                        break;
                }

                return mediaInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing file name: {ex.Message}");
                return new MediaInfo { Title = fileName };
            }
        }

        public async Task<MediaType> DetectMediaTypeAsync(string fileName)
        {
            // Remove file extension if present
            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

            if (_tvShowPattern.IsMatch(nameWithoutExt))
            {
                return await Task.FromResult(MediaType.TvShow);
            }
            else if (_moviePattern.IsMatch(nameWithoutExt))
            {
                return await Task.FromResult(MediaType.Movie);
            }

            return await Task.FromResult(MediaType.Unknown);
        }

        private void ParseMovieFileName(string fileName, MediaInfo mediaInfo)
        {
            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            Match match = _moviePattern.Match(nameWithoutExt);

            if (match.Success)
            {
                mediaInfo.Title = CleanTitle(match.Groups["title"].Value);

                if (match.Groups["year"].Success && int.TryParse(match.Groups["year"].Value, out int year))
                {
                    mediaInfo.Year = year;
                }

                if (match.Groups["resolution"].Success)
                {
                    mediaInfo.Resolution = match.Groups["resolution"].Value;
                }
            }
            else
            {
                mediaInfo.Title = CleanTitle(nameWithoutExt);
            }
        }

        private void ParseTvShowFileName(string fileName, MediaInfo mediaInfo)
        {
            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            Match match = _tvShowPattern.Match(nameWithoutExt);

            if (match.Success)
            {
                mediaInfo.Title = CleanTitle(match.Groups["title"].Value);

                if (match.Groups["season"].Success && int.TryParse(match.Groups["season"].Value, out int season))
                {
                    mediaInfo.Season = season;
                }

                if (match.Groups["episode"].Success && int.TryParse(match.Groups["episode"].Value, out int episode))
                {
                    mediaInfo.Episode = episode;
                }

                if (match.Groups["episodetitle"].Success)
                {
                    mediaInfo.EpisodeTitle = CleanTitle(match.Groups["episodetitle"].Value);
                }

                if (match.Groups["resolution"].Success)
                {
                    mediaInfo.Resolution = match.Groups["resolution"].Value;
                }
            }
            else
            {
                mediaInfo.Title = CleanTitle(nameWithoutExt);
            }
        }

        private string CleanTitle(string title)
        {
            // Replace dots/underscores with spaces and trim
            string cleaned = _cleanupPattern.Replace(title, " ").Trim();

            // Convert to title case
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(cleaned.ToLower());
        }

        public string GeneratePlexFileName(MediaInfo mediaInfo, MediaType mediaType)
        {
            try
            {
                string fileName = string.Empty;

                switch (mediaType)
                {
                    case MediaType.Movie:
                        // Plex movie naming convention: Movie Title (Year).extension
                        fileName = $"{mediaInfo.Title}";
                        if (mediaInfo.Year.HasValue)
                        {
                            fileName += $" ({mediaInfo.Year})";
                        }
                        break;

                    case MediaType.TvShow:
                        // Plex TV show naming convention: Series Title - S01E01 - Episode Title.extension
                        fileName = $"{mediaInfo.Title}";

                        if (mediaInfo.Season.HasValue && mediaInfo.Episode.HasValue)
                        {
                            fileName += $" - S{mediaInfo.Season:D2}E{mediaInfo.Episode:D2}";

                            if (!string.IsNullOrEmpty(mediaInfo.EpisodeTitle))
                            {
                                fileName += $" - {mediaInfo.EpisodeTitle}";
                            }
                        }
                        break;

                    default:
                        fileName = mediaInfo.Title;
                        break;
                }

                // Add resolution if available
                if (!string.IsNullOrEmpty(mediaInfo.Resolution))
                {
                    fileName += $" [{mediaInfo.Resolution}]";
                }

                return fileName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating Plex file name: {ex.Message}");
                return mediaInfo.Title;
            }
        }
    }
}
