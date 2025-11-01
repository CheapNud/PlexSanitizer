namespace CheapPlexSanitizer.Models
{
    public class MediaInfo
    {
        // Common properties
        public string Title { get; set; }
        public int? Year { get; set; }
        public string Resolution { get; set; }

        // TV Show specific properties
        public int? Season { get; set; }
        public int? Episode { get; set; }
        public string EpisodeTitle { get; set; }

        // Movie specific properties
        public string Edition { get; set; }
    }
}
