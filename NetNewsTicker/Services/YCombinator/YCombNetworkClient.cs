using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    public class YCombNetworkClient : NetworkClientBase
    {
        
        private readonly DataContractJsonSerializer jasonSer;
        private int currentMaxItem = 0;
        private bool mustRefresh = false;
       
        public YCombNetworkClient() : base()
        {                            
            jasonSer = new DataContractJsonSerializer((new YCombItem()).GetType());  
            newsServerBase = new Uri("https://hacker-news.firebaseio.com/v0/");
            logFileName = "NewsTickerLog.txt";            
        }

        public override async Task<(bool, List<IContentItem>, string)> FetchAllItemsAsync(string itemsURL, int howManyItems, CancellationToken cancel)
        {
            bool isOK;
            string error;            
            mustRefresh = howManyItems <= 0;
            if(mustRefresh)
            {
                howManyItems = 50;
            }
            List<IContentItem> fetchedItems = null;
            (bool success, bool needsRefreshing, string errorMsg) = await GetMaxItemAsync(cancel);
            isOK = success;
            error = errorMsg;
            if(isOK && needsRefreshing && !cancel.IsCancellationRequested)
            {
                (bool fetchedOK, List<int> list, string errorMessage) = await FetchItemIdsForPageAsync(itemsURL, cancel);
                isOK = fetchedOK;
                error = errorMessage;
                
                if(isOK && list.Count > 0 && !cancel.IsCancellationRequested)
                {
                    int howManyToFetch = list.Count >= howManyItems ? howManyItems : list.Count;
                    fetchedItems = new List<IContentItem>(howManyToFetch);
                    for(int i = 0; i < howManyToFetch; i++)
                    {
                        (bool itemOK, YCombItem item, _) = await GetOneItemAsync(list[i], cancel);
                        isOK = itemOK;
                        if(isOK)
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
            return (isOK & needsRefreshing, fetchedItems, error);
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
                using (HttpResponseMessage response = await client.GetAsync("maxitem.json", cancel).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        error = $"{response.StatusCode}: { response.ReasonPhrase}";
                        Logger.Log(error, Logger.Level.Error);
                        //return (isOk, 0, $"{response.StatusCode}: { response.ReasonPhrase}");               
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
                ReadOnlyMemory<char> content = (await response.Content.ReadAsStringAsync().ConfigureAwait(false)).AsMemory();
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

        private static List<int> GetIdsFromMem(ReadOnlyMemory<char> content)
        {
            var ids = new List<int>();
            ReadOnlySpan<char> localContent = content.Span.Slice(1, content.Length - 2);
            ReadOnlySpan<char> currentSlice = localContent.Slice(0);
            int position = 0, commaPosition = localContent.IndexOf(',');
            int counter = 0;
            while (commaPosition >= 0)
            {                
                ids.Add(int.Parse(new string(currentSlice.Slice(position, commaPosition).ToArray()), NumberStyles.Integer, CultureInfo.InvariantCulture));
                position = commaPosition + 1;
                currentSlice = currentSlice.Slice(position);
                commaPosition = currentSlice.IndexOf(',');
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
            bool success = false;
            string error = string.Empty;
            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync($"item/{itemID}.json", cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    error = $"{response.StatusCode.ToString()} {response.ReasonPhrase}";
                    Logger.Log(error, Logger.Level.Error);
                    //return (success, null);
                }
                else
                {
                    byte[] buff = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    using (var ms = new MemoryStream(buff))
                    {
                        serType = jasonSer.ReadObject(ms) as YCombItem;
                    }
                    success = serType != null;
                }
            }
            catch (HttpRequestException e)
            {
                Logger.Log(e.ToString(), Logger.Level.Error);
                serType = null;
                error += e.ToString();
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
            return (success, serType, error);
        }
    }
}
