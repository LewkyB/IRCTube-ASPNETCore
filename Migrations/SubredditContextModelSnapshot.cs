﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using luke_site_mvc.Data;

#nullable disable

namespace luke_site_mvc.Migrations
{
    [DbContext(typeof(SubredditContext))]
    partial class SubredditContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("luke_site_mvc.Data.Entities.RedditComment", b =>
                {
                    b.Property<string>("Subreddit")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("YoutubeLinkId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("CommentLink")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedUTC")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("RetrievedUTC")
                        .HasColumnType("datetime2");

                    b.Property<int>("Score")
                        .HasColumnType("int");

                    b.HasKey("Subreddit", "YoutubeLinkId");

                    b.HasIndex("Subreddit", "YoutubeLinkId")
                        .IsUnique();

                    b.ToTable("RedditComments");
                });
#pragma warning restore 612, 618
        }
    }
}
