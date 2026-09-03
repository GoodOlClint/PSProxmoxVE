using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Reads OVA archives and OVF descriptors into <see cref="OvfMetadata"/>.
    /// </summary>
    public static class OvfReader
    {
        private const string OvfNs = "http://schemas.dmtf.org/ovf/envelope/1";
        private const string RasdNs = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_ResourceAllocationSettingData";
        private const string VssdNs = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_VirtualSystemSettingData";

        // ovf:href is used as a path segment in a PVE property string (import-from=storage:import/ova/href);
        // PVE property strings are comma-separated, so ',' and any path separator must be rejected.
        // \A/\z (not ^/$) so a trailing newline cannot sneak past the anchor under .NET's default regex options.
        private static readonly Regex ValidHrefPattern = new Regex(@"\A[A-Za-z0-9._-]+\z", RegexOptions.Compiled);

        /// <summary>
        /// Parses an OVA file (TAR archive) and extracts OVF metadata.
        /// </summary>
        /// <param name="ovaPath">Path to the OVA file.</param>
        /// <returns>Parsed OVF metadata.</returns>
        public static OvfMetadata ReadOva(string ovaPath)
        {
            if (string.IsNullOrWhiteSpace(ovaPath))
                throw new ArgumentException("OVA path must not be null or empty.", nameof(ovaPath));
            if (!File.Exists(ovaPath))
                throw new FileNotFoundException("OVA file not found.", ovaPath);

            var ovfXml = ExtractOvfFromTar(ovaPath);
            if (ovfXml == null)
                throw new InvalidOperationException("No .ovf file found inside the OVA archive.");

            return ParseOvf(ovfXml);
        }

        /// <summary>
        /// Extracts the .ovf XML content from a TAR archive using SharpCompress.
        /// </summary>
        private static string? ExtractOvfFromTar(string tarPath)
        {
            using var stream = File.OpenRead(tarPath);
            using var reader = SharpCompress.Readers.ReaderFactory.OpenReader(stream, new SharpCompress.Readers.ReaderOptions());

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory &&
                    reader.Entry.Key != null &&
                    reader.Entry.Key.EndsWith(".ovf", StringComparison.OrdinalIgnoreCase))
                {
                    using var entryStream = reader.OpenEntryStream();
                    using var sr = new StreamReader(entryStream, Encoding.UTF8);
                    return sr.ReadToEnd();
                }
            }

            return null;
        }

        /// <summary>
        /// Parses OVF XML and extracts VM metadata.
        /// </summary>
        internal static OvfMetadata ParseOvf(string xml)
        {
            var doc = new XmlDocument();
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using (var stringReader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(stringReader, readerSettings))
            {
                doc.Load(xmlReader);
            }

            var nsm = new XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("ovf", OvfNs);
            nsm.AddNamespace("rasd", RasdNs);
            nsm.AddNamespace("vssd", VssdNs);

            var metadata = new OvfMetadata();

            // Extract VM name from VirtualSystem
            var vsNode = doc.SelectSingleNode("//ovf:VirtualSystem", nsm);
            if (vsNode != null)
            {
                // Try ovf:id attribute first, then Name element
                var idAttr = vsNode.Attributes?["ovf:id"];
                if (idAttr != null && !string.IsNullOrEmpty(idAttr.Value))
                {
                    metadata.Name = idAttr.Value;
                }
            }

            // Try VirtualSystemIdentifier from VirtualSystemSettingData
            var vsId = doc.SelectSingleNode("//vssd:VirtualSystemIdentifier", nsm);
            if (vsId != null && !string.IsNullOrEmpty(vsId.InnerText))
            {
                metadata.Name = vsId.InnerText;
            }

            // Extract OS type hint from OperatingSystemSection
            var osSection = doc.SelectSingleNode("//ovf:OperatingSystemSection", nsm);
            if (osSection != null)
            {
                var osTypeAttr = osSection.Attributes?["ovf:id"];
                var description = osSection.SelectSingleNode("ovf:Description", nsm);
                var osDesc = description?.InnerText ?? osTypeAttr?.Value ?? string.Empty;
                metadata.OsTypeHint = MapOsType(osDesc);
            }

            // Build file reference map: fileRef -> fileName
            var fileRefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fileNodes = doc.SelectNodes("//ovf:References/ovf:File", nsm);
            if (fileNodes != null)
            {
                foreach (XmlNode fileNode in fileNodes)
                {
                    var id = fileNode.Attributes?["ovf:id"]?.Value;
                    var href = fileNode.Attributes?["ovf:href"]?.Value;
                    if (id != null && href != null)
                    {
                        if (!ValidHrefPattern.IsMatch(href) || href == "." || href == "..")
                            throw new InvalidDataException($"OVF descriptor references a disallowed file name: '{href}'.");
                        fileRefs[id] = href;
                    }
                }
            }

            // Build disk reference map: diskId -> fileRef
            var diskFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var diskNodes = doc.SelectNodes("//ovf:DiskSection/ovf:Disk", nsm);
            if (diskNodes != null)
            {
                foreach (XmlNode diskNode in diskNodes)
                {
                    var diskId = diskNode.Attributes?["ovf:diskId"]?.Value;
                    var fileRef = diskNode.Attributes?["ovf:fileRef"]?.Value;
                    if (diskId != null && fileRef != null)
                    {
                        diskFileMap[diskId] = fileRef;
                    }
                }
            }

            // Parse hardware items
            var items = doc.SelectNodes("//ovf:VirtualHardwareSection/ovf:Item", nsm);
            if (items != null)
            {
                foreach (XmlNode item in items)
                {
                    var resourceTypeNode = item.SelectSingleNode("rasd:ResourceType", nsm);
                    if (resourceTypeNode == null) continue;

                    if (!int.TryParse(resourceTypeNode.InnerText.Trim(), out int resourceType))
                        continue;

                    switch (resourceType)
                    {
                        case 3: // Processor
                            var vcpuNode = item.SelectSingleNode("rasd:VirtualQuantity", nsm);
                            if (vcpuNode != null && int.TryParse(vcpuNode.InnerText.Trim(), out int vcpus))
                                metadata.CpuCount = vcpus;
                            break;

                        case 4: // Memory
                            var memNode = item.SelectSingleNode("rasd:VirtualQuantity", nsm);
                            var unitsNode = item.SelectSingleNode("rasd:AllocationUnits", nsm);
                            if (memNode != null && long.TryParse(memNode.InnerText.Trim(), out long memVal))
                            {
                                var units = unitsNode?.InnerText?.Trim() ?? "byte * 2^20";
                                metadata.MemoryMB = ConvertToMB(memVal, units);
                            }
                            break;

                        case 5:  // IDE Controller
                        case 6:  // Parallel SCSI HBA
                        case 20: // Other storage device (VMware's SATA AHCI controller)
                            // Controllers themselves don't produce disk entries; skip.
                            break;

                        case 17: // Disk Drive
                            var diskRef = ExtractDiskReference(item, nsm, diskFileMap, fileRefs);
                            if (diskRef != null)
                            {
                                // Determine bus type from parent controller
                                diskRef.BusType = DetermineBusType(item, items, nsm);
                                metadata.Disks.Add(diskRef);
                            }
                            break;

                        case 10: // Ethernet Adapter
                            var adapterName = item.SelectSingleNode("rasd:ElementName", nsm)?.InnerText
                                ?? item.SelectSingleNode("rasd:Caption", nsm)?.InnerText
                                ?? "Network adapter";
                            var connection = item.SelectSingleNode("rasd:Connection", nsm)?.InnerText ?? string.Empty;
                            var nicSubType = item.SelectSingleNode("rasd:ResourceSubType", nsm)?.InnerText ?? string.Empty;
                            metadata.NetworkAdapters.Add(new OvfNetworkAdapter
                            {
                                AdapterName = adapterName,
                                ConnectionName = connection,
                                ResourceSubType = nicSubType
                            });
                            break;
                    }
                }
            }

            return metadata;
        }

        private static OvfDiskReference? ExtractDiskReference(
            XmlNode item,
            XmlNamespaceManager nsm,
            Dictionary<string, string> diskFileMap,
            Dictionary<string, string> fileRefs)
        {
            // The HostResource element typically contains a reference like "ovf:/disk/vmdisk1"
            var hostResource = item.SelectSingleNode("rasd:HostResource", nsm)?.InnerText ?? string.Empty;
            string? diskId = null;

            if (hostResource.Contains("/disk/"))
            {
                var idx = hostResource.LastIndexOf("/disk/", StringComparison.Ordinal);
                diskId = hostResource.Substring(idx + 6);
            }
            else if (hostResource.Contains("disk/"))
            {
                var idx = hostResource.LastIndexOf("disk/", StringComparison.Ordinal);
                diskId = hostResource.Substring(idx + 5);
            }

            if (diskId != null && diskFileMap.TryGetValue(diskId, out var fileRef) && fileRefs.TryGetValue(fileRef, out var fileName))
            {
                return new OvfDiskReference { FileName = fileName };
            }

            return null;
        }

        private static string DetermineBusType(XmlNode diskItem, XmlNodeList allItems, XmlNamespaceManager nsm)
        {
            // Look at the Parent element to find which controller this disk is attached to
            var parentNode = diskItem.SelectSingleNode("rasd:Parent", nsm);
            if (parentNode == null)
                return "scsi"; // default

            var parentId = parentNode.InnerText.Trim();

            foreach (XmlNode item in allItems)
            {
                var instanceId = item.SelectSingleNode("rasd:InstanceID", nsm)?.InnerText?.Trim();
                if (instanceId != parentId) continue;

                var resourceTypeNode = item.SelectSingleNode("rasd:ResourceType", nsm);
                if (resourceTypeNode == null) continue;

                if (int.TryParse(resourceTypeNode.InnerText.Trim(), out int rt))
                {
                    switch (rt)
                    {
                        case 5:  return "ide";
                        case 6:  return "scsi";
                        case 20: return "sata";
                    }
                }
                break;
            }

            return "scsi"; // default fallback
        }

        private static int ConvertToMB(long value, string allocationUnits)
        {
            // Common OVF allocation units:
            // "byte * 2^20" = MiB
            // "byte * 2^30" = GiB
            // "byte * 2^10" = KiB
            // "MegaBytes" or "MB"
            var lower = allocationUnits.ToLowerInvariant();

            if (lower.Contains("2^30") || lower.Contains("gib") || lower.Contains("gigabyte"))
                return (int)(value * 1024);
            if (lower.Contains("2^20") || lower.Contains("mib") || lower.Contains("megabyte") || lower.Contains("mb"))
                return (int)value;
            if (lower.Contains("2^10") || lower.Contains("kib") || lower.Contains("kilobyte") || lower.Contains("kb"))
                return (int)(value / 1024);
            if (lower.Contains("byte"))
                return (int)(value / (1024 * 1024));

            // Default: assume MiB
            return (int)value;
        }

        private static string MapOsType(string osDescription)
        {
            if (string.IsNullOrEmpty(osDescription))
                return "other";

            var lower = osDescription.ToLowerInvariant();

            // Windows variants
            if (lower.Contains("windows 11") || lower.Contains("win11"))
                return "win11";
            if (lower.Contains("windows 10") || lower.Contains("win10"))
                return "win10";
            if (lower.Contains("windows server 2022") || lower.Contains("2022"))
                return "win11";
            if (lower.Contains("windows server 2019") || lower.Contains("2019"))
                return "win10";
            if (lower.Contains("windows server 2016") || lower.Contains("2016"))
                return "win10";
            if (lower.Contains("windows 8") || lower.Contains("win8"))
                return "win8";
            if (lower.Contains("windows 7") || lower.Contains("win7"))
                return "win7";
            if (lower.Contains("windows"))
                return "win10";

            // Linux variants
            if (lower.Contains("linux") || lower.Contains("ubuntu") || lower.Contains("debian") ||
                lower.Contains("centos") || lower.Contains("rhel") || lower.Contains("red hat") ||
                lower.Contains("fedora") || lower.Contains("suse") || lower.Contains("alma") ||
                lower.Contains("rocky"))
                return "l26";

            // FreeBSD
            if (lower.Contains("freebsd"))
                return "l26";

            // Solaris
            if (lower.Contains("solaris"))
                return "solaris";

            // Try numeric OVF OS ID
            if (int.TryParse(osDescription, out int osId))
            {
                // Common CIM OS IDs
                if (osId >= 56 && osId <= 70) return "win10";   // Various Windows
                if (osId >= 93 && osId <= 113) return "l26";    // Various Linux
                if (osId == 36) return "l26";                    // Linux
                if (osId == 101 || osId == 106) return "l26";   // Linux 64-bit
            }

            return "other";
        }
    }
}
