using System.ServiceModel.Syndication;

namespace NewsServices
{
    internal class RedditRSSItem : RSSItem
    {        

        public RedditRSSItem(SyndicationItem item) : base(item)
        {                                        
            itemCreationDate = item.LastUpdatedTime.UtcDateTime;
            hasSummary = false;
            itemSummary = string.Empty;
        }
        
    }
}
