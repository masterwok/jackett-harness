using Android.Runtime;
using Com.Masterwok.Xamarininterface.Contracts;
using Jackett.Harness.Contracts;
using ICardigannDefinitionRepository = Com.Masterwok.Xamarininterface.Contracts.ICardigannDefinitionRepository;

namespace Library
{
    [Register("com/masterwok/jackett/JackettHarness")]
    public class JackettHarness : Java.Lang.Object, IJackettHarness
    {
        private readonly IJackettHarnessCallbacks _jackettHarnessCallbacks;
        private readonly IJacketHarness _jackettHarness;

        private IIndexerService IndexerService => _jackettHarness.IndexerService;

        public bool IsInitialized => IndexerService.IsInitialized;

        public JackettHarness(
            ICardigannDefinitionRepository cardigannDefinitionRepository
            , IJackettHarnessCallbacks jackettHarnessCallbacks
        )
        {
            _jackettHarnessCallbacks = jackettHarnessCallbacks;

            _jackettHarness = new Jackett.Harness.JackettHarness(
                new CardigannDefinitionRepositoryWrapper(cardigannDefinitionRepository)
            );

            SubscribeToIndexerServiceEvents();
        }

        private void SubscribeToIndexerServiceEvents()
        {
            IndexerService.OnIndexersInitialized += (sender, args) => _jackettHarnessCallbacks
                .OnIndexersInitialized();

            IndexerService.OnIndexerInitialized += (sender, args) => _jackettHarnessCallbacks
                .OnIndexerInitialized();
        }

        public void Initialize() => IndexerService.Initialize();
    }
}