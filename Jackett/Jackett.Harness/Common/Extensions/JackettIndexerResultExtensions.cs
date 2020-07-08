using System.Linq;
using Jackett.Common.Indexers;
using Jackett.Harness.Common.Constants;
using Jackett.Harness.Common.Models;

namespace Jackett.Harness.Common.Extensions
{
    /// <summary>
    /// This static class provides extensions for the <see cref="IndexerResult"/> type.
    /// </summary>
    internal static class IndexerResultExtensions
    {
        /// <summary>
        /// Convert the IndexerResult instance into a QueryResult instance.
        /// </summary>
        /// <param name="indexerResult">The IndexerResult instance to convert.</param>
        /// <returns>The converted QueryResult instance.</returns>
        public static IndexerQueryResult ToQueryResult(this IndexerResult indexerResult) => new IndexerQueryResult(
            indexerResult
                .Indexer
                .ToIndexer()
            , indexerResult
                .Releases
                .Select(r => r.ToQueryResultItem())
                .ToList()
            , QueryState.Success
        );
    }
}