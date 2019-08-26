using System;

namespace NetNewsTicker.Model
{
    public class RefreshCompletedEventArgs : EventArgs
    {
        public bool HasNewItems { get; }        

        public RefreshCompletedEventArgs(bool hasNewItems)
        {
            HasNewItems = hasNewItems;            
        }
    }
}
