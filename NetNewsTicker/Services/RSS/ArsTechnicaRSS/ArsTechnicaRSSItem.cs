using System.ServiceModel.Syndication;

namespace NetNewsTicker.Services.RSS.ArsTechnicaRSS
{
    internal class ArsTechnicaRSSItem : RSSItem
    {
        public ArsTechnicaRSSItem(SyndicationItem item) : base(item)
        {
            itemSummary = item.Summary.Text;
            hasSummary = true;
            itemCreationDate = item.PublishDate.UtcDateTime;
        }
    }
}
