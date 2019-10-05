using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS
{
    public abstract class RSSNewsService : TickerCommunicationServiceBase
    {
        public RSSNewsService(bool useLogging) : base(useLogging)
        {
            //nwClient = new RSSNetworkClient();            
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.Front;
        }

        public override async Task<bool> RefreshItemsAsync()
        {
            sourceCancel = new CancellationTokenSource();
            cancelToken = sourceCancel.Token;
            var keepItems = new List<IContentItem>();
            newItems.Clear();
            (bool success, List<IContentItem> list, string error) = await nwClient.FetchAllItemsAsync(whichPage, nwClient.MaxItems, cancelToken).ConfigureAwait(false);
            if (!success)
            {
                errorMessage = error;
                return (success);
            }
            foreach (RSSItem item in list)
            {
                if (currentItems.Contains(item))
                {
                    keepItems.Add(item);
                }
                else
                {
                    newItems.Add(item);
                }
            }
            currentItems.Clear();
            foreach (RSSItem item in keepItems)
            {
                currentItems.Add(item);
            }
            foreach (RSSItem item in newItems)
            {
                currentItems.Add(item);
            }
            if (sourceCancel != null)
            {
                sourceCancel.Dispose();
            }
            sourceCancel = null;
            var e = new RefreshCompletedEventArgs(success);
            OnRefreshCompleted(e);
            isRefreshing = false;
            return success;
        }

    }
}
