using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Harness.Contracts;
using Jackett.Harness.Repositories;
using Jackett.Harness.Repositories.Contracts;
using Jackett.Harness.Services;
using NLog;

namespace Jackett.Harness
{
    public class JackettHarness : IJacketHarness
    {
        public IIndexerService IndexerService { get; }

        public JackettHarness(
            ICardigannDefinitionRepository cardigannDefinitionRepository
        )
        {
            var container = CreateContainer(cardigannDefinitionRepository);

            IndexerService = container.Resolve<IIndexerService>();
        }

        private static IContainer CreateContainer(
            ICardigannDefinitionRepository cardigannDefinitionRepository
        )
        {
            var builder = new ContainerBuilder();

            RegisterJackettDependencies(builder);
            RegisterRepositories(builder, cardigannDefinitionRepository);
            RegisterServices(builder);

            return builder.Build();
        }

        private static void RegisterJackettDependencies(ContainerBuilder builder)
        {
            builder.RegisterModule(new JackettModule(new RuntimeSettings()));
            builder.RegisterType<JackettHarnessProtectionService>().As<IProtectionService>();
            builder.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();

            builder.RegisterType<HttpWebClient>();
            builder.RegisterType<HttpWebClient2>().As<WebClient>();
        }

        private static void RegisterRepositories(
            ContainerBuilder builder
            , ICardigannDefinitionRepository cardigannDefinitionRepository
        )
        {
            builder
                .RegisterInstance(cardigannDefinitionRepository)
                .As<ICardigannDefinitionRepository>()
                .SingleInstance();

            builder
                .Register(c => new CardigannIndexerRepository(
                    c.Resolve<ICardigannDefinitionRepository>()
                    , c.Resolve<IIndexerConfigurationService>()
                    , c.Resolve<IProtectionService>()
                    , c.Resolve<WebClient>()
                    , c.Resolve<Logger>()
                    , c.Resolve<ICacheService>()
                    , c.Resolve<IProcessService>()
                    , c.Resolve<IConfigurationService>()
                    , c.Resolve<ServerConfig>()
                ))
                .As<ICardigannIndexerRepository>()
                .SingleInstance();

            builder
                .Register(c => new WebIndexerRepository(
                    c.Resolve<IIndexerConfigurationService>()
                    , c.Resolve<IProtectionService>()
                    , c.Resolve<WebClient>()
                    , c.Resolve<Logger>()
                    , c.Resolve<IProcessService>()
                    , c.Resolve<IConfigurationService>()
                    , c.Resolve<ServerConfig>()
                ))
                .As<IWebIndexerRepository>()
                .SingleInstance();
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder
                .Register(c => new IndexerService(
                    c.Resolve<ICardigannIndexerRepository>(),
                    c.Resolve<IWebIndexerRepository>()
                ))
                .As<IIndexerService>()
                .SingleInstance();
        }
    }
}