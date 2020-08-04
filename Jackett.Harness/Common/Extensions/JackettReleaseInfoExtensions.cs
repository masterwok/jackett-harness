using Jackett.Common.Models;
using Jackett.Harness.Common.Models;

namespace Jackett.Harness.Common.Extensions
{
    /// <summary>
    /// This static class provides extensions for the <see cref="ReleaseInfo"/> type.
    /// </summary>
    internal static class JackettReleaseInfoExtensions
    {
        /// <summary>
        /// Convert the <see cref="ReleaseInfo"/> instance into a <see cref="QueryResultItem"/> instance.
        /// </summary>
        /// <param name="releaseInfo">The <see cref="ReleaseInfo"/> instance to convert.</param>
        /// <returns>The converted <see cref="QueryResultItem"/> instance.</returns>
        public static QueryResultItem ToQueryResultItem(this ReleaseInfo releaseInfo) => new QueryResultItem
        {
            Title = releaseInfo.Title,
            Description = releaseInfo.Description,
            InfoHash = releaseInfo.InfoHash,
            MagnetUri = releaseInfo.MagnetUri,
            Seeders = releaseInfo.Seeders ?? 0,
            Peers = releaseInfo.Peers ?? 0,
            Link = releaseInfo.Link,
            Comments = releaseInfo.Comments,
            PublishedOn = releaseInfo.PublishDate,
            Size = releaseInfo.Size,
            Files = releaseInfo.Files,
            Grabs = releaseInfo.Grabs,
            RageID = releaseInfo.RageID,
            TVDBId = releaseInfo.TVDBId,
            Imdb = releaseInfo.Imdb,
            TMDb = releaseInfo.TMDb,
            BannerUrl = releaseInfo.BannerUrl,
            MinimumRatio = releaseInfo.MinimumRatio,
            MinimumSeedTime = releaseInfo.MinimumSeedTime,
            DownloadVolumeFactor = releaseInfo.DownloadVolumeFactor,
            UploadVolumeFactor = releaseInfo.UploadVolumeFactor
        };
    }
}