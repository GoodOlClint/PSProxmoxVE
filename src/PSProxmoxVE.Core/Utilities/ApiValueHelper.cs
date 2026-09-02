using System;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Helper methods for normalizing and checking values received from the Proxmox VE API.
    /// The API may return the same logical value in different formats (e.g., boolean true, integer 1, or string "1").
    /// </summary>
    public static class ApiValueHelper
    {
        /// <summary>
        /// Determines if a value represents a true/exited state.
        /// Accepts boolean true, integer 1 (as Int64 or Int32), and string "1" as true.
        /// All other values (false, 0, "0", null, etc.) are false.
        /// </summary>
        /// <param name="value">The value to check, typically from API response data.</param>
        /// <returns>True if the value represents an exited/true state, false otherwise.</returns>
        public static bool IsExited(object? value)
        {
            if (value == null)
                return false;

            if (value is bool b)
                return b;

            if (value is long l)
                return l == 1L;

            if (value is int i)
                return i == 1;

            if (value is string s)
                return s == "1";

            return false;
        }
    }
}
