using NetNewsTicker.Model;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NetNewsTicker.Services
{
    public class YCombNetworkClient : NetworkClientBase
    {
        private int currentMaxItem = 0;
        private bool mustRefresh = false;
        private readonly Dictionary<int, YCombItem> cacheContent;

        public YCombNetworkClient() : base()
        {
            newsServerBase = new Uri("https://hacker-news.firebaseio.com/v0/");
            logFileName = "NewsTickerLog.txt";
            cacheContent = new Dictionary<int, YCombItem>();
        }

        public override async Task<(bool, List<IContentItem>, string)> FetchAllItemsAsync(string itemsURL, int howManyItems, CancellationToken cancel)
        {            
            if (cacheContent.Count > 5000)
            {
                cacheContent.Clear();
            }
            bool isOK;
            string error;
            mustRefresh = howManyItems <= 0;
            if (mustRefresh)
            {
                howManyItems = 50;
            }
            List<IContentItem> fetchedItems = null;
            (bool success, bool needsRefreshing, string errorMsg) = await GetMaxItemAsync(cancel).ConfigureAwait(false);
            isOK = success;
            error = errorMsg;
            if (isOK && (needsRefreshing || mustRefresh) && !cancel.IsCancellationRequested)
            {
                (bool fetchedOK, List<int> list, string errorMessage) = await FetchItemIdsForPageAsync(itemsURL, cancel).ConfigureAwait(false);
                isOK = fetchedOK;
                error = errorMessage;

                if (isOK && list.Count > 0 && !cancel.IsCancellationRequested)
                {
                    int howManyToFetch = list.Count >= howManyItems ? howManyItems : list.Count;
                    fetchedItems = new List<IContentItem>(howManyToFetch);
                    for (int i = 0; i < howManyToFetch; i++)
                    {
                        (bool itemOK, YCombItem item, _) = await GetOneItemAsync(list[i], cancel).ConfigureAwait(false);
                        //isOK = itemOK;
                        if (itemOK)
                        {
                            if (item != null)
                            {
                                fetchedItems.Add(item);
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            return (isOK & (needsRefreshing | mustRefresh), fetchedItems, error);
        }

        /// <summary>
        /// Get the highest (newest) item ID currently on server
        /// </summary>
        /// <returns></returns>
        private async Task<(bool success, bool needsRefresh, string errorMsg)> GetMaxItemAsync(CancellationToken cancel)
        {
            bool isOk = false;
            if (!hasNetworkAccess)
            {
                Logger.Log("YCombNetworkClient: No network", Logger.Level.Error);
                return (false, false, "No network.");
            }
            if (!hasInternetAccess)
            {
                hasInternetAccess = IsInternetReachable();
                if (!hasInternetAccess)
                {
                    Logger.Log("YCombNetworkClient: No Internet access", Logger.Level.Error);
                    return (false, false, "No Internet access");
                }
            }
            string error = string.Empty;
            int maxItem = 0;
            bool needsRefresh = false;
            Logger.Log("Fetching highest item", Logger.Level.Information);
            try
            {
                using HttpResponseMessage response = await client.GetAsync("maxitem.json", cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    error = $"{response.StatusCode}: { response.ReasonPhrase}";
                    Logger.Log(error, Logger.Level.Error);
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!int.TryParse(responseBody, out maxItem))
                    {
                        error = $"Trouble: error parsing maxitem. String returned was {responseBody}.";
                        Logger.Log(error, Logger.Level.Error);
                    }
                    else
                    {
                        isOk = true;
                        needsRefresh = (maxItem > currentMaxItem) | mustRefresh;
                        mustRefresh = false;
                        currentMaxItem = maxItem;
                        Logger.Log("GetMaxItem finished ok", Logger.Level.Information);
#if DEBUG
                        Logger.Log($"Value retrieved {maxItem}", Logger.Level.Debug);
#endif
                    }

                }
            }
            catch (TaskCanceledException te)
            {
                Logger.Log(te.ToString(), Logger.Level.Information);
            }
            return (isOk, needsRefresh, error);
        }

        /// <summary>
        /// Gets all Ids for current page
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<(bool, List<int>, string)> FetchItemIdsForPageAsync(string page, CancellationToken cancel)
        {
            string error = "FetchItemIdsForPageAsync";
            HttpResponseMessage response = null;
            List<int> newIds = null;
            bool success = false;
            try
            {
                response = await client.GetAsync($"{page}.json", cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    error = $"{error}: {response.StatusCode.ToString()} {response.ReasonPhrase}";
                    Logger.Log(error, Logger.Level.Error);
                    return (success, null, error);
                }
                ReadOnlyMemory<byte> content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                newIds = GetIdsFromMem(content);
#if DEBUG
                Logger.Log($"Retrieved {newIds.Count} items for {page}", Logger.Level.Debug);
#endif
                if (newIds.Count == 0)
                {
                    return (success, newIds, "No items returned!");
                }
                else
                {
                    success = true;
                }
            }
            catch (HttpRequestException e)
            {
                error = $"{error}: {e.ToString()}";
                Logger.Log(error, Logger.Level.Error);
            }
            catch (TaskCanceledException te)
            {
                error = $"{error}: {te.ToString()}";
                Logger.Log(error, Logger.Level.Error);
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
            return (success, newIds, error);
        }

        private static List<int> GetIdsFromMem(ReadOnlyMemory<byte> content)
        {
            byte utfComma = 0x2C;
            var ids = new List<int>();
            ReadOnlySpan<byte> localContent = content.Span.Slice(1, content.Length - 2);
            ReadOnlySpan<byte> currentSlice = localContent.Slice(0);
            int position = 0, commaPosition = localContent.IndexOf(utfComma);
            int counter = 0;

            while (commaPosition >= 0)
            {
                if (Utf8Parser.TryParse(currentSlice.Slice(position, commaPosition), out int id, out _))
                {
                    ids.Add(id);
                }
                position = commaPosition + 1;
                currentSlice = currentSlice.Slice(position);
                commaPosition = currentSlice.IndexOf(utfComma);
                counter++;
            }
            return ids;
        }

        /// <summary>
        /// Fetches one news item content
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<(bool, YCombItem, string)> GetOneItemAsync(int itemID, CancellationToken cancel)
        {
            var serType = new YCombItem();
            bool success = true;
            string error = string.Empty;
            HttpResponseMessage response = null;
            if (cacheContent.ContainsKey(itemID))
            {
                serType = cacheContent[itemID];
            }
            else
            {
                try
                {
                    response = await client.GetAsync($"item/{itemID}.json", cancel).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        error = $"{response.StatusCode.ToString()} {response.ReasonPhrase}";
                        Logger.Log(error, Logger.Level.Error);
                    }
                    else
                    {
                        byte[] buff = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        serType = JsonSerializer.Deserialize<YCombItem>(buff);
                        success = serType != null;
                        if (success)
                        {
                            cacheContent.Add(serType.id, serType);
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Logger.Log(e.ToString(), Logger.Level.Error);
                    serType = null;
                    error += e.ToString();
                    success = false;
                }
                catch (TaskCanceledException te)
                {
                    Logger.Log(te.ToString(), Logger.Level.Information);
                    serType = null;
                    error += te.ToString();
                }
                finally
                {
                    if (response != null)
                    {
                        response.Dispose();
                    }
                }
            }
            return (success, serType, error);
        }
    }
}
