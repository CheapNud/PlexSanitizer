using CheapPlexSanitizer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheapPlexSanitizer.Services
{
    public interface IMetadataService
    {
        /// <summary>
        /// Search for metadata from multiple providers
        /// </summary>
        Task<MetadataMatchResult> SearchMetadataAsync(MetadataMatchRequest request);

        /// <summary>
        /// Get detailed metadata for a specific item
        /// </summary>
        Task<MetadataSearchResult> GetDetailedMetadataAsync(string externalId, MetadataProvider provider, MediaType mediaType);

        /// <summary>
        /// Get season and episode information for TV shows
        /// </summary>
        Task<List<SeasonInfo>> GetSeasonInfoAsync(string externalId, MetadataProvider provider);

        /// <summary>
        /// Match files against metadata and suggest season/episode numbers
        /// </summary>
        Task<List<FileItem>> MatchFilesWithMetadataAsync(List<FileItem> files, MetadataSearchResult metadata);

        /// <summary>
        /// Get available metadata providers
        /// </summary>
        List<MetadataProvider> GetAvailableProviders();

        /// <summary>
        /// Test if a metadata provider is available
        /// </summary>
        Task<bool> TestProviderAsync(MetadataProvider provider);
    }
}