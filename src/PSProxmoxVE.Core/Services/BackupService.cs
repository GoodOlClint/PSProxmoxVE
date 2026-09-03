using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Backup;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE Backup (vzdump) and backup job API operations.
    /// </summary>
    public class BackupService : PveServiceBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BackupService"/> with no injected client.
        /// Each method will create and dispose its own <see cref="PveHttpClient"/>.
        /// </summary>
        public BackupService() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BackupService"/> with an injected HTTP client.
        /// The caller owns the client's lifetime; this service will not dispose it.
        /// </summary>
        /// <param name="client">The HTTP client to use for all requests.</param>
        public BackupService(IPveHttpClient client) : base(client) { }

        // -------------------------------------------------------------------------
        // Ad-hoc backup (vzdump)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates an ad-hoc backup via vzdump. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node to run the backup on.</param>
        /// <param name="config">Backup configuration parameters.</param>
        public PveTask CreateBackup(PveSession session, string node, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/vzdump", config)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }

        // -------------------------------------------------------------------------
        // Backup jobs (scheduled)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all scheduled backup jobs.
        /// </summary>
        public PveBackupJob[] GetBackupJobs(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("cluster/backup").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveBackupJob[]>() ?? Array.Empty<PveBackupJob>();
            });
        }

        /// <summary>
        /// Returns a single scheduled backup job by ID.
        /// </summary>
        public PveBackupJob? GetBackupJob(PveSession session, string id)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"cluster/backup/{Uri.EscapeDataString(id)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveBackupJob>();
            });
        }

        /// <summary>
        /// Creates a scheduled backup job.
        /// </summary>
        public void CreateBackupJob(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PostAsync("cluster/backup", config).GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Updates a scheduled backup job.
        /// </summary>
        public void UpdateBackupJob(PveSession session, string id, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PutAsync($"cluster/backup/{Uri.EscapeDataString(id)}", config)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Removes a scheduled backup job.
        /// </summary>
        public void RemoveBackupJob(PveSession session, string id)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            Invoke(session, client =>
            {
                client.DeleteAsync($"cluster/backup/{Uri.EscapeDataString(id)}")
                    .GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Backup compliance info
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the list of guests not covered by any backup job.
        /// </summary>
        public List<Dictionary<string, object?>> GetNotBackedUp(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("cluster/backup-info/not-backed-up")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToListOfDictionaries(data as JArray);
            });
        }
    }
}
