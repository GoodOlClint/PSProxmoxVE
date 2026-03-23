using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Tasks
{
    /// <summary>
    /// <para type="synopsis">Lists tasks on a Proxmox VE node.</para>
    /// <para type="description">
    /// Returns a list of recent tasks on the specified node. Use optional parameters
    /// to filter by VM ID, source, or task type. Unlike Get-PveTask which retrieves
    /// a single task by UPID, this cmdlet lists multiple tasks.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveTaskList")]
    [OutputType(typeof(PveTask))]
    public sealed class GetPveTaskListCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node to list tasks from.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">Filter tasks by VM ID.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter tasks by VM ID.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">Filter by task source: all or active.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by task source: all or active.")]
        [ValidateSet("all", "active")]
        public string? Source { get; set; }

        /// <summary>
        /// <para type="description">Filter by task type (e.g., qmstart, vzdump).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by task type (e.g., qmstart, vzdump).")]
        public string? TypeFilter { get; set; }

        /// <summary>
        /// <para type="description">Maximum number of tasks to return. Defaults to 50.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum number of tasks to return. Defaults to 50.")]
        [ValidateRange(1, 10000)]
        public int Limit { get; set; } = 50;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new TaskService();

            WriteVerbose($"Getting tasks on node '{Node}' (limit={Limit})...");
            var tasks = service.GetTasks(session, Node, VmId, Source, TypeFilter, Limit);

            foreach (var task in tasks)
            {
                WriteObject(task);
            }
        }
    }
}
