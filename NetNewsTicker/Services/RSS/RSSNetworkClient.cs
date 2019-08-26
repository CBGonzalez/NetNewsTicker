using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NetNewsTicker.Model;

namespace NetNewsTicker.Services.RSS
{
    public class RSSNetworkClient : NetworkClientBase
    {
        private protected readonly List<IContentItem> newContent;
        private protected string rssTail = "";

        public RSSNetworkClient()
        {            
            maxItems = 50; // we only get up to 50 via RSS
            newContent = new List<IContentItem>();
            canFetchAllAtOnce = true;
        }

        public override async Task<(bool, List<IContentItem>, string)> FetchAllItemsAsync(string itemsURL, int howManyItems, CancellationToken cancel)
        {            
            SyndicationFeed rssFeed;
            bool success = false;
            string error = string.Empty;
            newContent.Clear();
            if (!hasInternetAccess)
            {
                hasInternetAccess = IsInternetReachable();
                if (!hasInternetAccess)
                {
                    Logger.Log("Reddit Network Client: No Internet access", Logger.Level.Error);
                    return (false, null, "No Internet access");
                }
            }
            try
            {                
                response = await client.GetAsync($"{itemsURL}{rssTail}", cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    error = $"{response.StatusCode.ToString()} {response.ReasonPhrase}";
                    Logger.Log(error, Logger.Level.Error);
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using (Stream str = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var reader = XmlReader.Create(str);
                        rssFeed = SyndicationFeed.Load(reader);
                        reader.Dispose();
                    }
                    if (rssFeed.Items == null)
                    {
                        error = "No content returned";
                    }
                    else
                    {

                        ParseContent(rssFeed);
                    }
                    if (newContent.Count == 0)
                    {
                        return (success, null, error);
                    }
                    success = true;
                    Logger.Log($"Fetched {newContent.Count} new articles", Logger.Level.Information);
                }
            }
            catch (HttpRequestException e)
            {
                Logger.Log(e.ToString(), Logger.Level.Error);
                //oneItem = null;
                error += e.ToString();
            }
            catch (TaskCanceledException te)
            {
                Logger.Log(te.ToString(), Logger.Level.Information);
                //oneItem = null;
                error += te.ToString();
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }
            }
            return (success, newContent, error);
        }

        internal virtual void ParseContent(SyndicationFeed feed)
        {
            RSSItem oneItem;
            foreach (SyndicationItem item in feed.Items)
            {
                oneItem = new RSSItem(item);
                if (oneItem != null)
                {
                    newContent.Add(oneItem);
                }
            }
        }
    }
}
