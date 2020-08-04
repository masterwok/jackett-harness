using System;
using Jackett.Common.Indexers;
using Jackett.Harness.Common.Constants;
using Jackett.Harness.Common.Models;

namespace Jackett.Harness.Common.Extensions
{
    /// <summary>
    /// This static class provides extension methods for classes deriving from <see cref="IIndexer"/>.
    /// </summary>
    internal static class JackettIndexerExtensions
    {
        /// <summary>
        /// Convert an <see cref="IIndexer"/> to a <see cref="Indexer"/> instance.
        /// </summary>
        /// <param name="indexer">The source instance.</param>
        /// <returns>The converted instance.</returns>
        public static Indexer ToIndexer(this IIndexer indexer) => new Indexer
        {
            Id = indexer.Id,
            Type = indexer.GetIndexerType(),
            DisplayName = indexer.DisplayName,
            DisplayDescription = indexer.DisplayDescription
        };

        /// <summary>
        /// Get the <see cref="IndexerType"/> of the provided <see cref="IIndexer"/>.
        /// </summary>
        /// <param name="indexer">The <see cref="IIndexer"/> instance.</param>
        /// <returns>The associated <see cref="IndexerType"/>.</returns>
        internal static IndexerType GetIndexerType(this IIndexer indexer) => indexer.Type switch
        {
            "private" => IndexerType.Private,
            "public" => IndexerType.Public,
            _ => throw new ArgumentOutOfRangeException(nameof(indexer.Type), $@"Unknown indexer type: {indexer.Type}")
        };
    }
}