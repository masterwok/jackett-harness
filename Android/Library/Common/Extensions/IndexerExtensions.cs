using Jackett.Harness.Common.Models;
using KotlinIndexer = Com.Masterwok.Xamarininterface.Models.Indexer;

namespace Library.Common.Extensions
{
    public static class IndexerExtensions
    {
        public static KotlinIndexer ToKotlinIndexer(
            this Indexer source
        ) => new KotlinIndexer(
            source.Id,
            source.Type.ToKotlinIndexerType(),
            source.DisplayName,
            source.DisplayDescription
        );
    }
}