using System.Collections.Generic;
using System.Threading.Tasks;
using Com.Masterwok.Xamarininterface.Contracts;

namespace Library
{
    internal class CardigannDefinitionRepositoryWrapper : Jackett.Harness.Contracts.ICardigannDefinitionRepository
    {
        private readonly ICardigannDefinitionRepository
            _cardigannDefinitionRepository;

        public CardigannDefinitionRepositoryWrapper(
            ICardigannDefinitionRepository cardigannDefinitionRepository
        ) => _cardigannDefinitionRepository = cardigannDefinitionRepository;

        public async Task<IList<string>> ReadDefinitions() =>
            await Task.Run(() => _cardigannDefinitionRepository.Definitions);

        public async Task<int> GetIndexerCount()
            => await Task.Run(() => _cardigannDefinitionRepository.IndexerCount);
    }
}