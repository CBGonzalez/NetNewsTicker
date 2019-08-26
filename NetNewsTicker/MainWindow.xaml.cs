using System.Windows;
using NetNewsTicker.ViewModels;

namespace NetNewsTicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        
        private readonly TickerViewModel TVM;
        private readonly OptionsWindow optWindow;
                

        public MainWindow()
        {            
            InitializeComponent();
                        
            optWindow = new OptionsWindow();
            TVM = new TickerViewModel();            
            DataContext = TVM;
            optWindow.DataContext = DataContext;            
        }

        private void ScrollWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(TVM != null)
            {
                TVM.Dispose();
            }
            if(optWindow != null)
            {
                optWindow.Close();
            }
        }        
    }    
}
