using System;
using Java.Util;

namespace Library.Common.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Convert the provided <see cref="DateTime"/> to a <see cref="Date"/>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The <see cref="Date"/> instance or null when null is provided.</returns>
        public static Date ToJavaDate(this DateTime? dateTime) => dateTime == null
            ? null
            : new Date(new DateTimeOffset(dateTime.Value).ToUnixTimeMilliseconds());
    }
}