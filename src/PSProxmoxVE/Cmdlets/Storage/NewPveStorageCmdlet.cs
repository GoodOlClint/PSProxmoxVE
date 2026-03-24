using System;
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
    public sealed class NewPveStorageCmdlet : PveCmdletBase
    {
        /// <summary>The unique storage identifier/name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The storage pool name.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The storage type (e.g., "dir", "nfs", "lvm", "zfspool", "cephfs", "rbd").</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage type (e.g. dir, nfs, lvm, zfspool).")]
        [ValidateSet("dir", "nfs", "lvm", "lvmthin", "zfspool", "zfs", "cephfs", "rbd",
                     "iscsi", "iscsidirect", "glusterfs", "cifs", "pbs", IgnoreCase = true)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Comma-separated list of content types to support (e.g., "iso,vztmpl,backup").</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated content types to support.")]
        public string? Content { get; set; }

        /// <summary>Base directory path (for "dir" type storages).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Base directory path (for dir type).")]
        public string? Path { get; set; }

        /// <summary>NFS/CIFS server hostname or IP address.</summary>
        [Parameter(Mandatory = false, HelpMessage = "NFS/CIFS server hostname or IP.")]
        public string? Server { get; set; }

        /// <summary>NFS export path or CIFS share name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "NFS export path or CIFS share name.")]
        public string? Export { get; set; }

        /// <summary>LVM volume group name (for "lvm"/"lvmthin" types).</summary>
        [Parameter(Mandatory = false, HelpMessage = "LVM volume group name.")]
        public string? VgName { get; set; }

        /// <summary>LVM thin pool name (for "lvmthin" type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "LVM thin pool name.")]
        public string? ThinPool { get; set; }

        /// <summary>ZFS pool name (for "zfspool" type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "ZFS pool name.")]
        public string? Pool { get; set; }

        /// <summary>Ceph pool name (for "rbd"/"cephfs" types).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Ceph pool name.")]
        public string? CephPool { get; set; }

        /// <summary>Monitor list for Ceph storages (comma-separated host:port pairs).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Ceph monitor host list.")]
        public string? MonHost { get; set; }

        /// <summary>iSCSI target IQN (for "iscsi" type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "iSCSI target IQN (e.g. iqn.2024-01.com.example:storage).")]
        public string? Target { get; set; }

        /// <summary>iSCSI portal address (for "iscsi" type). Defaults to server:3260 if not specified.</summary>
        [Parameter(Mandatory = false, HelpMessage = "iSCSI portal address (host:port).")]
        public string? Portal { get; set; }

        /// <summary>Whether this storage is shared across cluster nodes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Storage is shared across cluster nodes.")]
        public SwitchParameter Shared { get; set; }

        /// <summary>Whether this storage is enabled. Defaults to enabled.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Create the storage in disabled state.")]
        public SwitchParameter Disable { get; set; }

        /// <summary>Limit nodes that can access this storage (comma-separated node names).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Limit access to these nodes (comma-separated).")]
        public string? Nodes { get; set; }

        private static void AddIfNotEmpty(Dictionary<string, string> data, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                data[key] = value!;
        }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Storage, "Create PVE Storage"))
                return;

            if (!string.IsNullOrEmpty(Pool) && !string.IsNullOrEmpty(CephPool))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException("Pool and CephPool cannot both be specified. Use Pool for ZFS pools or CephPool for Ceph/RBD pools."),
                    "PoolConflict", ErrorCategory.InvalidArgument, null));
                return;
            }

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Creating storage '{Storage}'...");
            var data = new Dictionary<string, string>
            {
                ["storage"] = Storage,
                ["type"]    = Type
            };

            AddIfNotEmpty(data, "content", Content);
            AddIfNotEmpty(data, "path", Path);
            AddIfNotEmpty(data, "export", Export);
            AddIfNotEmpty(data, "vgname", VgName);
            AddIfNotEmpty(data, "thinpool", ThinPool);
            AddIfNotEmpty(data, "pool", !string.IsNullOrEmpty(Pool) ? Pool : CephPool);
            AddIfNotEmpty(data, "monhost", MonHost);
            AddIfNotEmpty(data, "target", Target);

            // For iSCSI types, 'server' is not a valid API field; derive portal from Server if Portal omitted.
            bool isIscsiType = string.Equals(Type, "iscsi", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Type, "iscsidirect", StringComparison.OrdinalIgnoreCase);
            if (isIscsiType)
            {
                string? portalValue;
                if (!string.IsNullOrEmpty(Portal))
                    portalValue = Portal;
                else if (!string.IsNullOrEmpty(Server))
                    portalValue = $"{Server}:3260";
                else
                    portalValue = null;
                AddIfNotEmpty(data, "portal", portalValue);
            }
            else
            {
                AddIfNotEmpty(data, "server", Server);
                AddIfNotEmpty(data, "portal", Portal);
            }
            AddIfNotEmpty(data, "nodes", Nodes);
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
