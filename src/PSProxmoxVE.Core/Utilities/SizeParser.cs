using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Parses storage size strings (e.g. "32G", "1T", "60") and normalizes them
    /// to a bare integer count of gibibytes for use in PVE disk specs.
    /// </summary>
    /// <remarks>
    /// PVE accepts a size suffix on file-backed storages (NFS, directory) but parses
    /// the value after the colon as a volume name on LVM-backed storages — so
    /// <c>local-lvm:32G</c> fails with "unable to parse lvm volume name '32G'" while
    /// <c>local-lvm:32</c> works on every storage type. Cmdlets that build disk specs
    /// must normalize size inputs through this helper before joining with the storage.
    /// </remarks>
    public static class SizeParser
    {
        private static readonly Regex Pattern = new Regex(
            @"^\s*(?<num>\d+)\s*(?<unit>[A-Za-z]*)\s*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses a size string and returns the value as a bare integer count of GiB.
        /// Accepts values like "60", "60G", "60GB" (= 60), "1T", "1TB" (= 1024).
        /// Sub-GB units are rejected because PVE disk allocation is GB-granular.
        /// </summary>
        /// <param name="value">The size string supplied by the user.</param>
        /// <param name="parameterName">Parameter name used in the error message.</param>
        /// <returns>The size in whole GiB as a string, suitable for direct use in disk specs.</returns>
        /// <exception cref="ArgumentException">The input cannot be parsed or uses an unsupported unit.</exception>
        public static string NormalizeToGibibytes(string value, string parameterName = "size")
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} must not be null or empty.", parameterName);

            var match = Pattern.Match(value);
            if (!match.Success)
                throw new ArgumentException(
                    $"{parameterName} '{value}' is not a valid size. Expected a positive integer optionally suffixed with G, GB, T, or TB (e.g. '32G', '1T', '60').",
                    parameterName);

            if (!long.TryParse(match.Groups["num"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num) || num <= 0)
                throw new ArgumentException(
                    $"{parameterName} '{value}' must be a positive integer.",
                    parameterName);

            var unit = match.Groups["unit"].Value.ToUpperInvariant();
            long gib;
            switch (unit)
            {
                case "":
                case "G":
                case "GB":
                case "GIB":
                    gib = num;
                    break;
                case "T":
                case "TB":
                case "TIB":
                    gib = checked(num * 1024L);
                    break;
                default:
                    throw new ArgumentException(
                        $"{parameterName} '{value}' uses unsupported unit '{unit}'. Use G, GB, T, or TB. Sub-GB units (M, MB, K, KB) are not supported by PVE disk allocation.",
                        parameterName);
            }

            return gib.ToString(CultureInfo.InvariantCulture);
        }
    }
}
