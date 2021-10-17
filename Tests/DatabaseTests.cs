﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using luke_site_mvc.Data;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace luke_site_mvc
{
    public class DatabaseTests
    {
        [Fact]
        public void GetAllLinksTest()
        {
            var result = GetChatLinks("#bash");
            

            Assert.NotNull(result);

        }
        //public IReadOnlyList<string> GetChatLinks(string chatname)
        public IEnumerable<Chatroom> GetChatLinks(string chatname)
        {
            // TODO: RELOCATE
            using IDbConnection connection =
                new SqlConnection("Data Source=localhost;Initial Catalog=ChatDB;Integrated Security=True;MultipleActiveResultSets = true");

            var parameters = new { Chatname = chatname };
            var sql = "SELECT DISTINCT Link FROM Chatrooms WHERE Name LIKE CONCAT('%',@Chatname,'%');";
            //var chatnames = connection.Query<string>(sql, parameters).ToList().AsReadOnly();
            var chatnames = connection.Query<Chatroom>(sql, parameters);

            //var list = chatnames.Where(x => x.)

            return chatnames;
        }
    }
}