using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    public class YComNewsService : TickerCommunicationServiceBase
    {
        public new enum NewsPage { Front = 0, Newest = 1, Best = 2, Ask = 3, Show = 4, Job = 5 };             
        private readonly YCombNetworkClient yClient;
        
        public YComNewsService() : base()
        {
            nwClient = new YCombNetworkClient();
            yClient = (YCombNetworkClient)nwClient;
            yClient.InitializeNetworClient(enableLogging);
            logPath = yClient.LogLocation;
            maxNewsPageItem = 5;
            viewIdsAndDescriptions = new List<(int, string)>() { (0, "Front page"), (1, "Newest items"), (2, "Best items"), (3, "Ask HN"), (4, "Show HN"), (5, "Jobs") };
        }   
        
        public YComNewsService(bool useLogging) : this()
        {
            enableLogging = useLogging;
        }

        public override async Task<bool> RefreshItemsAsync()
        {
            isRefreshing = true;
            sourceCancel = new CancellationTokenSource();
            cancelToken = sourceCancel.Token;
            bool isOK = false;
            try
            {
                (bool isSucces, List<IContentItem> list, string error) = await yClient.FetchAllItemsAsync(whichPage, itemCount, cancelToken);
                isOK = isSucces;
                errorMessage = error;
                if (isOK && !cancelToken.IsCancellationRequested)
                {
                    newItems.Clear();
                    var keepItems = new List<IContentItem>();
                    var newIds = new List<int>();
                    var keepIds = new List<int>();

                    int howManyToFetch = list.Count >= itemCount ? itemCount : list.Count;
                    var newIdsList = new List<int>(howManyToFetch);

                    for (int i = 0; i < howManyToFetch; i++)
                    {
                        newIdsList.Add(list[i].ItemId);
                    }
                    for (int i = 0; i < howManyToFetch; i++)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            isOK = false;
                            break;
                        }
                        if (!currentIds.Contains(newIdsList[i]))
                        {
                            newItems.Add(list[i]);
                            newIds.Add(newIdsList[i]);
                        }
                        else
                        {
                            keepItems.Add(list[i]);
                            keepIds.Add(newIdsList[i]);
                        }
                    }

                    currentItems.Clear();
                    currentIds.Clear();
                    foreach (IContentItem item in keepItems)
                    {
                        currentItems.Add(item);
                        currentIds.Add(item.ItemId);
                    }
                    foreach (IContentItem item in newItems)
                    {
                        currentItems.Add(item);
                        currentIds.Add(item.ItemId);
                    }
                }
            }
            catch(TaskCanceledException te)
            {
                errorMessage = $"{errorMessage}: {te.ToString()}";
                Logger.Log(errorMessage, Logger.Level.Error);
            }
            if (sourceCancel != null)
            {
                sourceCancel.Dispose();
            }
            sourceCancel = null;            
            var e = new RefreshCompletedEventArgs(isOK);
            OnRefreshCompleted(e);
            isRefreshing = false;
            return isOK;
        }
        
        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            switch (nPage)
            {
                case NewsPage.Front:
                    whichPage = "topstories";
                    break;
                case NewsPage.Newest:
                    whichPage = "newstories";
                    break;
                case NewsPage.Best:
                    whichPage = "beststories";
                    break;
                case NewsPage.Ask:
                    whichPage = "askstories";
                    break;
                case NewsPage.Show:
                    whichPage = "showstories";
                    break;
                case NewsPage.Job:
                    whichPage = "jobstories";
                    break;
            }
        }
    }

}
