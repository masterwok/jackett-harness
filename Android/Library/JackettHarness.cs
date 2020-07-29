using System.Threading.Tasks;
using Android.Runtime;
using Com.Masterwok.Xamarininterface.Contracts;
using Jackett.Harness.Contracts;
using ICardigannDefinitionRepository = Com.Masterwok.Xamarininterface.Contracts.ICardigannDefinitionRepository;

namespace Library
{
    [Register("com/masterwok/jackett/JackettHarness")]
    public class JackettHarness : Java.Lang.Object, IJackettHarness
    {
        private readonly IJacketHarness _jackettHarness;

        private IJackettHarnessListener _jackettHarnessListener;

        private IIndexerService IndexerService => _jackettHarness.IndexerService;

        public int IndexerCount => Task.Run(async () => await IndexerService.GetIndexerCount()).Result;

        public bool IsInitialized => IndexerService.IsInitialized;

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
        }

        public void Initialize() => IndexerService.Initialize();

        public void SetListener(
            IJackettHarnessListener jackettHarnessListener
        ) => _jackettHarnessListener = jackettHarnessListener;
    }
}