﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using luke_site_mvc.Data;
using luke_site_mvc.Data.Entities;
using luke_site_mvc.Models.PsawSearchOptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PsawSharp.Entries;

namespace luke_site_mvc.Services
{
    public interface IPushshiftService
    {
        string FindYoutubeId(string commentBody);
        Task<List<RedditComment>> GetLinksFromCommentsAsync(string selected_subreddit, string order = "desc");
        Task<List<string>> GetSubreddits();
    }
    public class PushshiftService : IPushshiftService
    {
        private readonly ILogger<PushshiftService> _logger;
        private readonly IDistributedCache _cache;
        private readonly SubredditContext _subredditContext;
        private readonly IPsawService _psawService;

        public PushshiftService(ILogger<PushshiftService> logger, IDistributedCache distributedCache, SubredditContext subredditContext, IPsawService psawService)
        {
            _logger = logger;
            _cache = distributedCache;
            _subredditContext = subredditContext;
            _psawService = psawService;
        }
        public async Task<List<string>> GetSubreddits()
        {
            // TODO: provide a better list of subreddits
            List<string> subreddits = new List<string>()
            {
                "space",
                "science",
                "mealtimevideos",
                "skookum",
                "artisanvideos",
                "AIDKE",
                "linux",
                "movies",
                "dotnet",
                "csharp",
                "biology",
                "astronomy",
                "photography",
                "aviation",
                "lectures",
                "homebrewing",
                "fantasy",
                "homeimprovement",
                "woodworking"
            };

            // sort the list alphabetically
            return subreddits.OrderBy(x => x).ToList();
        }

        // TODO: try to move all of the database calls into a repository
        public async Task<List<RedditComment>> GetLinksFromCommentsAsync(string selected_subreddit, string order = "desc")
        {

            List<RedditComment> redditComments = new List<RedditComment>();

            // used to hold remaining comments after duplicate entries are filtered out
            //List<RedditComment> redditCommentsNoDuplicateEntries = new List<RedditComment>();

            //var client = new PsawClient();
            //var comments = await client.Search<CommentEntry>(new SearchOptions
            var comments = await _psawService.Search<CommentEntry>(new SearchOptions
            {
                Subreddit = selected_subreddit,
                Query = "www.youtube.com/watch", // TODO: seperate out the query for the other link and score
                //Query = "www.youtube.com/watch?&q=youtu.be/", // TODO: seperate out the query for the other link and score
                Size = 100
                //After = "30d"  //s,m,h,d
                //Before = "0,0,0,30"  //s,m,h,d
            });

            foreach (var comment in comments)
            {
                var validated_link = FindYoutubeId(comment.Body);

                // if the link is empty do not include it
                if (validated_link.Equals("") || validated_link is null)
                {
                    _logger.LogDebug("invalid link, breaking loop");
                    break;
                }

                RedditComment redditComment = new RedditComment
                {
                    Subreddit = comment.Subreddit,
                    YoutubeLinkId = FindYoutubeId(comment.Body),
                    CommentLink = comment.LinkId, // TODO: not sure if this is what i think it is
                    CreatedUTC = comment.CreatedUtc,
                    Score = comment.Score,
                    RetrievedUTC = comment.RetrievedOn
                };

                redditComments.Add(redditComment);

                await _subredditContext.AddAsync(redditComment);
            }


            // TODO: can i make this more efficient? i tried using unique composite index and it would crash on second page load
            // filter out any entries that are already in the database 
            // that have the same Subreddit and YoutubeLinkId combination
            //foreach (var comment in redditComments)
            //{
            //    if (!_subredditContext.RedditComments.Any(
            //        c => c.Subreddit == comment.Subreddit && c.YoutubeLinkId == comment.YoutubeLinkId))
            //    {
            //        redditCommentsNoDuplicateEntries.Add(comment);
            //    }
            //}

            // load up the database
            //await _subredditContext.AddRangeAsync(redditCommentsNoDuplicateEntries);
            //await _subredditContext.AddRangeAsync(redditComments);

            await _subredditContext.SaveChangesAsync();

            // sort comments so that the highest scored video shows at the top
            List<RedditComment> commentsSorted = new List<RedditComment>();

            // default is desc
            if (order.Equals("desc"))
            {
                commentsSorted = redditComments.OrderByDescending(m => m.Score).ToList();
            }

            if (order.Equals("asc"))
            {
                commentsSorted = redditComments.OrderBy(m => m.Score).ToList();
            }

            return commentsSorted;
        }

        // TODO: is this worth having outside the function below?
        const string link_pattern = @"http(?:s?):\/\/(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([\w\-\]*)(&(amp;)?[\w\?‌​=]*)?";
        Regex link_regex = new Regex(link_pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // should this return multiples? do i need to change youtube linkid to be an array
        // TODO: worried about performance on the regex here?
        public string FindYoutubeId(string commentBody)
        {
            MatchCollection link_matches;
            link_matches = link_regex.Matches(commentBody);

            // TODO: can this ever be null?
            if (link_matches is null)
            {
                return "";
            }

            // TODO: what happens when there is multiple youtube links in a body? is there something to do with that
            foreach (Match match in link_matches)
            {
                if (match is null || match.Equals(""))
                {
                    return "";
                }

                // get the regex groups
                GroupCollection groups = match.Groups;

                if (groups[1].Length < 11) break;

                // trim down id, it should be a maximum of 11 characters
                return (groups[1].Length > 11) ? groups[1].Value.Remove(11) : groups[1].Value;
            }

            return "";
        }
    }
}
