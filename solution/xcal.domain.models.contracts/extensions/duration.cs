﻿using System;
using reexjungle.xcal.core.domain.contracts.models.values;

namespace reexjungle.xcal.core.domain.contracts.extensions
{
    /// <summary>
    /// Provides extensions for duration-related features-.
    /// </summary>
    public static class DurationExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="TimeSpan"/> instance into a <see cref="DURATION"/> value.
        /// </summary>
        /// <param name="timespan">The time span instance to convert.</param>
        /// <returns>The equivalent <see cref="DURATION"/> instance resulting from the conversion.</returns>
        public static DURATION AsDURATION(this TimeSpan timespan) => new DURATION(timespan);
    }
}
