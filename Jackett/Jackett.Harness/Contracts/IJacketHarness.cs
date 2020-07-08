namespace Jackett.Harness.Contracts
{
    /// <summary>
    /// This contract provides a set of methods and properties for interacting with Jackett.
    /// </summary>
    public interface IJacketHarness
    {
        /// <summary>
        /// The <see cref="IIndexerService"/> that can be used for interacting with indexers.
        /// </summary>
        IIndexerService IndexerService { get; }
    }
}