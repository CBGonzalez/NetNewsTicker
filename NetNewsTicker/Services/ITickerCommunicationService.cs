using System;
using System.Collections.Generic;

using NetNewsTicker.Model;

namespace NetNewsTicker.Services
{
    public interface ITickerCommunicationService : IDisposable
    {

        string LogPath { get; }

        bool HasDifferentCategories { get; }

        /// <summary>
        /// A list of available categories to display
        /// </summary>
        List<(int, string)> ViewIdsAndDescriptions { get;}

        /// <summary>
        /// If true, signals that the service is amidst a refresh cycle
        /// </summary>
        bool IsRefreshing { get; }

        /// <summary>
        /// An event signalling that a refresh cycle has started.
        /// </summary>
        event EventHandler<RefreshCompletedEventArgs> RefreshStartedHandler;

        /// <summary>
        /// An event signalling that a refresh cycle has completed.
        /// </summary>
        event EventHandler<RefreshCompletedEventArgs> RefreshCompletedHandler;

        /// <summary>
        /// Start (or resume) the service´s content refresh.
        /// </summary>
        /// <param name="refreshIntervalSeconds">Tine between refreshes, in seconds.</param>
        /// <returns></returns>
        (bool success, string errorMessage) StartRefreshing(int refreshIntervalSeconds, int category);

        /// <summary>
        /// Resume) the service´s content refresh.
        /// </summary>
        /// <returns></returns>
        void ResumeRefreshing();

        /// <summary>
        /// Forces an immediate getting of new content;
        /// </summary>
        void ImmediateRefresh();

        /// <summary>
        /// Get the reference to the news items available from the service
        /// </summary>
        /// <returns>A list of IContenItems retrieved from the service.</returns>
        List<IContentItem> GetAllItemsList();

        /// <summary>
        /// Get the reference to the newest items available after the last refresh
        /// </summary>
        /// <returns>A list of IContenItems newly feetched during the last refresh.</returns>
        List<IContentItem> GetNewItemsList();

        bool ChangeDesiredRefreshInterval(int refreshIntervalSeconds);

        bool ChangeContentCategory(int newCategory);

        void ChangeLogging(bool enable);

        //Task<bool> RefreshNewsAsync(List<(uint, NewsItemBase.NewsPage)> inList, ItemNews.NewsPage page, CancellationToken cancel);
        /// <summary>
        /// Stop the service´s content refresh.
        /// </summary>
        void StopRefreshing();

        /// <summary>
        /// Pauses the service´s content refresh.
        /// </summary>
        void PauseRefreshing();
    }
}
