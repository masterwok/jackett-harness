using Jackett.Harness.Common.Constants;
using Jackett.Harness.Common.Models;
using KotlinQuery = Com.Masterwok.Xamarininterface.Models.Query;

namespace Library.Common.Extensions
{
    internal static class KotlinQueryExtensions
    {
        public static Query ToJackettHarnessQuery( this KotlinQuery kotlinQuery ) => new Query
        {
            Type = QueryType.Search,
            SearchTerm = kotlinQuery.QueryString
        };
    }
}