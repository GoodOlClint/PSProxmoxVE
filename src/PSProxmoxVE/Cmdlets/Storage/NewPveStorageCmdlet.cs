using System.Collections.Generic;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Storage;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Creates a new storage definition in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new storage backend to the Proxmox VE cluster configuration.
    /// The storage will be available cluster-wide after creation.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveStorage", SupportsShouldProcess = true)]
    [OutputType(typeof(PveStorage))]
    public class NewPveStorageCmdlet : PveCmdletBase
    {
        /// <summary>The unique storage identifier/name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The storage type (e.g., "dir", "nfs", "lvm", "zfspool", "cephfs", "rbd").</summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateSet("dir", "nfs", "lvm", "lvmthin", "zfspool", "zfs", "cephfs", "rbd",
                     "iscsi", "iscsidirect", "glusterfs", "cifs", "pbs", IgnoreCase = true)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Comma-separated list of content types to support (e.g., "iso,vztmpl,backup").</summary>
        [Parameter(Mandatory = false)]
        public string? Content { get; set; }

        /// <summary>Base directory path (for "dir" type storages).</summary>
        [Parameter(Mandatory = false)]
        public string? Path { get; set; }

        /// <summary>NFS/CIFS server hostname or IP address.</summary>
        [Parameter(Mandatory = false)]
        public string? Server { get; set; }

        /// <summary>NFS export path or CIFS share name.</summary>
        [Parameter(Mandatory = false)]
        public string? Export { get; set; }

        /// <summary>LVM volume group name (for "lvm"/"lvmthin" types).</summary>
        [Parameter(Mandatory = false)]
        public string? VgName { get; set; }

        /// <summary>LVM thin pool name (for "lvmthin" type).</summary>
        [Parameter(Mandatory = false)]
        public string? ThinPool { get; set; }

        /// <summary>ZFS pool name (for "zfspool" type).</summary>
        [Parameter(Mandatory = false)]
        public string? Pool { get; set; }

        /// <summary>Ceph pool name (for "rbd"/"cephfs" types).</summary>
        [Parameter(Mandatory = false)]
        public string? CephPool { get; set; }

        /// <summary>Monitor list for Ceph storages (comma-separated host:port pairs).</summary>
        [Parameter(Mandatory = false)]
        public string? MonHost { get; set; }

        /// <summary>Whether this storage is shared across cluster nodes.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Shared { get; set; }

        /// <summary>Whether this storage is enabled. Defaults to enabled.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Disable { get; set; }

        /// <summary>Limit nodes that can access this storage (comma-separated node names).</summary>
        [Parameter(Mandatory = false)]
        public string? Nodes { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Storage, "Create PVE Storage"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var data = new Dictionary<string, string>
            {
                ["storage"] = Storage,
                ["type"]    = Type
            };

            if (!string.IsNullOrEmpty(Content))  data["content"]  = Content;
            if (!string.IsNullOrEmpty(Path))      data["path"]     = Path;
            if (!string.IsNullOrEmpty(Server))    data["server"]   = Server;
            if (!string.IsNullOrEmpty(Export))    data["export"]   = Export;
            if (!string.IsNullOrEmpty(VgName))    data["vgname"]   = VgName;
            if (!string.IsNullOrEmpty(ThinPool))  data["thinpool"] = ThinPool;
            if (!string.IsNullOrEmpty(Pool))      data["pool"]     = Pool;
            if (!string.IsNullOrEmpty(CephPool))  data["pool"]     = CephPool;
            if (!string.IsNullOrEmpty(MonHost))   data["monhost"]  = MonHost;
            if (!string.IsNullOrEmpty(Nodes))     data["nodes"]    = Nodes;
            if (Shared.IsPresent)  data["shared"]  = "1";
            if (Disable.IsPresent) data["disable"] = "1";

            client.PostAsync("storage", data).GetAwaiter().GetResult();

            // Return the storage object representing what was created
            var storage = new PveStorage
            {
                Storage = Storage,
                Type    = Type,
                Content = Content,
                Enabled = Disable.IsPresent ? 0 : 1,
                Shared  = Shared.IsPresent ? 1 : 0
            };
            WriteObject(storage);
        }
    }
}
