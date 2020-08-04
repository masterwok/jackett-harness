using System;
using System.Linq;
using Com.Masterwok.Xamarininterface.Models;
using IndexerQueryResult = Jackett.Harness.Common.Models.IndexerQueryResult;
using KotlinIndexerQueryResult = Com.Masterwok.Xamarininterface.Models.IndexerQueryResult;

namespace Library.Common.Extensions
{
    public static class IndexerQueryResultExtensions
    {
        public static KotlinIndexerQueryResult ToKotlinIndexerQueryResult(
            this IndexerQueryResult source
        ) => new KotlinIndexerQueryResult(
            source.Indexer.ToKotlinIndexer(),
            source
                .Items
                .Select(i => i.ToKotlinQueryResultItem())
                .ToList(),
            source.QueryState.ToKotlinQueryState(),
            source.FailureReason
        );
    }
}