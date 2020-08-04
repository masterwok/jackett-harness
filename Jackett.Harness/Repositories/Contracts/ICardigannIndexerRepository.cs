using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Indexers;

namespace Jackett.Harness.Repositories.Contracts
{
    /// <summary>
    /// This contract provides a set of methods for reading <see cref="CardigannIndexer"/> instances.
    /// </summary>
    internal interface ICardigannIndexerRepository
    {
        /// <summary>
        /// Read all of the <see cref="CardigannIndexer"/> instances.
        /// </summary>
        /// <returns>
        /// An asynchronous <see cref="Task"/> that returns a list of <see cref="CardigannIndexer"/> definitions.
        /// </returns>
        Task<IList<CardigannIndexer>> ReadCardigannIndexers();

        Task<int> GetIndexerCount();
        event EventHandler OnIndexerInitProcessed;
    }
}