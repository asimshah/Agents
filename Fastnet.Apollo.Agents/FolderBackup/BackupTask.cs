using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Agents
{
    public class BackupTask : SinglePipelineTask
    {
        private readonly BlockingCollection<FolderCopier> foldersToCopy = new BlockingCollection<FolderCopier>();
        //private readonly List<Task> tasks = new List<Task>();
        private readonly BackupConfiguration backupConfiguration;
        public BackupTask(IOptions<BackupConfiguration> config, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            backupConfiguration = config.Value;
        }

        protected async override Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken, params object[] args)
        {
            log.Information("start");
            await Task.Delay(0);
            foreach (var bd in backupConfiguration.Definitions)
            {
                QueueACopy(bd.Source, bd.Destination);
            }
            StartConsumers();
            return null as ITaskState;
        }
        public void QueueACopy(string source, string destination)
        {
            var fc = new FolderCopier(this, source, destination);
            if (fc.CanCopy())
            {
                foldersToCopy.Add(fc);
                log.Trace($"{fc.Index}: {fc.sourceDi.FullName} => {fc.destinationDi.FullName} queued");
            }
        }
        private void StartConsumers()
        {
            var taskList = new List<Task>();
            int consumerCount = 8;
            for(int index = 0;index < consumerCount;++index)
            {
                taskList.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        FolderCopier fc;
                        if(foldersToCopy.TryTake(out fc, TimeSpan.FromSeconds(10)))
                        {
                            fc.Start();
                        }
                        else
                        {
                            break;
                        }

                    }
                }));
            }
            Task.WaitAll(taskList.ToArray());
        }
    }
    public class FolderCopier
    {
        private static int total = 0;
        public int Index { get; private set; }
        public readonly DirectoryInfo sourceDi;
        public readonly DirectoryInfo destinationDi;
        private readonly ILogger log;
        private readonly BackupTask bt;
        public FolderCopier(BackupTask bt, string source, string destination)
        {
            this.bt = bt;
            log = ApplicationLoggerFactory.CreateLogger<FolderCopier>();
            sourceDi = new DirectoryInfo(source);
            destinationDi = new DirectoryInfo(destination);
            Index = ++total;
        }
        public void QueueSubFolders()
        {
            var destinationSubfolders = Directory.EnumerateDirectories(destinationDi.FullName);
            var sourceFolders = Directory.EnumerateDirectories(sourceDi.FullName);
            var destinationOnlyNames = destinationSubfolders.Select(x => Path.GetFileName(x))
                .Except(sourceFolders.Select(x => Path.GetFileName(x)));
            var destinationOnlyFolders = destinationSubfolders.Except(sourceFolders, StringComparer.CurrentCultureIgnoreCase);
            foreach (var name in destinationOnlyNames)
            {
                DirectoryInfo d = new DirectoryInfo(Path.Combine(destinationDi.FullName, name));
                d.Clear();
                d.Delete();
                log.Information($"{d.FullName} deleted");
            }
            foreach (var folder in sourceFolders)
            {
                bt.QueueACopy(folder, Path.Combine(destinationDi.FullName, Path.GetFileName(folder)));
            }
        }
        public bool CanCopy()
        {
            if (!sourceDi.FullName.CanAccess())
            {
                log.Warning($"source {sourceDi.FullName} is not accessible");
                return false;
            }
            var destinationroot = Path.GetPathRoot(destinationDi.FullName);
            if (!Directory.Exists(destinationroot))
            {
                log.Warning($"destination root {destinationroot} is not accessible");
                return false;
            }
            return true;
        }
        public void Start()
        {
            log.Information($"Start {sourceDi.FullName} => {destinationDi.FullName}");
            if (!destinationDi.Exists)
            {
                destinationDi.Create();
                log.Information($"{destinationDi.FullName} created");
            }
            QueueSubFolders();
            SyncFiles();
        }

        private void SyncFiles()
        {
            var destinationFiles = Directory.EnumerateFiles(destinationDi.FullName);
            var sourceFiles = Directory.EnumerateFiles(sourceDi.FullName);
            var destinationOnlyNames = destinationFiles.Select(x => Path.GetFileName(x))
               .Except(sourceFiles.Select(x => Path.GetFileName(x)));
            foreach (var name in destinationOnlyNames)
            {
                var file = Path.Combine(destinationDi.FullName, name);
                File.Delete(file);
                log.Information($"{file} deleted - not found in source");
            }
            foreach (var file in sourceFiles)
            {
                var srcFileInfo = new FileInfo(file);
                var destFileInfo = new FileInfo(Path.Combine(destinationDi.FullName, Path.GetFileName(file)));
                if (!destFileInfo.Exists || destFileInfo.Length != srcFileInfo.Length || srcFileInfo.LastWriteTime > destFileInfo.LastWriteTime)
                {
                    var overWritten = destFileInfo.Exists;
                    srcFileInfo.CopyTo(destFileInfo.FullName, true);
                    log.Information($"{destFileInfo.FullName} created{(overWritten ? " - existing file overwritten" : "")}");
                }
                else
                {
                    log.Trace($"{destFileInfo.FullName} up to date ");
                }
            }
        }
    }
}
