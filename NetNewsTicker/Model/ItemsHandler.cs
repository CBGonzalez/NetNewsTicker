using System;
using System.Collections.Generic;
using NetNewsTicker.Services;


namespace NetNewsTicker.Model
{
    class ItemsHandler : IDisposable
    {
        private bool disposedValue = false; // To detect redundant calls of Dispose()        
        private int currentCategory = 0;
        private List<IContentItem> newContent;
        private List<IContentItem> allContent;
        private List<(int, string)> allCategories;
        private readonly List<(int, string)> allServices;
        private EventHandler<RefreshCompletedEventArgs> itemsRefreshCompleted, itemsRefreshStarted;
        private bool hasNewItems;
        private ITickerCommunicationService tickerService;
        private int refreshSeconds;
        private int currentService;
        private readonly int maxService;
        private bool logEnabled;
        private string logPath = string.Empty;
        private readonly Dictionary<int, List<string>> allServicesPages;

        public string LogPath => logPath;
        public bool HasNewItems => hasNewItems;
        public List<IContentItem> NewContent => newContent;
        public List<IContentItem> AllContent => allContent;
        public int CurrentCategory => currentCategory;
        public List<(int, string)> AllCategories => allCategories;
        public Dictionary<int, List<string>> AllServicesPages => allServicesPages;

        public List<(int, string)> AllServices => allServices;

        public event EventHandler<RefreshCompletedEventArgs> ItemsRefreshStartedHandler
        {
            add
            {
                itemsRefreshStarted += value;
            }
            remove
            {
                itemsRefreshStarted -= value;
            }
        }

        public event EventHandler<RefreshCompletedEventArgs> ItemsRefreshCompletedHandler
        {
            add
            {
                itemsRefreshCompleted += value;
            }
            remove
            {
                itemsRefreshCompleted -= value;
                if (itemsRefreshCompleted == null)
                {
                    tickerService.StopRefreshing();
                }
            }
        }

        public bool IsServiceRefreshing => tickerService.IsRefreshing;

        public ItemsHandler(int refreshIntervalSeconds, bool useLogging, int selectedService = 0, int selectedPage = 0)
        {
            logEnabled = useLogging;
            hasNewItems = false;
            allServices = new List<(int, string)>();
            allServicesPages = ServiceSelector.ServicesItems;
            foreach (KeyValuePair<int, string> s in ServiceSelector.ServiceList)
            {
                allServices.Add((s.Key, s.Value));
            }
            currentService = selectedService;
            currentCategory = selectedPage;
            maxService = allServices.Count - 1;
            tickerService = ServiceSelector.CreateService(currentService, logEnabled);
            logPath = tickerService.LogPath;
            allContent = tickerService.GetAllItemsList();
            newContent = tickerService.GetNewItemsList();
            if (tickerService.HasDifferentCategories)
            {
                allCategories = tickerService.ViewIdsAndDescriptions;
            }
            tickerService.RefreshCompletedHandler += TickerService_RefreshCompletedHandler;
            tickerService.RefreshStartedHandler += TickerService_RefreshStartedHandler;
            refreshSeconds = refreshIntervalSeconds;
            tickerService.StartRefreshing(refreshSeconds, currentCategory);
        }

        private void TickerService_RefreshStartedHandler(object sender, RefreshCompletedEventArgs e)
        {
            OnRefreshStarted(e);
        }

        private void TickerService_RefreshCompletedHandler(object sender, RefreshCompletedEventArgs e)
        {
            if (e.HasNewItems)
            {
                hasNewItems = true;
                OnRefreshCompleted(e);
            }
            else
            {
                hasNewItems = false;
            }
        }

        protected virtual void OnRefreshStarted(RefreshCompletedEventArgs e)
        {
            EventHandler<RefreshCompletedEventArgs> handler = itemsRefreshStarted;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRefreshCompleted(RefreshCompletedEventArgs e)
        {
            EventHandler<RefreshCompletedEventArgs> handler = itemsRefreshCompleted;
            handler?.Invoke(this, e);
        }

        public bool RefreshItems()
        {
            allContent = tickerService.GetAllItemsList();
            return true;
        }

        public void PauseRefresh()
        {
            tickerService.PauseRefreshing();
        }

        public void ResumeRefreshing()
        {
            tickerService.ResumeRefreshing();
        }

        public void ControlLogging(bool doLoggings)
        {
            if (doLoggings != logEnabled)
            {
                logEnabled = doLoggings;
                tickerService.ChangeLogging(logEnabled);
                logPath = tickerService.LogPath;
            }
        }

        public void Close()
        {
            tickerService.StopRefreshing();
            Dispose();
        }

        public void ChangeCurrentCategory(int newPage)
        {
            if (newPage != currentCategory)
            {
                currentCategory = newPage;
                tickerService.ChangeContentCategory(newPage);
                tickerService.ImmediateRefresh();
            }
        }

        public void ChangeCurrentService(int newService, int whichPage = 0, bool enableLogging = false)
        {
            if (newService <= maxService && currentService != newService)
            {
                logEnabled = enableLogging;
                currentCategory = whichPage;
                hasNewItems = false;
                currentService = newService;
                tickerService = ServiceSelector.CreateService(newService, logEnabled);
                logPath = tickerService.LogPath;
                allContent = tickerService.GetAllItemsList();
                newContent = tickerService.GetNewItemsList();
                if (tickerService.HasDifferentCategories)
                {
                    allCategories = tickerService.ViewIdsAndDescriptions;
                }
                tickerService.ChangeContentCategory(currentCategory);
                tickerService.RefreshCompletedHandler += TickerService_RefreshCompletedHandler;
                tickerService.RefreshStartedHandler += TickerService_RefreshStartedHandler;
                tickerService.StartRefreshing(refreshSeconds, currentCategory);
            }
        }

        public void ChangeRefreshInterval(int newIntervalSeconds)
        {
            if (newIntervalSeconds != refreshSeconds)
            {
                refreshSeconds = newIntervalSeconds;
                tickerService.ChangeDesiredRefreshInterval(refreshSeconds);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && tickerService != null)
                {
                    tickerService.StopRefreshing();
                    tickerService.Dispose();
                    tickerService = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
