using System;
using System.Collections.Generic;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Parses Corosync link strings (e.g. "link0=10.0.0.1") as sent to the cluster
    /// create/join/add-node endpoints.
    /// </summary>
    public static class CorosyncLinks
    {
        /// <summary>
        /// Parses an array of Corosync link strings into a dictionary.
        /// </summary>
        /// <param name="links">Array of link strings in "linkN=address" format.</param>
        /// <returns>
        /// The parsed dictionary (null if <paramref name="links"/> is null, or if every entry
        /// was malformed), and the malformed entries verbatim — the caller decides how to
        /// report them.
        /// </returns>
        public static (Dictionary<string, string>? Links, IReadOnlyList<string> Malformed) Parse(string[]? links)
        {
            if (links == null) return (null, Array.Empty<string>());

            var result = new Dictionary<string, string>();
            var malformed = new List<string>();
            foreach (var link in links)
            {
                var parts = link?.Split(new[] { '=' }, 2) ?? Array.Empty<string>();
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    malformed.Add(link ?? string.Empty);
                    continue;
                }
                result[parts[0].Trim()] = parts[1].Trim();
            }
            return (result.Count > 0 ? result : null, malformed);
        }
    }
}
