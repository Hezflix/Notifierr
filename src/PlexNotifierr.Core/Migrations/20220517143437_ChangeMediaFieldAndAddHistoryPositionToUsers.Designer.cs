﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlexNotifierr.Core.Models;

#nullable disable

namespace PlexNotifierr.Core.Migrations
{
    [DbContext(typeof(PlexNotifierrDbContext))]
    [Migration("20220517143437_ChangeMediaFieldAndAddHistoryPositionToUsers")]
    partial class ChangeMediaFieldAndAddHistoryPositionToUsers
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("PlexNotifierr.Core.Models.Media", b =>
                {
                    b.Property<int>("RatingKey")
                        .HasColumnType("INTEGER")
                        .HasColumnName("rating_key");

                    b.Property<DateTime>("LastNotified")
                        .HasColumnType("TEXT")
                        .HasColumnName("last_notified");

                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("summary");

                    b.Property<string>("ThumbUrl")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("thumb");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("title");

                    b.HasKey("RatingKey");

                    b.ToTable("medias");
                });

            modelBuilder.Entity("PlexNotifierr.Core.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasColumnName("id");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER")
                        .HasColumnName("active");

                    b.Property<string>("DiscordId")
                        .HasColumnType("TEXT")
                        .HasColumnName("discord_id");

                    b.Property<int>("HistoryPosition")
                        .HasColumnType("INTEGER")
                        .HasColumnName("history_position");

                    b.Property<int>("PlexId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("plex_id");

                    b.Property<string>("PlexName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("plex_name");

                    b.HasKey("Id");

                    b.ToTable("users");
                });

            modelBuilder.Entity("PlexNotifierr.Core.Models.UserSubscription", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<int>("RatingKey")
                        .HasColumnType("INTEGER")
                        .HasColumnName("rating_key");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER")
                        .HasColumnName("active");

                    b.HasKey("UserId", "RatingKey");

                    b.HasIndex("RatingKey");

                    b.ToTable("user_subscriptions");
                });

            modelBuilder.Entity("PlexNotifierr.Core.Models.UserSubscription", b =>
                {
                    b.HasOne("PlexNotifierr.Core.Models.Media", "Media")
                        .WithMany("Users")
                        .HasForeignKey("RatingKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PlexNotifierr.Core.Models.User", "User")
                        .WithMany("Medias")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Media");

                    b.Navigation("User");
                });

            modelBuilder.Entity("PlexNotifierr.Core.Models.Media", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("PlexNotifierr.Core.Models.User", b =>
                {
                    b.Navigation("Medias");
                });
#pragma warning restore 612, 618
        }
    }
}
