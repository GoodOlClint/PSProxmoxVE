using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

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

        // OVF namespace constants
        private const string OvfNs = "http://schemas.dmtf.org/ovf/envelope/1";
        private const string RasdNs = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_ResourceAllocationSettingData";
        private const string VssdNs = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_VirtualSystemSettingData";

        /// <summary>
        /// Parses an OVA file (TAR archive) and extracts OVF metadata.
        /// </summary>
        /// <param name="ovaPath">Path to the OVA file.</param>
        /// <returns>Parsed OVF metadata.</returns>
        public static OvfMetadata FromOva(string ovaPath)
        {
            if (string.IsNullOrWhiteSpace(ovaPath))
                throw new ArgumentException("OVA path must not be null or empty.", nameof(ovaPath));
            if (!File.Exists(ovaPath))
                throw new FileNotFoundException("OVA file not found.", ovaPath);

            var ovfXml = ExtractOvfFromTar(ovaPath);
            if (ovfXml == null)
                throw new InvalidOperationException("No .ovf file found inside the OVA archive.");

            return ParseOvfXml(ovfXml);
        }

        /// <summary>
        /// Extracts the .ovf XML content from a TAR archive.
        /// Uses System.Formats.Tar on .NET 9+ and manual TAR parsing on older frameworks.
        /// </summary>
        private static string? ExtractOvfFromTar(string tarPath)
        {
#if NET7_0_OR_GREATER
            return ExtractOvfUsingSystemTar(tarPath);
#else
            return ExtractOvfManualTar(tarPath);
#endif
        }

#if NET7_0_OR_GREATER
        private static string? ExtractOvfUsingSystemTar(string tarPath)
        {
            using var stream = File.OpenRead(tarPath);
            using var reader = new System.Formats.Tar.TarReader(stream);

            System.Formats.Tar.TarEntry? entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                if (entry.Name.EndsWith(".ovf", StringComparison.OrdinalIgnoreCase) && entry.DataStream != null)
                {
                    using var sr = new StreamReader(entry.DataStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                    return sr.ReadToEnd();
                }
            }

            return null;
        }
#else
        /// <summary>
        /// Minimal TAR reader for netstandard2.0/net48.
        /// TAR format: 512-byte header per file, name at offset 0 (100 bytes), size at offset 124 (12 bytes octal),
        /// followed by file data padded to 512-byte boundary.
        /// </summary>
        private static string? ExtractOvfManualTar(string tarPath)
        {
            using var stream = File.OpenRead(tarPath);
            var header = new byte[512];

            while (true)
            {
                var bytesRead = ReadFull(stream, header, 0, 512);
                if (bytesRead < 512)
                    break;

                // Check for end-of-archive (two zero blocks)
                if (IsZeroBlock(header))
                    break;

                // Extract name (offset 0, 100 bytes)
                var name = ReadNullTerminatedString(header, 0, 100);

                // Extract size (offset 124, 12 bytes, octal)
                var sizeStr = ReadNullTerminatedString(header, 124, 12).Trim();
                long size = 0;
                if (!string.IsNullOrEmpty(sizeStr))
                {
                    try { size = Convert.ToInt64(sizeStr, 8); }
                    catch { size = 0; }
                }

                // Check for GNU/POSIX long name prefix (offset 345, 155 bytes)
                var prefix = ReadNullTerminatedString(header, 345, 155);
                if (!string.IsNullOrEmpty(prefix))
                    name = prefix + "/" + name;

                if (name.EndsWith(".ovf", StringComparison.OrdinalIgnoreCase) && size > 0)
                {
                    var data = new byte[size];
                    var dataRead = ReadFull(stream, data, 0, (int)size);
                    if (dataRead < size)
                        throw new InvalidOperationException("Unexpected end of OVA archive while reading .ovf entry.");
                    return Encoding.UTF8.GetString(data);
                }

                // Skip past the file data (padded to 512-byte boundary)
                if (size > 0)
                {
                    var paddedSize = ((size + 511) / 512) * 512;
                    SkipBytes(stream, paddedSize);
                }
            }

            return null;
        }

        private static int ReadFull(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        private static void SkipBytes(Stream stream, long count)
        {
            if (stream.CanSeek)
            {
                stream.Seek(count, SeekOrigin.Current);
            }
            else
            {
                var buf = new byte[Math.Min(count, 8192)];
                long remaining = count;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(remaining, buf.Length);
                    int read = stream.Read(buf, 0, toRead);
                    if (read == 0) break;
                    remaining -= read;
                }
            }
        }

        private static bool IsZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] != 0) return false;
            }
            return true;
        }

        private static string ReadNullTerminatedString(byte[] buffer, int offset, int maxLength)
        {
            int end = offset;
            int limit = offset + maxLength;
            while (end < limit && buffer[end] != 0)
                end++;
            return Encoding.ASCII.GetString(buffer, offset, end - offset);
        }
#endif

        /// <summary>
        /// Parses OVF XML and extracts VM metadata.
        /// </summary>
        private static OvfMetadata ParseOvfXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

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

                        case 6:  // Parallel SCSI HBA (sometimes used as SATA controller)
                        case 5:  // IDE Controller
                        case 20: // SCSI/SAS controller (storage)
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
                            metadata.NetworkAdapters.Add(new OvfNetworkAdapter
                            {
                                AdapterName = adapterName,
                                ConnectionName = connection
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
                        case 6:  return "sata";
                        case 20: return "scsi";
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
