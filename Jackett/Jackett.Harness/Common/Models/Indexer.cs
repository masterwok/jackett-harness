using Jackett.Harness.Common.Constants;

namespace Jackett.Harness.Common.Models
{
    /// <summary>
    /// This model represents information associated with an indexer.
    /// </summary>
    public sealed class Indexer
    {
        /// <summary>
        /// The unique identifier of the indexer.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The type of the indexer.
        /// </summary>
        public IndexerType Type { get; set; }

        /// <summary>
        /// The display name of the indexer.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The display description of the indexer.
        /// </summary>
        public string DisplayDescription { get; set; }
    }
}