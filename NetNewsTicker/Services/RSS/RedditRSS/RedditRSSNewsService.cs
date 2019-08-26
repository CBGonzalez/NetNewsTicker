using System.Collections.Generic;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS.RedditRSS
{
    public class RedditRSSNewsService : RSSNewsService
    {
        public new enum NewsPage  { Front = 0, DotNetDeveloper = 1 }; // specific for Reddit RSS service        

        public RedditRSSNewsService() : base()
        {
            nwClient = new RedditRSSNetworkClient();            
            viewIdsAndDescriptions = new List<(int, string)>() { (0, "Front page"), (1, "dotnetdeveloper") };
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.DotNetDeveloper;
        }
        
        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            switch (nPage)
            {
                case NewsPage.Front:
                    whichPage = "";
                    break;
                case NewsPage.DotNetDeveloper:
                    whichPage = "/user/Alavan/m/dotnetdeveloper";
                    break;

            }
        }
    }
}
