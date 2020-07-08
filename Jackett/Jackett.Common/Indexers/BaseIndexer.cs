using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public abstract class BaseIndexer : IIndexer
    {
        public string Id { get; protected set; }
        public string SiteLink { get; protected set; }
        public virtual string[] LegacySiteLinks { get; protected set; }
        public string DefaultSiteLink { get; protected set; }
        public virtual string[] AlternativeSiteLinks { get; protected set; } = new string[] { };
        public string DisplayDescription { get; protected set; }
        public string DisplayName { get; protected set; }
        public string Language { get; protected set; }
        public string Type { get; protected set; }


        [JsonConverter(typeof(EncodingJsonConverter))]
        public Encoding Encoding { get; protected set; }

        public virtual bool IsConfigured { get; protected set; }
        protected Logger logger;
        protected IIndexerConfigurationService configurationService;
        protected IProtectionService protectionService;

        protected ConfigurationData configData;

        protected string CookieHeader
        {
            get => configData.CookieHeader.Value;
            set => configData.CookieHeader.Value = value;
        }

        public string LastError
        {
            get => configData.LastError.Value;
            set
            {
                var SaveNeeded = configData.LastError.Value != value && IsConfigured;
                configData.LastError.Value = value;
                if (SaveNeeded)
                    SaveConfig();
            }
        }

        public abstract TorznabCapabilities TorznabCaps { get; protected set; }

        // standard constructor used by most indexers
        public BaseIndexer(string link, string id, string name, string description,
                           IIndexerConfigurationService configService, Logger logger, ConfigurationData configData,
                           IProtectionService p)
        {
            this.logger = logger;
            configurationService = configService;
            protectionService = p;

            if (!link.EndsWith("/", StringComparison.Ordinal))
                throw new Exception("Site link must end with a slash.");

            Id = id;
            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            DefaultSiteLink = link;
            this.configData = configData;
            if (configData != null)
                LoadValuesFromJson(null);
        }

        public virtual Task<ConfigurationData> GetConfigurationForSetup() => Task.FromResult<ConfigurationData>(configData);

        public virtual void ResetBaseConfig()
        {
            CookieHeader = string.Empty;
            IsConfigured = false;
        }

        public virtual void SaveConfig() => configurationService.Save(this as IIndexer, configData.ToJson(protectionService, forDisplay: false));

        protected void LoadLegacyCookieConfig(JToken jsonConfig)
        {
            var legacyCookieHeader = (string)jsonConfig["cookie_header"];
            if (!string.IsNullOrEmpty(legacyCookieHeader))
            {
                CookieHeader = legacyCookieHeader;
            }
            else
            {
                // Legacy cookie key
                var jcookies = jsonConfig["cookies"];
                if (jcookies is JArray)
                {
                    var array = (JArray)jcookies;
                    legacyCookieHeader = string.Empty;
                    for (var i = 0; i < array.Count; i++)
                    {
                        if (i != 0)
                            legacyCookieHeader += "; ";
                        legacyCookieHeader += array[i];
                    }
                    CookieHeader = legacyCookieHeader;
                }
                else if (jcookies != null)
                {
                    CookieHeader = (string)jcookies;
                }
            }
        }

        public virtual void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            IProtectionService ps = null;
            if (useProtectionService)
                ps = protectionService;
            configData.LoadValuesFromJson(jsonConfig, ps);
            if (string.IsNullOrWhiteSpace(configData.SiteLink.Value))
            {
                configData.SiteLink.Value = DefaultSiteLink;
            }

            if (!configData.SiteLink.Value.EndsWith("/", StringComparison.Ordinal))
                configData.SiteLink.Value += "/";

            // reset site link to default if it's a legacy (defunc link)
            if (LegacySiteLinks != null && LegacySiteLinks.Contains(configData.SiteLink.Value))
            {
                logger.Debug(string.Format("changing legacy site link from {0} to {1}", configData.SiteLink.Value, DefaultSiteLink));
                configData.SiteLink.Value = DefaultSiteLink;
            }

            // check whether the site link is well-formatted
            var siteUri = new Uri(configData.SiteLink.Value);
            SiteLink = configData.SiteLink.Value;
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JArray)
            {
                if (!MigratedFromDPAPI(jsonConfig))
                {
                    LoadValuesFromJson(jsonConfig, true);
                    IsConfigured = true;
                }
            }
            // read and upgrade old settings file format
            else if (jsonConfig is object)
            {
                LoadLegacyCookieConfig(jsonConfig);
                SaveConfig();
                IsConfigured = true;
            }
        }

        //TODO: Remove this section once users have moved off DPAPI
        private bool MigratedFromDPAPI(JToken jsonConfig)
        {
            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

            if (!isWindows && DotNetCoreUtil.IsRunningOnDotNetCore)
            {
                // User isn't running Windows, but is running on .NET Core framework, no access to the DPAPI, so don't bother trying to migrate
                return false;
            }

            LoadValuesFromJson(jsonConfig, false);

            StringItem passwordPropertyValue = null;
            var passwordValue = "";

            try
            {
                // try dynamic items first (e.g. all cardigann indexers)
                passwordPropertyValue = (StringItem)configData.GetDynamicByName("password");

                if (passwordPropertyValue == null) // if there's no dynamic password try the static property
                {
                    passwordPropertyValue = (StringItem)configData.GetType().GetProperty("Password").GetValue(configData, null);

                    // protection is based on the item.Name value (property name might be different, example: Abnormal), so check the Name again
                    if (!string.Equals(passwordPropertyValue.Name, "password", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.Debug($"Skipping non default password property (unencrpyted password) for [{Id}] while attempting migration");
                        return false;
                    }
                }

                passwordValue = passwordPropertyValue.Value;
            }
            catch (Exception)
            {
                logger.Debug($"Unable to source password for [{Id}] while attempting migration, likely a tracker without a password setting");
                return false;
            }

            if (!string.IsNullOrEmpty(passwordValue))
            {
                try
                {
                    protectionService.UnProtect(passwordValue);
                    //Password successfully unprotected using Microsoft.AspNetCore.DataProtection, no further action needed as we've already converted the password previously
                    return false;
                }
                catch (Exception ex)
                {
                    if (ex.Message != "The provided payload cannot be decrypted because it was not protected with this protection provider.")
                    {
                        logger.Info($"Password could not be unprotected using Microsoft.AspNetCore.DataProtection - {Id} : " + ex);
                    }

                    logger.Info($"Attempting legacy Unprotect - {Id} : ");

                    try
                    {
                        var unprotectedPassword = protectionService.LegacyUnProtect(passwordValue);
                        //Password successfully unprotected using Windows/Mono DPAPI

                        passwordPropertyValue.Value = unprotectedPassword;
                        SaveConfig();
                        IsConfigured = true;

                        logger.Info($"Password successfully migrated for {Id}");

                        return true;
                    }
                    catch (Exception exception)
                    {
                        logger.Info($"Password could not be unprotected using legacy DPAPI - {Id} : " + exception);
                    }
                }
            }

            return false;
        }

        protected async Task ConfigureIfOK(string cookies, bool isLoggedin, Func<Task> onError)
        {
            if (isLoggedin)
            {
                CookieHeader = cookies;
                IsConfigured = true;
                SaveConfig();
            }
            else
            {
                await onError();
            }
        }

        protected virtual IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> results)
        {
            if (query.Categories.Length == 0)
                return results;

            var filteredResults = results.Where(
                result => result.Category?.Any() != true || query.Categories.Intersect(result.Category).Any() ||
                          TorznabCatType.QueryContainsParentCategory(query.Categories, result.Category));

            return filteredResults;
        }

        public virtual bool CanHandleQuery(TorznabQuery query)
        {
            if (query == null)
                return false;
            if (query.QueryType == "caps")
                return true;

            var caps = TorznabCaps;

            if (query.HasSpecifiedCategories)
                if (!caps.SupportsCategories(query.Categories))
                    return false;
            if (caps.SupportsImdbTVSearch && query.IsImdbQuery && query.IsTVSearch)
                return true;
            if (caps.SupportsImdbMovieSearch && query.IsImdbQuery && query.IsMovieSearch)
                return true;
            else if (!caps.SupportsImdbMovieSearch && query.IsImdbQuery && query.QueryType != "TorrentPotato") // potato query should always contain imdb+search term
                return false;
            if (caps.SearchAvailable && query.IsSearch)
                return true;
            if (caps.TVSearchAvailable && query.IsTVSearch)
                return true;
            if (caps.MovieSearchAvailable && query.IsMovieSearch)
                return true;
            if (caps.MusicSearchAvailable && query.IsMusicSearch)
                return true;
            if (caps.SupportsTVRageSearch && query.IsTVRageSearch)
                return true;
            if (caps.SupportsImdbMovieSearch && query.IsImdbQuery)
                return true;

            return false;
        }

        public void Unconfigure()
        {
            IsConfigured = false;
            SiteLink = DefaultSiteLink;
            CookieHeader = ""; // clear cookies
        }

        public abstract Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson);

        public virtual async Task<IndexerResult> ResultsForQuery(TorznabQuery query)
        {
            try
            {
                if (!CanHandleQuery(query))
                    return new IndexerResult(this, new ReleaseInfo[0]);
                var results = await PerformQuery(query);
                results = FilterResults(query, results);
                if (query.Limit > 0)
                {
                    results = results.Take(query.Limit);
                }
                results = results.Select(r =>
                {
                    r.Origin = this;

                    // Some trackers do not keep their clocks up to date and can be ~20 minutes out!
                    if (r.PublishDate > DateTime.Now)
                        r.PublishDate = DateTime.Now;
                    return r;
                });

                return new IndexerResult(this, results);
            }
            catch (Exception ex)
            {
                throw new IndexerException(this, ex);
            }
        }

        protected abstract Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query);
    }

    public abstract class BaseWebIndexer : BaseIndexer, IWebIndexer
    {
        protected BaseWebIndexer(string link, string id, string name, string description,
                                 IIndexerConfigurationService configService, WebClient client, Logger logger,
                                 ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null,
                                 string downloadBase = null)
            : base(link, id, name, description, configService, logger, configData, p)
        {
            webclient = client;
            downloadUrlBase = downloadBase;

            if (caps == null)
                caps = TorznabUtil.CreateDefaultTorznabTVCaps();
            TorznabCaps = caps;
        }

        // minimal constructor used by e.g. cardigann generic indexer
        protected BaseWebIndexer(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p)
            : base("/", "", "", "", configService, logger, null, p) => webclient = client;

        public virtual async Task<byte[]> Download(Uri link)
        {
            var uncleanLink = UncleanLink(link);
            return await Download(uncleanLink, RequestType.GET);
        }

        protected async Task<byte[]> Download(Uri link, RequestType method, string refererlink = null)
        {
            // return magnet link
            if (link.Scheme == "magnet")
                return Encoding.UTF8.GetBytes(link.OriginalString);

            // do some extra escaping, needed for HD-Torrents
            var requestLink = link.ToString()
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("'", "%27");
            var response = await RequestBytesWithCookiesAndRetry(requestLink, null, method, requestLink);

            // if referer link is provied it will be used
            if (refererlink != null)
                response = await RequestBytesWithCookiesAndRetry(requestLink, null, method, refererlink);
            if (response.IsRedirect)
            {
                await FollowIfRedirect(response);
            }
            if (response.Status != System.Net.HttpStatusCode.OK && response.Status != System.Net.HttpStatusCode.Continue && response.Status != System.Net.HttpStatusCode.PartialContent)
            {
                logger.Error("Failed download cookies: " + CookieHeader);
                if (response.Content != null)
                    logger.Error("Failed download response:\n" + Encoding.UTF8.GetString(response.Content));
                throw new Exception($"Remote server returned {response.Status.ToString()}" + (response.IsRedirect ? " => " + response.RedirectingTo : ""));
            }

            return response.Content;
        }

        protected async Task<WebClientByteResult> RequestBytesWithCookiesAndRetry(string url, string cookieOverride = null, RequestType method = RequestType.GET, string referer = null, IEnumerable<KeyValuePair<string, string>> data = null)
        {
            Exception lastException = null;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestBytesWithCookies(url, cookieOverride, method, referer, data);
                }
                catch (Exception e)
                {
                    logger.Error(e, string.Format("On attempt {0} downloading from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected async Task<WebClientStringResult> RequestStringWithCookies(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = CookieHeader,
                Referer = referer,
                Headers = headers,
                Encoding = Encoding
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            var result = await webclient.GetString(request);
            CheckTrackerDown(result);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebClientStringResult> RequestStringWithCookiesAndRetry(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            Exception lastException = null;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestStringWithCookies(url, cookieOverride, referer, headers);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} checking for results from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected virtual async Task<WebClientByteResult> RequestBytesWithCookies(string url, string cookieOverride = null, RequestType method = RequestType.GET, string referer = null, IEnumerable<KeyValuePair<string, string>> data = null, Dictionary<string, string> headers = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = method,
                Cookies = cookieOverride ?? CookieHeader,
                PostData = data,
                Referer = referer,
                Headers = headers,
                Encoding = Encoding
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            var result = await webclient.GetBytes(request);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebClientStringResult> PostDataWithCookies(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookieOverride ?? CookieHeader,
                PostData = data,
                Referer = referer,
                Headers = headers,
                RawBody = rawbody,
                Encoding = Encoding
            };

            if (emulateBrowser.HasValue)
                request.EmulateBrowser = emulateBrowser.Value;
            var result = await webclient.GetString(request);
            CheckTrackerDown(result);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebClientStringResult> PostDataWithCookiesAndRetry(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            Exception lastException = null;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    return await PostDataWithCookies(url, data, cookieOverride, referer, headers, rawbody, emulateBrowser);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} checking for results from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected async Task<WebClientStringResult> RequestLoginAndFollowRedirect(string url, IEnumerable<KeyValuePair<string, string>> data, string cookies, bool returnCookiesFromFirstCall, string redirectUrlOverride = null, string referer = null, bool accumulateCookies = false)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookies,
                Referer = referer,
                PostData = data,
                Encoding = Encoding
            };
            var response = await webclient.GetString(request);
            CheckTrackerDown(response);
            if (accumulateCookies)
            {
                response.Cookies = ResolveCookies((request.Cookies == null ? "" : request.Cookies + " ") + response.Cookies);
            }
            var firstCallCookies = response.Cookies;

            if (response.IsRedirect)
            {
                await FollowIfRedirect(response, request.Url, redirectUrlOverride, response.Cookies, accumulateCookies);
            }

            if (returnCookiesFromFirstCall)
            {
                response.Cookies = ResolveCookies(firstCallCookies + (accumulateCookies ? " " + response.Cookies : ""));
            }

            return response;
        }

        protected void CheckTrackerDown(WebClientStringResult response)
        {
            if (response.Status == System.Net.HttpStatusCode.BadGateway
                || response.Status == System.Net.HttpStatusCode.GatewayTimeout
                || (int)response.Status == 521 // used by cloudflare to signal the original webserver is refusing the connection
                || (int)response.Status == 522 // used by cloudflare to signal the original webserver is not reachable at all (timeout)
                || (int)response.Status == 523 // used by cloudflare to signal the original webserver is not reachable at all (Origin is unreachable)
                )
            {
                throw new Exception("Request to " + response.Request.Url + " failed (Error " + response.Status + ") - The tracker seems to be down.");
            }

            if (response.Status == System.Net.HttpStatusCode.Forbidden && response.Content.Contains("<span data-translate=\"complete_sec_check\">Please complete the security check to access</span>"))
            {
                throw new Exception("Request to " + response.Request.Url + " failed (Error " + response.Status + ") - The page is protected by an Cloudflare reCaptcha. The page is in aggressive DDoS mitigation mode or your IP might be blacklisted (e.g. in case of shared VPN IPs). There's no easy way of making it usable with Jackett.");
            }
        }

        protected async Task FollowIfRedirect(WebClientStringResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            var byteResult = new WebClientByteResult();
            // Map to byte
            Mapper.Map(response, byteResult);
            await FollowIfRedirect(byteResult, referrer, overrideRedirectUrl, overrideCookies, accumulateCookies);
            // Map to string
            Mapper.Map(byteResult, response);
        }

        protected async Task FollowIfRedirect(WebClientByteResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            // Follow up  to 5 redirects
            for (var i = 0; i < 5; i++)
            {
                if (!response.IsRedirect)
                    break;
                await DoFollowIfRedirect(response, referrer, overrideRedirectUrl, overrideCookies, accumulateCookies);
                if (accumulateCookies)
                {
                    CookieHeader = ResolveCookies((CookieHeader != null && CookieHeader != "" ? CookieHeader + " " : "") + (overrideCookies != null && overrideCookies != "" ? overrideCookies + " " : "") + response.Cookies);
                    overrideCookies = response.Cookies = CookieHeader;
                }
                if (overrideCookies != null && response.Cookies == null)
                {
                    response.Cookies = overrideCookies;
                }
            }
        }

        private string ResolveCookies(string incomingCookies = "")
        {
            var redirRequestCookies = string.IsNullOrWhiteSpace(CookieHeader) ? incomingCookies : CookieHeader + " " + incomingCookies;
            var cookieDictionary = CookieUtil.CookieHeaderToDictionary(redirRequestCookies);

            // These cookies are causing BadGateway errors, so we drop them, see issue #2306
            cookieDictionary.Remove("cf_use_ob");
            cookieDictionary.Remove("cf_ob_info");

            return CookieUtil.CookieDictionaryToHeader(cookieDictionary);
        }

        // Update CookieHeader with new cookies and save the config if something changed (e.g. a new CloudFlare clearance cookie was issued)
        protected virtual void UpdateCookieHeader(string newCookies, string cookieOverride = null)
        {
            var newCookieHeader = ResolveCookies((cookieOverride != null && cookieOverride != "" ? cookieOverride + " " : "") + newCookies);
            if (CookieHeader != newCookieHeader)
            {
                logger.Debug(string.Format("updating Cookies {0} => {1}", CookieHeader, newCookieHeader));
                CookieHeader = newCookieHeader;
                if (IsConfigured)
                    SaveConfig();
            }
        }

        private async Task DoFollowIfRedirect(WebClientByteResult incomingResponse, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            if (incomingResponse.IsRedirect)
            {
                var redirRequestCookies = "";
                if (accumulateCookies)
                {
                    redirRequestCookies = ResolveCookies((CookieHeader != "" ? CookieHeader + " " : "") + (overrideCookies != null ? overrideCookies : ""));
                }
                else
                {
                    redirRequestCookies = (overrideCookies != null ? overrideCookies : "");
                }
                // Do redirect
                var redirectedResponse = await webclient.GetBytes(new WebRequest()
                {
                    Url = overrideRedirectUrl ?? incomingResponse.RedirectingTo,
                    Referer = referrer,
                    Cookies = redirRequestCookies,
                    Encoding = Encoding
                });
                Mapper.Map(redirectedResponse, incomingResponse);
            }
        }

        protected List<string> GetAllTrackerCategories() => categoryMapping.Select(x => x.TrackerCategory).ToList();

        protected void AddCategoryMapping(string trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
            {
                TorznabCaps.Categories.Add(newznabCategory);
                if (TorznabCatType.Movies.Contains(newznabCategory))
                    TorznabCaps.MovieSearchAvailable = true;
            }

            // add 1:1 categories
            if (trackerCategoryDesc != null && trackerCategory != null)
            {
                //TODO convert to int.TryParse() to avoid using throw as flow control
                try
                {
                    var trackerCategoryInt = int.Parse(trackerCategory);
                    var CustomCat = new TorznabCategory(trackerCategoryInt + 100000, trackerCategoryDesc);
                    if (!TorznabCaps.Categories.Contains(CustomCat))
                        TorznabCaps.Categories.Add(CustomCat);
                }
                catch (FormatException)
                {
                    // trackerCategory is not an integer, continue
                }
            }
        }

        protected void AddCategoryMapping(int trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null) => AddCategoryMapping(trackerCategory.ToString(), newznabCategory, trackerCategoryDesc);

        protected void AddMultiCategoryMapping(TorznabCategory newznabCategory, params int[] trackerCategories)
        {
            foreach (var trackerCat in trackerCategories)
            {
                AddCategoryMapping(trackerCat, newznabCategory);
            }
        }

        protected virtual List<string> MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false)
        {
            var result = new List<string>();
            foreach (var cat in query.Categories)
            {
                // use 1:1 mapping to tracker categories for newznab categories >= 100000
                if (cat >= 100000)
                {
                    result.Add((cat - 100000).ToString());
                    continue;
                }

                var queryCats = new List<int> { cat };
                var newznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.ID == cat);
                if (newznabCat != null)
                {
                    queryCats.AddRange(newznabCat.SubCategories.Select(c => c.ID));
                }

                if (mapChildrenCatsToParent)
                {
                    var parentNewznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.SubCategories.Contains(newznabCat));
                    if (parentNewznabCat != null)
                    {
                        queryCats.Add(parentNewznabCat.ID);
                    }
                }

                foreach (var mapping in categoryMapping.Where(c => queryCats.Contains(c.NewzNabCategory)))
                {
                    result.Add(mapping.TrackerCategory);
                }
            }

            return result.Distinct().ToList();
        }

        protected ICollection<int> MapTrackerCatToNewznab(string input)
        {
            if (input == null)
                return new List<int>();

            var cats = categoryMapping.Where(m => m.TrackerCategory != null && m.TrackerCategory.ToLowerInvariant() == input.ToLowerInvariant()).Select(c => c.NewzNabCategory).ToList();

            // 1:1 category mapping
            try
            {
                var trackerCategoryInt = int.Parse(input);
                cats.Add(trackerCategoryInt + 100000);
            }
            catch (FormatException)
            {
                // input is not an integer, continue
            }

            return cats;
        }

        protected ICollection<int> MapTrackerCatDescToNewznab(string input)
        {
            var cats = new List<int>();
            if (null != input)
            {
                var mapping = categoryMapping.Where(m => m.TrackerCategoryDesc != null && m.TrackerCategoryDesc.ToLowerInvariant() == input.ToLowerInvariant()).FirstOrDefault();
                if (mapping != null)
                {
                    cats.Add(mapping.NewzNabCategory);

                    if (mapping.TrackerCategory != null)
                    {
                        // 1:1 category mapping
                        try
                        {
                            var trackerCategoryInt = int.Parse(mapping.TrackerCategory);
                            cats.Add(trackerCategoryInt + 100000);
                        }
                        catch (FormatException)
                        {
                            // mapping.TrackerCategory is not an integer, continue
                        }
                    }
                }
            }
            return cats;
        }

        private IEnumerable<ReleaseInfo> CleanLinks(IEnumerable<ReleaseInfo> releases)
        {
            if (string.IsNullOrEmpty(downloadUrlBase))
                return releases;
            foreach (var release in releases)
            {
                if (release.Link.ToString().StartsWith(downloadUrlBase, StringComparison.Ordinal))
                {
                    release.Link = new Uri(release.Link.ToString().Substring(downloadUrlBase.Length), UriKind.Relative);
                }
            }

            return releases;
        }

        public override async Task<IndexerResult> ResultsForQuery(TorznabQuery query)
        {
            var result = await base.ResultsForQuery(query);
            result.Releases = CleanLinks(result.Releases);

            return result;
        }

        protected virtual Uri UncleanLink(Uri link)
        {
            if (string.IsNullOrWhiteSpace(downloadUrlBase))
            {
                return link;
            }

            if (link.ToString().StartsWith(downloadUrlBase, StringComparison.Ordinal))
            {
                return link;
            }

            return new Uri(downloadUrlBase + link.ToString(), UriKind.RelativeOrAbsolute);
        }

        protected void OnParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), DisplayName);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
            throw new Exception("Parse error", ex);
        }

        public override TorznabCapabilities TorznabCaps { get; protected set; }

        private readonly List<CategoryMapping> categoryMapping = new List<CategoryMapping>();
        protected WebClient webclient;
        protected readonly string downloadUrlBase = "";
    }

    public abstract class BaseCachingWebIndexer : BaseWebIndexer
    {
        protected BaseCachingWebIndexer(string link,string id, string name, string description,
                                        IIndexerConfigurationService configService, WebClient client, Logger logger,
                                        ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null,
                                        string downloadBase = null)
            : base(link, id, name, description, configService, client, logger, configData, p, caps, downloadBase)
        {
        }

        protected void CleanCache()
        {
            foreach (var expired in cache.Where(i => DateTime.Now - i.Created > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }

        protected static List<CachedQueryResult> cache = new List<CachedQueryResult>();
        protected static readonly TimeSpan cacheTime = new TimeSpan(0, 9, 0);
    }
}
