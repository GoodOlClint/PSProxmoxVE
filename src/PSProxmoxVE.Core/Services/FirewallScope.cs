using System;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Validates the Level/Node/VmId/Group combination shared by the firewall cmdlets.
    /// </summary>
    public static class FirewallScope
    {
        /// <summary>
        /// Returns <c>false</c> and sets <paramref name="errorId"/>/<paramref name="message"/> to the
        /// first violated rule (Node, then VmId, then Group) when the identifiers required for
        /// <paramref name="level"/> are missing; otherwise returns <c>true</c>.
        /// </summary>
        public static bool TryValidate(string level, string? node, int? vmid, string? group,
            out string errorId, out string message)
        {
            var isCluster = string.Equals(level, "Cluster", StringComparison.OrdinalIgnoreCase);
            var isGroup = string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase);
            var isVmOrContainer = string.Equals(level, "Vm", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(level, "Container", StringComparison.OrdinalIgnoreCase);

            if (!isCluster && !isGroup && string.IsNullOrEmpty(node))
            {
                errorId = "NodeRequired";
                message = "Node is required when Level is not Cluster.";
                return false;
            }

            if (isVmOrContainer && !vmid.HasValue)
            {
                errorId = "VmIdRequired";
                message = "VmId is required when Level is Vm or Container.";
                return false;
            }

            if (isGroup && string.IsNullOrWhiteSpace(group))
            {
                errorId = "GroupRequired";
                message = "Group is required when Level is Group.";
                return false;
            }

            errorId = string.Empty;
            message = string.Empty;
            return true;
        }
    }
}
