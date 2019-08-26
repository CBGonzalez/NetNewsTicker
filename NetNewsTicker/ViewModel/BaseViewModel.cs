using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetNewsTicker.Model;

namespace NetNewsTicker.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged, IDisposable
    {

        #region Fields        
        private protected ItemsHandler contentHandler;
        private protected DropDownCategory selectedCategory, selectedService;
        private protected int selectedPage = 0, selectedNews = 0;
        #endregion

        #region Public properties
        public event PropertyChangedEventHandler PropertyChanged;        
        #endregion
             
        public BaseViewModel()
        {
            
        }
        

        internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(contentHandler != null)
                    {
                        contentHandler.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            
            Dispose(true);
            GC.SuppressFinalize(this);           
        }
        #endregion
    }
}
