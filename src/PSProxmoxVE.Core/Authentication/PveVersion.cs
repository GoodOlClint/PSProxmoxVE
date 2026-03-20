using System;

namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Represents a Proxmox VE server version (major.minor only).</summary>
    public class PveVersion
    {
        /// <summary>The major version number (e.g., 8 in PVE 8.1).</summary>
        public int Major { get; }

        /// <summary>The minor version number (e.g., 1 in PVE 8.1).</summary>
        public int Minor { get; }

        /// <summary>Creates a new PVE version with the specified major and minor components.</summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        public PveVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        /// <summary>Parse version from PVE API format "major.minor-patchlevel" e.g. "9.1-1"</summary>
        public static PveVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                throw new ArgumentException("Version string cannot be null or empty.", nameof(versionString));

            // Strip patchlevel: "9.1-1" -> "9.1"
            var dashIndex = versionString.IndexOf('-');
            var majorMinor = dashIndex >= 0 ? versionString.Substring(0, dashIndex) : versionString;

            var parts = majorMinor.Split('.');
            if (parts.Length < 2)
                throw new FormatException($"Invalid PVE version format: '{versionString}'. Expected 'major.minor' or 'major.minor-patchlevel'.");

            if (!int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
                throw new FormatException($"Invalid PVE version format: '{versionString}'. Major and minor must be integers.");

            return new PveVersion(major, minor);
        }

        /// <summary>Returns true if this version is greater than or equal to the specified version.</summary>
        /// <param name="major">The minimum major version.</param>
        /// <param name="minor">The minimum minor version.</param>
        public bool IsAtLeast(int major, int minor)
        {
            return Major > major || (Major == major && Minor >= minor);
        }

        /// <inheritdoc />
        public override string ToString() => $"{Major}.{Minor}";
    }
}
