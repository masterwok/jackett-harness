using Android.Runtime;
using Com.Masterwok.Xamarininterface.Contracts;
using Jackett.Harness;

namespace Library
{
    [Register("com/masterwok/jackett/JackettHarness")]
    public class JackettHarnessWrapper : Java.Lang.Object, Com.Masterwok.Xamarininterface.Contracts.IJackettHarness
    {
        private readonly JackettHarness _jackettHarness;

        public JackettHarnessWrapper(
            ICardigannDefinitionRepository cardigannDefinitionRepository
        ) => _jackettHarness = new JackettHarness(
            new CardigannDefinitionRepositoryWrapper(cardigannDefinitionRepository)
        );
    }
}