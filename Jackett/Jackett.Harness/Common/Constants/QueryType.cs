using System;

namespace Jackett.Harness.Common.Constants
{
    public enum QueryType
    {
        TvSearch,
        Search,
        Music,
        Movie,
        Caps
    }

    internal static class QueryTypeExtensions
    {
        internal static string ToJackettQueryType(this QueryType queryType) => queryType switch
        {
            QueryType.TvSearch => "tvsearch",
            QueryType.Search => "search",
            QueryType.Music => "music",
            QueryType.Movie => "movie",
            QueryType.Caps => "caps",
            _ => throw new ArgumentOutOfRangeException(nameof(queryType), $"Unknown query type: {queryType}")
        };
    }
}