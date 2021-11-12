﻿// <auto-generated />
using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    [DbContext(typeof(SqliteServerDbContext))]
    [Migration("20211112194222_PlaytimeTracking")]
    partial class PlaytimeTracking
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0");

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<int?>("AdminRankId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT")
                        .HasColumnName("title");

                    b.HasKey("UserId")
                        .HasName("PK_admin");

                    b.HasIndex("AdminRankId")
                        .HasDatabaseName("IX_admin_admin_rank_id");

                    b.ToTable("admin", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminFlag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_flag_id");

                    b.Property<Guid>("AdminId")
                        .HasColumnType("TEXT")
                        .HasColumnName("admin_id");

                    b.Property<string>("Flag")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("flag");

                    b.Property<bool>("Negative")
                        .HasColumnType("INTEGER")
                        .HasColumnName("negative");

                    b.HasKey("Id")
                        .HasName("PK_admin_flag");

                    b.HasIndex("AdminId")
                        .HasDatabaseName("IX_admin_flag_admin_id");

                    b.HasIndex("Flag", "AdminId")
                        .IsUnique();

                    b.ToTable("admin_flag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminRank", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("PK_admin_rank");

                    b.ToTable("admin_rank", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminRankFlag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_flag_id");

                    b.Property<int>("AdminRankId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Flag")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("flag");

                    b.HasKey("Id")
                        .HasName("PK_admin_rank_flag");

                    b.HasIndex("AdminRankId")
                        .HasDatabaseName("IX_admin_rank_flag_admin_rank_id");

                    b.HasIndex("Flag", "AdminRankId")
                        .IsUnique();

                    b.ToTable("admin_rank_flag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Antag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("antag_id");

                    b.Property<string>("AntagName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("antag_name");

                    b.Property<int>("ProfileId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.HasKey("Id")
                        .HasName("PK_antag");

                    b.HasIndex("ProfileId", "AntagName")
                        .IsUnique();

                    b.ToTable("antag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AssignedUserId", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("assigned_user_id_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("PK_assigned_user_id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.HasIndex("UserName")
                        .IsUnique();

                    b.ToTable("assigned_user_id", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Job", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("job_id");

                    b.Property<string>("JobName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("job_name");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER")
                        .HasColumnName("priority");

                    b.Property<int>("ProfileId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.HasKey("Id")
                        .HasName("PK_job");

                    b.HasIndex("ProfileId")
                        .HasDatabaseName("IX_job_profile_id");

                    b.HasIndex("ProfileId", "JobName")
                        .IsUnique();

                    b.HasIndex(new[] { "ProfileId" }, "IX_job_one_high_priority")
                        .IsUnique()
                        .HasFilter("priority = 3");

                    b.ToTable("job", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("player_id");

                    b.Property<DateTime>("FirstSeenTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("first_seen_time");

                    b.Property<string>("LastSeenAddress")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_address");

                    b.Property<byte[]>("LastSeenHWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("last_seen_hwid");

                    b.Property<DateTime>("LastSeenTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_time");

                    b.Property<string>("LastSeenUserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_user_name");

                    b.Property<int>("LivingPlaytime")
                        .HasColumnType("INTEGER")
                        .HasColumnName("living_playtime");

                    b.Property<int>("TotalPlaytime")
                        .HasColumnType("INTEGER")
                        .HasColumnName("total_playtime");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_player");

                    b.HasIndex("LastSeenUserName");

                    b.ToTable("player", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Preference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("preference_id");

                    b.Property<string>("AdminOOCColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("admin_ooc_color");

                    b.Property<int>("SelectedCharacterSlot")
                        .HasColumnType("INTEGER")
                        .HasColumnName("selected_character_slot");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_preference");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("preference", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.Property<int>("Age")
                        .HasColumnType("INTEGER")
                        .HasColumnName("age");

                    b.Property<string>("Backpack")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("backpack");

                    b.Property<string>("CharacterName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("char_name");

                    b.Property<string>("Clothing")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("clothing");

                    b.Property<string>("EyeColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("eye_color");

                    b.Property<string>("FacialHairColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("facial_hair_color");

                    b.Property<string>("FacialHairName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("facial_hair_name");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("gender");

                    b.Property<string>("HairColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("hair_color");

                    b.Property<string>("HairName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("hair_name");

                    b.Property<int>("PreferenceId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("preference_id");

                    b.Property<int>("PreferenceUnavailable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("pref_unavailable");

                    b.Property<string>("Sex")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("sex");

                    b.Property<string>("SkinColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("skin_color");

                    b.Property<int>("Slot")
                        .HasColumnType("INTEGER")
                        .HasColumnName("slot");

                    b.HasKey("Id")
                        .HasName("PK_profile");

                    b.HasIndex("PreferenceId")
                        .HasDatabaseName("IX_profile_preference_id");

                    b.HasIndex("Slot", "PreferenceId")
                        .IsUnique();

                    b.ToTable("profile", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteConnectionLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("connection_log_id");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("address");

                    b.Property<byte[]>("HWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("hwid");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT")
                        .HasColumnName("time");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("PK_connection_log");

                    b.ToTable("connection_log", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("ban_id");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT")
                        .HasColumnName("address");

                    b.Property<DateTime>("BanTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("ban_time");

                    b.Property<Guid?>("BanningAdmin")
                        .HasColumnType("TEXT")
                        .HasColumnName("banning_admin");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("expiration_time");

                    b.Property<byte[]>("HWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("hwid");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("reason");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_ban");

                    b.ToTable("ban", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerUnban", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("unban_id");

                    b.Property<int>("BanId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ban_id");

                    b.Property<DateTime>("UnbanTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("unban_time");

                    b.Property<Guid?>("UnbanningAdmin")
                        .HasColumnType("TEXT")
                        .HasColumnName("unbanning_admin");

                    b.HasKey("Id")
                        .HasName("PK_unban");

                    b.HasIndex("BanId")
                        .IsUnique();

                    b.ToTable("unban", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.HasOne("Content.Server.Database.AdminRank", "AdminRank")
                        .WithMany("Admins")
                        .HasForeignKey("AdminRankId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .HasConstraintName("FK_admin_admin_rank_admin_rank_id");

                    b.Navigation("AdminRank");
                });

            modelBuilder.Entity("Content.Server.Database.AdminFlag", b =>
                {
                    b.HasOne("Content.Server.Database.Admin", "Admin")
                        .WithMany("Flags")
                        .HasForeignKey("AdminId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_flag_admin_admin_id");

                    b.Navigation("Admin");
                });

            modelBuilder.Entity("Content.Server.Database.AdminRankFlag", b =>
                {
                    b.HasOne("Content.Server.Database.AdminRank", "Rank")
                        .WithMany("Flags")
                        .HasForeignKey("AdminRankId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_rank_flag_admin_rank_admin_rank_id");

                    b.Navigation("Rank");
                });

            modelBuilder.Entity("Content.Server.Database.Antag", b =>
                {
                    b.HasOne("Content.Server.Database.Profile", "Profile")
                        .WithMany("Antags")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_antag_profile_profile_id");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("Content.Server.Database.Job", b =>
                {
                    b.HasOne("Content.Server.Database.Profile", "Profile")
                        .WithMany("Jobs")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_job_profile_profile_id");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.HasOne("Content.Server.Database.Preference", "Preference")
                        .WithMany("Profiles")
                        .HasForeignKey("PreferenceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_profile_preference_preference_id");

                    b.Navigation("Preference");
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerUnban", b =>
                {
                    b.HasOne("Content.Server.Database.SqliteServerBan", "Ban")
                        .WithOne("Unban")
                        .HasForeignKey("Content.Server.Database.SqliteServerUnban", "BanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_unban_ban_ban_id");

                    b.Navigation("Ban");
                });

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.Navigation("Flags");
                });

            modelBuilder.Entity("Content.Server.Database.AdminRank", b =>
                {
                    b.Navigation("Admins");

                    b.Navigation("Flags");
                });

            modelBuilder.Entity("Content.Server.Database.Preference", b =>
                {
                    b.Navigation("Profiles");
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.Navigation("Antags");

                    b.Navigation("Jobs");
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerBan", b =>
                {
                    b.Navigation("Unban");
                });
#pragma warning restore 612, 618
        }
    }
}
