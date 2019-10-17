using System.Collections.Generic;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS.BBCNewsRSS
{
    public class BBCNewsRSSNewsService : RSSNewsService
    {
        public new enum NewsPage { FrontPage = 0, World, Technology, Science_Environment, Europe, UK, US_Canada, Latin_America, Asia, Africa }; // specific for BBCNews RSS service


        public BBCNewsRSSNewsService(bool logEnabled) : base(logEnabled)
        {
            nwClient = new BBCNewsRSSNetworkClient(logEnabled);

            viewIdsAndDescriptions = new List<(int, string)>() { ((int)NewsPage.FrontPage, "Top Stories"), ((int)NewsPage.World, "World"), ((int)NewsPage.Technology, "Technology"), ((int)NewsPage.Science_Environment, "Science & Environment"),
                                        ((int)NewsPage.Europe, "Europe"), ((int)NewsPage.UK, "UK"), ((int)NewsPage.US_Canada, "US & Canada"), ((int)NewsPage.Latin_America, "Latin America"), ((int)NewsPage.Asia, "Asia"),
                                        ((int)NewsPage.Africa, "Africa")};
            newItems = new List<IContentItem>();
            maxNewsPageItem = (int)NewsPage.Africa;
        }

        internal override void SetCorrectUrl(int page)
        {
            var nPage = (NewsPage)page;
            whichPage = nPage switch
            {
                NewsPage.FrontPage => "/news",
                NewsPage.World => "/news/world",
                NewsPage.Technology => "/news/technology",
                NewsPage.Science_Environment => "/news/science_and_environment",
                NewsPage.Europe => "/news/world/europe",
                NewsPage.UK => "/news/uk",
                NewsPage.US_Canada => "/news/world/us_and_canada",
                NewsPage.Latin_America => "/news/world/latin_america",
                NewsPage.Asia => "/news/world/asia",
                NewsPage.Africa => "/news/world/africa",
                _ => "/news",
            };
        }
    }
}
