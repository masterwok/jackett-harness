using System.Collections.Generic;
using System.Linq.Expressions;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Harness.Services
{
    public class DummyCacheService : ICacheService
    {
        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query) => null!;

        public void CacheResults(
            IIndexer indexer
            , TorznabQuery query
            , List<ReleaseInfo> releases
        ) => Expression.Empty();

        public List<TrackerCacheResult> GetCachedResults() => new List<TrackerCacheResult>();

        public void CleanIndexerCache(IIndexer indexer) => Expression.Empty();

        public void CleanCache() => Expression.Empty();
    }
}