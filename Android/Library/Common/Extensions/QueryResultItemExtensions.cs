using Jackett.Harness.Common.Models;
using KotlinQueryResultItem = Com.Masterwok.Xamarininterface.Models.QueryResultIem;

namespace Library.Common.Extensions
{
    public static class QueryResultItemExtensions
    {
        public static KotlinQueryResultItem ToKotlinQueryResultItem(
            this QueryResultItem source
        ) => new KotlinQueryResultItem(
            source.Title
        );
    }
}