using System;
using System.Collections.Generic;
using NetNewsTicker.Services.RSS.BBCNewsRSS;
using NetNewsTicker.Services.RSS.RedditRSS;

namespace NetNewsTicker.Services
{
    public static class ServiceSelector
    {
        private static readonly Dictionary<int, string> serviceList = new Dictionary<int, string>() { { 0, "Hacker News" }, { 1, "Reddit" }, { 2, "BBC News" } };
        private static readonly Dictionary<int, List<string>> servicesItems;
        private static ITickerCommunicationService service = null;
        private const int maxServiceIndex = 2;

        static ServiceSelector()
        {
            servicesItems = new Dictionary<int, List<string>>();
            // populate pages for all services so we can display in settings window
            foreach (KeyValuePair<int, string> kvp in serviceList)
            {
                ITickerCommunicationService dummyService = CreateService(kvp.Key, false);
                var items = new List<string>();
                foreach ((int, string description) s in dummyService.ViewIdsAndDescriptions)
                {
                    items.Add(s.description);
                }
                servicesItems.Add(kvp.Key, items);
                dummyService.Dispose();
            }
        }

        public static Dictionary<int, string> ServiceList => serviceList;
        public static Dictionary<int, List<string>> ServicesItems => servicesItems;

        public static ITickerCommunicationService CreateService(int whichService, bool useLogging)
        {
            if (whichService > maxServiceIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(whichService));
            }
            if (service != null)
            {
                service.StopRefreshing();
                service.Dispose();
                service = null;
            }
            switch (whichService)
            {
                case 0:
                    service = new YComNewsService(useLogging);
                    break;
                case 1:
                    service = new RedditRSSNewsService(useLogging);
                    break;
                case 2:
                    service = new BBCNewsRSSNewsService(useLogging);
                    break;
            }
            return service;
        }
    }
}
