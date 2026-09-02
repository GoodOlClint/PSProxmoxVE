using System;
using Newtonsoft.Json.Linq;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Reads the fields of a guest status/current response that decide whether a
    /// lifecycle wait (-Wait on Start, Stop, Restart, Reset, Resume) is finished.
    /// </summary>
    public static class GuestStatusSnapshot
    {
        /// <summary>
        /// Evaluates a status/current response body against the status a caller is waiting for.
        /// </summary>
        /// <param name="json">Raw status/current response body.</param>
        /// <param name="expectedStatus">The status being waited for (e.g. "running", "stopped", "paused").</param>
        /// <returns>
        /// StatusMatched: the guest reports <paramref name="expectedStatus"/>. qmpstatus is
        /// preferred over status when present, because PVE reports status=running with
        /// qmpstatus=paused for a suspended VM.
        /// Locked: the guest config carries a `lock:` property (backup, clone, migrate,
        /// snapshot). This is not the /var/lock/qemu-server flock, which PVE does not expose
        /// through status/current or any other endpoint — see docs/decisions/ ADR 0015 and 0020.
        /// </returns>
        public static (bool StatusMatched, bool Locked) Evaluate(string json, string expectedStatus)
        {
            if (string.IsNullOrEmpty(json))
                return (false, false);

            var data = JObject.Parse(json)["data"];
            var status = data?["status"]?.ToString();
            var qmpStatus = data?["qmpstatus"]?.ToString();
            var effectiveStatus = qmpStatus ?? status;

            var matched = string.Equals(effectiveStatus, expectedStatus, StringComparison.OrdinalIgnoreCase);
            var locked = !string.IsNullOrEmpty(data?["lock"]?.ToString());

            return (matched, locked);
        }
    }
}
