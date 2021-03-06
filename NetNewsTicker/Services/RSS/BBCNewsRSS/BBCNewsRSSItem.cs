﻿using System.ServiceModel.Syndication;

namespace NetNewsTicker.Services.RSS.BBCNewsRSS
{
    internal class BBCNewsRSSItem : RSSItem
    {
        public BBCNewsRSSItem(SyndicationItem item) : base(item)
        {
            itemSummary = item.Summary.Text;
            hasSummary = true;
            itemCreationDate = item.PublishDate.UtcDateTime;
        }
    }
}
