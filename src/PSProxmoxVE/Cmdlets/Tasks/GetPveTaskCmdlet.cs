using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Tasks
{
    /// <summary>
    /// <para type="synopsis">Gets the status of a Proxmox VE task by UPID.</para>
    /// <para type="description">
    /// Retrieves the current status and exit information for a Proxmox VE task identified
    /// by its UPID (Unique Process Identifier). The Node must be the node that is running
    /// or ran the task.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveTask")]
    [OutputType(typeof(PveTask))]
    public class GetPveTaskCmdlet : PveCmdletBase
    {
        /// <summary>The node on which the task ran.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The UPID of the task to query.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Upid { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new TaskService();
            var task    = service.GetTask(session, Node, Upid);
            WriteObject(task);
        }
    }
}
