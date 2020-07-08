using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class NordicBits : BaseCachingWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string LoginCheckUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";
        private string TorrentCommentUrl => SiteLink + "details.php?id={id}&comonly=1#page1";
        private string TorrentDescriptionUrl => SiteLink + "details.php?id={id}";
        private string TorrentDownloadUrl => SiteLink + "download.php?torrent={id}&torrent_pass={passkey}";
        private bool Latency => ConfigData.Latency.Value;
        private bool DevMode => ConfigData.DevMode.Value;
        private bool CacheMode => ConfigData.HardDriveCache.Value;
        private static string Directory => Path.Combine(Path.GetTempPath(), "Jackett", MethodBase.GetCurrentMethod().DeclaringType?.Name);

        private readonly Dictionary<string, string> _emulatedBrowserHeaders = new Dictionary<string, string>();

        private ConfigurationDataNordicbits ConfigData => (ConfigurationDataNordicbits)configData;

        public NordicBits(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l, IProtectionService ps)
            : base(id: "nordicbits",
                   name: "NordicBits",
                   description: "NordicBits is a Danish Private site for MOVIES / TV / GENERAL",
                   link: "https://nordicb.org/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataNordicbits())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "da-dk";
            Type = "private";

            // Apps
            AddCategoryMapping("cat=63", TorznabCatType.PCPhoneAndroid, "APPS - Android");
            AddCategoryMapping("cat=17", TorznabCatType.PC, "APPS - MAC");
            AddCategoryMapping("cat=12", TorznabCatType.PCMac, "APPS - Windows");
            AddCategoryMapping("cat=62", TorznabCatType.PCPhoneIOS, "APPS - IOS");
            AddCategoryMapping("cat=64", TorznabCatType.PC, "APPS - Linux");

            // Books
            AddCategoryMapping("cat=54", TorznabCatType.AudioAudiobook, "Books - Audiobooks");
            AddCategoryMapping("cat=9", TorznabCatType.BooksEbook, "Books - E-Books");
            AddCategoryMapping("cat=84", TorznabCatType.BooksEbook, "Books - Education");

            // Games
            AddCategoryMapping("cat=24", TorznabCatType.PCGames, "Games - PC");
            AddCategoryMapping("cat=53", TorznabCatType.Console, "Games - Nintendo");
            AddCategoryMapping("cat=49", TorznabCatType.ConsolePS4, "Games - Playstation");
            AddCategoryMapping("cat=51", TorznabCatType.ConsoleXbox, "Games - XBOX");

            // Movies
            AddCategoryMapping("cat=35", TorznabCatType.Movies3D, "Movies - 3D");
            AddCategoryMapping("cat=42", TorznabCatType.MoviesUHD, "Movies - 4K/2160p");
            AddCategoryMapping("cat=47", TorznabCatType.MoviesUHD, "Movies - 4k/2160p Boxset");
            AddCategoryMapping("cat=15", TorznabCatType.MoviesBluRay, "Movies - BluRay");
            AddCategoryMapping("cat=58", TorznabCatType.MoviesHD, "Movies - Remux");
            AddCategoryMapping("cat=16", TorznabCatType.MoviesDVD, "Movies - DVD Boxset");
            AddCategoryMapping("cat=6", TorznabCatType.MoviesDVD, "Movies - DVD");
            AddCategoryMapping("cat=21", TorznabCatType.MoviesHD, "Movies - HD-1080p");
            AddCategoryMapping("cat=19", TorznabCatType.MoviesHD, "Movies - HD-1080p Boxset");
            AddCategoryMapping("cat=22", TorznabCatType.MoviesHD, "Movies - HD-720p");
            AddCategoryMapping("cat=20", TorznabCatType.MoviesHD, "Movies - HD-720p Boxset");
            AddCategoryMapping("cat=25", TorznabCatType.MoviesHD, "Movies - Kids");
            AddCategoryMapping("cat=10", TorznabCatType.MoviesSD, "Movies - SD");
            AddCategoryMapping("cat=23", TorznabCatType.MoviesSD, "Movies - MP4 Tablet");
            AddCategoryMapping("cat=65", TorznabCatType.XXX, "Movies - Porn");
            AddCategoryMapping("cat=90", TorznabCatType.MoviesHD, "Movies - No Nordic Subs");

            // Music
            AddCategoryMapping("cat=28", TorznabCatType.AudioLossless, "Music - FLAC");
            AddCategoryMapping("cat=60", TorznabCatType.AudioLossless, "Music - FLAC Boxset");
            AddCategoryMapping("cat=4", TorznabCatType.AudioMP3, "Music - MP3");
            AddCategoryMapping("cat=59", TorznabCatType.AudioMP3, "Music - MP3 Boxset");
            AddCategoryMapping("cat=61", TorznabCatType.AudioMP3, "Music - Musicvideos Boxset");
            AddCategoryMapping("cat=1", TorznabCatType.AudioMP3, "Music - Musicvideos");

            // Series
            AddCategoryMapping("cat=48", TorznabCatType.TVUHD, "TV - HD-4K/2160p");
            AddCategoryMapping("cat=57", TorznabCatType.TVUHD, "TV - HD-4K/2160p Boxset");
            AddCategoryMapping("cat=11", TorznabCatType.TVSD, "TV - Boxset");
            AddCategoryMapping("cat=80", TorznabCatType.TVSD, "TV - Danish Boxset");
            AddCategoryMapping("cat=71", TorznabCatType.TVHD, "TV - Danish HD-1080p");
            AddCategoryMapping("cat=72", TorznabCatType.TVHD, "TV - Danish HD-720p");
            AddCategoryMapping("cat=73", TorznabCatType.TVSD, "TV - Danish SD");
            AddCategoryMapping("cat=83", TorznabCatType.TVSD, "TV - Finnish Boxset");
            AddCategoryMapping("cat=77", TorznabCatType.TVHD, "TV - Finnish HD-1080p");
            AddCategoryMapping("cat=78", TorznabCatType.TVHD, "TV - Finnish HD-720p");
            AddCategoryMapping("cat=79", TorznabCatType.TVSD, "TV - Finnish SD");
            AddCategoryMapping("cat=89", TorznabCatType.TVSD, "TV - Nordic Boxset");
            AddCategoryMapping("cat=86", TorznabCatType.TVHD, "TV - Nordic HD-1080p");
            AddCategoryMapping("cat=87", TorznabCatType.TVHD, "TV - Nordic HD-720p");
            AddCategoryMapping("cat=88", TorznabCatType.TVSD, "TV - Nordic SD");
            AddCategoryMapping("cat=82", TorznabCatType.TVSD, "TV - Norwegian Boxset");
            AddCategoryMapping("cat=74", TorznabCatType.TVHD, "TV - Norwegian HD-1080p");
            AddCategoryMapping("cat=75", TorznabCatType.TVHD, "TV - Norwegian HD-720p");
            AddCategoryMapping("cat=76", TorznabCatType.TVSD, "TV - Norwegian SD");
            AddCategoryMapping("cat=81", TorznabCatType.TVSD, "TV - Swedish Boxset");
            AddCategoryMapping("cat=68", TorznabCatType.TVHD, "TV - Swedish HD-1080p");
            AddCategoryMapping("cat=69", TorznabCatType.TVHD, "TV - Swedish HD-720p");
            AddCategoryMapping("cat=70", TorznabCatType.TVSD, "TV - Swedish SD");
            AddCategoryMapping("cat=91", TorznabCatType.TVHD, "TV - No Nordic Subs");
            AddCategoryMapping("cat=7", TorznabCatType.TVHD, "TV - HD-1080p");
            AddCategoryMapping("cat=31", TorznabCatType.TVHD, "TV - HD-1080p Boxset");
            AddCategoryMapping("cat=30", TorznabCatType.TVHD, "TV - HD-720p");
            AddCategoryMapping("cat=32", TorznabCatType.TVHD, "TV - HD-720p Boxset");
            AddCategoryMapping("cat=5", TorznabCatType.TVSD, "TV - SD");
            AddCategoryMapping("cat=66", TorznabCatType.TVSport, "TV - SD/HD Mixed");
        }

        /// <summary>
        /// Configure our FADN Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            // Retrieve config values set by Jackett's user
            LoadValuesFromJson(configJson);

            // Check & Validate Config
            ValidateConfig();

            // Setting our data for a better emulated browser (maximum security)
            // TODO: Encoded Content not supported by Jackett at this time
            // emulatedBrowserHeaders.Add("Accept-Encoding", "gzip, deflate");

            // If we want to simulate a browser
            if (ConfigData.Browser.Value)
            {
                // Clean headers
                _emulatedBrowserHeaders.Clear();

                // Inject headers
                _emulatedBrowserHeaders.Add("Accept", ConfigData.HeaderAccept.Value);
                _emulatedBrowserHeaders.Add("Accept-Language", ConfigData.HeaderAcceptLang.Value);
                _emulatedBrowserHeaders.Add("DNT", Convert.ToInt32(ConfigData.HeaderDnt.Value).ToString());
                _emulatedBrowserHeaders.Add("Upgrade-Insecure-Requests", Convert.ToInt32(ConfigData.HeaderUpgradeInsecure.Value).ToString());
                _emulatedBrowserHeaders.Add("User-Agent", ConfigData.HeaderUserAgent.Value);
                _emulatedBrowserHeaders.Add("Referer", LoginUrl);
            }

            await DoLogin();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Perform login to racker
        /// </summary>
        /// <returns></returns>
        private async Task DoLogin()
        {
            // Build WebRequest for index
            var myIndexRequest = new Utils.Clients.WebRequest()
            {
                Type = RequestType.GET,
                Url = SiteLink,
                Headers = _emulatedBrowserHeaders,
                Encoding = Encoding
            };

            // Get index page for cookies
            Output("\nGetting index page (for cookies).. with " + SiteLink);
            var indexPage = await webclient.GetString(myIndexRequest);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value }
            };

            // Build WebRequest for login
            var myRequestLogin = new Utils.Clients.WebRequest()
            {
                Type = RequestType.GET,
                Url = LoginUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Referer = SiteLink,
                Encoding = Encoding
            };

            // Get login page -- (not used, but simulation needed by tracker security's checks)
            LatencyNow();
            Output("\nGetting login page (user simulation).. with " + LoginUrl);
            await webclient.GetString(myRequestLogin);

            // Build WebRequest for submitting authentification
            var request = new Utils.Clients.WebRequest()
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginCheckUrl,
                Headers = _emulatedBrowserHeaders,
                Cookies = indexPage.Cookies,
                Encoding = Encoding
            };

            // Perform loggin
            LatencyNow();
            Output("\nPerform loggin.. with " + LoginCheckUrl);
            var response = await webclient.GetString(request);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Cookies != null && response.Cookies.Contains("uid="), () =>
            {
                // Default error message
                var message = "Error during attempt !";
                // Parse redirect header
                var redirectTo = response.RedirectingTo;

                // Oops, unable to login
                Output("-> Login failed: " + message, "error");
                throw new ExceptionWithConfigData("Login failed: " + message, configData);
            });

            Output("\nCookies saved for future uses...");
            ConfigData.CookieHeader.Value = indexPage.Cookies + " " + response.Cookies + " ts_username=" + ConfigData.Username.Value;

            Output("\n-> Login Success\n");
        }

        /// <summary>
        /// Check logged-in state for provider
        /// </summary>
        /// <returns></returns>
        private async Task CheckLogin()
        {
            // Checking ...
            Output("\n-> Checking logged-in state....");
            var loggedInCheck = await RequestStringWithCookies(SearchUrl);
            if (!loggedInCheck.Content.Contains("logout.php"))
            {
                // Cookie expired, renew session on provider
                Output("-> Not logged, login now...\n");

                await DoLogin();
            }
            else
            {
                // Already logged, session active
                Output("-> Already logged, continue...\n");
            }
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var exactSearchTerm = query.GetQueryString();
            var searchUrl = SearchUrl;

            // Check login before performing a query
            await CheckLogin();

            // Check cache first so we don't query the server (if search term used or not in dev mode)
            if (!DevMode && !string.IsNullOrEmpty(exactSearchTerm))
            {
                lock (cache)
                {
                    // Remove old cache items
                    CleanCache();

                    // Search in cache
                    var cachedResult = cache.FirstOrDefault(i => i.Query == exactSearchTerm);
                    if (cachedResult != null)
                        return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray();
                }
            }

            var SearchTerms = new List<string> { exactSearchTerm };

            // duplicate search without diacritics
            var baseSearchTerm = StringUtil.RemoveDiacritics(exactSearchTerm);
            if (baseSearchTerm != exactSearchTerm)
                SearchTerms.Add(baseSearchTerm);

            foreach (var searchTerm in SearchTerms)
            {
                // Build our query
                var request = BuildQuery(searchTerm, query, searchUrl);

                // Getting results & Store content
                var response = await RequestStringWithCookiesAndRetry(request, ConfigData.CookieHeader.Value);
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);

                try
                {
                    var firstPageRows = FindTorrentRows(dom);

                    // If pagination available
                    int nbResults;
                    int pageLinkCount;
                    pageLinkCount = 1;

                    // Check if we have a minimum of one result
                    if (firstPageRows?.Length > 1)
                    {
                        // Retrieve total count on our alone page
                        nbResults = firstPageRows.Length;
                    }
                    else
                    {
                        // No result found for this query
                        Output("\nNo result found for your query, please try another search term or change the theme you're currently using on the site as this is an unsupported solution...\n", "info");
                        break;
                    }

                    Output("\nFound " + nbResults + " result(s) (+/- " + firstPageRows.Length + ") in " + pageLinkCount + " page(s) for this query !");
                    Output("\nThere are " + (firstPageRows.Length - 2) + " results on the first page !");

                    // Loop on results

                    foreach (var row in firstPageRows.Skip(1).Take(firstPageRows.Length - 2))
                    {
                        Output("Torrent #" + (releases.Count + 1));

                        // ID
                        var idOrig = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").GetAttribute("href").Split('=')[1];
                        var id = idOrig.Substring(0, idOrig.Length - 4);
                        Output("ID: " + id);

                        // Release Name
                        var name = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)").TextContent.Trim();

                        // Category
                        var categoryId = row.QuerySelector("td:nth-of-type(1) > a:nth-of-type(1)").GetAttribute("href").Split('?').Last();
                        var newznab = MapTrackerCatToNewznab(categoryId);
                        Output("Category: " + (newznab.Count > 0 ? newznab.First().ToString() : "unknown category") + " (" + categoryId + ")");

                        // Seeders
                        var seeders = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(10)").TextContent, @"\d+").Value);
                        Output("Seeders: " + seeders);

                        // Leechers
                        var leechers = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(11)").TextContent, @"\d+").Value);
                        Output("Leechers: " + leechers);

                        // Files
                        var files = 1;
                        files = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(5)").TextContent, @"\d+").Value);
                        Output("Files: " + files);

                        // Completed
                        var completed = ParseUtil.CoerceInt(Regex.Match(row.QuerySelector("td:nth-of-type(9)").TextContent, @"\d+").Value);
                        Output("Completed: " + completed);

                        // Size
                        var humanSize = row.QuerySelector("td:nth-of-type(8)").TextContent.ToLowerInvariant();
                        var size = ReleaseInfo.GetBytes(humanSize);
                        Output("Size: " + humanSize + " (" + size + " bytes)");

                        // Publish DateToString
                        var dateTimeOrig = row.QuerySelector("td:nth-of-type(7)").TextContent;
                        var datestr = Regex.Replace(dateTimeOrig, @"<[^>]+>|&nbsp;", "").Trim();
                        datestr = Regex.Replace(datestr, "Today", DateTime.Now.ToString("MMM dd yyyy"), RegexOptions.IgnoreCase);
                        datestr = Regex.Replace(datestr, "Yesterday", DateTime.Now.Date.AddDays(-1).ToString("MMM dd yyyy"), RegexOptions.IgnoreCase);
                        var date = DateTimeUtil.FromUnknown(datestr, "DK");
                        Output("Released on: " + date);

                        // Torrent Details URL
                        var detailsLink = new Uri(TorrentDescriptionUrl.Replace("{id}", id.ToString()));
                        Output("Details: " + detailsLink.AbsoluteUri);

                        // Torrent Comments URL
                        var commentsLink = new Uri(TorrentCommentUrl.Replace("{id}", id.ToString()));
                        Output("Comments Link: " + commentsLink.AbsoluteUri);

                        // Torrent Download URL
                        var passkey = row.QuerySelector("td:nth-of-type(3) > a:nth-of-type(1)").GetAttribute("href");
                        var key = Regex.Match(passkey, "(?<=torrent_pass\\=)([a-zA-z0-9]*)");
                        var downloadLink = new Uri(TorrentDownloadUrl.Replace("{id}", id.ToString()).Replace("{passkey}", key.ToString()));
                        Output("Download Link: " + downloadLink.AbsoluteUri);

                        // Building release infos
                        var release = new ReleaseInfo
                        {
                            Category = newznab,
                            Title = name,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = date,
                            Size = size,
                            Files = files,
                            Grabs = completed,
                            Guid = detailsLink,
                            Comments = commentsLink,
                            Link = downloadLink,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800 // 48 hours
                        };

                        // IMDB
                        var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink);

                        if (row.QuerySelector("img[title=\"Free Torrent\"]") != null)
                            release.DownloadVolumeFactor = 0;
                        else if (row.QuerySelector("img[title=\"Halfleech\"]") != null)
                            release.DownloadVolumeFactor = 0.5;
                        else if (row.QuerySelector("img[title=\"90% Freeleech\"]") != null)
                            release.DownloadVolumeFactor = 0.1;
                        else
                            release.DownloadVolumeFactor = 1;

                        release.UploadVolumeFactor = 1;

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError("Error, unable to parse result \n" + ex.StackTrace, ex);
                }
            }
            // Return found releases
            return releases;
        }

        /// <summary>
        /// Build query to process
        /// </summary>
        /// <param name="searchTerm">Term to search</param>
        /// <param name="query">Torznab Query for categories mapping</param>
        /// <param name="url">Search url for provider</param>
        /// <param name="page">Page number to request</param>
        /// <returns>URL to query for parsing and processing results</returns>
        private string BuildQuery(string searchTerm, TorznabQuery query, string url, int page = 0)
        {
            var categoriesList = MapTorznabCapsToTrackers(query);

            // Building our tracker query
            var qc = new NameValueCollection
            {
                {"incldead", "1"}
            };
            if (query.IsImdbQuery)
            {
                qc.Add("searchin", "imdb");
                qc.Add("search", query.ImdbID);
            }
            else
            {
                qc.Add("searchin", "title");
                qc.Add("search", searchTerm);
            }

            // Loop on categories and change the catagories for search purposes
            for (var i = 0; i < categoriesList.Count; i++)
            {
                // APPS
                if (new[] { "63", "17", "12", "62", "64" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats5[]=");
                }
                // Books
                if (new[] { "54", "9", "84" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats6[]=");
                }
                // Games
                if (new[] { "24", "53", "49", "51" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats3[]=");
                }
                // Movies
                if (new[] { "35", "42", "47", "15", "58", "16", "6", "21", "19", "22", "20", "25", "10", "23", "65", "90" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats1[]=");
                }
                // Music
                if (new[] { "28", "60", "4", "59", "61", "1" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats4[]=");
                }
                // Series
                if (new[] { "48", "57", "11", "80", "71", "72", "73", "83", "77", "78", "79", "89", "86", "87", "88", "82", "74", "75", "76", "81", "68", "69", "70", "91", "7", "31", "30", "32", "5", "66" }.Any(c => categoriesList[i].Contains(categoriesList[i])))
                {
                    categoriesList[i] = categoriesList[i].Replace("cat=", "cats2[]=");
                }
            }

            // Building our query
            url += "?" + qc.GetQueryString() + "&" + string.Join("&", categoriesList);

            Output("\nBuilt query for \"" + searchTerm + "\"... " + url);

            // Return our search url
            return url;
        }

        /// <summary>
        /// Switch Method for Querying
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> QueryExec(string request)
        {
            WebClientStringResult results;

            // Switch in we are in DEV mode with Hard Drive Cache or not
            if (DevMode && CacheMode)
            {
                // Check Cache before querying and load previous results if available
                results = await QueryCache(request);
            }
            else
            {
                // Querying tracker directly
                results = await QueryTracker(request);
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Cache by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> QueryCache(string request)
        {
            WebClientStringResult results;

            // Create Directory if not exist
            System.IO.Directory.CreateDirectory(Directory);

            // Clean Storage Provider Directory from outdated cached queries
            CleanCacheStorage();

            // Create fingerprint for request
            var file = Directory + request.GetHashCode() + ".json";

            // Checking modes states
            if (System.IO.File.Exists(file))
            {
                // File exist... loading it right now !
                Output("Loading results from hard drive cache ..." + request.GetHashCode() + ".json");
                results = JsonConvert.DeserializeObject<WebClientStringResult>(System.IO.File.ReadAllText(file));
            }
            else
            {
                // No cached file found, querying tracker directly
                results = await QueryTracker(request);

                // Cached file didn't exist for our query, writing it right now !
                Output("Writing results to hard drive cache ..." + request.GetHashCode() + ".json");
                System.IO.File.WriteAllText(file, JsonConvert.SerializeObject(results));
            }
            return results;
        }

        /// <summary>
        /// Get Torrents Page from Tracker by Query Provided
        /// </summary>
        /// <param name="request">URL created by Query Builder</param>
        /// <returns>Results from query</returns>
        private async Task<WebClientStringResult> QueryTracker(string request)
        {
            // Cache mode not enabled or cached file didn't exist for our query
            Output("\nQuerying tracker for results....");

            // Request our first page
            LatencyNow();
            var results = await RequestStringWithCookiesAndRetry(request, ConfigData.CookieHeader.Value, SearchUrl, _emulatedBrowserHeaders);

            // Return results from tracker
            return results;
        }

        /// <summary>
        /// Clean Hard Drive Cache Storage
        /// </summary>
        /// <param name="force">Force Provider Folder deletion</param>
        private void CleanCacheStorage(bool force = false)
        {
            // Check cleaning method
            if (force)
            {
                // Deleting Provider Storage folder and all files recursively
                Output("\nDeleting Provider Storage folder and all files recursively ...");

                // Check if directory exist
                if (System.IO.Directory.Exists(Directory))
                {
                    // Delete storage directory of provider
                    System.IO.Directory.Delete(Directory, true);
                    Output("-> Storage folder deleted successfully.");
                }
                else
                {
                    // No directory, so nothing to do
                    Output("-> No Storage folder found for this provider !");
                }
            }
            else
            {
                var i = 0;
                // Check if there is file older than ... and delete them
                Output("\nCleaning Provider Storage folder... in progress.");
                System.IO.Directory.GetFiles(Directory)
                .Select(f => new System.IO.FileInfo(f))
                .Where(f => f.LastAccessTime < DateTime.Now.AddMilliseconds(-Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value)))
                .ToList()
                .ForEach(f =>
                {
                    Output("Deleting cached file << " + f.Name + " >> ... done.");
                    f.Delete();
                    i++;
                });

                // Inform on what was cleaned during process
                if (i > 0)
                {
                    Output("-> Deleted " + i + " cached files during cleaning.");
                }
                else
                {
                    Output("-> Nothing deleted during cleaning.");
                }
            }
        }

        /// <summary>
        /// Generate a random fake latency to avoid detection on tracker side
        /// </summary>
        private void LatencyNow()
        {
            // Need latency ?
            if (Latency)
            {
                var random = new Random(DateTime.Now.Millisecond);
                var waiting = random.Next(Convert.ToInt32(ConfigData.LatencyStart.Value),
                    Convert.ToInt32(ConfigData.LatencyEnd.Value));
                Output("\nLatency Faker => Sleeping for " + waiting + " ms...");

                // Sleep now...
                System.Threading.Thread.Sleep(waiting);
            }
            // Generate a random value in our range
        }

        /// <summary>
        /// Find torrent rows in search pages
        /// </summary>
        /// <returns>List of rows</returns>
        private IHtmlCollection<IElement> FindTorrentRows(IHtmlDocument dom)
        {
            var defaultTheme = new[] { "/templates/1/", "/templates/2/", "/templates/3/", "/templates/4/", "/templates/5/", "/templates/6/", "/templates/11/", "/templates/12/" };
            var oldV2 = new[] { "/templates/7/", "/templates/8/", "/templates/9/", "/templates/10/" };
            var xmas = new[] { "/templates/14/" };

            if (xmas.Any(dom.Body.InnerHtml.Contains))
            {
                // Return all occurrences of torrents found
                return dom.QuerySelectorAll("#base_around > table.mainouter > tbody > tr > td.outer > div.article > table  > tbody")[1].QuerySelectorAll("tr");
            }

            // template 7 contains a reference to template 2 (logout button), so check for oldV2 first
            if (oldV2.Any(dom.Body.InnerHtml.Contains))
            {
                // Return all occurrences of torrents found
                return dom.QuerySelectorAll("#base_content > table.mainouter > tbody > tr > td.outer > div.article > table > tbody > tr").Skip(1).ToCollection();
            }

            if (defaultTheme.Any(dom.Body.InnerHtml.Contains))
            {
                // Return all occurrences of torrents found
                return dom.QuerySelectorAll("#base_content2 > div.article > table > tbody")[1].QuerySelectorAll("tr");
            }
            return null;
        }

        /// <summary>
        /// Download torrent file from tracker
        /// </summary>
        /// <param name="link">URL string</param>
        /// <returns></returns>
        public override async Task<byte[]> Download(Uri link)
        {
            // Retrieving ID from link provided
            var id = ParseUtil.CoerceInt(Regex.Match(link.AbsoluteUri, @"\d+").Value);
            Output("Torrent Requested ID: " + id);

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "torrentid", id.ToString() },
                { "_", string.Empty } // ~~ Strange, blank param...
            };

            // Add emulated XHR request
            _emulatedBrowserHeaders.Add("X-Prototype-Version", "1.6.0.3");
            _emulatedBrowserHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // Get torrent file now
            Output("Getting torrent file now....");
            var response = await base.Download(link);

            // Remove our XHR request header
            _emulatedBrowserHeaders.Remove("X-Prototype-Version");
            _emulatedBrowserHeaders.Remove("X-Requested-With");

            // Return content
            return response;
        }

        /// <summary>
        /// Output message for logging or developpment (console)
        /// </summary>
        /// <param name="message">Message to output</param>
        /// <param name="level">Level for Logger</param>
        private void Output(string message, string level = "debug")
        {
            // Check if we are in dev mode
            if (DevMode)
            {
                // Output message to console
                Console.WriteLine(message);
            }
            else
            {
                // Send message to logger with level
                switch (level)
                {
                    default:
                        goto case "debug";
                    case "debug":
                        // Only if Debug Level Enabled on Jackett
                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug(message);
                        }
                        break;

                    case "info":
                        logger.Info(message);
                        break;

                    case "error":
                        logger.Error(message);
                        break;
                }
            }
        }

        /// <summary>
        /// Validate Config entered by user on Jackett
        /// </summary>
        private void ValidateConfig()
        {
            Output("\nValidating Settings ... \n");

            // Check Username Setting
            if (string.IsNullOrEmpty(ConfigData.Username.Value))
            {
                throw new ExceptionWithConfigData("You must provide a username for this tracker to login !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- Username (auth) => " + ConfigData.Username.Value);
            }

            // Check Password Setting
            if (string.IsNullOrEmpty(ConfigData.Password.Value))
            {
                throw new ExceptionWithConfigData("You must provide a password with your username for this tracker to login !", ConfigData);
            }
            else
            {
                Output("Validated Setting -- Password (auth) => " + ConfigData.Password.Value);
            }

            // Check Max Page Setting
            if (!string.IsNullOrEmpty(ConfigData.Pages.Value))
            {
                try
                {
                    Output("Validated Setting -- Max Pages => " + Convert.ToInt32(ConfigData.Pages.Value));
                }
                catch (Exception)
                {
                    throw new ExceptionWithConfigData("Please enter a numeric maximum number of pages to crawl !", ConfigData);
                }
            }
            else
            {
                throw new ExceptionWithConfigData("Please enter a maximum number of pages to crawl !", ConfigData);
            }

            // Check Latency Setting
            if (ConfigData.Latency.Value)
            {
                Output("\nValidated Setting -- Latency Simulation enabled");

                // Check Latency Start Setting
                if (!string.IsNullOrEmpty(ConfigData.LatencyStart.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Latency Start => " + Convert.ToInt32(ConfigData.LatencyStart.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric latency start in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Latency Simulation enabled, Please enter a start latency !", ConfigData);
                }

                // Check Latency End Setting
                if (!string.IsNullOrEmpty(ConfigData.LatencyEnd.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Latency End => " + Convert.ToInt32(ConfigData.LatencyEnd.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric latency end in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Latency Simulation enabled, Please enter a end latency !", ConfigData);
                }
            }

            // Check Browser Setting
            if (ConfigData.Browser.Value)
            {
                Output("\nValidated Setting -- Browser Simulation enabled");

                // Check ACCEPT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAccept.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- ACCEPT (header) => " + ConfigData.HeaderAccept.Value);
                }

                // Check ACCEPT-LANG header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderAcceptLang.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an ACCEPT-LANG header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- ACCEPT-LANG (header) => " + ConfigData.HeaderAcceptLang.Value);
                }

                // Check USER-AGENT header Setting
                if (string.IsNullOrEmpty(ConfigData.HeaderUserAgent.Value))
                {
                    throw new ExceptionWithConfigData("Browser Simulation enabled, Please enter an USER-AGENT header !", ConfigData);
                }
                else
                {
                    Output("Validated Setting -- USER-AGENT (header) => " + ConfigData.HeaderUserAgent.Value);
                }
            }
            else
            {
                // Browser simulation must be enabled (otherwhise, this provider will not work due to tracker's security)
                throw new ExceptionWithConfigData("Browser Simulation must be enabled for this provider to work, please enable it !", ConfigData);
            }

            // Check Dev Cache Settings
            if (ConfigData.HardDriveCache.Value)
            {
                Output("\nValidated Setting -- DEV Hard Drive Cache enabled");

                // Check if Dev Mode enabled !
                if (!ConfigData.DevMode.Value)
                {
                    throw new ExceptionWithConfigData("Hard Drive is enabled but not in DEV MODE, Please enable DEV MODE !", ConfigData);
                }

                // Check Cache Keep Time Setting
                if (!string.IsNullOrEmpty(ConfigData.HardDriveCacheKeepTime.Value))
                {
                    try
                    {
                        Output("Validated Setting -- Cache Keep Time (ms) => " + Convert.ToInt32(ConfigData.HardDriveCacheKeepTime.Value));
                    }
                    catch (Exception)
                    {
                        throw new ExceptionWithConfigData("Please enter a numeric hard drive keep time in ms !", ConfigData);
                    }
                }
                else
                {
                    throw new ExceptionWithConfigData("Hard Drive Cache enabled, Please enter a maximum keep time for cache !", ConfigData);
                }
            }
            else
            {
                // Delete cache if previously existed
                CleanCacheStorage(true);
            }
        }
    }
}
