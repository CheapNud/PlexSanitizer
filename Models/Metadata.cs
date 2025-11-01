using System;
using System.Collections.Generic;

namespace CheapPlexSanitizer.Models
{
    public class MetadataSearchResult
    {
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public int? Year { get; set; }
        public string Overview { get; set; }
        public string PosterUrl { get; set; }
        public string BackdropUrl { get; set; }
        public double? Rating { get; set; }
        public string ExternalId { get; set; }
        public MetadataProvider Provider { get; set; }
        public MediaType MediaType { get; set; }

        // TV Show specific
        public List<SeasonInfo> Seasons { get; set; } = new List<SeasonInfo>();

        // Additional metadata
        public List<string> Genres { get; set; } = new List<string>();
        public string Status { get; set; }
        public DateTime? FirstAirDate { get; set; }
        public DateTime? LastAirDate { get; set; }
        public int? Runtime { get; set; }
        public string Network { get; set; }
    }

    public class SeasonInfo
    {
        public int SeasonNumber { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public DateTime? AirDate { get; set; }
        public string PosterUrl { get; set; }
        public List<EpisodeInfo> Episodes { get; set; } = new List<EpisodeInfo>();
    }

    public class EpisodeInfo
    {
        public int EpisodeNumber { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public DateTime? AirDate { get; set; }
        public string StillUrl { get; set; }
        public double? Rating { get; set; }
        public int? Runtime { get; set; }
    }

    public enum MetadataProvider
    {
        TheMovieDb,
        TheTvDb,
        AniDb,
        Imdb,
        MyAnimeList
    }

    public class MetadataMatchRequest
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public MediaType MediaType { get; set; }
        public List<MetadataProvider> PreferredProviders { get; set; } = new List<MetadataProvider>();
    }

    public class MetadataMatchResult
    {
        public List<MetadataSearchResult> Matches { get; set; } = new List<MetadataSearchResult>();
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public MetadataProvider UsedProvider { get; set; }
    }
}