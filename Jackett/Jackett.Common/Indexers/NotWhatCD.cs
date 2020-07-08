using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class NotWhatCD : GazelleTracker
    {
        public NotWhatCD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "notwhatcd",
                   name: "notwhat.cd",
                   description: "A music tracker",
                   link: "https://notwhat.cd/",
                   caps: new TorznabCapabilities
                   {
                       SupportedMusicSearchParamsList = new List<string> { "q", "album", "artist", "label", "year" }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: true)
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.PC, "Applications");
            AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(5, TorznabCatType.Movies, "E-Learning Videos");
            AddCategoryMapping(6, TorznabCatType.TV, "Comedy");
            AddCategoryMapping(7, TorznabCatType.Books, "Comics");
        }
    }
}
