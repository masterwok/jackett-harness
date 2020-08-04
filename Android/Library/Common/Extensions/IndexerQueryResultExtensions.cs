using Jackett.Harness.Common.Models;
using KotlinIndexerQueryResult = Com.Masterwok.Xamarininterface.Models.IndexerQueryResult;

namespace Library.Common.Extensions
{
    public static class IndexerQueryResultExtensions
    {
        public static KotlinIndexerQueryResult ToKotlinIndexerQueryResult(
            this IndexerQueryResult source
        ) => new KotlinIndexerQueryResult(
            source.Indexer.ToKotlinIndexer()
        );
    }
}