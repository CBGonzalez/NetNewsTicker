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
            yClient.InitializeNetworClient();
            maxNewsPageItem = 5;
            viewIdsAndDescriptions = new List<(int, string)>() { (0, "Front page"), (1, "Newest items"), (2, "Best items"), (3, "Ask HN"), (4, "Show HN"), (5, "Jobs") };
        }       

        public override async Task<bool> RefreshItemsAsync()
        {
            isRefreshing = true;
            sourceCancel = new CancellationTokenSource();
            cancelToken = sourceCancel.Token;
            bool isSuccess = await GetNewestItemAsync(cancelToken).ConfigureAwait(false);
            if(!isSuccess)
            {                
                return (false);
            }
            if(needsRefresh && !cancelToken.IsCancellationRequested)
            {                
                newItems.Clear();
                var keepItems = new List<IContentItem>();
                var newIds = new List<int>();
                var keepIds = new List<int>();
                (bool success, List<uint> list, string error) fetchIdsResult = await FetchIdsFromCurrentPageAsync(cancelToken).ConfigureAwait(false);
                if (fetchIdsResult.success)
                {
                    int howManyToFetch = fetchIdsResult.list.Count >= itemCount ? itemCount : fetchIdsResult.list.Count;
                    for (int i = 0; i < howManyToFetch; i++)
                    {              
                        if(cancelToken.IsCancellationRequested)
                        {
                            isSuccess = false;
                            break;
                        }
                        if (!currentIds.Contains((int)fetchIdsResult.list[i]))
                        {                            
                            (bool success, YCombItem item, string error) = await yClient.GetOneItemAsync(fetchIdsResult.list[i], cancelToken).ConfigureAwait(false);
                            if (!success)
                            {
                                errorMessage = error;
                            }
                            else if(item != null)
                            {                                
                                    newItems.Add(item);
                                    newIds.Add(item.id);                                
                            }                            
                        }
                        else
                        {
                            keepItems.Add(currentItems.Find(x => x.ItemId == (int)fetchIdsResult.list[i]));                            
                            keepIds.Add((int)fetchIdsResult.list[i]);
                        }
                    }
                    if (isSuccess)
                    {
                        // recreate current items (eliminates stale ones)
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
            }
            if (sourceCancel != null)
            {
                sourceCancel.Dispose();
            }
            sourceCancel = null;            
            var e = new RefreshCompletedEventArgs(isSuccess);
            OnRefreshCompleted(e);
            isRefreshing = false;
            return isSuccess;
        }
        
        private async Task<bool> GetNewestItemAsync(CancellationToken cancel)
        {
            (bool success, uint id, _) = await yClient.GetMaxItemAsync(cancel).ConfigureAwait(false);
            if(success)
            {
                if (id != previousMaxItem)
                {
                    previousMaxItem = id;
                    needsRefresh = true;
                    Logger.Log("Refresh needed", Logger.Level.Information);
                }
                else
                {
                    Logger.Log("No refresh needed", Logger.Level.Information);
                }
            }
            return success;
        }
        
        private Task<(bool, List<uint>, string)> FetchIdsFromCurrentPageAsync(CancellationToken cancel)
        {                                             
            return yClient.FetchItemIdsForPageAsync(whichPage, cancel);
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
