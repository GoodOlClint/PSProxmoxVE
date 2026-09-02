using System.IO;
using System.Xml;
using Xunit;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class OvfMetadataTests
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

        [Fact]
        public void ParseOvfXml_VMwareScsiController_ResourceType6_MapsToScsi()
        {
            var xml = BuildDescriptor("disk1.vmdk", 6);
            var metadata = OvfMetadata.ParseOvfXml(xml);

            Assert.Single(metadata.Disks);
            Assert.Equal("scsi", metadata.Disks[0].BusType);
        }

        [Fact]
        public void ParseOvfXml_VMwareSataController_ResourceType20_MapsToSata()
        {
            var xml = BuildDescriptor("disk1.vmdk", 20);
            var metadata = OvfMetadata.ParseOvfXml(xml);

            Assert.Single(metadata.Disks);
            Assert.Equal("sata", metadata.Disks[0].BusType);
        }

        [Fact]
        public void ParseOvfXml_HrefWithEmbeddedProperty_Throws()
        {
            var xml = BuildDescriptor("d.vmdk,cache=unsafe", 6);

            Assert.Throws<InvalidDataException>(() => OvfMetadata.ParseOvfXml(xml));
        }

        [Fact]
        public void ParseOvfXml_HrefWithPathTraversal_Throws()
        {
            var xml = BuildDescriptor("../x.vmdk", 6);

            Assert.Throws<InvalidDataException>(() => OvfMetadata.ParseOvfXml(xml));
        }

        [Fact]
        public void ParseOvfXml_HrefIsDotDot_Throws()
        {
            var xml = BuildDescriptor("..", 6);

            Assert.Throws<InvalidDataException>(() => OvfMetadata.ParseOvfXml(xml));
        }

        [Fact]
        public void ParseOvfXml_HrefIsDot_Throws()
        {
            var xml = BuildDescriptor(".", 6);

            Assert.Throws<InvalidDataException>(() => OvfMetadata.ParseOvfXml(xml));
        }

        [Fact]
        public void ParseOvfXml_DescriptorWithInternalEntity_Throws()
        {
            var xml =
                "<?xml version=\"1.0\"?>" +
                "<!DOCTYPE Envelope [<!ENTITY boom \"boom\">]>" +
                OvfHeader +
                "<VirtualSystem ovf:id=\"&boom;\"/>" +
                "</Envelope>";

            Assert.ThrowsAny<XmlException>(() => OvfMetadata.ParseOvfXml(xml));
        }
    }
}
