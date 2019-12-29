using System.Collections.Generic;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS.ArsTechnicaRSS
{
    public class ArsTechnicaRSSNewsService : RSSNewsService
    {
        public new enum NewsPage { FrontPage = 0 }; // specific for ArsTechnica RSS service


        public ArsTechnicaRSSNewsService(bool logEnabled) : base(logEnabled)
        {
            nwClient = new ArsTechnicaRSSNetworkClient(logEnabled);

            viewIdsAndDescriptions = new List<(int, string)>() { ((int)NewsPage.FrontPage, "All stories")};
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.FrontPage;
        }

        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            whichPage = nPage switch
            {
                NewsPage.FrontPage => "arstechnica/index",                
                _ => "/index",
            };
        }
    }
}
