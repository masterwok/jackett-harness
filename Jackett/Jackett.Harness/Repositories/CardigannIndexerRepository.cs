using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Harness.Contracts;
using Jackett.Harness.Repositories.Contracts;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Harness.Repositories
{
    internal class CardigannIndexerRepository : ICardigannIndexerRepository
    {
        private readonly ICardigannDefinitionRepository _cardigannDefinitionRepository;
        private readonly IIndexerConfigurationService _configService;
        private readonly IProtectionService _protectionService;
        private readonly WebClient _webClient;
        private readonly Logger _logger;
        private readonly ICacheService _cache;
        private readonly IProcessService _processService;
        private readonly IConfigurationService _globalConfigService;
        private readonly ServerConfig _serverConfig;
        private readonly IDeserializer _deserializer;

        public event EventHandler OnIndexerInitProcessed;

        public CardigannIndexerRepository(
            ICardigannDefinitionRepository cardigannDefinitionRepository
            , IIndexerConfigurationService configService
            , IProtectionService protectionService
            , WebClient webClient
            , Logger logger
            , ICacheService cache
            , IProcessService processService
            , IConfigurationService globalConfigService
            , ServerConfig serverConfig
        )
        {
            _cardigannDefinitionRepository = cardigannDefinitionRepository;
            _configService = configService;
            _protectionService = protectionService;
            _webClient = webClient;
            _logger = logger;
            _cache = cache;
            _processService = processService;
            _globalConfigService = globalConfigService;
            _serverConfig = serverConfig;

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            InitAutoMapper();
        }

        private static void InitAutoMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<WebClientByteResult, WebClientStringResult>()
                    .ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((be, str) =>
                    {
                        var encoding = be.Request.Encoding ?? Encoding.UTF8;
                        str.Content = encoding.GetString(be.Content);
                    });

                cfg.CreateMap<WebClientStringResult, WebClientByteResult>()
                    .ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((str, be) =>
                    {
                        if (!string.IsNullOrEmpty(str.Content))
                        {
                            var encoding = str.Request.Encoding ?? Encoding.UTF8;
                            be.Content = encoding.GetBytes(str.Content);
                        }
                    });

                cfg.CreateMap<WebClientStringResult, WebClientStringResult>();
                cfg.CreateMap<WebClientByteResult, WebClientByteResult>();
                cfg.CreateMap<ReleaseInfo, ReleaseInfo>();

                cfg.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
                {
                    if (r.Category != null)
                    {
                        t.CategoryDesc = string.Join(", ",
                            r.Category.Select(x => TorznabCatType.GetCatDesc(x)).Where(x => !string.IsNullOrEmpty(x)));
                    }
                    else
                    {
                        t.CategoryDesc = "";
                    }
                });
            });
        }

        private CardigannIndexer? CreateFromDefinition(IndexerDefinition definition)
        {
            try
            {
                var indexerWebClientInstance = (WebClient) Activator.CreateInstance(
                    _webClient.GetType()
                    , _processService
                    , _logger
                    , _globalConfigService
                    , _serverConfig
                );

                var indexer = new CardigannIndexer(
                    _configService
                    , indexerWebClientInstance
                    , _logger
                    , _protectionService
                    , definition
                );
                // _configService.Load(indexer);

                return indexer;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while creating Cardigann instance from Definition: " + ex.Message);
                return null;
            }
            finally
            {
                OnIndexerInitProcessed?.Invoke(this, null);
            }
        }

        private IndexerDefinition DeserializeDefinitionString(string definitionString)
        {
            try
            {
                return _deserializer.Deserialize<IndexerDefinition>(definitionString);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to deserialize: {ex.Message}");

                return null;
            }
        }

        public async Task<int> GetIndexerCount() => await _cardigannDefinitionRepository.GetIndexerCount();

        public async Task<IList<CardigannIndexer>> ReadCardigannIndexers()
        {
            var definitionStrings = await _cardigannDefinitionRepository.ReadDefinitions();

            return definitionStrings
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(DeserializeDefinitionString)
                .Select(CreateFromDefinition)
                .Where(d => d != null)
                .ToList();
        }
    }
}