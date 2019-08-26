using System;

namespace NetNewsTicker.Model
{
    public interface IContentItem : IEquatable<IContentItem>
    {
        string ItemHeadline { get; }

        string ItemSummary { get; }
        int ItemId { get; }
        bool HasLink { get; }

        bool HasSummary { get; }
        string Link { get; }
        string SecondaryLink { get; }
        bool HasSubItems { get; }
        Memory<int> SubItems { get; }
        DateTime ItemCreationDate { get; }       
    }
}
