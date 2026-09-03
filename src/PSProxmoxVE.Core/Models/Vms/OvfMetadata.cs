using System.Collections.Generic;

namespace PSProxmoxVE.Core.Models.Vms
{
    /// <summary>
    /// Represents a disk reference extracted from an OVF descriptor.
    /// </summary>
    public class OvfDiskReference
    {
        /// <summary>The bus type hint (ide, scsi, sata).</summary>
        public string BusType { get; set; } = "scsi";

        /// <summary>The VMDK filename within the OVA archive.</summary>
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a network adapter extracted from an OVF descriptor.
    /// </summary>
    public class OvfNetworkAdapter
    {
        /// <summary>The adapter name (e.g. "Network adapter 1").</summary>
        public string AdapterName { get; set; } = string.Empty;

        /// <summary>The connection name (e.g. "VM Network", "bridged").</summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>The OVF ResourceSubType (e.g. "E1000", "vmxnet3", "VirtualE1000e").</summary>
        public string ResourceSubType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Metadata extracted from an OVF descriptor inside an OVA archive.
    /// </summary>
    public class OvfMetadata
    {
        /// <summary>The VM name from the OVF.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Number of CPU cores.</summary>
        public int CpuCount { get; set; } = 1;

        /// <summary>Memory in MB.</summary>
        public int MemoryMB { get; set; } = 1024;

        /// <summary>Disk references found in the OVF.</summary>
        public List<OvfDiskReference> Disks { get; set; } = new List<OvfDiskReference>();

        /// <summary>Network adapters found in the OVF.</summary>
        public List<OvfNetworkAdapter> NetworkAdapters { get; set; } = new List<OvfNetworkAdapter>();

        /// <summary>OS type hint from the OVF OperatingSystemSection.</summary>
        public string OsTypeHint { get; set; } = string.Empty;

        /// <summary>
        /// Maps an OVF ResourceSubType for a NIC to a PVE network model string.
        /// </summary>
        public static string MapNicModel(string resourceSubType)
        {
            if (string.IsNullOrEmpty(resourceSubType))
                return "virtio";

            var lower = resourceSubType.ToLowerInvariant();

            if (lower.Contains("vmxnet3"))
                return "vmxnet3";
            if (lower.Contains("e1000e") || lower.Contains("virtuale1000e"))
                return "e1000e";
            if (lower.Contains("e1000") || lower.Contains("virtuale1000"))
                return "e1000";
            if (lower.Contains("vmxnet2") || lower.Contains("vmxnet"))
                return "vmxnet3"; // PVE doesn't support vmxnet2, use vmxnet3
            if (lower.Contains("pcnet"))
                return "e1000"; // pcnet not supported on PVE, fallback to e1000
            if (lower.Contains("virtio"))
                return "virtio";

            // Unknown model — default to e1000 (widely compatible, no driver install needed)
            return "e1000";
        }
    }
}
