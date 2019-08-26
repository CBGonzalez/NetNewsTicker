using System;
using System.ServiceModel.Syndication;

namespace NetNewsTicker.Services.RSS.RedditRSS
{
    internal class RedditRSSNetworkClient : RSSNetworkClient
    {        
        public RedditRSSNetworkClient() : base()
        {
            newsServerBase = new Uri("https://www.reddit.com");
            client.BaseAddress = newsServerBase;
            logFileName = "NewsTicker-RSS.txt";
            rssTail = "/new/.rss?sort=new";            
            InitializeNetworClient();
        }

        internal override void ParseContent(SyndicationFeed feed)
        {
            RedditRSSItem oneItem;
            foreach (SyndicationItem item in feed.Items)
            {
                oneItem = new RedditRSSItem(item);
                if (oneItem != null)
                {
                    newContent.Add(oneItem);
                }
            }
        }
    }
}
