using System;
using System.Collections.Generic;
using NetNewsTicker.Services.RSS.BBCNewsRSS;
using NetNewsTicker.Services.RSS.RedditRSS;

namespace NetNewsTicker.Services
{
    public static class ServiceSelector
    {
        private static readonly Dictionary<int, string> serviceList = new Dictionary<int, string>() { { 0, "Hacker News" }, { 1, "Reddit" }, { 2, "BBC News" } };
        private static ITickerCommunicationService service = null;
        private const int maxServiceIndex = 2;

        public static Dictionary<int, string> ServiceList => serviceList;

        public static ITickerCommunicationService CreateService(int whichService)
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
            switch(whichService)
            {
                case 0:
                    service = new YComNewsService();
                    break;
                case 1:
                    service = new RedditRSSNewsService();
                    break;
                case 2:
                    service = new BBCNewsRSSNewsService();
                    break;
            }
            return service;
        }
    }
}
