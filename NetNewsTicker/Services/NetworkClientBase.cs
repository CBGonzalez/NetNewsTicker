using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    public abstract class NetworkClientBase : INetworkClient
    {
        private bool disposedValue = false; // To detect redundant calls
        
        private protected HttpClient client = null;
        private protected HttpResponseMessage response = null;
        private protected Uri newsServerBase; 
        private protected const string appName = "NETNewsTicker";
        private protected string logFileName;
        private protected string pathToLogfile;
        private protected NetworkInterface[] nics;
        private protected bool hasNetworkAccess = false, hasInternetAccess = false;
        private protected bool canFetchAllAtOnce;
        private protected int maxItems;        

        //public bool CanFetchAllAtOnce => canFetchAllAtOnce;
        public int MaxItems => maxItems;

        internal NetworkClientBase()
        {
            if (client == null)
            {
                client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", $"{appName} C# 1.0 {Environment.OSVersion.ToString()}");                
            }
        }

        public void InitializeNetworClient()
        {
            client.BaseAddress = newsServerBase;
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            pathToLogfile = Path.Combine(appDataFolder, logFileName);
            Logger.InitializeLogger(pathToLogfile);
            Logger.Log("Starting network activity", Logger.Level.Information);
            nics = NetworkInterface.GetAllNetworkInterfaces();
            hasNetworkAccess = IsNetworkup(ref nics);            
            if (!hasNetworkAccess)
            {
                Logger.Log("No network access detected!", Logger.Level.Error);
            }
            else
            {
                hasInternetAccess = IsInternetReachable();
                if(!hasInternetAccess)
                {
                    Logger.Log("No Internet access available!", Logger.Level.Error);
                }
            }
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        internal static bool IsNetworkup(ref NetworkInterface[] nics)
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

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            hasNetworkAccess = e.IsAvailable;
            if (!hasNetworkAccess)
            {
                hasInternetAccess = false;                
            }
        }

        internal static bool IsInternetReachable()
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
        }

        public abstract Task<(bool, List<IContentItem>, string)> FetchAllItemsAsync(string itemsURL, int howManyItems, CancellationToken cancel);
        

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (client != null)
                    {
                        client.CancelPendingRequests();
                        client.Dispose();
                        client = null;
                    }
                    if(response != null)
                    {
                        response.Dispose();
                    }
                    Logger.Log("Disposing NetworkClient", Logger.Level.Information);
                    Logger.Close();
                    NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
                    disposedValue = true;
                }
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }        

        #endregion

    }
}
