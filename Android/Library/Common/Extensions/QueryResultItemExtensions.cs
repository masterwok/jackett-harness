using Jackett.Harness.Common.Models;
using Java.Lang;
using Double = Java.Lang.Double;
using KotlinQueryResultItem = Com.Masterwok.Xamarininterface.Models.QueryResultItem;
using Uri = Android.Net.Uri;

namespace Library.Common.Extensions
{
    public static class QueryResultItemExtensions
    {
        public static KotlinQueryResultItem ToKotlinQueryResultItem(
            this QueryResultItem source
        ) => new KotlinQueryResultItem(
            title: source.Title,
            description: source.Description,
            linkInfo: new KotlinQueryResultItem.LinkInfo(
                magnetUri: source.MagnetUri == null ? null : Uri.Parse(source.MagnetUri.ToString()),
                infoHash: source.InfoHash,
                link: source.Link == null ? null : Uri.Parse(source.Link.ToString()),
                comments: source.Comments == null ? null : Uri.Parse(source.Comments.ToString()),
                bannerUri: source.BannerUrl == null ? null : Uri.Parse(source.BannerUrl.ToString())
            ),
            socialInfo: new KotlinQueryResultItem.SocialInfo(
                rageId: source.RageID == null ? null : new Long(source.RageID.Value),
                tvdbId: source.TVDBId == null ? null : new Long(source.TVDBId.Value),
                imdb: source.Imdb == null ? null : new Long(source.Imdb.Value),
                tmdb: source.TMDb == null ? null : new Long(source.TMDb.Value)
            ),
            statInfo: new KotlinQueryResultItem.StatInfo(
                publishedOn: source.PublishedOn.ToJavaDate(),
                seeders: source.Seeders == null ? null : new Long(source.Seeders.Value),
                peers: source.Peers == null ? null : new Long(source.Peers.Value),
                size: source.Size == null ? null : new Long(source.Size.Value),
                files: source.Files == null ? null : new Long(source.Files.Value),
                grabs: source.Grabs == null ? null : new Long(source.Grabs.Value),
                minimumRatio: source.MinimumRatio == null ? null : new Double(source.MinimumRatio.Value),
                minimumSeedTime: source.MinimumSeedTime == null ? null : new Long(source.MinimumSeedTime.Value),
                downloadVolumeFactor: source.DownloadVolumeFactor == null
                    ? null
                    : new Double(source.DownloadVolumeFactor.Value),
                uploadVolumeFactor: source.UploadVolumeFactor == null
                    ? null
                    : new Double(source.UploadVolumeFactor.Value)
            )
        );
    }
}