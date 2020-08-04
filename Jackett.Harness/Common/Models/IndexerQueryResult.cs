using System.Collections.Generic;
using System.Linq;
using Jackett.Harness.Common.Constants;

namespace Jackett.Harness.Common.Models
{
    /// <summary>
    /// This model represents the result of a query.
    /// </summary>
    public sealed class IndexerQueryResult
    {
        /// <summary>
        /// The count of <see cref="Items"/> that have magnet URIs defined.
        /// </summary>
        public int MagnetItemCount => Items.Count(c => c.MagnetUri != null);

        /// <summary>
        /// The count of <see cref="Items"/> that don't have magnet URIs defined.
        /// </summary>
        public int LinkItemCount => Items.Count(c => c.MagnetUri == null);

        /// <summary>
        /// The <see cref="Indexer"/> that was queried.
        /// </summary>
        public Indexer Indexer { get; }

        /// <summary>
        /// The query result items from the associated <see cref="Indexer"/>.
        /// </summary>
        public IList<QueryResultItem> Items { get; }

        /// <summary>
        /// The <see cref="QueryState"/> of the result.
        /// </summary>
        public QueryState QueryState { get; }

        /// <summary>
        /// The optional failure reason.
        /// </summary>
        public string? FailureReason { get; }

        public IndexerQueryResult(
            Indexer indexer
            , IList<QueryResultItem> items
            , QueryState queryState
            , string? failureReason = null
        )
        {
            Indexer = indexer;
            Items = items;
            QueryState = queryState;
            FailureReason = failureReason;
        }
    }
}