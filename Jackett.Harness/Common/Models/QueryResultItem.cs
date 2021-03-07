using System;

namespace Jackett.Harness.Common.Models
{
    /// <summary>
    /// This model represents a single item of a <see cref="IndexerQueryResult"/>.
    /// </summary>
    public sealed class QueryResultItem
    {
        /// <summary>
        /// The query result display title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The magnet URI.
        /// </summary>
        public Uri? MagnetUri { get; set; }

        /// <summary>
        /// The number of seeders.
        /// </summary>
        public long? Seeders { get; set; }

        /// <summary>
        /// The number of peers.
        /// </summary>
        public long? Peers { get; set; }

        public Uri? Link { get; set; }

        public Uri? Details { get; set; }

        public DateTime? PublishedOn { get; set; }

        public long? Size { get; set; }

        public long? Files { get; set; }

        public long? Grabs { get; set; }

        public string? Description { get; set; }

        public long? RageID { get; set; }

        public long? TVDBId { get; set; }

        public long? Imdb { get; set; }

        public long? TMDb { get; set; }

        public Uri? PosterUri { get; set; }

        public string? InfoHash { get; set; }

        public double? MinimumRatio { get; set; }

        public long? MinimumSeedTime { get; set; }

        public double? DownloadVolumeFactor { get; set; }

        public double? UploadVolumeFactor { get; set; }
    }
}