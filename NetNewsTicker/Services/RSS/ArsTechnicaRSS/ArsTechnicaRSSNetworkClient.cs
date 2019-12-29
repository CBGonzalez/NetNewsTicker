using System;
using System.ServiceModel.Syndication;

namespace NetNewsTicker.Services.RSS.ArsTechnicaRSS
{
    internal class ArsTechnicaRSSNetworkClient : RSSNetworkClient
    {
        public ArsTechnicaRSSNetworkClient(bool enableLogging) : base(enableLogging)
        {
            newsServerBase = new Uri("http://feeds.arstechnica.com");
            rssTail = string.Empty;
            client.BaseAddress = newsServerBase;
            logFileName = "ArsTechnicaTicker-RSS.txt";
            InitializeNetworClient(isLoggingEnabled);
        }

        internal override void ParseContent(SyndicationFeed feed)
        {
            ArsTechnicaRSSItem oneItem;
            foreach (SyndicationItem item in feed.Items)
            {
                oneItem = new ArsTechnicaRSSItem(item);
                if (oneItem != null)
                {
                    newContent.Add(oneItem);
                }
            }
        }
    }
}
