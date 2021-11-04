﻿using luke_site_mvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace luke_site_mvc.Data
{
    public class ChatroomContext : DbContext
    {
        public ChatroomContext(DbContextOptions<ChatroomContext> options)
            : base(options)
        {
        }

        public DbSet<Chatroom> Chatrooms { get; set; }
        public DbSet<RedditComment> RedditComments { get; set; }

        // TODO: need a way to dynamically change connection string without the need to inject
        // used to enable newing up DbContext, but
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=ChatDB;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
    }
}