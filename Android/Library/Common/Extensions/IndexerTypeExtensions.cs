using System;
using Jackett.Harness.Common.Constants;
using Jackett.Harness.Common.Models;
using KotlinIndexerType = Com.Masterwok.Xamarininterface.Enums.IndexerType;

namespace Library.Common.Extensions
{
    public static class IndexerTypeExtensions
    {
        public static KotlinIndexerType ToKotlinIndexerType(
            this IndexerType source
        ) => source switch
        {
            IndexerType.Public => KotlinIndexerType.Public,
            IndexerType.Private => KotlinIndexerType.Private,
            IndexerType.Aggregate => KotlinIndexerType.Aggregate,
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }
}