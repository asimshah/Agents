using Fastnet.Core.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fastnet.Core.UserAccounts;
using Fastnet.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Fastnet.Agents.Server.Models
{
    //public abstract class WebDbContext : DbContext //where T : WebDbContext<T>
    //{
    //    private string connectionString;
    //    private readonly string connectionName;
    //    private readonly IConfiguration config;
    //    private readonly IWebHostEnvironment env;
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="cn"></param>
    //    public WebDbContext(string connectionName, IConfiguration cfg, IWebHostEnvironment environment)
    //    {
    //        this.connectionName = connectionName;
    //        this.config = cfg;
    //        this.env = environment;
    //        //SetConnectionString(connectionName);
    //    }
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="contextOptions"></param>
    //    public WebDbContext(DbContextOptions contextOptions) : base(contextOptions)
    //    {

    //    }
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="optionsBuilder"></param>
    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    {
    //        if (!optionsBuilder.IsConfigured)
    //        {
    //            if(!string.IsNullOrWhiteSpace(connectionName))
    //            {
    //                SetConnectionString(connectionName);
    //            }
    //            optionsBuilder.UseSqlServer(connectionString)
    //                .UseLazyLoadingProxies();
    //            ;
    //        }
    //        else
    //        {
    //            base.OnConfiguring(optionsBuilder);
    //        }
    //    }
    //    private void SetConnectionString(string connectionName)
    //    {
    //        //var config = this.Database.GetService<IConfiguration>();
    //        //Debug.Assert(config != null);
    //        var cs = config.GetConnectionString(connectionName);
    //        Debug.Assert(cs != null && cs != string.Empty, $"connection string for {connectionName} not found");
    //        //var env = this.Database.GetService<IWebHostEnvironment>();
    //        //Debug.Assert(env != null);
    //        this.connectionString = env.LocaliseConnectionString(cs);
    //        Debug.Assert(!string.IsNullOrWhiteSpace(connectionString));
    //    }
    //}

    public class AgentsDBFactory : IDesignTimeDbContextFactory<AgentsDb>
    {
        const string cs = "Data Source=.\\SQLEXPRESS2019;AttachDbFilename=|DataDirectory|\\Agents.mdf;Initial Catalog=Agents-dev;Integrated Security=True;MultipleActiveResultSets=True";
        public AgentsDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDb>();
            optionsBuilder.UseSqlServer(cs);
            return new AgentsDb(optionsBuilder.Options);
        }
    }
    public class InitialiserService : SiteInitialiserService
    {
        public InitialiserService(IServiceProvider serviceProvider, ILogger<InitialiserService> log) : base(serviceProvider, log)
        {
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await MigrateDatabaseAsync(async (SiteInitialiserCompletedArgs<AgentsDb> args) =>
            {
                (AgentsDb db, bool migrated, IServiceScope scope) = args;
                if(migrated)
                {
                    log.Information($"{nameof(AgentsDb)} migrated");
                }
                await EnsureOwnersAsync(db);
                //return Task.CompletedTask;
            });
            await ExecuteUsingScopeAsync(async (scope) =>
            {
                try
                {
                    var userManager = scope.ServiceProvider.GetService<UserManager>();
                    var roles = new string[] { "administrator", "superadmin" };
                    foreach (var role in roles)
                    {
                        await userManager.CreateRoleAsync(role);
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                    throw;
                }
            });
        }
        private async Task EnsureOwnersAsync(AgentsDb db)
        {
            var owners = new string[] { "Asim", "QPara" };
            foreach(var owner in owners)
            {
                var item = await db.Owners.SingleOrDefaultAsync(x => x.Name == owner);
                if(item == null)
                {
                    await db.Owners.AddAsync(new Owner { Name = owner });
                }
            }
            await db.SaveChangesAsync();
        }
    }
    public class AgentsDb : WebDbContext
    {
        //private string connectionString;
        public DbSet<Backup> Backups { get; set;}
        public DbSet<BackupSourceFolder> BackupSourceFolders { get; set; }
        public DbSet<Owner> Owners { get; set; }

        public AgentsDb(string connectionName, IConfiguration cfg, IWebHostEnvironment environment) : base(connectionName,cfg, environment)
        {

        }
        public AgentsDb(DbContextOptions<AgentsDb> contextOptions) : base(contextOptions)
        {

        }

    }

}
