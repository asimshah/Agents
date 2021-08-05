using Fastnet.Agents.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client.Pages.Backups
{
    public partial class Backups
    {

        [Parameter] public string Name { get; set; }
        private bool isBusy;
        private BackupSourceForm bsf;
        private OwnerDTO Owner { get; set; }
        private Dictionary<BackupSourceFolderDTO, bool> toggleStates = new();
        protected override async Task OnParametersSetAsync()
        {
            await LoadOwnerAsync();
        }

        private async Task LoadOwnerAsync()
        {
            var result = await backupService.GetOwner(Name);
            if(result.Success)
            {
                Owner = result.Data;
                Console.WriteLine($"");
            }
        }
        private async Task ShowFormAsync(BackupSourceFolderDTO sf = null)
        {
            await bsf.ShowAsync(sf, async (dr) => {
                if (!dr.IsCancelled)
                {
                    await LoadOwnerAsync();
                    StateHasChanged();
                }
            });
        }
        private async Task AddSourceFolderAsync()
        {
            //bsf.SetModel(new BackupSourceFolderDTO());
            await ShowFormAsync();
        }
        private async Task EditSourceFolderAsync(BackupSourceFolderDTO item)
        {
            //bsf.SetModel(item);
            await ShowFormAsync(item);
        }
        private string GetToggleStateClass(BackupSourceFolderDTO item)
        {
            return GetToggleState(item) ? "oi-expand-up" : "oi-expand-down";
        }
        private bool GetToggleState(BackupSourceFolderDTO item)
        {
            if (!toggleStates.ContainsKey(item))
            {
                return false;
            }
            else
            {
                return toggleStates[item];
            }
        }
        private void ToggleSourceFolder(BackupSourceFolderDTO item)
        {
            if(!toggleStates.ContainsKey(item))
            {
                toggleStates.Add(item, true);
            }
            else
            {
                toggleStates[item] = !toggleStates[item];
            }
        }
        private async Task DeleteBackupAsync(BackupSourceFolderDTO sf,  BackupDTO backup)
        {
            isBusy = true;
            var result = await backupService.DeleteBackupAsync(backup);
            if(result.Success)
            {
                List<BackupDTO> list = sf.Backups.ToList();
                var index = list.FindIndex(x => x.Id == backup.Id);
                list[index] = result.Data;
                sf.Backups = list;
                StateHasChanged();
            }
            isBusy = false;
        }
        //private async Task DownloadBackupAsync(BackupDTO backup)
        //{
        //    var result = await backupService.DownloadBackup2(backup);
        //    if(result.Success)
        //    {
        //        ZipDownload zdl = result.Data;
        //        await test.SaveAsFile(zdl.Filename, zdl.Base64Data);
        //    }

        //}
    }
}
