using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Indexers;

namespace Jackett.Harness.Repositories.Contracts
{
    public interface IWebIndexerRepository
    {
        Task<IList<IWebIndexer>> ReadWebIndexers();
        Task<int> GetIndexerCount();
        event EventHandler OnIndexerInitProcessed;
    }
}