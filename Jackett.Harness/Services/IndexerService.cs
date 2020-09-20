using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Harness.Common.Constants;
using Jackett.Harness.Common.Extensions;
using Jackett.Harness.Common.Models;
using Jackett.Harness.Contracts;
using Jackett.Harness.Repositories.Contracts;

namespace Jackett.Harness.Services
{
    public class IndexerService : IIndexerService
    {
        public event EventHandler<IndexerQueryResult>? OnIndexerQueryResult;
        public event EventHandler? OnQueryFinished;
        public event EventHandler? OnIndexersInitialized;

        public event EventHandler OnIndexerInitialized
        {
            add
            {
                _cardigannIndexerRepository.OnIndexerInitProcessed += value;
                _webIndexerRepository.OnIndexerInitProcessed += value;
            }
            remove
            {
                _cardigannIndexerRepository.OnIndexerInitProcessed += value;
                _webIndexerRepository.OnIndexerInitProcessed -= value;
            }
        }

        private readonly ICardigannIndexerRepository _cardigannIndexerRepository;
        private readonly IWebIndexerRepository _webIndexerRepository;
        private readonly IList<IIndexer> _indexers = new List<IIndexer>();

        internal IndexerService(
            ICardigannIndexerRepository cardigannIndexerRepository,
            IWebIndexerRepository webIndexerRepository
        )
        {
            _cardigannIndexerRepository = cardigannIndexerRepository;
            _webIndexerRepository = webIndexerRepository;
        }

        public bool IsInitialized { get; private set; }

        public async Task Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            await Task.WhenAll(
                InitCardigannIndexers(),
                InitWebIndexers()
            );

            IsInitialized = true;

            OnIndexersInitialized?.Invoke(this, null);
        }

        public async Task Query(Query query, CancellationToken cancellationToken)
        {
            ThrowIfNotInitialized();

            var queryTasks = _indexers
                .Where(c => c.Type.Equals("public", StringComparison.InvariantCultureIgnoreCase))
                .Select(async indexer =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var indexerResult = await indexer.ResultsForQuery(query.ToTorznabQuery());

                        cancellationToken.ThrowIfCancellationRequested();

                        OnIndexerQueryResult?.Invoke(this, indexerResult.ToQueryResult());
                    }
                    catch (OperationCanceledException)
                    {
                        OnIndexerQueryResult?.Invoke(
                            this
                            , new IndexerQueryResult(
                                indexer.ToIndexer()
                                , ArraySegment<QueryResultItem>.Empty
                                , QueryState.Aborted
                            )
                        );
                    }
                    catch (ObjectDisposedException)
                    {
                        OnIndexerQueryResult?.Invoke(
                            this
                            , new IndexerQueryResult(
                                indexer.ToIndexer()
                                , ArraySegment<QueryResultItem>.Empty
                                , QueryState.Aborted
                            )
                        );
                    }
                    catch (Exception exception)
                    {
                        OnIndexerQueryResult?.Invoke(
                            this
                            , new IndexerQueryResult(
                                indexer.ToIndexer()
                                , ArraySegment<QueryResultItem>.Empty
                                , QueryState.Failure
                                , exception.Message
                            )
                        );
                    }
                });

            await Task.WhenAll(queryTasks);

            if (!cancellationToken.IsCancellationRequested)
            {
                OnQueryFinished?.Invoke(this, null);
            }
        }

        public async Task<int> GetIndexerCount()
        {
            var cardigannIndexerCountTask = _cardigannIndexerRepository.GetIndexerCount();
            var webIndexerCountTask = _webIndexerRepository.GetIndexerCount();

            await Task.WhenAll(cardigannIndexerCountTask, webIndexerCountTask);

            return cardigannIndexerCountTask.Result + webIndexerCountTask.Result;
        }

        private async Task InitCardigannIndexers()
        {
            var indexers = await _cardigannIndexerRepository.ReadCardigannIndexers();

            foreach (var item in indexers)
            {
                _indexers.Add(item);
            }
        }

        private async Task InitWebIndexers()
        {
            var indexers = await _webIndexerRepository.ReadWebIndexers();

            foreach (var item in indexers)
            {
                _indexers.Add(item);
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Indexer service must be initialized.");
            }
        }
    }
}