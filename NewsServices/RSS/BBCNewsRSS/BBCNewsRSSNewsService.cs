using System.Collections.Generic;
using NetNewsTicker.Model;

namespace NewsServices
{
    public class BBCNewsRSSNewsService : RSSNewsService
    {
        public new enum NewsPage { FrontPage = 0, World, Technology, Science_Environment, Europe, UK, US_Canada, Latin_America, Asia, Africa }; // specific for BBCNews RSS service


        public BBCNewsRSSNewsService(bool logEnabled) : base(logEnabled)
        {
            nwClient = new BBCNewsRSSNetworkClient();
            
            viewIdsAndDescriptions = new List<(int, string)>() { ((int)NewsPage.FrontPage, "Top Stories"), ((int)NewsPage.World, "World"), ((int)NewsPage.Technology, "Technology"), ((int)NewsPage.Science_Environment, "Science & Environment"),
                                        ((int)NewsPage.Europe, "Europe"), ((int)NewsPage.UK, "UK"), ((int)NewsPage.US_Canada, "US & Canada"), ((int)NewsPage.Latin_America, "Latin America"), ((int)NewsPage.Asia, "Asia"),
                                        ((int)NewsPage.Africa, "Africa")};
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.Africa;
        }                

        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            switch (nPage)
            {
                case NewsPage.FrontPage:
                    whichPage = "/news";
                    break;
                case NewsPage.World:
                    whichPage = "/news/world";
                    break;
                case NewsPage.Technology:
                    whichPage = "/news/technology";
                    break;
                case NewsPage.Science_Environment:
                    whichPage = "/news/science_and_environment";
                    break;
                case NewsPage.Europe:
                    whichPage = "/news/world/europe";
                    break;
                case NewsPage.UK:
                    whichPage = "/news/uk";
                    break;
                case NewsPage.US_Canada:
                    whichPage = "/news/world/us_and_canada";
                    break;
                case NewsPage.Latin_America:
                    whichPage = "/news/world/latin_america";
                    break;
                case NewsPage.Asia:
                    whichPage = "/news/world/asia";
                    break;
                case NewsPage.Africa:
                    whichPage = "/news/world/africa";
                    break;
                default:
                    whichPage = "/news";
                    break;
            }
        }
    }
}
