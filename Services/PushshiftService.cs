using luke_site_mvc.Data;
using luke_site_mvc.Data.Entities;
using luke_site_mvc.Models.PsawSearchOptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using luke_site_mvc.Data.Entities.PsawEntries;

namespace luke_site_mvc.Services
{
    public interface IPushshiftService
    {
        string FindYoutubeId(string commentBody);
        Task GetLinksFromCommentsAsync();
        List<string> GetSubreddits();
        Task GetUniqueRedditComments(List<string> subreddit, int daysToGet, int numEntriesPerDay);
    }
    public class PushshiftService : IPushshiftService
    {
        private readonly ILogger<PushshiftService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IPsawService _psawService;
        private readonly ISubredditRepository _subredditRepository;

        public PushshiftService(ILogger<PushshiftService> logger, IDistributedCache distributedCache, IPsawService psawService, ISubredditRepository subredditRepository)
        {
            _logger = logger;
            _cache = distributedCache;
            _psawService = psawService;
            _subredditRepository = subredditRepository;
        }

        // TODO: provide a better list of subreddits
        // avoid magic strings
        private readonly List<string> _subreddits = new()
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
            "woodworking",
            "medicine",
            "ultralight",
            "travel",
            "askHistorians",
            "camping",
            "cats",
            "cpp",
            "chemistry",
            "beer",
            "whisky",
            "games",
            "moviesuggestions",
            "utarlington",
            "docker",
            "dotnetcore",
            "math",
            "askculinary",
            "tesla",
            "nintendoswitch",
            "diy",
            "aww",
            "history",
            "youtube",
            "askscience",
            "dallas",
            "galveston",
            "arlington",
            "programming",
            "arch",
            "buildapcsales",
            "cooking",
            "hunting",
            "askculinary",
            "blender",
            "CC0Textures",
            "DigitalArt",
            "blenderTutorials",
            "computergraphics",
            "3Dmodeling",
            "blenderhelp",
            "cgiMemes",
            "learnblender",
            "blenderpython",
            "low_poly",
        };

            // shorter list while dealing with rate limit problems
            //private readonly List<string> _subreddits = new()
            //{
            //    "homeimprovement",
            //    "woodworking",
            //    "medicine",
            //    "ultralight",
            //    "travel",
            //    "askculinary",
            //    "space",
            //    "skookum",
            //    "AIDKE",
            //    "linux",
            //    "dotnet",
            //    "csharp",
            //    "biology",
            //    "aviation",
            //    "homebrewing",
            //    "fantasy",
            //    "whisky",
            //    "docker",
            //    "math",
            //    "history",
            //    "mealtimevideos",
            //    "movies",
            //    "gaming",
            //    "youtube",
            //};

        public List<string> GetSubreddits()
        {
            var validSubreddits = new List<string>();

            // only return subreddits that have any content
            foreach (var subreddit in _subreddits)
            {
                if (_subredditRepository.GetSubredditLinkCount(subreddit) > 0)
                    validSubreddits.Add(subreddit);
            }

            // return list in alphabetical order
            return validSubreddits.OrderBy(x => x).ToList();
        }

        public async Task GetLinksFromCommentsAsync()
            => await GetUniqueRedditComments(_subreddits, daysToGet: 365, numEntriesPerDay: 100);

        public async Task GetUniqueRedditComments(List<string> subreddit, int daysToGet, int numEntriesPerDay)
        {
            if (subreddit is null) throw new NullReferenceException(nameof(subreddit));

            var redditComments = new List<RedditComment>();

            var subredditCsv = String.Join(",", subreddit.Select(x => x).ToArray());

            // going by hour gets more detailed results
            var daysToGetInHours = daysToGet * 24;
            for (var i = 0; i < daysToGetInHours; i++)
            {
                string before = ((daysToGetInHours - i)).ToString() + "h";
                string after = ((daysToGetInHours + 1) - i).ToString() + "h";

                var rawComments = await _psawService.Search<CommentEntry>(new SearchOptions
                {
                    Subreddit = subredditCsv,
                    Query = "www.youtube.com/watch", // TODO: seperate out the query for the other link and score
                    Before = before,
                    After = after,
                    Size = numEntriesPerDay
                });

                _logger.LogInformation($"{i} out of {daysToGetInHours}\tFetched {rawComments.Count()}\tBefore:{daysToGetInHours - i} After:{(daysToGetInHours + 1) - i}");

                foreach (var comment in rawComments)
                {
                    // check to make sure comment body has a valid YoutubeLinkId
                    var validatedLink = FindYoutubeId(comment.Body);

                    // if not valid YoutubeLinkId then do not continue
                    if (string.IsNullOrEmpty(validatedLink)) break;

                    // load up RedditComment with data from the API response
                    var redditComment = new RedditComment
                    {
                        Subreddit = comment.Subreddit,
                        YoutubeLinkId = FindYoutubeId(comment.Body), // use regex to pull youtubeId from comment body
                        CreatedUTC = comment.CreatedUtc,
                        Score = comment.Score,
                        RetrievedUTC = comment.RetrievedOn,
                        Permalink = comment.Permalink
                    };

                    redditComments.Add(redditComment);
                }

                _subredditRepository.SaveUniqueComments(redditComments.Distinct().ToList());

                // clear the list to avoid keeping too much in memory
                redditComments.Clear();
            }
        }

        private const string YoutubeLinkIdRegexPattern = @"http(?:s?):\/\/(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([\w\-\]*)(&(amp;)?[\w\?‌​=]*)?";

        // worth compiling because this regex is used so heavily
        private readonly Regex _youtubeLinkIdRegex = new(YoutubeLinkIdRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // should this return multiples? do i need to change youtube linkid to be an array
        // TODO: worried about performance on the regex here?
        public string FindYoutubeId(string commentBody)
        {
            if (string.IsNullOrEmpty(commentBody)) return string.Empty;

            var linkMatches = _youtubeLinkIdRegex.Matches(commentBody);

            // TODO: what happens when there is multiple youtube links in a body? is there something to do with that
            foreach (Match match in linkMatches)
            {
                if (match is null || match.Equals(""))
                {
                    // TODO: how to get method name in log message?
                    _logger.LogTrace("FindYoutubeId(string commentBody) | invalid link, breaking loop");
                    return "";
                }
                GroupCollection groups = match.Groups;

                if (match.Groups[1].Length < 11) return string.Empty;

                // trim down id, it should be a maximum of 11 characters
                return (groups[1].Length > 11) ? groups[1].Value.Remove(11) : groups[1].Value;
            }

            return string.Empty;
        }
    }
}
