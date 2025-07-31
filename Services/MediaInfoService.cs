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
        // Updated patterns for better matching
        private readonly Regex _moviePattern = new(@"^(?<title>.+?)(?:\W|_)*(?<year>19\d{2}|20\d{2})(?:\W|_)*(?<resolution>(?:480|720|1080|2160)[pi])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _tvShowPattern = new(@"^(?<title>.+?)(?:\W|_)*[Ss](?<season>\d{1,2})(?:\W|_)*[Ee](?<episode>\d{1,2})(?:\W|_)*(?<episodetitle>.+?)?(?:\W|_)*(?<resolution>(?:480|720|1080|2160)[pi])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Common prefixes to remove
        private readonly string[] _commonPrefixes =
        {
            "DVDR", "NL Gespr", "DMT", "DutchReleaseTeam", "Xvid", "DivX",
            "BluRay", "BRRip", "DVDRip", "HDRip", "WEBRip", "HDTV", "PDTV"
        };

        // Patterns for cleaning
        private readonly Regex _bracketsPattern = new(@"\[.*?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _parenthesesPattern = new(@"\((?!(?:19|20)\d{2}\))[^)]*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _englishLabelsPattern = new(@"\b(?:eng|english|nl|dutch|sub|subs|subtitles)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _deadMousePattern = new(@"\bDeadMou[s5]e\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _cleanupPattern = new(@"[\._-]+", RegexOptions.Compiled);
        private readonly Regex _extraSpacesPattern = new(@"\s+", RegexOptions.Compiled);
        private readonly Regex _yearPattern = new(@"\b(19|20)\d{2}\b", RegexOptions.Compiled);

        // Tags pattern (content within brackets or parentheses that should be preserved)
        private readonly Regex _tagsPattern = new(@"(?:\[([^\]]+)\]|\(([^)]+)\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                        mediaInfo.Title = CleanFileName(fileName);
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

            // Extract year first before cleaning
            var yearMatch = _yearPattern.Match(nameWithoutExt);
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out int year))
            {
                mediaInfo.Year = year;
            }

            // Extract tags before cleaning
            ExtractTags(nameWithoutExt, mediaInfo);

            // Clean the filename
            string cleanedName = CleanFileName(nameWithoutExt);

            // Try pattern matching first
            Match match = _moviePattern.Match(nameWithoutExt);
            if (match.Success)
            {
                mediaInfo.Title = CleanTitle(match.Groups["title"].Value);

                if (!mediaInfo.Year.HasValue && match.Groups["year"].Success && int.TryParse(match.Groups["year"].Value, out int patternYear))
                {
                    mediaInfo.Year = patternYear;
                }

                if (string.IsNullOrEmpty(mediaInfo.Resolution) && match.Groups["resolution"].Success)
                {
                    mediaInfo.Resolution = match.Groups["resolution"].Value;
                }
            }
            else
            {
                // Fallback to cleaned filename
                mediaInfo.Title = cleanedName;
            }
        }

        private void ParseTvShowFileName(string fileName, MediaInfo mediaInfo)
        {
            string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Extract tags before cleaning
            ExtractTags(nameWithoutExt, mediaInfo);

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

                if (string.IsNullOrEmpty(mediaInfo.Resolution) && match.Groups["resolution"].Success)
                {
                    mediaInfo.Resolution = match.Groups["resolution"].Value;
                }
            }
            else
            {
                mediaInfo.Title = CleanFileName(nameWithoutExt);
            }
        }

        private void ExtractTags(string fileName, MediaInfo mediaInfo)
        {
            // Extract resolution and other tags from brackets/parentheses
            var matches = _tagsPattern.Matches(fileName);

            foreach (Match match in matches)
            {
                string tag = match.Groups[1].Value + match.Groups[2].Value;

                // Check for resolution
                if (Regex.IsMatch(tag, @"(?:480|720|1080|2160)[pi]", RegexOptions.IgnoreCase))
                {
                    mediaInfo.Resolution = tag;
                }
            }
        }

        private string CleanFileName(string fileName)
        {
            string cleaned = fileName;

            // Remove common prefixes
            foreach (var prefix in _commonPrefixes)
            {
                // Remove prefix from start (case insensitive)
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                }

                // Also remove if found anywhere with separators
                cleaned = Regex.Replace(cleaned, $@"\b{Regex.Escape(prefix)}\b", "", RegexOptions.IgnoreCase);
            }

            // Remove content in brackets (except years in parentheses)
            cleaned = _bracketsPattern.Replace(cleaned, "");

            // Remove content in parentheses (except years)
            cleaned = _parenthesesPattern.Replace(cleaned, "");

            // Remove English labels
            cleaned = _englishLabelsPattern.Replace(cleaned, "");

            // Remove DeadMouse pattern
            cleaned = _deadMousePattern.Replace(cleaned, "");

            // Extract year before further cleaning
            var yearMatch = _yearPattern.Match(cleaned);
            string year = yearMatch.Success ? yearMatch.Value : "";

            // Remove year from title for now
            if (!string.IsNullOrEmpty(year))
            {
                cleaned = cleaned.Replace(year, "").Trim();
            }

            // Clean up separators
            cleaned = _cleanupPattern.Replace(cleaned, " ");

            // Remove extra spaces
            cleaned = _extraSpacesPattern.Replace(cleaned, " ");

            // Trim and convert to title case
            cleaned = CleanTitle(cleaned);

            return cleaned;
        }

        private string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Clean up separators and trim
            string cleaned = _cleanupPattern.Replace(title, " ").Trim();
            cleaned = _extraSpacesPattern.Replace(cleaned, " ");

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