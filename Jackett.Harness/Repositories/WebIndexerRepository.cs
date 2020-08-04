using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Harness.Repositories.Contracts;
using NLog;

namespace Jackett.Harness.Repositories
{
    public class WebIndexerRepository : IWebIndexerRepository
    {
        public event EventHandler OnIndexerInitProcessed;

        private static readonly Type[] IndexerConstructorArgumentTypes =
        {
            typeof(IIndexerConfigurationService),
            typeof(WebClient),
            typeof(Logger),
            typeof(IProtectionService)
        };

        private readonly IIndexerConfigurationService _configService;
        private readonly IProtectionService _protectionService;
        private readonly WebClient _webClient;
        private readonly Logger _logger;
        private readonly IProcessService _processService;
        private readonly IConfigurationService _globalConfigService;
        private readonly ServerConfig _serverConfig;

        public WebIndexerRepository(
            IIndexerConfigurationService configService
            , IProtectionService protectionService
            , WebClient webClient
            , Logger logger
            , IProcessService processService
            , IConfigurationService globalConfigService
            , ServerConfig serverConfig
        )
        {
            _configService = configService;
            _protectionService = protectionService;
            _webClient = webClient;
            _logger = logger;
            _processService = processService;
            _globalConfigService = globalConfigService;
            _serverConfig = serverConfig;
        }

        public async Task<int> GetIndexerCount() => GetIndexerTypes().Count();

        public async Task<IList<IWebIndexer>> ReadWebIndexers()
        {
            var indexerTypes = GetIndexerTypes();

            return indexerTypes.Select(type =>
                {
                    var constructorInfo = type.GetConstructor(IndexerConstructorArgumentTypes);

                    try
                    {
                        return CreateInstance(constructorInfo);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error($@"Failed to instantiate {type.Name}: {exception.Message}");
                        return null;
                    }
                    finally
                    {
                        OnIndexerInitProcessed?.Invoke(this, null);
                    }
                })
                .OfType<IWebIndexer>()
                .ToList();
        }

        private static IEnumerable<Type> GetIndexerTypes()
        {
            var allTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(t => t.GetTypes());

            var allIndexerTypes = allTypes
                .Where(p => typeof(IIndexer).IsAssignableFrom(p));

            var allInstantiatableIndexerTypes = allIndexerTypes
                .Where(p => !p.IsInterface && !p.IsAbstract);

            var allNonMetaInstantiatableIndexerTypes = allInstantiatableIndexerTypes
                .Where(p => !typeof(BaseMetaIndexer).IsAssignableFrom(p));

            return allNonMetaInstantiatableIndexerTypes
                .Where(p => p.Name != "CardigannIndexer");
        }

        private IWebIndexer CreateInstance(ConstructorInfo constructorInfo)
        {
            var webClientInstance = (WebClient) Activator.CreateInstance(
                _webClient.GetType()
                , _processService
                , _logger
                , _globalConfigService
                , _serverConfig
            );

            return (IWebIndexer) constructorInfo.Invoke(new object[]
            {
                _configService,
                webClientInstance,
                _logger,
                _protectionService
            });
        }
    }
}