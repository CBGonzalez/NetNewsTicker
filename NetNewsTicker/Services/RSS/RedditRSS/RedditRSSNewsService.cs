using System.Collections.Generic;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS.RedditRSS
{
    public class RedditRSSNewsService : RSSNewsService
    {
        public new enum NewsPage  { DotNetDeveloper = 0, Front = 1 }; // specific for Reddit RSS service        

        public RedditRSSNewsService(bool useLogging) : base(useLogging)
        {
            nwClient = new RedditRSSNetworkClient();            
            viewIdsAndDescriptions = new List<(int, string)>() { (0, "dotnetdeveloper"), (1, "Front page") };
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.Front;
        }
        
       
        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            switch (nPage)
            {                
                case NewsPage.DotNetDeveloper:
                    whichPage = "/user/Alavan/m/dotnetdeveloper";
                    break;
                case NewsPage.Front:
                    whichPage = "";
                    break;
            }
        }
    }
}
