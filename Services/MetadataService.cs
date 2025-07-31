using PlexSanitizer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<MetadataProvider, string> _baseUrls;
        private readonly Dictionary<MetadataProvider, string> _apiKeys;

        public MetadataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PlexSanitizer/1.0");

            // Initialize base URLs for different providers
            _baseUrls = new Dictionary<MetadataProvider, string>
            {
                { MetadataProvider.TheMovieDb, "https://api.themoviedb.org/3" },
                { MetadataProvider.TheTvDb, "https://api4.thetvdb.com/v4" },
                { MetadataProvider.MyAnimeList, "https://api.myanimelist.net/v2" },
                // Note: AniDB doesn't have a public API, IMDB doesn't allow scraping
                // These would need alternative approaches or paid APIs
            };

            // API Keys would normally come from configuration
            // For demo purposes, these would need to be set up properly
            _apiKeys = new Dictionary<MetadataProvider, string>
            {
                // TODO: Add your API keys here or get them from configuration
                { MetadataProvider.TheMovieDb, "" }, // Get from https://www.themoviedb.org/settings/api
                { MetadataProvider.TheTvDb, "" },    // Get from https://thetvdb.com/api-information
                { MetadataProvider.MyAnimeList, "" } // Get from https://myanimelist.net/apiconfig
            };
        }

        public List<MetadataProvider> GetAvailableProviders()
        {
            return new List<MetadataProvider>
            {
                MetadataProvider.TheMovieDb,
                MetadataProvider.TheTvDb,
                MetadataProvider.MyAnimeList
                // MetadataProvider.AniDb,  // Would need special handling
                // MetadataProvider.Imdb    // Would need special handling
            };
        }

        public async Task<bool> TestProviderAsync(MetadataProvider provider)
        {
            try
            {
                if (!_baseUrls.ContainsKey(provider))
                    return false;

                if (string.IsNullOrEmpty(_apiKeys[provider]))
                {
                    Debug.WriteLine($"No API key configured for {provider}");
                    return false;
                }

                // Simple test request to check if the provider is accessible
                string testUrl = provider switch
                {
                    MetadataProvider.TheMovieDb => $"{_baseUrls[provider]}/configuration?api_key={_apiKeys[provider]}",
                    MetadataProvider.TheTvDb => $"{_baseUrls[provider]}/login",
                    MetadataProvider.MyAnimeList => $"{_baseUrls[provider]}/users/@me",
                    _ => null
                };

                if (testUrl == null)
                    return false;

                var response = await _httpClient.GetAsync(testUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Provider test failed for {provider}: {ex.Message}");
                return false;
            }
        }

        public async Task<MetadataMatchResult> SearchMetadataAsync(MetadataMatchRequest request)
        {
            var result = new MetadataMatchResult();
            var allMatches = new List<MetadataSearchResult>();

            // If no preferred providers specified, use all available
            var providersToSearch = request.PreferredProviders.Any()
                ? request.PreferredProviders
                : GetAvailableProviders();

            foreach (var provider in providersToSearch)
            {
                try
                {
                    var matches = await SearchProviderAsync(provider, request);
                    allMatches.AddRange(matches);

                    // If we found good matches, we can stop searching
                    if (matches.Any() && matches.First().Rating > 0.8)
                        break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Search failed for provider {provider}: {ex.Message}");
                }
            }

            result.Matches = allMatches.OrderByDescending(m => m.Rating ?? 0).ToList();
            result.IsSuccessful = allMatches.Any();
            return result;
        }

        private async Task<List<MetadataSearchResult>> SearchProviderAsync(MetadataProvider provider, MetadataMatchRequest request)
        {
            return provider switch
            {
                MetadataProvider.TheMovieDb => await SearchTmdbAsync(request),
                MetadataProvider.TheTvDb => await SearchTvdbAsync(request),
                MetadataProvider.MyAnimeList => await SearchMalAsync(request),
                _ => new List<MetadataSearchResult>()
            };
        }

        private async Task<List<MetadataSearchResult>> SearchTmdbAsync(MetadataMatchRequest request)
        {
            var results = new List<MetadataSearchResult>();

            try
            {
                if (string.IsNullOrEmpty(_apiKeys[MetadataProvider.TheMovieDb]))
                {
                    // Return demo data if no API key
                    return GetDemoTmdbResults(request);
                }

                string endpoint = request.MediaType == MediaType.Movie ? "movie" : "tv";
                string url = $"{_baseUrls[MetadataProvider.TheMovieDb]}/search/{endpoint}?" +
                           $"api_key={_apiKeys[MetadataProvider.TheMovieDb]}&" +
                           $"query={Uri.EscapeDataString(request.Title)}";

                if (request.Year.HasValue && request.MediaType == MediaType.Movie)
                {
                    url += $"&year={request.Year}";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tmdbResponse = JsonSerializer.Deserialize<TmdbSearchResponse>(json);

                    foreach (var item in tmdbResponse.Results.Take(5)) // Limit to top 5 results
                    {
                        var metadata = new MetadataSearchResult
                        {
                            Title = request.MediaType == MediaType.Movie ? item.Title : item.Name,
                            OriginalTitle = request.MediaType == MediaType.Movie ? item.OriginalTitle : item.OriginalName,
                            Year = ParseYear(request.MediaType == MediaType.Movie ? item.ReleaseDate : item.FirstAirDate),
                            Overview = item.Overview,
                            PosterUrl = string.IsNullOrEmpty(item.PosterPath) ? null : $"https://image.tmdb.org/t/p/w500{item.PosterPath}",
                            BackdropUrl = string.IsNullOrEmpty(item.BackdropPath) ? null : $"https://image.tmdb.org/t/p/w1280{item.BackdropPath}",
                            Rating = item.VoteAverage / 10.0, // Convert to 0-1 scale
                            ExternalId = item.Id.ToString(),
                            Provider = MetadataProvider.TheMovieDb,
                            MediaType = request.MediaType
                        };

                        results.Add(metadata);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TMDB search error: {ex.Message}");
            }

            return results;
        }

        private List<MetadataSearchResult> GetDemoTmdbResults(MetadataMatchRequest request)
        {
            // Return some demo results when no API key is available
            var demos = new List<MetadataSearchResult>();

            if (request.MediaType == MediaType.Movie)
            {
                demos.Add(new MetadataSearchResult
                {
                    Title = $"{request.Title} - Demo Result",
                    Year = request.Year ?? 2020,
                    Overview = "This is a demo result. Configure TMDB API key for real results.",
                    Rating = 0.85,
                    ExternalId = "demo_123",
                    Provider = MetadataProvider.TheMovieDb,
                    MediaType = MediaType.Movie,
                    Genres = new List<string> { "Action", "Adventure", "Demo" }
                });
            }
            else if (request.MediaType == MediaType.TvShow)
            {
                demos.Add(new MetadataSearchResult
                {
                    Title = $"{request.Title} - Demo TV Show",
                    Year = request.Year ?? 2020,
                    Overview = "This is a demo TV show result. Configure TMDB API key for real results.",
                    Rating = 0.90,
                    ExternalId = "demo_tv_456",
                    Provider = MetadataProvider.TheMovieDb,
                    MediaType = MediaType.TvShow,
                    Genres = new List<string> { "Drama", "Comedy", "Demo" },
                    Status = "Ended",
                    Seasons = new List<SeasonInfo>
                    {
                        new SeasonInfo
                        {
                            SeasonNumber = 1,
                            Name = "Season 1",
                            Overview = "First season of the demo show.",
                            Episodes = Enumerable.Range(1, 10).Select(ep => new EpisodeInfo
                            {
                                EpisodeNumber = ep,
                                Name = $"Episode {ep}",
                                Overview = $"Demo episode {ep} description."
                            }).ToList()
                        },
                        new SeasonInfo
                        {
                            SeasonNumber = 2,
                            Name = "Season 2",
                            Overview = "Second season of the demo show.",
                            Episodes = Enumerable.Range(1, 12).Select(ep => new EpisodeInfo
                            {
                                EpisodeNumber = ep,
                                Name = $"Episode {ep}",
                                Overview = $"Demo episode {ep} description."
                            }).ToList()
                        }
                    }
                });
            }

            return demos;
        }

        private async Task<List<MetadataSearchResult>> SearchTvdbAsync(MetadataMatchRequest request)
        {
            // TVDB implementation would go here
            // For now, return demo data
            Debug.WriteLine("TVDB search not implemented yet. Returning demo data.");
            return new List<MetadataSearchResult>();
        }

        private async Task<List<MetadataSearchResult>> SearchMalAsync(MetadataMatchRequest request)
        {
            // MyAnimeList implementation would go here
            // For now, return demo data for anime
            Debug.WriteLine("MyAnimeList search not implemented yet. Returning demo data.");
            return new List<MetadataSearchResult>();
        }

        public async Task<MetadataSearchResult> GetDetailedMetadataAsync(string externalId, MetadataProvider provider, MediaType mediaType)
        {
            return provider switch
            {
                MetadataProvider.TheMovieDb => await GetDetailedTmdbAsync(externalId, mediaType),
                MetadataProvider.TheTvDb => await GetDetailedTvdbAsync(externalId, mediaType),
                MetadataProvider.MyAnimeList => await GetDetailedMalAsync(externalId, mediaType),
                _ => null
            };
        }

        private async Task<MetadataSearchResult> GetDetailedTmdbAsync(string externalId, MediaType mediaType)
        {
            // Implementation for getting detailed TMDB data
            Debug.WriteLine($"Getting detailed TMDB data for {externalId}");
            return null; // Placeholder
        }

        private async Task<MetadataSearchResult> GetDetailedTvdbAsync(string externalId, MediaType mediaType)
        {
            // Implementation for getting detailed TVDB data
            Debug.WriteLine($"Getting detailed TVDB data for {externalId}");
            return null; // Placeholder
        }

        private async Task<MetadataSearchResult> GetDetailedMalAsync(string externalId, MediaType mediaType)
        {
            // Implementation for getting detailed MAL data
            Debug.WriteLine($"Getting detailed MAL data for {externalId}");
            return null; // Placeholder
        }

        public async Task<List<SeasonInfo>> GetSeasonInfoAsync(string externalId, MetadataProvider provider)
        {
            // Get season and episode information for TV shows
            return provider switch
            {
                MetadataProvider.TheMovieDb => await GetTmdbSeasonsAsync(externalId),
                MetadataProvider.TheTvDb => await GetTvdbSeasonsAsync(externalId),
                _ => new List<SeasonInfo>()
            };
        }

        private async Task<List<SeasonInfo>> GetTmdbSeasonsAsync(string externalId)
        {
            // Implementation for getting TMDB season data
            Debug.WriteLine($"Getting TMDB seasons for {externalId}");
            return new List<SeasonInfo>(); // Placeholder
        }

        private async Task<List<SeasonInfo>> GetTvdbSeasonsAsync(string externalId)
        {
            // Implementation for getting TVDB season data
            Debug.WriteLine($"Getting TVDB seasons for {externalId}");
            return new List<SeasonInfo>(); // Placeholder
        }

        public async Task<List<FileItem>> MatchFilesWithMetadataAsync(List<FileItem> files, MetadataSearchResult metadata)
        {
            if (metadata.MediaType != MediaType.TvShow)
                return files; // Only process TV shows for now

            foreach (var file in files.Where(f => f.MediaType == MediaType.TvShow))
            {
                // Try to match the file's season/episode against the metadata
                if (file.MediaInfo.Season.HasValue && file.MediaInfo.Episode.HasValue)
                {
                    var season = metadata.Seasons?.FirstOrDefault(s => s.SeasonNumber == file.MediaInfo.Season.Value);
                    if (season != null)
                    {
                        var episode = season.Episodes?.FirstOrDefault(e => e.EpisodeNumber == file.MediaInfo.Episode.Value);
                        if (episode != null)
                        {
                            // Update the episode title if found
                            file.MediaInfo.EpisodeTitle = episode.Name;

                            // Regenerate the file name with the correct episode title
                            var mediaInfoService = new MediaInfoService();
                            string newFileName = mediaInfoService.GeneratePlexFileName(file.MediaInfo, file.MediaType);
                            file.NewName = newFileName + file.Extension;
                            file.NewPath = Path.Combine(file.Directory, file.NewName);
                        }
                    }
                }
            }

            return files;
        }

        private int? ParseYear(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out DateTime date))
                return date.Year;

            return null;
        }

        // Helper classes for TMDB JSON deserialization
        private class TmdbSearchResponse
        {
            [JsonPropertyName("results")]
            public List<TmdbSearchItem> Results { get; set; } = new List<TmdbSearchItem>();
        }

        private class TmdbSearchItem
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("original_title")]
            public string OriginalTitle { get; set; }

            [JsonPropertyName("original_name")]
            public string OriginalName { get; set; }

            [JsonPropertyName("overview")]
            public string Overview { get; set; }

            [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }

            [JsonPropertyName("backdrop_path")]
            public string BackdropPath { get; set; }

            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }

            [JsonPropertyName("first_air_date")]
            public string FirstAirDate { get; set; }

            [JsonPropertyName("vote_average")]
            public double VoteAverage { get; set; }
        }
    }
}