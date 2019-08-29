using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    public abstract class TickerCommunicationServiceBase : ITickerCommunicationService
    {
        protected enum NewsPage { Front = 0 };
        private protected int maxNewsPageItem = 0;
        private protected List<(int, string)> viewIdsAndDescriptions;
        private protected bool disposedValue = false; // To detect redundant calls to Dispose()
        private protected bool doRefreshing;
        private protected readonly List<int> currentIds;
        private protected int itemCount = 50;
        private protected uint previousMaxItem;
        private protected string errorMessage;
        private protected bool needsRefresh, hasDifferentCategories = true;
        private protected Timer timer = null;
        private protected bool isRefreshing;
        private protected CancellationTokenSource sourceCancel = null;
        private protected CancellationToken cancelToken;
        private protected EventHandler<RefreshCompletedEventArgs> refreshStarted, refreshCompleted;
        private protected NewsPage currentPage = NewsPage.Front;
        private protected string whichPage = string.Empty;
        private protected List<IContentItem> currentItems, newItems;
        private protected int isTimerCBRunning = 0;
        private protected int currentRefresh;
        private protected Delegate RefreshDelegate;
        private protected INetworkClient nwClient;

        public string LastError => errorMessage;
        public bool IsRefreshing => isRefreshing;

        public TickerCommunicationServiceBase()
        {
            currentItems = new List<IContentItem>(itemCount);
            newItems = new List<IContentItem>();
            currentIds = new List<int>();
            for (int i = 0; i < itemCount; i++)
            {
                currentIds.Add(0);
            }
            previousMaxItem = 0;
            needsRefresh = true;
            isRefreshing = false;
            doRefreshing = true;
        }

        public bool HasDifferentCategories => hasDifferentCategories;

        public List<(int, string)> ViewIdsAndDescriptions => viewIdsAndDescriptions;
        
        public event EventHandler<RefreshCompletedEventArgs> RefreshStartedHandler
        {
            add
            {
                refreshStarted += value;
            }
            remove
            {
                refreshStarted -= value;
            }
        }

        public event EventHandler<RefreshCompletedEventArgs> RefreshCompletedHandler
        {
            add
            {
                refreshCompleted += value;
            }
            remove
            {
                refreshCompleted -= value;
                if (refreshCompleted == null)
                {
                    StopRefreshing();
                }
            }
        }

        protected virtual void OnRefreshStarted(RefreshCompletedEventArgs e)
        {
            EventHandler<RefreshCompletedEventArgs> handler = refreshStarted;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRefreshCompleted(RefreshCompletedEventArgs e)
        {
            EventHandler<RefreshCompletedEventArgs> handler = refreshCompleted;
            handler?.Invoke(this, e);
        }

        public virtual bool ChangeContentCategory(int newCategory)
        {
            bool isOk = false;
            if (newCategory <= maxNewsPageItem)
            {
                currentPage = (NewsPage)newCategory;
                SetCorrectUrl(newCategory);
                isOk = true;
            }
            return isOk;
        }

        public bool ChangeDesiredRefreshInterval(int refreshIntervalSeconds)
        {
            if (timer == null)
            {
                return false;
            }
            bool success = timer.Change(currentRefresh * 1000, refreshIntervalSeconds * 1000);
            currentRefresh = refreshIntervalSeconds;
            return success;
        }

        public List<IContentItem> GetAllItemsList()
        {
            return currentItems;
        }

        public List<IContentItem> GetNewItemsList()
        {
            return newItems;
        }

        public async void ImmediateRefresh()
        {
            while (isTimerCBRunning != 0)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            timer.Change(1000, currentRefresh * 1000);
        }

        public void PauseRefreshing()
        {
            doRefreshing = false;
        }

        public void ResumeRefreshing()
        {
            doRefreshing = true;
        }

        public void StopRefreshing()
        {
            if (timer == null)
            {
                return;
            }
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            currentRefresh = -1;
            if (isRefreshing && sourceCancel != null)
            {
                sourceCancel.Cancel();
            }
        }

        public (bool success, string errorMessage) StartRefreshing(int refreshIntervalSeconds, int category)
        {
            string error = string.Empty;
            currentRefresh = refreshIntervalSeconds;
            if (timer != null)
            {
                return (false, "Refresh already in progress.");
            }
            timer = new Timer(new TimerCallback(TimerCB));
            if (category <= maxNewsPageItem)
            {
                currentPage = (NewsPage)category;
            }
            else
            {
                currentPage = NewsPage.Front;
                error = "Invalid category, using Front Page";
            }
            SetCorrectUrl((int)currentPage);
            bool success = timer.Change(1000, refreshIntervalSeconds * 1000);
            return (success, error);
        }

        internal abstract void SetCorrectUrl(int page);        
       
        private protected virtual async void TimerCB(Object stateInfo)
        {
            bool stillRunning = Interlocked.CompareExchange(ref isTimerCBRunning, 1, 0) != 0;
            if (stillRunning)
            {
                return;
            }
            if (isRefreshing | !doRefreshing)
            {
                return;
            }
            OnRefreshStarted(null);
            bool _ = await RefreshItemsAsync().ConfigureAwait(false);
            Interlocked.Exchange(ref isTimerCBRunning, 0);
        }

        public abstract Task<bool> RefreshItemsAsync();
        
        
        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {                                        
                    if (timer != null)
                    {
                        timer.Dispose();
                        timer = null;
                    }
                    if (sourceCancel != null)
                    {
                        sourceCancel.Cancel();
                        sourceCancel.Dispose();
                        sourceCancel = null;
                    }
                    if (nwClient != null)
                    {
                        nwClient.Dispose();
                        nwClient = null;
                    }
                }                
                disposedValue = true;
            }
        }        

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {            
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
