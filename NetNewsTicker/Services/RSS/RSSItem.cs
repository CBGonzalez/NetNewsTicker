using System;
using System.ServiceModel.Syndication;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS
{
    public class RSSItem : IContentItem
    {
        private protected string link, secondaryLink;
        private protected bool hasLink;
        private protected bool hasSummary;
        private protected string itemHeadline, itemSummary;
        private protected DateTime itemCreationDate;

        public string ItemHeadline => itemHeadline;
        public string ItemSummary => itemSummary;
        public int ItemId => -1;
        public bool HasLink => hasLink;
        public bool HasSummary => hasSummary;
        public string Link => link;
        public string SecondaryLink => secondaryLink;
        public bool HasSubItems => false;
        public Memory<int> SubItems => null;
        public DateTime ItemCreationDate => itemCreationDate;

        public RSSItem(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }            
            itemHeadline = item.Title.Text;
            if (item.Links.Count > 0)
            {
                link = item.Links[0].Uri.ToString();
                hasLink = true;
            }
            else
            {
                hasLink = false;
                link = string.Empty;
            }            
        }

        public bool Equals(IContentItem other)
        {
            if (other != null)
            {
                return itemHeadline.Equals(other.ItemHeadline, StringComparison.InvariantCulture);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object other)
        {
            return other is IContentItem otherItem ? Equals(otherItem) : false;
        }

        public override int GetHashCode()
        {
            return itemHeadline.GetHashCode();

        }
    }
}
