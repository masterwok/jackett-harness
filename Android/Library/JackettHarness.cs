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

        public bool IsInitialized => _jackettHarness
            .IndexerService
            .IsInitialized;

        public JackettHarness(ICardigannDefinitionRepository cardigannDefinitionRepository)
        {
            _jackettHarness = new Jackett.Harness.JackettHarness(
                new CardigannDefinitionRepositoryWrapper(cardigannDefinitionRepository)
            );
        }

        public void Initialize() => _jackettHarness
            .IndexerService
            .Initialize();
    }
}