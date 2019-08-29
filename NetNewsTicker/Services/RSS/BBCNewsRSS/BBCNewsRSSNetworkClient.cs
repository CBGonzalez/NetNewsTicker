using System;
using System.ServiceModel.Syndication;

namespace NetNewsTicker.Services.RSS.BBCNewsRSS
{
    internal class BBCNewsRSSNetworkClient : RSSNetworkClient
    {        
        public BBCNewsRSSNetworkClient() : base()
        {
            newsServerBase = new Uri("https://feeds.bbci.co.uk");
            rssTail = "/rss.xml";
            client.BaseAddress = newsServerBase;
            logFileName = "BBCNewsTicker-RSS.txt";            
            InitializeNetworClient(isLoggingEnabled);
        }        

        internal override void ParseContent(SyndicationFeed feed)
        {
            BBCNewsRSSItem oneItem;
            foreach (SyndicationItem item in feed.Items)
            {
                oneItem = new BBCNewsRSSItem(item);
                if (oneItem != null)
                {
                    newContent.Add(oneItem);
                }
            }
        }
    }
}
