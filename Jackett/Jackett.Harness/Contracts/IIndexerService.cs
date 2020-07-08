using System;
using System.Threading;
using System.Threading.Tasks;
using Jackett.Harness.Common.Models;

namespace Jackett.Harness.Contracts
{
    /// <summary>
    /// This contract is used as an interface for Jackett indexers.
    /// </summary>
    public interface IIndexerService
    {
        /// <summary>
        /// An event that is emitted when a single indexer completes as part of a <see cref="Query"/>.
        /// </summary>
        event EventHandler<IndexerQueryResult> OnIndexerQueryResult;

        /// <summary>
        /// An event that is emitted when a <see cref="Query"/> completes.
        /// </summary>
        event EventHandler OnQueryFinished;

        /// <summary>
        /// Whether or not the service is initialized. The service must be initialized before it can be used.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initialize the service.
        /// </summary>
        /// <returns></returns>
        Task Initialize();

        /// <summary>
        /// Query the indexers.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the query.</param>
        /// <returns>An asynchronous Task that completes on query completion.</returns>
        Task Query(Query query, CancellationToken cancellationToken);

        event EventHandler OnIndexersInitialized;
        Task<int> GetIndexerCount();
        event EventHandler OnIndexerInitProcessed;
    }
}