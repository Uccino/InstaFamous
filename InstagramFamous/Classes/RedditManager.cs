using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramFamous.Classes
{
    class RedditManager
    {
        // Reddit / Headers / Download path
        private string _subreddit;
        private string _directoryName;
        private int _upvoteThreshold;

        public string Subreddit
        {
            get
            {
                return _subreddit;
            }
            set
            {
                if (value.Contains("/r/"))
                {
                    value = value.Replace("/r/", "");
                    
                }
                _subreddit = value;
            }
        }
        public string DirectoryName
        {
            get
            {
                return _directoryName;
            }
            set
            {
                _directoryName = Regex.Replace(value, "[^0-9a-zA-Z]+", "");
            }
        }
        public int UpvoteThreshold
        {
            get
            {
                return _upvoteThreshold;
            }
            set
            {
                _upvoteThreshold = value;
            }
        }

        public RedditManager()
        {
            // Get all the reddit related settings and set them.
            Subreddit       = Properties.Config.Default.RedditSubreddit;
            DirectoryName   = Properties.Config.Default.RedditSubreddit;
            UpvoteThreshold = int.Parse(Properties.Config.Default.RedditUpvoteRequirement);
        }

        /// <summary>
        /// Create a webrequest to reddit and get the hottest posts from a certain subreddit
        /// </summary>
        /// <returns></returns>
        public dynamic GetPosts()
        {
            // Prepare an URI and send a webrequest to http://reddit.com/
            Uri redditUrl = BuildUrl(Subreddit);
            WebRequest redditRequest = WebRequest.Create(redditUrl);
            HttpWebResponse redditResponse = (HttpWebResponse)redditRequest.GetResponse();

            if (redditResponse.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    // Get the stream containing content returned by the server.
                    Stream dataStream = redditResponse.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    dynamic redditPosts = JsonConvert.DeserializeObject(responseFromServer);

                    return redditPosts;
                }
                catch (IOException ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new WebException("Couldn't retrieve the JSON ");
            }
        }

        /// <summary>
        /// Filter the posts based on a upvote requirement.
        /// </summary>
        /// <param name="posts"></param>
        /// <returns></returns>
        public List<Dictionary<string, string>> FilterPosts(dynamic posts)
        {
            List<dynamic> approvedPosts = new List<dynamic>();

            // Loop through the posts and check the "score", if its higher than or equal to the threshold we add it to the list for later download purposes.
            foreach (dynamic post in posts)
            {
                int postScore = int.Parse(post["data"]["ups"].ToString());
                if (postScore >= UpvoteThreshold && post["data"]["is_video"] != "true")
                {
                    approvedPosts.Add(post);
                }
            }

            List<Dictionary<string, string>> lstApprovedPosts = new List<Dictionary<string, string>>();
            
            foreach(dynamic post in approvedPosts)
            {
                string postTitle    = post["data"]["title"];
                string postLink     = post["data"]["url"];
                postTitle = Regex.Replace(postTitle, "[^0-9a-zA-Z ]+", "");

                Dictionary<string, string> dictPostInfo = new Dictionary<string, string>
                {
                    { "Title", postTitle },
                    { "Link", postLink }
                };

                lstApprovedPosts.Add(dictPostInfo);


            }

            return lstApprovedPosts;
        }

        /// <summary>
        /// Returns a reddit uri
        /// </summary>
        /// <param name="subreddit">Subreddit to build the uri for.</param>
        /// <returns></returns>
        private Uri BuildUrl(string subreddit)
        {
            string url = $"https://reddit.com/r/{subreddit}/hot.json?count=50";
            Uri redditUrl = new Uri(url);
            return redditUrl;
        }
    }
}
