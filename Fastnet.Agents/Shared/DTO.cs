using Fastnet.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Agents.Shared
{
    public enum SourceType
    {
        Folder, // copied every day into a fresh zip file
        Website, // copied every day into a fresh zip file after closing the site (and then reopening it!)
        Replication // contents replicated to a destination
    }
    public enum BackupState
    {
        Pending, // i. e. should start (soon)
        Started, // i.e. is currently in progress
        Skipped, // i.e. a later backup superseded this one
        Finished, // i.e. is complete
        Failed, // i.e. failed during backup
        Deleted //  has been deleted, i. e. backup files are no longer present
    }
    public class BackupDTO : ICopyable
    {
        public int Id { get; set; }
        public DateTimeOffset BackedUpOnUTC { get; set; }
        public DateTimeOffset BackupDateUTC { get; set; }
        public BackupState State { get; set; }
        public string FullFilename { get; set; }
        public long Length { get; set; }
        public string Remark { get; set;  }
        public int BackupSourceFolderId { get; set; }

        public void AfterCopy()
        {
            Length = State == BackupState.Finished ? new FileInfo(FullFilename).Length : 0;
        }
    }
    public class BackupSourceFolderDTO : ICopyable
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "A Name is required")]
        public string DisplayName { get; set; }
        [Required(ErrorMessage = "A Full Path is required")]
        public string FullPath { get; set; }
        public bool BackupEnabled { get; set; } = true;
        public SourceType Type { get; set; }
        public bool AutoDelete { get; set; } = true;
        [RangeIf(nameof(AutoDelete), true, 1, 60, ErrorMessage = "Valid range is from 1 to 60 days inclusive")]
        public int DeleteAfter { get; set; } = 30;
        public string BackupDriveLabel { get; set; }
        [Required(ErrorMessage = "Backup Folder is required (target folder on the drive specified)")]
        public string BackupFolder { get; set; }
        [RequiredIf(nameof(Type), SourceType.Website, ErrorMessage = "Required for Website backups")]
        public string ContentRoot { get; set; }
        public int OwnerId { get; set; }
        public List<BackupDTO> Backups { get; set; } = new();// Enumerable.Empty<BackupDTO>();

        public void AfterCopy()
        {
            Backups = Backups.OrderByDescending(x => x.BackupDateUTC).ToList();
        }
    }
    public class OwnerDTO : ICopyable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<BackupSourceFolderDTO> BackupSourceFolders { get;  set; } = new();// Enumerable.Empty<BackupSourceFolderDTO>();

        public void AfterCopy()
        {
            BackupSourceFolders = BackupSourceFolders.OrderBy(x => x.DisplayName).ToList();
        }
    }
    public class DriveDTO
    {
        public int Index { get; set; }
        public DriveType DriveType { get; set; }
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public override string ToString()
        {
            string name = Name.EndsWith("\\") ? Name.Substring(0, Name.Length - 1) : Name;
            if (!string.IsNullOrWhiteSpace(VolumeLabel))
            {
                return $"{VolumeLabel} ({name})";
            }
            return name;
        }
    }
    public class ZipDownload
    {
        public string  Filename { get; set; }
        public string Base64Data { get; set; }
        public string MimeType { get; set; }
    }
}
