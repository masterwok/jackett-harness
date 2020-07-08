using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class XSpeeds : BaseWebIndexer
    {
        private string LandingUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string GetRSSKeyUrl => SiteLink + "getrss.php";
        private string SearchUrl => SiteLink + "browse.php";
        private string RSSUrl => SiteLink + "rss.php?secret_key={0}&feedtype=download&timezone=0&showrows=50&categories=all";
        private string CommentUrl => SiteLink + "details.php?id={0}";
        private string DownloadUrl => SiteLink + "download.php?id={0}";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public XSpeeds(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "xspeeds",
                   name: "XSpeeds",
                   description: "XSpeeds (XS) is a Private Torrent Tracker for MOVIES / TV / GENERAL",
                   link: "https://www.xspeeds.eu/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            configData.DisplayText.Value = "Expect an initial delay (often around 10 seconds) due to XSpeeds CloudFlare DDoS protection";
            configData.DisplayText.Name = "Notice";

            TorznabCaps.SupportsImdbMovieSearch = true;

            AddCategoryMapping(92, TorznabCatType.MoviesUHD, "4K Movies");
            AddCategoryMapping(91, TorznabCatType.TVUHD, "4K TV");
            AddCategoryMapping(94, TorznabCatType.TVUHD, "4K TV Boxsets");
            AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(4, TorznabCatType.PC, "Apps");
            AddCategoryMapping(82, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(80, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(66, TorznabCatType.MoviesBluRay, "Blu-Ray");
            AddCategoryMapping(48, TorznabCatType.Books, "Books Magazines");
            AddCategoryMapping(68, TorznabCatType.MoviesOther, "Cams/TS");
            AddCategoryMapping(65, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(10, TorznabCatType.MoviesDVD, "DVDR");
            AddCategoryMapping(72, TorznabCatType.MoviesForeign, "Foreign");
            AddCategoryMapping(74, TorznabCatType.TVOTHER, "Kids");
            AddCategoryMapping(44, TorznabCatType.TVSport, "MMA");
            AddCategoryMapping(11, TorznabCatType.Movies, "Movie Boxsets");
            AddCategoryMapping(12, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(13, TorznabCatType.Audio, "Music");
            AddCategoryMapping(15, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(32, TorznabCatType.ConsoleNDS, "NDS Games");
            AddCategoryMapping(9, TorznabCatType.Other, "Other");
            AddCategoryMapping(6, TorznabCatType.PCGames, "PC Games");
            AddCategoryMapping(45, TorznabCatType.Other, "Pictures");
            AddCategoryMapping(31, TorznabCatType.ConsolePS4, "Playstation");
            AddCategoryMapping(71, TorznabCatType.TV, "PPV");
            AddCategoryMapping(54, TorznabCatType.TV, "Soaps");
            AddCategoryMapping(20, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(86, TorznabCatType.TVSport, "MotorSports");
            AddCategoryMapping(89, TorznabCatType.TVSport, "Olympics 2016");
            AddCategoryMapping(88, TorznabCatType.TVSport, "World Cup");
            AddCategoryMapping(83, TorznabCatType.Movies, "TOTM");
            AddCategoryMapping(21, TorznabCatType.TVSD, "TV Boxsets");
            AddCategoryMapping(76, TorznabCatType.TVHD, "HD Boxsets");
            AddCategoryMapping(47, TorznabCatType.TVHD, "TV-HD");
            AddCategoryMapping(16, TorznabCatType.TVSD, "TV-SD");
            AddCategoryMapping(7, TorznabCatType.ConsoleWii, "Wii Games");
            AddCategoryMapping(43, TorznabCatType.TVSport, "Wrestling");
            AddCategoryMapping(8, TorznabCatType.ConsoleXbox, "Xbox Games");

            // RSS Textual categories
            AddCategoryMapping("4K Movies", TorznabCatType.MoviesUHD);
            AddCategoryMapping("4K TV", TorznabCatType.TVUHD);
            AddCategoryMapping("4K TV Boxsets", TorznabCatType.TVUHD);
            AddCategoryMapping("Anime", TorznabCatType.TVAnime);
            AddCategoryMapping("Apps", TorznabCatType.PC);
            AddCategoryMapping("Mac", TorznabCatType.PCMac);
            AddCategoryMapping("Audiobooks", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("Blu-Ray", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("Books Magazines", TorznabCatType.Books);
            AddCategoryMapping("Cams/TS", TorznabCatType.MoviesOther);
            AddCategoryMapping("Documentaries", TorznabCatType.TVDocumentary);
            AddCategoryMapping("DVDR", TorznabCatType.MoviesDVD);
            AddCategoryMapping("Foreign", TorznabCatType.MoviesForeign);
            AddCategoryMapping("Kids", TorznabCatType.TVOTHER);
            AddCategoryMapping("MMA", TorznabCatType.TVSport);
            AddCategoryMapping("Movie Boxsets", TorznabCatType.Movies);
            AddCategoryMapping("Movies", TorznabCatType.Movies);
            AddCategoryMapping("Music", TorznabCatType.Audio);
            AddCategoryMapping("Music Videos", TorznabCatType.AudioVideo);
            AddCategoryMapping("NDS Games", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("Other", TorznabCatType.Other);
            AddCategoryMapping("PC Games", TorznabCatType.PCGames);
            AddCategoryMapping("Pictures", TorznabCatType.Other);
            AddCategoryMapping("Playstation", TorznabCatType.ConsolePS4);
            AddCategoryMapping("PPV", TorznabCatType.TV);
            AddCategoryMapping("Soaps", TorznabCatType.TV);
            AddCategoryMapping("Sports", TorznabCatType.TVSport);
            AddCategoryMapping("MotorSports", TorznabCatType.TVSport);
            AddCategoryMapping("Olympics 2016", TorznabCatType.TVSport);
            AddCategoryMapping("World Cup", TorznabCatType.TVSport);
            AddCategoryMapping("TOTM", TorznabCatType.Movies);
            AddCategoryMapping("TV Boxsets", TorznabCatType.TVSD);
            AddCategoryMapping("HD Boxsets", TorznabCatType.TVHD);
            AddCategoryMapping("TV-HD", TorznabCatType.TVHD);
            AddCategoryMapping("TV-SD", TorznabCatType.TVSD);
            AddCategoryMapping("Wii Games", TorznabCatType.ConsoleWii);
            AddCategoryMapping("Wrestling", TorznabCatType.TVSport);
            AddCategoryMapping("Xbox Games", TorznabCatType.ConsoleXbox);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LandingUrl);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var qCaptchaImg = dom.QuerySelector("img#regimage");
            if (qCaptchaImg != null)
            {
                var CaptchaUrl = qCaptchaImg.GetAttribute("src");
                var captchaImage = await RequestBytesWithCookies(CaptchaUrl, loginPage.Cookies, RequestType.GET, LandingUrl);

                var CaptchaImage = new ImageItem { Name = "Captcha Image" };
                var CaptchaText = new StringItem { Name = "Captcha Text" };

                CaptchaImage.Value = captchaImage.Content;

                configData.AddDynamic("CaptchaImage", CaptchaImage);
                configData.AddDynamic("CaptchaText", CaptchaText);
            }
            else
            {
                logger.Debug(string.Format("{0}: No captcha image found", Id));
            }

            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                        {
                            {"username", configData.Username.Value},
                            {"password", configData.Password.Value}
                        };

            var CaptchaText = (StringItem)configData.GetDynamic("CaptchaText");
            if (CaptchaText != null)
            {
                pairs.Add("imagestring", CaptchaText.Value);
            }

            //var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, LandingUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("logout.php") == true,
                () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.Content);
                    var errorMessage = dom.QuerySelector(".left_side table:nth-of-type(1) tr:nth-of-type(2)")?.TextContent.Trim().Replace("\n\t", " ");
                    if (string.IsNullOrWhiteSpace(errorMessage))
                        errorMessage = dom.QuerySelector("div.notification-body").TextContent.Trim().Replace("\n\t", " ");
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });

            try
            {
                // Get RSS key
                var rssParams = new Dictionary<string, string>
                                {
                                    {"feedtype", "download"},
                                    {"timezone", "0"},
                                    {"showrows", "50"}
                                };
                var rssPage = await PostDataWithCookies(GetRSSKeyUrl, rssParams, result.Cookies);
                var match = Regex.Match(rssPage.Content, "(?<=secret_key\\=)([a-zA-z0-9]*)");
                configData.RSSKey.Value = match.Success ? match.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(configData.RSSKey.Value))
                    throw new Exception("Failed to get RSS Key");
                SaveConfig();
            }
            catch
            {
                IsConfigured = false;
                throw;
            }
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var prevCook = CookieHeader + "";
            var searchStringIsImdbQuery = (ParseUtil.GetImdbID(searchString) != null);

            // If we have no query use the RSS Page as their server is slow enough at times!
            // ~15.01.2019 they removed the description tag making the RSS feed almost useless, we don't use it for now. See #4458
            // if (false && query.IsTest || string.IsNullOrWhiteSpace(searchString))
            /*
            if (false)
            {
                var rssPage = await RequestStringWithCookiesAndRetry(string.Format(RSSUrl, configData.RSSKey.Value));
                try
                {
                    if (rssPage.Content.EndsWith("\0"))
                    {
                        rssPage.Content = rssPage.Content.Substring(0, rssPage.Content.Length - 1);
                    }
                    rssPage.Content = RemoveInvalidXmlChars(rssPage.Content);
                    var rssDoc = XDocument.Parse(rssPage.Content);

                    foreach (var item in rssDoc.Descendants("item"))
                    {
                        var title = item.Descendants("title").First().Value;
                        var description = item.Descendants("description").First().Value;
                        var link = item.Descendants("link").First().Value;
                        var category = item.Descendants("category").First().Value;
                        var date = item.Descendants("pubDate").First().Value;

                        var torrentIdMatch = Regex.Match(link, "(?<=id=)(\\d)*");
                        var torrentId = torrentIdMatch.Success ? torrentIdMatch.Value : string.Empty;
                        if (string.IsNullOrWhiteSpace(torrentId))
                            throw new Exception("Missing torrent id");

                        var infoMatch = Regex.Match(description, @"Category:\W(?<cat>.*)\W\/\WSeeders:\W(?<seeders>[\d\,]*)\W\/\WLeechers:\W(?<leechers>[\d\,]*)\W\/\WSize:\W(?<size>[\d\.]*\W\S*)");
                        if (!infoMatch.Success)
                            throw new Exception("Unable to find info");

                        var release = new ReleaseInfo
                        {
                            Title = title,
                            Description = title,
                            Guid = new Uri(string.Format(DownloadUrl, torrentId)),
                            Comments = new Uri(string.Format(CommentUrl, torrentId)),
                            PublishDate = DateTime.ParseExact(date, "yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture), //2015-08-08 21:20:31
                            Link = new Uri(string.Format(DownloadUrl, torrentId)),
                            Seeders = ParseUtil.CoerceInt(infoMatch.Groups["seeders"].Value),
                            Peers = ParseUtil.CoerceInt(infoMatch.Groups["leechers"].Value),
                            Size = ReleaseInfo.GetBytes(infoMatch.Groups["size"].Value),
                            Category = MapTrackerCatToNewznab(category)
                        };

                        release.Peers += release.Seeders;
                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("XSpeeds: Error while parsing the RSS feed:");
                    logger.Error(rssPage.Content);
                    throw ex;
                }
            }
            */
            //if (query.IsTest || !string.IsNullOrWhiteSpace(searchString))
            /*
            if (searchString.Length < 3 && !query.IsTest)
            {
                OnParseError("", new Exception("Minimum search length is 3"));
                return releases;
            }
            */
            var searchParams = new Dictionary<string, string> {
                { "do", "search" },
                { "category", "0" },
                { "include_dead_torrents", "no" }
            };

            if (query.IsImdbQuery)
            {
                searchParams.Add("keywords", query.ImdbID);
                searchParams.Add("search_type", "t_both");
            }
            else
            {
                searchParams.Add("keywords", searchString);
                if (searchStringIsImdbQuery)
                    searchParams.Add("search_type", "t_both");
                else
                    searchParams.Add("search_type", "t_name");
            }

            var searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams, CookieHeader);
            // Occasionally the cookies become invalid, login again if that happens
            if (searchPage.IsRedirect)
            {
                await ApplyConfiguration(null);
                searchPage = await PostDataWithCookiesAndRetry(SearchUrl, searchParams, CookieHeader);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(searchPage.Content);
                var rows = dom.QuerySelectorAll("table#sortabletable > tbody > tr:has(div > a[href*=\"details.php?id=\"])");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]");
                    var qTitle = qDetails; // #7975

                    release.Title = qTitle.TextContent;

                    release.Guid = new Uri(row.QuerySelector("td:nth-of-type(3) a").GetAttribute("href"));
                    release.Link = release.Guid;
                    release.Comments = new Uri(qDetails.GetAttribute("href"));
                    //08-08-2015 12:51
                    release.PublishDate = DateTime.ParseExact(
                        row.QuerySelectorAll("td:nth-of-type(2) div").Last().TextContent.Trim(), "dd-MM-yyyy H:mm",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)").TextContent);
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)").TextContent.Trim());
                    release.Size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-of-type(5)").TextContent.Trim());

                    var qBanner = row.QuerySelector("td:nth-of-type(2) .tooltip-content img");
                    if (qBanner != null)
                        release.BannerUrl = new Uri(qBanner.GetAttribute("src"));

                    var cat = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                    var catSplit = cat.LastIndexOf('=');
                    if (catSplit > -1)
                        cat = cat.Substring(catSplit + 1);
                    release.Category = MapTrackerCatToNewznab(cat);

                    var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.QuerySelector("img[alt^=\"Free Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt^=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    if (row.QuerySelector("img[alt^=\"x2 Torrent\"]") != null)
                        release.UploadVolumeFactor = 2;
                    else
                        release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(searchPage.Content, ex);
            }
            if (!CookieHeader.Trim().Equals(prevCook.Trim()))
            {
                SaveConfig();
            }
            return releases;
        }
    }
}
