using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jackett.Harness.Contracts
{
    /// <summary>
    /// This contract provides methods for reading Cardigann definitions.
    /// </summary>
    public interface ICardigannDefinitionRepository
    {
        /// <summary>
        /// Read all of the Cardigann definitions.
        /// </summary>
        /// <returns>A list of definition strings for each Cardigann indexer..</returns>
        Task<IList<string>> ReadDefinitions();

        Task<int> GetIndexerCount();
    }
}