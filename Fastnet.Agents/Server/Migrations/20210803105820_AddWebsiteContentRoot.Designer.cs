﻿// <auto-generated />
using System;
using Fastnet.Agents.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fastnet.Agents.Server.Migrations
{
    [DbContext(typeof(AgentsDb))]
    [Migration("20210803105820_AddWebsiteContentRoot")]
    partial class AddWebsiteContentRoot
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Fastnet.Agents.Server.Models.Backup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset>("BackedUpOnUTC")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("BackupDateUTC")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("BackupSourceFolderId")
                        .HasColumnType("int");

                    b.Property<string>("FullFilename")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BackupSourceFolderId");

                    b.ToTable("Backups");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.BackupSourceFolder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("AutoDelete")
                        .HasColumnType("bit");

                    b.Property<string>("BackupDriveLabel")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("BackupEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("BackupFolder")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ContentRoot")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DeleteAfter")
                        .HasColumnType("int");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FullPath")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OwnerId")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.ToTable("BackupSourceFolders");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.Owner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Owners");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.Backup", b =>
                {
                    b.HasOne("Fastnet.Agents.Server.Models.BackupSourceFolder", "SourceFolder")
                        .WithMany("Backups")
                        .HasForeignKey("BackupSourceFolderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SourceFolder");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.BackupSourceFolder", b =>
                {
                    b.HasOne("Fastnet.Agents.Server.Models.Owner", "Owner")
                        .WithMany("BackupSourceFolders")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.BackupSourceFolder", b =>
                {
                    b.Navigation("Backups");
                });

            modelBuilder.Entity("Fastnet.Agents.Server.Models.Owner", b =>
                {
                    b.Navigation("BackupSourceFolders");
                });
#pragma warning restore 612, 618
        }
    }
}
