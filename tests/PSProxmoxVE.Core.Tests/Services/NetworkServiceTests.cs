using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class NetworkServiceTests
    {
        private readonly Mock<IPveHttpClient> _mockClient;
        private readonly NetworkService _service;
        private readonly PveSession _session;

        private const string Node = "pve1";
        private const string Vnet = "vnet1";
        private const string ApplyUpid = "UPID:pve1:000ABC:00000001:5F1234AB:srvreload:root@pam:";

        public NetworkServiceTests()
        {
            _mockClient = new Mock<IPveHttpClient>();
            _service = new NetworkService(_mockClient.Object);
            _session = new PveSession(
                "pve.example.com",
                8006,
                skipCertificateCheck: true,
                apiToken: "root@pam!test=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        private sealed class CapturedRequest
        {
            public int Calls { get; set; }
            public string? Path { get; set; }
            public Dictionary<string, string>? Form { get; set; }
        }

        private static string UpidJson(string upid) => $@"{{""data"": ""{upid}""}}";

        private CapturedRequest CapturePost(string json)
        {
            var captured = new CapturedRequest();
            _mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>?>((path, form) =>
                {
                    captured.Calls++;
                    captured.Path = path;
                    captured.Form = form;
                })
                .ReturnsAsync(json);
            return captured;
        }

        private CapturedRequest CapturePut(string json)
        {
            var captured = new CapturedRequest();
            _mockClient.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>?>((path, form) =>
                {
                    captured.Calls++;
                    captured.Path = path;
                    captured.Form = form;
                })
                .ReturnsAsync(json);
            return captured;
        }

        // -----------------------------------------------------------------
        // GetNetworks
        // -----------------------------------------------------------------

        [Fact]
        public void GetNetworks_NoType_RequestsPlainPath()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": [{""iface"": ""vmbr0"", ""type"": ""bridge""}]}");

            var networks = _service.GetNetworks(_session, Node);

            Assert.Single(networks);
            Assert.Equal("vmbr0", networks[0].Iface);
            _mockClient.Verify(c => c.GetAsync($"nodes/{Node}/network"), Times.Once);
        }

        [Fact]
        public void GetNetworks_WithType_AppendsQueryString()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": []}");

            _service.GetNetworks(_session, Node, "bridge");

            _mockClient.Verify(c => c.GetAsync($"nodes/{Node}/network?type=bridge"), Times.Once);
        }

        [Fact]
        public void GetNetworks_EscapesNodeInPath()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": []}");

            _service.GetNetworks(_session, "pve node");

            _mockClient.Verify(c => c.GetAsync("nodes/pve%20node/network"), Times.Once);
        }

        [Fact]
        public void GetNetworks_EmptyData_ReturnsEmptyArray()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": []}");

            var networks = _service.GetNetworks(_session, Node);

            Assert.Empty(networks);
        }

        [Fact]
        public void GetNetworks_NullData_ReturnsEmptyArray()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": null}");

            var networks = _service.GetNetworks(_session, Node);

            Assert.Empty(networks);
        }

        [Fact]
        public void GetNetworks_EmptyTypeString_OmitsQueryString()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": []}");

            _service.GetNetworks(_session, Node, string.Empty);

            _mockClient.Verify(c => c.GetAsync($"nodes/{Node}/network"), Times.Once);
        }

        [Fact]
        public void GetNetworks_EscapesTypeInQueryString()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(@"{""data"": []}");

            _service.GetNetworks(_session, Node, "any bridge");

            _mockClient.Verify(c => c.GetAsync($"nodes/{Node}/network?type=any%20bridge"), Times.Once);
        }

        [Fact]
        public void GetNetworks_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.GetNetworks(null!, Node));
        }

        [Fact]
        public void GetNetworks_WhitespaceNode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("node", () => _service.GetNetworks(_session, "  "));
        }

        // -----------------------------------------------------------------
        // CreateNetwork
        // -----------------------------------------------------------------

        [Fact]
        public void CreateNetwork_RequiredFieldsOnly_SendsExactForm()
        {
            var captured = CapturePost(@"{""data"": {""iface"": ""vmbr1"", ""type"": ""bridge""}}");
            var config = new Dictionary<string, object> { ["iface"] = "vmbr1", ["type"] = "bridge" };

            var result = _service.CreateNetwork(_session, Node, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"nodes/{Node}/network", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("vmbr1", captured.Form!["iface"]);
            Assert.Equal("bridge", captured.Form["type"]);
            Assert.Equal(2, captured.Form.Count);
            Assert.Equal("vmbr1", result.Iface);
        }

        [Fact]
        public void CreateNetwork_NonStringConfigValues_AreStringified()
        {
            var captured = CapturePost(@"{""data"": {}}");
            var config = new Dictionary<string, object>
            {
                ["iface"] = "vmbr1",
                ["type"] = "bridge",
                ["mtu"] = 9000,
                ["comments"] = null!
            };

            _service.CreateNetwork(_session, Node, config);

            Assert.Equal("9000", captured.Form!["mtu"]);
            Assert.Equal(string.Empty, captured.Form["comments"]);
        }

        [Fact]
        public void CreateNetwork_AllFields_SendsExactForm()
        {
            var captured = CapturePost(@"{""data"": {""iface"": ""vmbr1"", ""type"": ""bridge""}}");
            var config = new Dictionary<string, object>
            {
                ["iface"] = "vmbr1",
                ["type"] = "bridge",
                ["address"] = "10.0.0.1",
                ["netmask"] = "255.255.255.0",
                ["gateway"] = "10.0.0.254",
                ["bridge_ports"] = "eth0",
                ["bridge_vlan_aware"] = "1",
                ["mtu"] = "9000",
                ["autostart"] = "1",
                ["comments"] = "test bridge"
            };

            _service.CreateNetwork(_session, Node, config);

            Assert.Equal(10, captured.Form!.Count);
            Assert.Equal("10.0.0.1", captured.Form["address"]);
            Assert.Equal("255.255.255.0", captured.Form["netmask"]);
            Assert.Equal("10.0.0.254", captured.Form["gateway"]);
            Assert.Equal("eth0", captured.Form["bridge_ports"]);
            Assert.Equal("1", captured.Form["bridge_vlan_aware"]);
            Assert.Equal("9000", captured.Form["mtu"]);
            Assert.Equal("1", captured.Form["autostart"]);
            Assert.Equal("test bridge", captured.Form["comments"]);
        }

        [Fact]
        public void CreateNetwork_EscapesNodeInPath()
        {
            var captured = CapturePost(@"{""data"": {}}");
            var config = new Dictionary<string, object> { ["iface"] = "vmbr1", ["type"] = "bridge" };

            _service.CreateNetwork(_session, "pve node", config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal("nodes/pve%20node/network", captured.Path);
        }

        [Fact]
        public void CreateNetwork_NullSession_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["iface"] = "vmbr1" };
            Assert.Throws<ArgumentNullException>("session", () => _service.CreateNetwork(null!, Node, config));
        }

        [Fact]
        public void CreateNetwork_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("config", () => _service.CreateNetwork(_session, Node, null!));
        }

        // -----------------------------------------------------------------
        // SetNetwork
        // -----------------------------------------------------------------

        [Fact]
        public void SetNetwork_SendsConfigAsFormAgainstIfacePath()
        {
            var captured = CapturePut(@"{""data"": null}");
            var config = new Dictionary<string, object> { ["type"] = "bridge" };

            _service.SetNetwork(_session, Node, "vmbr0", config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"nodes/{Node}/network/vmbr0", captured.Path);
            Assert.Equal("bridge", captured.Form!["type"]);
            Assert.Single(captured.Form);
        }

        [Fact]
        public void SetNetwork_DeleteKey_IsSentVerbatim()
        {
            var captured = CapturePut(@"{""data"": null}");
            var config = new Dictionary<string, object> { ["type"] = "bridge", ["delete"] = "bridge_vlan_aware" };

            _service.SetNetwork(_session, Node, "vmbr0", config);

            Assert.Equal("bridge_vlan_aware", captured.Form!["delete"]);
            Assert.False(captured.Form.ContainsKey("bridge_vlan_aware"));
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void SetNetwork_EscapesNodeAndIfaceInPath()
        {
            var captured = CapturePut(@"{""data"": null}");
            var config = new Dictionary<string, object> { ["type"] = "bridge" };

            _service.SetNetwork(_session, "pve node", "vmbr 0", config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal("nodes/pve%20node/network/vmbr%200", captured.Path);
        }

        [Fact]
        public void SetNetwork_NullSession_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["type"] = "bridge" };
            Assert.Throws<ArgumentNullException>("session", () => _service.SetNetwork(null!, Node, "vmbr0", config));
        }

        [Fact]
        public void SetNetwork_WhitespaceIface_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["type"] = "bridge" };
            Assert.Throws<ArgumentNullException>("iface", () => _service.SetNetwork(_session, Node, " ", config));
        }

        // -----------------------------------------------------------------
        // RemoveNetwork
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveNetwork_CallsDeleteAsync()
        {
            _mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");

            _service.RemoveNetwork(_session, Node, "vmbr1");

            _mockClient.Verify(c => c.DeleteAsync($"nodes/{Node}/network/vmbr1"), Times.Once);
            _mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveNetwork_EscapesNodeAndIfaceInPath()
        {
            _mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");

            _service.RemoveNetwork(_session, "pve node", "vmbr 1");

            _mockClient.Verify(c => c.DeleteAsync("nodes/pve%20node/network/vmbr%201"), Times.Once);
            _mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveNetwork_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.RemoveNetwork(null!, Node, "vmbr1"));
        }

        // -----------------------------------------------------------------
        // ApplyNetworkConfig
        // -----------------------------------------------------------------

        [Fact]
        public void ApplyNetworkConfig_CallsPutAsyncWithNoBody_ReturnsRunningTask()
        {
            _mockClient.Setup(c => c.PutAsync($"nodes/{Node}/network", null))
                .ReturnsAsync(UpidJson(ApplyUpid));

            var task = _service.ApplyNetworkConfig(_session, Node);

            Assert.Equal(ApplyUpid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
            _mockClient.Verify(c => c.PutAsync($"nodes/{Node}/network", null), Times.Once);
        }

        [Fact]
        public void ApplyNetworkConfig_EscapesNodeInPath()
        {
            _mockClient.Setup(c => c.PutAsync("nodes/pve%20node/network", null))
                .ReturnsAsync(UpidJson(ApplyUpid));

            _service.ApplyNetworkConfig(_session, "pve node");

            _mockClient.Verify(c => c.PutAsync("nodes/pve%20node/network", null), Times.Once);
        }

        [Fact]
        public void ApplyNetworkConfig_NullData_ReturnsEmptyUpidWithoutStatus()
        {
            _mockClient.Setup(c => c.PutAsync($"nodes/{Node}/network", null))
                .ReturnsAsync(@"{""data"": null}");

            var task = _service.ApplyNetworkConfig(_session, Node);

            Assert.Equal(string.Empty, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Null(task.Status);
        }

        [Fact]
        public void ApplyNetworkConfig_ObjectShapedData_KeepsServerStatusAndOverridesNode()
        {
            _mockClient.Setup(c => c.PutAsync($"nodes/{Node}/network", null))
                .ReturnsAsync($@"{{""data"": {{""upid"": ""{ApplyUpid}"", ""node"": ""other"", ""status"": ""stopped"", ""exitstatus"": ""OK""}}}}");

            var task = _service.ApplyNetworkConfig(_session, Node);

            Assert.Equal(ApplyUpid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
        }

        [Fact]
        public void ApplyNetworkConfig_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.ApplyNetworkConfig(null!, Node));
        }

        // -----------------------------------------------------------------
        // GetSdnZones / GetSdnVnets / GetSdnSubnets
        // -----------------------------------------------------------------

        [Fact]
        public void GetSdnZones_ReturnsZoneArray()
        {
            _mockClient.Setup(c => c.GetAsync("cluster/sdn/zones"))
                .ReturnsAsync(@"{""data"": [{""zone"": ""zone1"", ""type"": ""simple""}]}");

            var zones = _service.GetSdnZones(_session);

            Assert.Single(zones);
            Assert.Equal("zone1", zones[0].Zone);
            _mockClient.Verify(c => c.GetAsync("cluster/sdn/zones"), Times.Once);
        }

        [Fact]
        public void GetSdnZones_NullData_ReturnsEmptyArray()
        {
            _mockClient.Setup(c => c.GetAsync("cluster/sdn/zones")).ReturnsAsync(@"{""data"": null}");

            var zones = _service.GetSdnZones(_session);

            Assert.Empty(zones);
        }

        [Fact]
        public void GetSdnZones_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.GetSdnZones(null!));
        }

        [Fact]
        public void GetSdnVnets_ReturnsVnetArray()
        {
            _mockClient.Setup(c => c.GetAsync("cluster/sdn/vnets"))
                .ReturnsAsync(@"{""data"": [{""vnet"": ""vnet1"", ""zone"": ""zone1""}]}");

            var vnets = _service.GetSdnVnets(_session);

            Assert.Single(vnets);
            Assert.Equal("vnet1", vnets[0].Vnet);
            _mockClient.Verify(c => c.GetAsync("cluster/sdn/vnets"), Times.Once);
        }

        [Fact]
        public void GetSdnVnets_NullData_ReturnsEmptyArray()
        {
            _mockClient.Setup(c => c.GetAsync("cluster/sdn/vnets")).ReturnsAsync(@"{""data"": null}");

            var vnets = _service.GetSdnVnets(_session);

            Assert.Empty(vnets);
        }

        [Fact]
        public void GetSdnVnets_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.GetSdnVnets(null!));
        }

        [Fact]
        public void GetSdnSubnets_RequestsVnetSubnetsPath()
        {
            _mockClient.Setup(c => c.GetAsync($"cluster/sdn/vnets/{Vnet}/subnets"))
                .ReturnsAsync(@"{""data"": [{""subnet"": ""10.0.0.0/24""}]}");

            var subnets = _service.GetSdnSubnets(_session, Vnet);

            Assert.Single(subnets);
            Assert.Equal("10.0.0.0/24", subnets[0].Subnet);
            _mockClient.Verify(c => c.GetAsync($"cluster/sdn/vnets/{Vnet}/subnets"), Times.Once);
        }

        [Fact]
        public void GetSdnSubnets_EscapesVnetInPath()
        {
            _mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": []}");

            _service.GetSdnSubnets(_session, "vnet 1");

            _mockClient.Verify(c => c.GetAsync("cluster/sdn/vnets/vnet%201/subnets"), Times.Once);
        }

        [Fact]
        public void GetSdnSubnets_NullData_ReturnsEmptyArray()
        {
            _mockClient.Setup(c => c.GetAsync($"cluster/sdn/vnets/{Vnet}/subnets")).ReturnsAsync(@"{""data"": null}");

            var subnets = _service.GetSdnSubnets(_session, Vnet);

            Assert.Empty(subnets);
        }

        [Fact]
        public void GetSdnSubnets_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.GetSdnSubnets(null!, Vnet));
        }

        [Fact]
        public void GetSdnSubnets_WhitespaceVnet_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("vnet", () => _service.GetSdnSubnets(_session, " "));
        }

        // -----------------------------------------------------------------
        // CreateSdnZone
        // -----------------------------------------------------------------

        [Fact]
        public void CreateSdnZone_RequiredFieldsOnly_SendsExactForm()
        {
            var captured = CapturePost(@"{""data"": {""zone"": ""zone1"", ""type"": ""simple""}}");
            var config = new Dictionary<string, object> { ["zone"] = "zone1", ["type"] = "simple" };

            var result = _service.CreateSdnZone(_session, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal("cluster/sdn/zones", captured.Path);
            Assert.Equal(2, captured.Form!.Count);
            Assert.Equal("zone1", result.Zone);
        }

        [Fact]
        public void CreateSdnZone_NullSession_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["zone"] = "zone1" };
            Assert.Throws<ArgumentNullException>("session", () => _service.CreateSdnZone(null!, config));
        }

        // -----------------------------------------------------------------
        // CreateSdnVnet
        // -----------------------------------------------------------------

        [Fact]
        public void CreateSdnVnet_RequiredFieldsOnly_SendsExactForm()
        {
            var captured = CapturePost(@"{""data"": {""vnet"": ""vnet1"", ""zone"": ""zone1""}}");
            var config = new Dictionary<string, object> { ["vnet"] = "vnet1", ["zone"] = "zone1" };

            var result = _service.CreateSdnVnet(_session, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal("cluster/sdn/vnets", captured.Path);
            Assert.Equal(2, captured.Form!.Count);
            Assert.Equal("vnet1", result.Vnet);
        }

        [Fact]
        public void CreateSdnVnet_NullSession_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["vnet"] = "vnet1" };
            Assert.Throws<ArgumentNullException>("session", () => _service.CreateSdnVnet(null!, config));
        }

        // -----------------------------------------------------------------
        // CreateSdnSubnet
        // -----------------------------------------------------------------

        [Fact]
        public void CreateSdnSubnet_RequiredFieldsOnly_SendsExactForm()
        {
            var captured = CapturePost(@"{""data"": null}");
            var config = new Dictionary<string, object> { ["subnet"] = "10.0.0.0/24", ["type"] = "subnet" };

            _service.CreateSdnSubnet(_session, Vnet, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"cluster/sdn/vnets/{Vnet}/subnets", captured.Path);
            Assert.Equal("10.0.0.0/24", captured.Form!["subnet"]);
            Assert.Equal("subnet", captured.Form["type"]);
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void CreateSdnSubnet_EscapesVnetInPath()
        {
            var captured = CapturePost(@"{""data"": null}");
            var config = new Dictionary<string, object> { ["subnet"] = "10.0.0.0/24", ["type"] = "subnet" };

            _service.CreateSdnSubnet(_session, "vnet 1", config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal("cluster/sdn/vnets/vnet%201/subnets", captured.Path);
        }

        [Fact]
        public void CreateSdnSubnet_NullSession_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["subnet"] = "10.0.0.0/24" };
            Assert.Throws<ArgumentNullException>("session", () => _service.CreateSdnSubnet(null!, Vnet, config));
        }

        [Fact]
        public void CreateSdnSubnet_WhitespaceVnet_ThrowsArgumentNullException()
        {
            var config = new Dictionary<string, object> { ["subnet"] = "10.0.0.0/24" };
            Assert.Throws<ArgumentNullException>("vnet", () => _service.CreateSdnSubnet(_session, " ", config));
        }

        // -----------------------------------------------------------------
        // RemoveSdnSubnet
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveSdnSubnet_CallsDeleteAsyncAgainstVnetSubnetPath()
        {
            _mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");

            _service.RemoveSdnSubnet(_session, Vnet, "10.0.0.0/24");

            _mockClient.Verify(
                c => c.DeleteAsync($"cluster/sdn/vnets/{Vnet}/subnets/10.0.0.0%2F24"), Times.Once);
            _mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveSdnSubnet_EscapesVnetInPath()
        {
            _mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");

            _service.RemoveSdnSubnet(_session, "vnet 1", "10.0.0.0/24");

            _mockClient.Verify(
                c => c.DeleteAsync("cluster/sdn/vnets/vnet%201/subnets/10.0.0.0%2F24"), Times.Once);
        }

        [Fact]
        public void RemoveSdnSubnet_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("session", () => _service.RemoveSdnSubnet(null!, Vnet, "10.0.0.0/24"));
        }

        // -----------------------------------------------------------------
        // RemoveSdnZone / RemoveSdnVnet
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveSdnZone_EscapesPathTraversalInName()
        {
            _mockClient.Setup(c => c.DeleteAsync("cluster/sdn/zones/..%2Faccess%2Fusers%2Fx"))
                .ReturnsAsync(@"{""data"":null}");

            _service.RemoveSdnZone(_session, "../access/users/x");

            _mockClient.Verify(c => c.DeleteAsync("cluster/sdn/zones/..%2Faccess%2Fusers%2Fx"), Times.Once);
        }

        [Fact]
        public void RemoveSdnVnet_EscapesPathTraversalInName()
        {
            _mockClient.Setup(c => c.DeleteAsync("cluster/sdn/vnets/..%2Faccess%2Fusers%2Fx"))
                .ReturnsAsync(@"{""data"":null}");

            _service.RemoveSdnVnet(_session, "../access/users/x");

            _mockClient.Verify(c => c.DeleteAsync("cluster/sdn/vnets/..%2Faccess%2Fusers%2Fx"), Times.Once);
        }
    }
}
