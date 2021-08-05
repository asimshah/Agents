using Fastnet.Agents.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;

namespace Fastnet.Agents.Server.Models
{

    public static partial class Extensions
    {
        //public static TO ToDTO<TO>(this object item) where TO : new()
        //{
        //    TO dto = new();
        //    dto.CopyProperties(item);
        //    return dto;
        //}
        //public static OwnerDTO ToDTO(this Owner owner)
        //{
        //    return new OwnerDTO
        //    {
        //        Id = owner.Id,
        //        Name = owner.Name,
        //        BackupSourceFolders = owner.BackupSourceFolders.Select(x => x.ToDTO())
        //        .OrderBy(x => x.DisplayName).ToList()
        //    };
        //}
        //public static BackupSourceFolderDTO ToDTO(this BackupSourceFolder bsf)
        //{
        //    BackupSourceFolderDTO dto = new();
        //    dto.CopyProperties(bsf);
        //    //dto.Backups.ToList().ForEach(x => x.Length = x.State == BackupState.Finished ? new FileInfo(x.FullFilename).Length : 0);
        //    //dto.Backups = dto.Backups.OrderByDescending(x => x.BackupDateUTC);
        //    return dto;
        //    //return new BackupSourceFolderDTO
        //    //{
        //    //    Id = bsf.Id,
        //    //    DisplayName = bsf.DisplayName,
        //    //    FullPath = bsf.FullPath,
        //    //    BackupEnabled = bsf.BackupEnabled,
        //    //    Type = bsf.Type,
        //    //    ContentRoot = bsf.ContentRoot,
        //    //    AutoDelete = bsf.AutoDelete,
        //    //    DeleteAfter = bsf.DeleteAfter,
        //    //    BackupDriveLabel = bsf.BackupDriveLabel,
        //    //    BackupFolder = bsf.BackupFolder,
        //    //    OwnerId = bsf.OwnerId,
        //    //    Backups = bsf.Backups.Select(b => b.ToDTO())
        //    //        .OrderByDescending(x => x.BackupDateUTC).ToList()
        //    //};
        //}
        //public static BackupDTO ToDTO(this Backup backup)
        //{
        //    var dto = backup.ToDTO<BackupDTO>();
        //    //dto.Length = backup.State == BackupState.Finished ? new FileInfo(backup.FullFilename).Length : 0;
        //    return dto;
        //    //return new BackupDTO
        //    //{
        //    //    Id = backup.Id,
        //    //    BackedUpOnUTC = backup.BackedUpOnUTC,
        //    //    BackupDateUTC = backup.BackupDateUTC,
        //    //    State = backup.State,
        //    //    FullFilename = backup.FullFilename,
        //    //    Length = backup.State == BackupState.Finished ? new FileInfo(backup.FullFilename).Length : 0,
        //    //    Remark = backup.Remark,
        //    //    BackupSourceFolderId = backup.BackupSourceFolderId
        //    //};
        //}
        public static string GetFullBackupPath(this BackupSourceFolder bsf)
        {
            var di = DriveInfo.GetDrives().SingleOrDefault(x => x.IsReady && string.Compare(bsf.BackupDriveLabel, x.VolumeLabel, true) == 0);
            return di == null ? null : @"\\?\" + Path.Combine(di.RootDirectory.FullName, bsf.BackupFolder);
        }
    }

    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<BackupSourceFolder> BackupSourceFolders { get; set; } = new HashSet<BackupSourceFolder>();
    }
    public class BackupSourceFolder
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool BackupEnabled { get; set; }
        public SourceType Type { get; set; }
        public bool AutoDelete { get; set; } = true;
        public int DeleteAfter { get; set; } = 30;// no of days
        public string BackupDriveLabel { get; set; } = string.Empty;
        public string BackupFolder { get; set; } = string.Empty;
        public string ContentRoot { get; set; } = string.Empty; // used only if Type is Website
        public int OwnerId { get; set; }
        public virtual Owner Owner { get; set; }
        public virtual ICollection<Backup> Backups { get; set; } = new HashSet<Backup>();
    }
    public class Backup
    {
        public int Id { get; set; }
        //public DateTimeOffset ScheduledOn { get; set; }
        public DateTimeOffset BackupDateUTC { get; set; } // date for this backup, time values are all zero (i.e. the date on which this backup should occur)
        public DateTimeOffset BackedUpOnUTC { get; set; } // date and time on which this backup completed (or failed ,...)
        public BackupState State { get; set; }
        public string FullFilename { get; set; }
        public string Remark { get; set; }
        public int BackupSourceFolderId { get; set; }
        public virtual BackupSourceFolder SourceFolder { get; set; }
    }
}
