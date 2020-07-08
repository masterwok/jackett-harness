using Jackett.Common.Models;
using Jackett.Harness.Common.Constants;

namespace Jackett.Harness.Common.Models
{
    /// <summary>
    /// This model represents a query that can be made against the indexers.
    /// </summary>
    public sealed class Query
    {
        /// <summary>
        /// The query type.
        /// </summary>
        public QueryType Type { get; set; }

        /// <summary>
        /// The search term to query.
        /// </summary>
        public string SearchTerm { get; set; }
    }

    /// <summary>
    /// This static class provides extensions for the <see cref="Query"/> type.
    /// </summary>
    internal static class QueryExtensions
    {
        /// <summary>
        /// Convert the <see cref="Query"/> instance into an <see cref="ToTorznabQuery"/> instance.
        /// </summary>
        /// <param name="query">The source <see cref="Query"/> instance.</param>
        /// <returns>The converted <see cref="ToTorznabQuery"/> instance.</returns>
        public static TorznabQuery ToTorznabQuery(this Query query) => new TorznabQuery
        {
            QueryType = query.Type.ToJackettQueryType(),
            SearchTerm = query.SearchTerm
        };
    }
}