using System.Threading;
using System.Threading.Tasks;
using Android.Runtime;
using Com.Masterwok.Xamarininterface.Contracts;
using Com.Masterwok.Xamarininterface.Enums;
using Com.Masterwok.Xamarininterface.Models;
using Jackett.Harness.Contracts;
using Library.Common.Extensions;
using Library.Common.Utils;
using ICardigannDefinitionRepository = Com.Masterwok.Xamarininterface.Contracts.ICardigannDefinitionRepository;

namespace Library
{
    [Register("com/masterwok/jackett/JackettHarness")]
    public class JackettHarness : Java.Lang.Object, IJackettHarness
    {
        private readonly IJacketHarness _jackettHarness;

        private IJackettHarnessListener _jackettHarnessListener;
        private CancellationTokenSource _cancellationTokenSource;
        private QueryState _queryState;

        private IIndexerService IndexerService => _jackettHarness.IndexerService;

        public int IndexerCount => Task.Run(async () => await IndexerService.GetIndexerCount()).Result;

        public bool IsInitialized => IndexerService.IsInitialized;

        public QueryState QueryState
        {
            get => _queryState;
            private set
            {
                _queryState = value;
                _jackettHarnessListener.OnQueryStateChange(value);
            }
        }

        public JackettHarness(ICardigannDefinitionRepository cardigannDefinitionRepository)
        {
            _jackettHarness = new Jackett.Harness.JackettHarness(
                new CardigannDefinitionRepositoryWrapper(cardigannDefinitionRepository)
            );

            SubscribeToIndexerServiceEvents();
        }

        private void SubscribeToIndexerServiceEvents()
        {
            IndexerService.OnIndexersInitialized += (sender, args) => _jackettHarnessListener
                .OnIndexersInitialized();

            IndexerService.OnIndexerInitialized += (sender, args) => _jackettHarnessListener
                .OnIndexerInitialized();

            IndexerService.OnIndexerQueryResult += (sender, indexerQueryResult) => _jackettHarnessListener
                .OnIndexerQueryResult(indexerQueryResult.ToKotlinIndexerQueryResult());

            IndexerService.OnQueryFinished += (sender, indexerQueryResult) => { QueryState = QueryState.Completed; };
        }

        public void CancelQuery()
        {
            QueryState = QueryState.Aborted;

            _cancellationTokenSource?.Cancel();
        }

        public void Query(Query query)
        {
            QueryState = QueryState.Pending;

            _cancellationTokenSource = new CancellationTokenSource();

            TaskUtil.RunAndForget(() => IndexerService.Query(
                query.ToJackettHarnessQuery()
                , _cancellationTokenSource.Token
            ));
        }

        public void Initialize() => IndexerService.Initialize();

        public void SetListener(
            IJackettHarnessListener jackettHarnessListener
        ) => _jackettHarnessListener = jackettHarnessListener;
    }
}