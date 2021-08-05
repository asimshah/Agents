using Fastnet.Agents.Shared;
using Fastnet.Blazor.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client.Services
{
    public class BackupService : WebApiService
    {
        public BackupService(HttpClient client, ILogger<BackupService> logger) : base(client, logger)
        {
        }
        public async Task<WebApiResult<OwnerDTO>> GetOwner(string name)
        {
            string query = $"backup/get/owner/{name}";
            return await GetAsync<OwnerDTO>(query);
        }
        public async Task<WebApiResult<DriveDTO[]>> GetDrives()
        {
            string query = $"backup/get/drives";
            return await GetAsync<DriveDTO[]>(query);
        }
        //public async Task<WebApiResult<string[]>> GetDriveFolders(string volumeLabel)
        //{
        //    string query = $"backup/get/folders/{volumeLabel}";
        //    return await GetAsync<string[]>(query);
        //}
        public async Task<WebApiResult<BackupSourceFolderDTO>> AddOrUpdateBackupSourceFolder(BackupSourceFolderDTO sf)
        {
            string query = $"backup/addorupdate/sourcefolder";
            return await PostAsync<BackupSourceFolderDTO, BackupSourceFolderDTO>(query, sf);
        }
        public async Task<WebApiResult> DeleteBackupSourceFolderAsync(BackupSourceFolderDTO bsf)
        {
            string query = $"backup/delete/sourcefolder/{bsf.Id}";
            return await DeleteAsync(query);
        }
        public async Task<WebApiResult<BackupDTO>> DeleteBackupAsync(BackupDTO backup)
        {
            string query = $"backup/delete/backup/{backup.Id}";
            return await DeleteAsync<BackupDTO>(query);
        }
        //public async Task<(string, byte[])> DownloadBackup(BackupDTO backup)
        //{
        //    var query = $"backup/download/backup/{backup.Id}";
        //    var r = await client.GetAsync(query);
        //    var filename = r.Content.Headers.ContentDisposition.FileName;
        //    var bytes = await r.Content.ReadAsByteArrayAsync();
        //    return (filename, bytes);
        //}
        //public async Task<WebApiResult<ZipDownload>> DownloadBackup2(BackupDTO backup)
        //{
        //    var query = $"backup/download/backup2/{backup.Id}";
        //    return await GetAsync<ZipDownload>(query);
        //}
    }
}
