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
       
        public YCombNetworkClient() : base()
        {                            
            jasonSer = new DataContractJsonSerializer((new YCombItem()).GetType());  
            newsServerBase = new Uri("https://hacker-news.firebaseio.com/v0/");
            logFileName = "NewsTickerLog.txt";
        }

        /*
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            hasNetworkAccess = e.IsAvailable;
            if (!hasNetworkAccess)
            {
                hasInternetAccess = false;
            }
        }

        private bool IsNetworkup(ref NetworkInterface[] nics)
        {
            if (nics == null)
            {
                return false;
            }
            bool isUp = false;
            foreach (NetworkInterface n in nics)
            {
                isUp |= n.OperationalStatus == OperationalStatus.Up;
            }
            return isUp;
        } 

        private static bool IsInternetReachable()
        {
            bool internetUp = false;
            using (var myPing = new Ping())
            {
                var myPingOptions = new PingOptions();
                byte[] buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                PingReply reply = myPing.Send("1.1.1.1", 2000);
                internetUp |= reply.Status == IPStatus.Success;
                reply = myPing.Send("8.8.8.8", 2000);
                internetUp |= reply.Status == IPStatus.Success;
            }
            return internetUp;
        } */

        /// <summary>
        /// Get the highest (newest) item ID currently on server
        /// </summary>
        /// <returns></returns>
        public async Task<(bool success, uint itemID, string errorMsg)> GetMaxItemAsync(CancellationToken cancel)
        {
            bool isOk = false;
            if (!hasNetworkAccess)
            {
                Logger.Log("YCombNetworkClient: No network", Logger.Level.Error);
                return (false, 0, "No network.");
            }
            if (!hasInternetAccess)
            {
                hasInternetAccess = IsInternetReachable();
                if (!hasInternetAccess)
                {
                    Logger.Log("YCombNetworkClient: No Internet access", Logger.Level.Error);
                    return (false, 0, "No Internet access");
                }
            }
            string error = string.Empty;
            uint maxItem = 0;
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
                        if (!uint.TryParse(responseBody, out maxItem))
                        {
                            error = $"Trouble: error parsing maxitem. String returned was {responseBody}.";
                            Logger.Log(error, Logger.Level.Error);
                        }
                        else
                        {
                            isOk = true;
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
            return (isOk, maxItem, error);
        }

        /// <summary>
        /// Gets all Ids for current page
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<(bool, List<uint>, string)> FetchItemIdsForPageAsync(string page, CancellationToken cancel)
        {
            string error = "FetchItemIdsForPageAsync";
            HttpResponseMessage response = null;
            List<uint> newIds = null;
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

        private static List<uint> GetIdsFromMem(ReadOnlyMemory<char> content)
        {
            var ids = new List<uint>();
            ReadOnlySpan<char> localContent = content.Span.Slice(1, content.Length - 2);
            ReadOnlySpan<char> currentSlice = localContent.Slice(0);
            int position = 0, commaPosition = localContent.IndexOf(',');
            int counter = 0;
            while (commaPosition >= 0)
            {                
                ids.Add(uint.Parse(new string(currentSlice.Slice(position, commaPosition).ToArray()), NumberStyles.Integer, CultureInfo.InvariantCulture));
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
        public async Task<(bool, YCombItem, string)> GetOneItemAsync(uint itemID, CancellationToken cancel)
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
