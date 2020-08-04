using System;
using Jackett.Harness.Common.Constants;
using KotlinQueryState = Com.Masterwok.Xamarininterface.Enums.QueryState;

namespace Library.Common.Extensions
{
    public static class QueryStateExtensions
    {
        public static KotlinQueryState ToKotlinQueryState(
            this QueryState source
        ) => source switch
        {
            QueryState.Success => KotlinQueryState.Success,
            QueryState.Failure => KotlinQueryState.Failure,
            QueryState.Aborted => KotlinQueryState.Aborted,
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        }; 
    }
}