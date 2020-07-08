using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AnimeBytes : BaseCachingWebIndexer
    {
        private string ScrapeUrl => SiteLink + "scrape.php";
        public bool AllowRaws => configData.IncludeRaw.Value;
        public bool PadEpisode => configData.PadEpisode != null && configData.PadEpisode.Value;
        public bool AddSynonyms => configData.AddSynonyms.Value;
        public bool FilterSeasonEpisode => configData.FilterSeasonEpisode.Value;

        private new ConfigurationDataAnimeBytes configData
        {
            get => (ConfigurationDataAnimeBytes)base.configData;
            set => base.configData = value;
        }

        public AnimeBytes(IIndexerConfigurationService configService, Utils.Clients.WebClient client, Logger l, IProtectionService ps)
            : base(id: "animebytes",
                   name: "AnimeBytes",
                   description: "Powered by Tentacles",
                   link: "https://animebytes.tv/",
                   configService: configService,
                   client: client,
                   caps: new TorznabCapabilities(TorznabCatType.TVAnime,
                                                 TorznabCatType.Movies,
                                                 TorznabCatType.BooksComics,
                                                 TorznabCatType.ConsolePSP,
                                                 TorznabCatType.ConsoleOther,
                                                 TorznabCatType.PCGames,
                                                 TorznabCatType.AudioMP3,
                                                 TorznabCatType.AudioLossless,
                                                 TorznabCatType.AudioOther),
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataAnimeBytes("Note: Go to AnimeBytes site and open your account settings. Go to 'Account' tab, move cursor over black part near 'Passkey' and copy its value. Your username is case sensitive."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            webclient.EmulateBrowser = false; // Animebytes doesn't like fake user agents (issue #1535)

            AddCategoryMapping("anime[tv_series]", TorznabCatType.TVAnime, "TV Series");
            AddCategoryMapping("anime[tv_special]", TorznabCatType.TVAnime, "TV Special");
            AddCategoryMapping("anime[ova]", TorznabCatType.TVAnime, "OVA");
            AddCategoryMapping("anime[ona]", TorznabCatType.TVAnime, "ONA");
            AddCategoryMapping("anime[dvd_special]", TorznabCatType.TVAnime, "DVD Special");
            AddCategoryMapping("anime[bd_special]", TorznabCatType.TVAnime, "BD Special");
            AddCategoryMapping("anime[movie]", TorznabCatType.Movies, "Movie");
            AddCategoryMapping("gamec[game]", TorznabCatType.PCGames, "Game");
            AddCategoryMapping("gamec[visual_novel]", TorznabCatType.PCGames, "Visual Novel");
            AddCategoryMapping("printedtype[manga]", TorznabCatType.BooksComics, "Manga");
            AddCategoryMapping("printedtype[oneshot]", TorznabCatType.BooksComics, "Oneshot");
            AddCategoryMapping("printedtype[anthology]", TorznabCatType.BooksComics, "Anthology");
            AddCategoryMapping("printedtype[manhwa]", TorznabCatType.BooksComics, "Manhwa");
            AddCategoryMapping("printedtype[light_novel]", TorznabCatType.BooksComics, "Light Novel");
            AddCategoryMapping("printedtype[artbook]", TorznabCatType.BooksComics, "Artbook");

        }
        // Prevent filtering
        protected override IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> input) =>

            input;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Passkey.Value.Length != 32 && configData.Passkey.Value.Length != 48)
                throw new Exception("invalid passkey configured: expected length: 32 or 48, got " + configData.Passkey.Value.Length.ToString());

            var results = await PerformQuery(new TorznabQuery());
            if (results.Count() == 0)
            {
                throw new Exception("no results found, please report this bug");
            }

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        private string StripEpisodeNumber(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W\d+$", string.Empty);
            return term;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // The result list
            var releases = new List<ReleaseInfo>();

            if (ContainsMusicCategories(query.Categories))
            {
                foreach (var result in await GetResults(query, "music", query.SanitizedSearchTerm))
                {
                    releases.Add(result);
                }
            }

            foreach (var result in await GetResults(query, "anime", StripEpisodeNumber(query.SanitizedSearchTerm)))
            {
                releases.Add(result);
            }

            return releases.ToArray();
        }

        private bool ContainsMusicCategories(int[] categories)
        {
            var music = new[]
            {
                TorznabCatType.Audio.ID,
                TorznabCatType.AudioMP3.ID,
                TorznabCatType.AudioLossless.ID,
                TorznabCatType.AudioOther.ID,
                TorznabCatType.AudioForeign.ID
            };

            return categories.Length == 0 || music.Any(categories.Contains);
        }

        private async Task<IEnumerable<ReleaseInfo>> GetResults(TorznabQuery query, string searchType, string searchTerm)
        {
            // The result list
            var releases = new List<ReleaseInfo>();

            var queryCollection = new NameValueCollection();

            var queryCats = MapTorznabCapsToTrackers(query);
            if (queryCats.Count > 0)
            {
                foreach (var cat in queryCats)
                {
                    queryCollection.Add(cat, "1");
                }
            }

            queryCollection.Add("username", configData.Username.Value);
            queryCollection.Add("torrent_pass", configData.Passkey.Value);
            queryCollection.Add("type", searchType);
            queryCollection.Add("searchstr", searchTerm);
            var queryUrl = ScrapeUrl + "?" + queryCollection.GetQueryString();

            // Check cache first so we don't query the server for each episode when searching for each episode in a series.
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                var cachedResult = cache.Where(i => i.Query == queryUrl).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
            }

            // Get the content from the tracker
            var response = await RequestStringWithCookiesAndRetry(queryUrl);
            if (!response.Content.StartsWith("{")) // not JSON => error
                throw new ExceptionWithConfigData("unexcepted response (not JSON)", configData);
            dynamic json = JsonConvert.DeserializeObject<dynamic>(response.Content);

            // Parse
            try
            {
                if (json["error"] != null)
                    throw new Exception(json["error"].ToString());

                var Matches = (long)json["Matches"];

                if (Matches > 0)
                {
                    var groups = (JArray)json.Groups;

                    foreach (JObject group in groups)
                    {
                        var synonyms = new List<string>();
                        var groupID = (long)group["ID"];
                        var Image = (string)group["Image"];
                        var ImageUrl = (string.IsNullOrWhiteSpace(Image) ? null : new Uri(Image));
                        var Year = (int)group["Year"];
                        var GroupName = (string)group["GroupName"];
                        var SeriesName = (string)group["SeriesName"];
                        var mainTitle = WebUtility.HtmlDecode((string)group["FullName"]);
                        if (SeriesName != null)
                            mainTitle = SeriesName;

                        synonyms.Add(mainTitle);
                        if (AddSynonyms)
                        {
                            foreach (string synonym in group["Synonymns"])
                                synonyms.Add(synonym);
                        }

                        List<int> Category = null;
                        var category = (string)group["CategoryName"];

                        var Description = (string)group["Description"];

                        foreach (JObject torrent in group["Torrents"])
                        {
                            var releaseInfo = "S01";
                            string episode = null;
                            int? season = null;
                            var EditionTitle = (string)torrent["EditionData"]["EditionTitle"];
                            if (!string.IsNullOrWhiteSpace(EditionTitle))
                                releaseInfo = WebUtility.HtmlDecode(EditionTitle);

                            var SeasonRegEx = new Regex(@"Season (\d+)", RegexOptions.Compiled);
                            var SeasonRegExMatch = SeasonRegEx.Match(releaseInfo);
                            if (SeasonRegExMatch.Success)
                                season = ParseUtil.CoerceInt(SeasonRegExMatch.Groups[1].Value);

                            var EpisodeRegEx = new Regex(@"Episode (\d+)", RegexOptions.Compiled);
                            var EpisodeRegExMatch = EpisodeRegEx.Match(releaseInfo);
                            if (EpisodeRegExMatch.Success)
                                episode = EpisodeRegExMatch.Groups[1].Value;

                            releaseInfo = releaseInfo.Replace("Episode ", "");
                            releaseInfo = releaseInfo.Replace("Season ", "S");
                            releaseInfo = releaseInfo.Trim();

                            if (PadEpisode && int.TryParse(releaseInfo, out var test) && releaseInfo.Length == 1)
                            {
                                releaseInfo = "0" + releaseInfo;
                            }

                            if (FilterSeasonEpisode)
                            {
                                if (query.Season != 0 && season != null && season != query.Season) // skip if season doesn't match
                                    continue;
                                if (query.Episode != null && episode != null && episode != query.Episode) // skip if episode doesn't match
                                    continue;
                            }
                            var torrentID = (long)torrent["ID"];
                            var Property = (string)torrent["Property"];
                            Property = Property.Replace(" | Freeleech", "");
                            var Link = (string)torrent["Link"];
                            var LinkUri = new Uri(Link);
                            var UploadTimeString = (string)torrent["UploadTime"];
                            var UploadTime = DateTime.ParseExact(UploadTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                            var PublushDate = DateTime.SpecifyKind(UploadTime, DateTimeKind.Utc).ToLocalTime();
                            var CommentsLink = SiteLink + "torrent/" + torrentID.ToString() + "/group";
                            var CommentsLinkUri = new Uri(CommentsLink);
                            var Size = (long)torrent["Size"];
                            var Snatched = (long)torrent["Snatched"];
                            var Seeders = (int)torrent["Seeders"];
                            var Leechers = (int)torrent["Leechers"];
                            var FileCount = (long)torrent["FileCount"];
                            var Peers = Seeders + Leechers;

                            var RawDownMultiplier = (int?)torrent["RawDownMultiplier"];
                            if (RawDownMultiplier == null)
                                RawDownMultiplier = 0;
                            var RawUpMultiplier = (int?)torrent["RawUpMultiplier"];
                            if (RawUpMultiplier == null)
                                RawDownMultiplier = 0;

                            if (searchType == "anime")
                            {
                                if (GroupName == "TV Series" || GroupName == "OVA")
                                    Category = new List<int> { TorznabCatType.TVAnime.ID };

                                // Ignore these categories as they'll cause hell with the matcher
                                // TV Special, OVA, ONA, DVD Special, BD Special

                                if (GroupName == "Movie" || GroupName == "Live Action Movie")
                                    Category = new List<int> { TorznabCatType.Movies.ID };

                                if (category == "Manga" || category == "Oneshot" || category == "Anthology" || category == "Manhwa" || category == "Manhua" || category == "Light Novel")
                                    Category = new List<int> { TorznabCatType.BooksComics.ID };

                                if (category == "Novel" || category == "Artbook")
                                    Category = new List<int> { TorznabCatType.BooksComics.ID };

                                if (category == "Game" || category == "Visual Novel")
                                {
                                    if (Property.Contains(" PSP "))
                                        Category = new List<int> { TorznabCatType.ConsolePSP.ID };
                                    if (Property.Contains("PSX"))
                                        Category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                    if (Property.Contains(" NES "))
                                        Category = new List<int> { TorznabCatType.ConsoleOther.ID };
                                    if (Property.Contains(" PC "))
                                        Category = new List<int> { TorznabCatType.PCGames.ID };
                                }
                            }
                            else if (searchType == "music")
                            {
                                if (category == "Single" || category == "EP" || category == "Album" || category == "Compilation" || category == "Soundtrack" || category == "Remix CD" || category == "PV" || category == "Live Album" || category == "Image CD" || category == "Drama CD" || category == "Vocal CD")
                                {
                                    if (Property.Contains(" Lossless "))
                                        Category = new List<int> { TorznabCatType.AudioLossless.ID };
                                    else if (Property.Contains("MP3"))
                                        Category = new List<int> { TorznabCatType.AudioMP3.ID };
                                    else
                                        Category = new List<int> { TorznabCatType.AudioOther.ID };
                                }
                            }

                            // We dont actually have a release name >.> so try to create one
                            var releaseTags = Property.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                            for (var i = releaseTags.Count - 1; i >= 0; i--)
                            {
                                releaseTags[i] = releaseTags[i].Trim();
                                if (string.IsNullOrWhiteSpace(releaseTags[i]))
                                    releaseTags.RemoveAt(i);
                            }

                            var releasegroup = releaseTags.LastOrDefault();
                            if (releasegroup != null && releasegroup.Contains("(") && releasegroup.Contains(")"))
                            {
                                // Skip raws if set
                                if (releasegroup.ToLowerInvariant().StartsWith("raw") && !AllowRaws)
                                {
                                    continue;
                                }

                                var start = releasegroup.IndexOf("(");
                                releasegroup = "[" + releasegroup.Substring(start + 1, (releasegroup.IndexOf(")") - 1) - start) + "] ";
                            }
                            else
                            {
                                releasegroup = string.Empty;
                            }
                            if (!AllowRaws && releaseTags.Contains("raw", StringComparer.InvariantCultureIgnoreCase))
                                continue;

                            var infoString = releaseTags.Aggregate("", (prev, cur) => prev + "[" + cur + "]");
                            var MinimumSeedTime = 259200;
                            //  Additional 5 hours per GB
                            MinimumSeedTime += (int)((Size / 1000000000) * 18000);

                            foreach (var title in synonyms)
                            {
                                string releaseTitle = null;
                                if (GroupName == "Movie")
                                {
                                    releaseTitle = string.Format("{0} {1} {2}{3}", title, Year, releasegroup, infoString);
                                }
                                else
                                {
                                    releaseTitle = string.Format("{0}{1} {2} {3}", releasegroup, title, releaseInfo, infoString);
                                }

                                var guid = new Uri(CommentsLinkUri + "&nh=" + StringUtil.Hash(title));
                                var release = new ReleaseInfo
                                {
                                    MinimumRatio = 1,
                                    MinimumSeedTime = MinimumSeedTime,
                                    Title = releaseTitle,
                                    Comments = CommentsLinkUri,
                                    Guid = guid, // Sonarr should dedupe on this url - allow a url per name.
                                    Link = LinkUri,
                                    BannerUrl = ImageUrl,
                                    PublishDate = PublushDate,
                                    Category = Category,
                                    Description = Description,
                                    Size = Size,
                                    Seeders = Seeders,
                                    Peers = Peers,
                                    Grabs = Snatched,
                                    Files = FileCount,
                                    DownloadVolumeFactor = RawDownMultiplier,
                                    UploadVolumeFactor = RawUpMultiplier
                                };

                                releases.Add(release);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            // Add to the cache
            lock (cache)
            {
                cache.Add(new CachedQueryResult(queryUrl, releases));
            }

            return releases.Select(s => (ReleaseInfo)s.Clone());
        }
    }
}
