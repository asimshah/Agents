using Fastnet.Agents.Server.Models;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server.Tasks
{
    public class BackupTask : SinglePipelineTask
    {
        private DateTimeOffset TodayUTC;
        private readonly AgentsDb db;
        private readonly IOptionsMonitor<BackupOptions> options;
        public BackupTask(IOptionsMonitor<BackupOptions> options, IConfiguration cfg, IWebHostEnvironment environment, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.db = new AgentsDb("agentsConnection", cfg, environment);
            this.options = options;
        }

        protected override async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            TodayUTC = DateTimeOffset.UtcNow.StripTimeAndZone();
            var backupHour = options.CurrentValue.BackupHour;
            var backupDateTime = TodayUTC.AddHours(backupHour);
            if (backupDateTime < DateTimeOffset.UtcNow)
            {
                var pendingBackups = await GetPendingBackupsAsync();
                foreach (var backup in pendingBackups)
                {
                    await PerformBackupAsync(backup);
                }
            }
            return null;
        }

        private async Task PerformBackupAsync(Backup backup)
        {
            try
            {
                var datePart = TodayUTC.ToString("yyyyMMdd");
                var fileName = $"{backup.SourceFolder.Owner.Name}-{backup.SourceFolder.DisplayName}-{datePart}.zip";
                var backupFolder = backup.SourceFolder.GetFullBackupPath();
                if(!backupFolder.CanAccess(CanWrite: true))
                {
                    log.Error($"Cannot access {backupFolder}");
                    backup.State = Shared.BackupState.Failed;
                    backup.Remark = $"failed to access {backupFolder} - offline?";
                    await db.SaveChangesAsync();
                    return;
                }
                var targetFolder = Path.Combine(backupFolder, backup.SourceFolder.Owner.Name);
                if(!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
                backup.FullFilename = Path.Combine(targetFolder, fileName);
                backup.State = Shared.BackupState.Started;
                await db.SaveChangesAsync();

                if(backup.SourceFolder.Type == Shared.SourceType.Website)
                {
                    await TakeSiteOfflineAsync(backup.SourceFolder);
                }
                if(File.Exists(backup.FullFilename))
                {
                    File.Delete(backup.FullFilename);
                    log.Warning($"{backup.FullFilename} deleted");
                }
                var duration = Zip(backup.SourceFolder.FullPath, backup.FullFilename);
                log.Information($"Backup of {backup.SourceFolder.DisplayName} to {backup.FullFilename} completed in {duration.TotalSeconds} seconds");
                backup.State = Shared.BackupState.Finished;
                backup.BackedUpOnUTC = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
                //if (backup.SourceFolder.Type == Shared.SourceType.Website)
                //{
                //    await BringSiteOnlineAsync(backup.SourceFolder);
                //}
            }
            catch (Exception xe)
            {
                log.Error(xe);
                backup.State = Shared.BackupState.Failed;
                backup.Remark = xe.Message;

                await db.SaveChangesAsync();
            }
            finally
            {
                if (backup.SourceFolder.Type == Shared.SourceType.Website)
                {
                    await BringSiteOnlineAsync(backup.SourceFolder);
                }
            }
        }

        private async Task<IEnumerable<Backup>> GetPendingBackupsAsync()
        {
            var todayUTC = DateTimeOffset.UtcNow.StripTimeAndZone();
            return await db.Owners.SelectMany(x => x.BackupSourceFolders)
                .Where(f => f.BackupEnabled && f.Type == Shared.SourceType.Folder || f.Type == Shared.SourceType.Website)
                .SelectMany(x => x.Backups)
                .Where(x => x.BackupDateUTC == todayUTC && x.State == Shared.BackupState.Pending)
                .ToArrayAsync();
        }
        private async Task TakeSiteOfflineAsync(BackupSourceFolder sf)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<html>");
            sb.AppendLine($"<head>");
            sb.AppendLine($"<title>Offline</title>");
            sb.AppendLine($"</head>");
            sb.AppendLine($"<body style='margin: 3em; font - family:sans - serif'>");
            sb.AppendLine($"<h2>Fastnet Backup Services</h2>");
            sb.AppendLine($"<p>This site is temporarily down for maintenance</p>");
            sb.AppendLine($"<p>Please return in a short time</p>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"</body>");
            sb.AppendLine($"</html>");
            string html = sb.ToString();
            string appOffline = Path.Combine(sf.ContentRoot, "app_offline.htm");
            File.WriteAllText(appOffline, html);
            await Task.Delay(options.CurrentValue.WaitForWebsiteOffline);
        }
        private async Task BringSiteOnlineAsync(BackupSourceFolder sf)
        {
            string appOffline = Path.Combine(sf.ContentRoot, "app_offline.htm");
            File.Delete(appOffline);
            await Task.Yield();
            //await Task.Delay(1000);
        }
        private TimeSpan Zip(string source, string destination)
        {
            var sw = new Stopwatch();
            sw.Start();
            ZipFile.CreateFromDirectory(source, destination);
            sw.Stop();
            return TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);
        }
    }
}
