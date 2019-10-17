using NetNewsTicker.Model;
using System;
using System.Globalization;

namespace NetNewsTicker.Services
{

    public class YCombItem : IContentItem
    {
        private const string hackerComments = "https://news.ycombinator.com/item?id=";

        #region JSON stuff
#pragma warning disable IDE1006        
        public int id { get; set; }

        public bool deleted { get; set; }

        public string type { get; set; }

        public string by { get; set; }

        public long time { get; set; }

        public string text { get; set; }

        public bool dead { get; set; }

        public int parent { get; set; }

        public int poll { get; set; }
#pragma warning disable CA1819        
        public int[] kids { get; set; }
#pragma warning restore CA1819        
        public string url { get; set; }
        public uint score { get; set; }

        public string title { get; set; }
#pragma warning disable CA1819        
        public uint[] parts { get; set; }
#pragma warning restore CA1819

        public uint descendants { get; set; }
#pragma warning restore IDE1006
        #endregion

        public DateTimeOffset UnTime => DateTimeOffset.FromUnixTimeSeconds(time);

        public int ItemId => id;
        public bool HasLink => true; //url != null; TODO clean up
        public string Link => url ?? $"{hackerComments}{id.ToString(CultureInfo.InvariantCulture)}";
        public string SecondaryLink => $"{hackerComments}{id.ToString(CultureInfo.InvariantCulture)}";
        public bool HasSubItems => kids != null;

        public bool HasSummary => false;
        public string ItemHeadline => HasSubItems ? $"({kids.Length}) {title}" : $"(0) {title}";
        public string ItemSummary => string.Empty;
        public Memory<int> SubItems => kids.AsMemory();
        public DateTime ItemCreationDate => UnTime.UtcDateTime;

        public YCombItem()
        {

        }

        public bool Equals(IContentItem other)
        {
            if (other == null)
            {
                return false;
            }
            return other.ItemId == ItemId;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is IContentItem objAsItem))
            {
                return false;
            }
            return Equals(objAsItem);
        }

        public override int GetHashCode()
        {
            return ItemId;
        }
    }
}
