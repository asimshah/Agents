using Fastnet.Agents.Server.Models;
using Fastnet.Agents.Shared;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server.Tasks
{
    public class ScheduleBackupsService : SinglePipelineTask
    {
        private AgentsDb db;
        private readonly IConfiguration cfg;
        private readonly IWebHostEnvironment environment;
        //private readonly IOptionsMonitor<BackupOptions> options;
        public ScheduleBackupsService(IConfiguration cfg, IWebHostEnvironment environment, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            //this.db =  new AgentsDb("agentsConnection", cfg, environment);
            //this.options = options;
            this.cfg = cfg;
            this.environment = environment;
        }

        protected override async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            using (db = new AgentsDb("agentsConnection", cfg, environment))
            {
                await EnsureTodaysBackupsAsync();
                await SkipSupersededBackupsAsync();
                await AutoDeleteBackupsAsync();
                await PurgeAsync();
            }
            return null;
        }
        private async Task PurgeAsync()
        {
            var todayUTC = DateTimeOffset.UtcNow.StripTimeAndZone();
            foreach (var owner in db.Owners)
            {
                foreach (var sf in owner.BackupSourceFolders)
                {
                    var deletionDate = sf.AutoDelete ? todayUTC.AddDays(-(sf.DeleteAfter + 7)) : todayUTC.AddDays(-30);
                    var toBePurged = sf.Backups.Where(b => b.BackupDateUTC < deletionDate).ToList();
                    foreach (var backup in toBePurged.ToArray())
                    {
                        var bf = backup.FullFilename;
                        if (System.IO.File.Exists(bf))
                        {
                            var folder = Path.GetDirectoryName(bf);
                            if (!folder.CanAccess(CanWrite: true))
                            {
                                backup.Remark = $"purge failed - cannot access {folder}";
                                backup.Remark = $"{todayUTC.ToDefault()}: purge failed - cannot access folder";
                                toBePurged.Remove(backup);
                            }
                            else
                            {
                                System.IO.File.Delete(bf);
                                log.Information($"{bf} deleted prior to purge");
                            }
                        }                        
                    }

                    foreach(var b in toBePurged.OrderBy(x => x.BackupDateUTC))
                    {
                        log.Information($"{b.SourceFolder.Owner.Name}, {b.SourceFolder.DisplayName}, backup for {b.BackupDateUTC.ToDefault()} purged");
                    }
                    db.Backups.RemoveRange(toBePurged);
                }
            }
            await db.SaveChangesAsync();
        }
        private async Task AutoDeleteBackupsAsync()
        {
            var todayUTC = DateTimeOffset.UtcNow.StripTimeAndZone();
            foreach (var owner in db.Owners)
            {
                foreach (var sf in owner.BackupSourceFolders.Where(f => f.BackupEnabled && f.AutoDelete))
                {
                    var deletionDate = todayUTC.AddDays(-sf.DeleteAfter);
                    var tobeDeleted = sf.Backups.Where(b => b.State == Shared.BackupState.Finished && b.BackupDateUTC < deletionDate);
                    foreach(var backup in tobeDeleted)
                    {
                        var bf = backup.FullFilename;
                        var folder = Path.GetDirectoryName(bf);
                        if (!folder.CanAccess(CanWrite: true))
                        {
                            backup.Remark = $"auto delete failed - cannot access {folder}";
                        }
                        else
                        {
                            if (System.IO.File.Exists(bf))
                            {
                                System.IO.File.Delete(bf);
                                var fn = bf.Replace(@"\\?\", "");
                                log.Information($"{bf} auto deleted");
                                backup.Remark = $"{fn} auto deleted";
                                backup.FullFilename = string.Empty;
                                backup.State = BackupState.Deleted;
                            }
                        }
                    }

                }
            }
            await db.SaveChangesAsync();
        }
        private async Task SkipSupersededBackupsAsync()
        {
            foreach (var owner in db.Owners)
            {
                foreach (var folder in owner.BackupSourceFolders.Where(f => f.BackupEnabled))
                {
                    var pending = folder.Backups.Where(b => b.State == Shared.BackupState.Pending)
                        .OrderByDescending(b => b.BackupDateUTC);
                    if (pending.Count() > 1)
                    {
                        foreach (var item in pending.Skip(1))
                        {
                            item.State = Shared.BackupState.Skipped;
                            item.Remark = $"skipped because superseded (on {DateTimeOffset.UtcNow.ToDefaultWithTime()} UTC)";
                        }
                    }

                }
            }
            await db.SaveChangesAsync();
        }

        private async Task EnsureTodaysBackupsAsync()
        {
            var todayUTC = DateTimeOffset.UtcNow.StripTimeAndZone();
            foreach (var owner in db.Owners)
            {
                foreach (var folder in owner.BackupSourceFolders
                    .Where(f => f.BackupEnabled && (f.Type == Shared.SourceType.Folder || f.Type == Shared.SourceType.Website)))
                {
                    var backups = folder.Backups.Where(b => b.BackupDateUTC == todayUTC);
                    if (backups.Count() == 0)
                    {
                        var b = new Backup
                        {
                            BackupDateUTC = todayUTC,
                            SourceFolder = folder,
                            State = Shared.BackupState.Pending
                        };
                        await db.Backups.AddAsync(b);
                        //await db.SaveChangesAsync();
                        log.Information($"{owner.Name}, {folder.DisplayName} backup added for {todayUTC.ToDefault()}");
                    }
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
