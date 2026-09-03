using System.IO;
using System.Text;
using System.Xml;
using Xunit;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class OvfReaderTests
    {
        private const string OvfHeader =
            "<Envelope xmlns=\"http://schemas.dmtf.org/ovf/envelope/1\" " +
            "xmlns:ovf=\"http://schemas.dmtf.org/ovf/envelope/1\" " +
            "xmlns:rasd=\"http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_ResourceAllocationSettingData\" " +
            "xmlns:vssd=\"http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_VirtualSystemSettingData\">";

        private static string BuildDescriptor(string href, int controllerResourceType)
        {
            return
                OvfHeader +
                "<References><File ovf:id=\"file1\" ovf:href=\"" + href + "\"/></References>" +
                "<DiskSection><Disk ovf:diskId=\"vmdisk1\" ovf:fileRef=\"file1\"/></DiskSection>" +
                "<VirtualSystem ovf:id=\"test-vm\">" +
                "<VirtualHardwareSection>" +
                "<Item>" +
                "<rasd:InstanceID>1</rasd:InstanceID>" +
                "<rasd:ResourceType>" + controllerResourceType + "</rasd:ResourceType>" +
                "</Item>" +
                "<Item>" +
                "<rasd:InstanceID>2</rasd:InstanceID>" +
                "<rasd:ResourceType>17</rasd:ResourceType>" +
                "<rasd:Parent>1</rasd:Parent>" +
                "<rasd:HostResource>ovf:/disk/vmdisk1</rasd:HostResource>" +
                "</Item>" +
                "</VirtualHardwareSection>" +
                "</VirtualSystem>" +
                "</Envelope>";
        }

        // Minimal POSIX ustar header: 512-byte fixed-size header fields,
        // followed by the file content padded to a 512-byte boundary,
        // followed by two all-zero 512-byte end-of-archive blocks.
        private static byte[] BuildTarWithEntry(string entryName, byte[] content)
        {
            using var ms = new MemoryStream();
            var header = new byte[512];
            var nameBytes = Encoding.ASCII.GetBytes(entryName);
            System.Array.Copy(nameBytes, header, nameBytes.Length);
            WriteOctalField(header, 100, 8, 0x1A4); // mode 0644
            WriteOctalField(header, 108, 8, 0);      // uid
            WriteOctalField(header, 116, 8, 0);      // gid
            WriteOctalField(header, 124, 12, content.Length);
            WriteOctalField(header, 136, 12, 0);     // mtime
            for (int i = 148; i < 156; i++) header[i] = (byte)' ';
            header[156] = (byte)'0'; // regular file
            var magic = Encoding.ASCII.GetBytes("ustar\0");
            System.Array.Copy(magic, 0, header, 257, magic.Length);
            header[263] = (byte)'0';
            header[264] = (byte)'0';

            int checksum = 0;
            foreach (var b in header) checksum += b;
            var chkBytes = Encoding.ASCII.GetBytes(System.Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ");
            System.Array.Copy(chkBytes, 0, header, 148, chkBytes.Length);

            ms.Write(header, 0, header.Length);
            ms.Write(content, 0, content.Length);
            var pad = (512 - content.Length % 512) % 512;
            if (pad > 0) ms.Write(new byte[pad], 0, pad);
            ms.Write(new byte[1024], 0, 1024); // two end-of-archive zero blocks

            return ms.ToArray();
        }

        private static void WriteOctalField(byte[] header, int offset, int length, long value)
        {
            var octal = System.Convert.ToString(value, 8);
            if (value < 0 || octal.Length > length - 1)
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Does not fit the octal field.");
            var bytes = Encoding.ASCII.GetBytes(octal.PadLeft(length - 1, '0'));
            System.Array.Copy(bytes, 0, header, offset, bytes.Length);
            header[offset + length - 1] = 0;
        }

        [Fact]
        public void ReadOva_TarWithOvfEntry_ParsesMetadata()
        {
            var xml = BuildDescriptor("disk1.vmdk", 6);
            var tarBytes = BuildTarWithEntry("appliance.ovf", Encoding.UTF8.GetBytes(xml));

            var tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempPath, tarBytes);

                var metadata = OvfReader.ReadOva(tempPath);

                Assert.Equal("test-vm", metadata.Name);
                Assert.Single(metadata.Disks);
                Assert.Equal("disk1.vmdk", metadata.Disks[0].FileName);
                Assert.Equal("scsi", metadata.Disks[0].BusType);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void ReadOva_TarWithNoOvfEntry_Throws()
        {
            var tarBytes = BuildTarWithEntry("readme.txt", Encoding.UTF8.GetBytes("not an ovf"));

            var tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempPath, tarBytes);

                Assert.Throws<System.InvalidOperationException>(() => OvfReader.ReadOva(tempPath));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void ParseOvf_VMwareScsiController_ResourceType6_MapsToScsi()
        {
            var xml = BuildDescriptor("disk1.vmdk", 6);
            var metadata = OvfReader.ParseOvf(xml);

            Assert.Single(metadata.Disks);
            Assert.Equal("scsi", metadata.Disks[0].BusType);
        }

        [Fact]
        public void ParseOvf_VMwareSataController_ResourceType20_MapsToSata()
        {
            var xml = BuildDescriptor("disk1.vmdk", 20);
            var metadata = OvfReader.ParseOvf(xml);

            Assert.Single(metadata.Disks);
            Assert.Equal("sata", metadata.Disks[0].BusType);
        }

        [Fact]
        public void ParseOvf_HrefWithEmbeddedProperty_Throws()
        {
            var xml = BuildDescriptor("d.vmdk,cache=unsafe", 6);

            Assert.Throws<InvalidDataException>(() => OvfReader.ParseOvf(xml));
        }

        [Fact]
        public void ParseOvf_HrefWithPathTraversal_Throws()
        {
            var xml = BuildDescriptor("../x.vmdk", 6);

            Assert.Throws<InvalidDataException>(() => OvfReader.ParseOvf(xml));
        }

        [Fact]
        public void ParseOvf_HrefIsDotDot_Throws()
        {
            var xml = BuildDescriptor("..", 6);

            Assert.Throws<InvalidDataException>(() => OvfReader.ParseOvf(xml));
        }

        [Fact]
        public void ParseOvf_HrefIsDot_Throws()
        {
            var xml = BuildDescriptor(".", 6);

            Assert.Throws<InvalidDataException>(() => OvfReader.ParseOvf(xml));
        }

        [Fact]
        public void ParseOvf_DescriptorWithInternalEntity_Throws()
        {
            var xml =
                "<?xml version=\"1.0\"?>" +
                "<!DOCTYPE Envelope [<!ENTITY boom \"boom\">]>" +
                OvfHeader +
                "<VirtualSystem ovf:id=\"&boom;\"/>" +
                "</Envelope>";

            Assert.ThrowsAny<XmlException>(() => OvfReader.ParseOvf(xml));
        }
    }
}
