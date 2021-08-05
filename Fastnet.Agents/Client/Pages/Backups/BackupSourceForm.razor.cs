using Fastnet.Agents.Shared;
using Fastnet.Blazor.Controls;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client.Pages.Backups
{
    public partial class BackupSourceForm
    {
        private MessageBox mb;
        private Dialogue<BackupSourceFolderDTO> dialogue;
        [Parameter] public BackupSourceFolderDTO Model { get; set; } = new();
        [Parameter] public OwnerDTO Owner { get; set; }
        private IEnumerable<DriveDTO> drives { get; set; } = Enumerable.Empty<DriveDTO>();
        //private IEnumerable<string> driveFolders { get; set; } = Enumerable.Empty<string>();
        private int driveIndex = 0;
        //private string selectedFolder;
        //public void SetModel(BackupSourceFolderDTO model)
        //{
        //    Model = model;
        //    var modelDrive = drives.FirstOrDefault(x => string.Compare(x.VolumeLabel, Model.BackupDriveLabel, true) == 0);
        //    if(modelDrive != null)
        //    {
        //        driveIndex = drives.
        //    }
        //}
        public async Task ShowAsync(BackupSourceFolderDTO sf,  Action<DialogResult> onClose)
        {
            var war = await backupService.GetDrives();
            if (war.Success)
            {
                sf ??= new();
                Model = sf;
                drives = war.Data;
                driveIndex = drives.FirstOrDefault(x => x.VolumeLabel == sf.BackupDriveLabel)?.Index ?? drives.First().Index; 
                //await LoadFoldersAsync();
            }
            await dialogue.ShowAsync(onClose);
        }
        //private async Task OnDriveChanged(int index)
        //{
        //    await LoadFoldersAsync();
        //}

        //private async Task LoadFoldersAsync()
        //{
        //    driveFolders = Enumerable.Empty<string>();
        //    var selectedDrive = drives.Single(x => x.Index == driveIndex);
        //    var war = await backupService.GetDriveFolders(selectedDrive.VolumeLabel);
        //    if (war.Success)
        //    {
        //        driveFolders = war.Data;
        //    }
        //}

        private async Task OnOK()
        {
            var isValid = dialogue.ValidateAnnotations();
            if (isValid)
            {
                Model.OwnerId = Owner.Id;
                Model.BackupDriveLabel = drives.Single(x => x.Index == driveIndex).VolumeLabel;
                var r = await backupService.AddOrUpdateBackupSourceFolder(Model);
                if (!r.Success)
                {
                    dialogue.AddErrors(r.Errors);
                }
                else
                {
                    dialogue.Close(DialogResult.Success(r.Data));
                }
            }
        }
        private void OnCancel()
        {
            dialogue.Close(DialogResult.Cancelled());
        }
        private async Task OnDelete()
        {
            var r = await backupService.DeleteBackupSourceFolderAsync(Model);
            if (r.Success)
            {
                await mb.ShowAsync($"Source folder {Model.DisplayName} deleted", (dr) =>
                {
                    dialogue.Close(DialogResult.Success());
                });
            }
            else
            {
                // site did not delete
                var error = r.Errors.Values.First().First();
                await mb.ShowAsync(
                    new string[] {
                    $"Source folder {Model.DisplayName} could not be deleted",
                    $"{error}" });
            }
        }
    }
}
