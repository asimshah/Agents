using Fastnet.Agents.Server.Models;
using Fastnet.Agents.Shared;
using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server.Controllers
{
    [Route("backup")]
    [ApiController]
    public class BackupController : RootController
    {
        private readonly AgentsDb db;
        public BackupController(AgentsDb agentsDb)
        {
            this.db = agentsDb;
        }
        [HttpGet("get/owner/{name}")]
        public async Task<IActionResult> GetOwner(string name)
        {
            var owner = await db.Owners.SingleOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
            var dto = owner.ToDTO<OwnerDTO>();
            return Ok(dto);
        }
        [HttpGet("get/drives")]
        public IActionResult GetAvailableDrives()
        {
            int index = 0;
            var drives = DriveInfo.GetDrives()
                .Where(x => x.IsReady)
                .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network)
                .Select(d => new DriveDTO { Index = index++, DriveType = d.DriveType, Name = d.Name, VolumeLabel = d.VolumeLabel });
            return Ok(drives);
        }
        //[HttpGet("get/folders/{volumelabel}")]
        //public IActionResult GetDriveFolders(string volumelabel)
        //{
        //    IEnumerable<string> exclusionList = new string[]
        //    {
        //        Environment.GetEnvironmentVariable("ProgramData"),
        //        Environment.GetEnvironmentVariable("ProgramFiles"),
        //        Environment.GetEnvironmentVariable("ProgramFiles(X86)"),
        //        Environment.GetEnvironmentVariable("SystemRoot")
        //    };
        //    int maxDepth = 2;
        //    void addFolders(List<string> folders, DirectoryInfo di, int depth)
        //    {
        //        try
        //        {
        //            if (depth < maxDepth)
        //            {
        //                var directories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
        //                foreach (var item in directories)
        //                {
        //                    folders.Add(item.FullName);
        //                    addFolders(folders, item, depth + 1);
        //                }
        //            }
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            log.Warning($"{di.FullName}");
        //        }
        //        catch(Exception xe)
        //        {
        //            log.Error(xe);
        //        }
        //    }

        //    //void addFolders(List<string> folders, DirectoryInfo di, int depth)
        //    //{
        //    //    int d = 0;
        //    //    do
        //    //    {
        //    //        scanDirectories(folders, di);
        //    //    } while (++d < depth);
        //    //}
        //    //IEnumerable<string> exclusionList = Enumerable.Empty<string>();
        //    var drive = DriveInfo.GetDrives()
        //        .Where(x => x.IsReady)
        //        .SingleOrDefault(x => string.Compare(volumelabel, x.VolumeLabel, true) == 0);
        //    //if (drive.Name.StartsWith(Environment.GetEnvironmentVariable("SystemDrive")))
        //    //{
        //    //    exclusionList = new string[]
        //    //    {
        //    //        Environment.GetEnvironmentVariable("ProgramFiles"),
        //    //        Environment.GetEnvironmentVariable("ProgramFiles(X86)"),
        //    //        Environment.GetEnvironmentVariable("SystemRoot"),
        //    //    };
        //    //}

        //    var e_options = new EnumerationOptions
        //    {
        //        IgnoreInaccessible = true,
        //        RecurseSubdirectories = true,
        //        ReturnSpecialDirectories = false
        //    };
        //    DirectoryInfo rootDirectory = drive.RootDirectory;
        //    var topDirectories = drive.RootDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly)
        //        .Where(x => exclusionList.All(z => !x.FullName.StartsWith(z)))
        //        .OrderBy(x => x.FullName);
        //    List<string> folders = new();
        //    foreach (var td in topDirectories.Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)))
        //    {
        //        addFolders(folders, td, 0);
        //    }
        //    //var folders = Directory.EnumerateDirectories(drive.RootDirectory.FullName, "*", e_options)
        //    //    .Where(x => exclusionList.All(z => !x.StartsWith(z)))
        //    //    .OrderBy(x => x);
        //    foreach (var f in folders)
        //    {
        //        log.Information($"{f}");
        //    }
        //    return Ok(folders);
        //}
        [HttpPost("addorupdate/sourcefolder")]
        public async Task<IActionResult> AddOrUpdateSourceFolder([FromBody] BackupSourceFolderDTO sf)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Debug.Assert(sf.OwnerId > 0, "No owner provided");
                    BackupSourceFolder bsf;
                    var owner = await db.Owners.FindAsync(sf.OwnerId);
                    if (sf.Id == 0)
                    {
                        if (owner.BackupSourceFolders.Select(x => x.DisplayName).Contains(sf.DisplayName, StringComparer.InvariantCultureIgnoreCase))
                        {
                            ModelState.AddModelError("DisplayName", $"A source folder with this name already exists");
                            return BadRequest(ModelState);
                        }
                        //bsf = sf.MapProperties<BackupSourceFolder>(null);
                        bsf = new();
                        await db.BackupSourceFolders.AddAsync(bsf);
                    }
                    else
                    {
                        bsf = await db.BackupSourceFolders.FindAsync(sf.Id);
                        //bsf = sf.MapProperties(bsf);
                    }
                    bsf.CopyProperties(sf);
                    if (!bsf.FullPath.CanAccess())
                    {
                        ModelState.AddModelError("FullPath", $"Folder not found");

                    }
                    if (!DriveExists(bsf.BackupDriveLabel))
                    {
                        ModelState.AddModelError("BackupDriveLabel", $"No drive found with this label");
                    }
                    //if (!GetFullBackupPath(bsf).CanAccess())
                    if (!bsf.GetFullBackupPath().CanAccess())
                    {
                        ModelState.AddModelError("BackupFolder", $"Folder not found");
                    }
                    if (ModelState.ErrorCount == 0)
                    {
                        await db.SaveChangesAsync();
                        return Ok(bsf.ToDTO<BackupSourceFolderDTO>());
                    }

                }
                return BadRequest(ModelState);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }

        [HttpDelete("delete/sourcefolder/{id}")]
        public async Task<IActionResult> DeleteSourceFolder(int id)
        {
            var bsf = await db.BackupSourceFolders.FindAsync(id);
            if (bsf == null)
            {
                ModelState.AddModelError("", "Source folder not found");
                return BadRequest(ModelState);
            }
            //if (bsf.Backups.Where(b => b.State != BackupState.Deleted).Count() > 0)
            //{
            //    ModelState.AddModelError("", $"Cannot remove a source folder that has undeleted backups");
            //    return BadRequest(ModelState);
            //}
            var backups = bsf.Backups.ToArray();
            bsf.Backups.Clear();
            db.Backups.RemoveRange(backups);
            db.BackupSourceFolders.Remove(bsf);
            await db.SaveChangesAsync();
            return Ok();
        }
        [HttpDelete("delete/backup/{id}")]
        public async Task<IActionResult> DeleteBackup(int id)
        {
            var backup = await db.Backups.FindAsync(id);
            if(backup == null)
            {
                ModelState.AddModelError("", "Backup not found");
                return BadRequest(ModelState);
            }
            switch(backup.State)
            {
                case BackupState.Finished:
                    var bf = backup.FullFilename;
                    var folder = Path.GetDirectoryName(bf);
                    if(!folder.CanAccess(CanWrite: true))
                    {
                        backup.Remark = $"delete failed - cannot access folder";
                        ModelState.AddModelError("", "Backup not foundd");
                        return BadRequest(ModelState);
                    }
                    else
                    {
                        if(System.IO.File.Exists(bf))
                        {
                            System.IO.File.Delete(bf);
                            var fn = bf.Replace(@"\\?\", "");
                            log.Information($"{bf} deleted on request");
                            backup.Remark = $"{fn} deleted on request";
                            backup.FullFilename = string.Empty;
                            backup.State = BackupState.Deleted;
                        }
                    }
                    break;
                default:
                    break;
            }
            await db.SaveChangesAsync();
            return Ok(backup.ToDTO<BackupDTO>());
        }
        [HttpGet("download/backup/{id}")]
        public async Task<FileContentResult> DownloadBackup(int id)
        {
            // **2Aug2021** there is no client side call to this method because
            // the zip files are too large for the .net to javascript interop calls
            // which convert byte[] to base64
            //.Net 6 introduces byte[] as a parameter to javascript calls
            // and this should (?) avoid the need to goto base64 first.
            // subsequently I will need a blob based method to write the data to
            // a file on the client side (perhaps the javascript  FileSystem Api will help?)
            var backup = await db.Backups.FindAsync(id);
            if(backup.State == BackupState.Finished)
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(backup.FullFilename);
                var filename = Path.GetFileName(backup.FullFilename);
                return File(bytes, "application/x-zip-compressed", filename);
            }
            return null;
        }
        [HttpGet("clean")]
        public async Task<IActionResult> CleanupBackups()
        {
            var toBeCleaned = db.Backups.Where(b => b.State != BackupState.Finished).ToArray();
            db.Backups.RemoveRange(toBeCleaned);
            await db.SaveChangesAsync();
            return Ok();
        }
        private string GetFullBackupPath(BackupSourceFolder bsf)
        {
            var di = DriveInfo.GetDrives().SingleOrDefault(x => x.IsReady && string.Compare(bsf.BackupDriveLabel, x.VolumeLabel, true) == 0);
            return di == null ? null : @"\\?\" + Path.Combine(di.RootDirectory.FullName, bsf.BackupFolder);
        }
        private bool DriveExists(string driveLabel)
        {
            var di = DriveInfo.GetDrives().SingleOrDefault(x => x.IsReady && string.Compare(driveLabel, x.VolumeLabel, true) == 0);
            return di != null;
        }
    }
}
