using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    interface INetworkClient : IDisposable
    {
        int MaxItems { get; }
        void InitializeNetworClient(bool enableLogging);

        void ControlLogging(bool enable);

        Task<(bool, List<IContentItem>, string)> FetchAllItemsAsync(string itemsURL, int howManyItems, CancellationToken cancel);
    }
}
