using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS
{
    public class RSSNewsService : TickerCommunicationServiceBase
    {
        public new enum NewsPage { Front = 0, DotNetDeveloper = 1 }; // specific for Reddit RSS service


        public RSSNewsService() : base()
        {
            nwClient = new RSSNetworkClient();
            viewIdsAndDescriptions = new List<(int, string)>() { (0, "Front page"), (1, "dotnetdeveloper") };
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.DotNetDeveloper;
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

        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            switch (nPage)
            {
                case NewsPage.Front:
                    whichPage = "/";
                    break;                
            }
        }
    }
}
