﻿using NetNewsTicker.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NetNewsTicker.ViewModels
{
    public class TickerViewModel : BaseViewModel
    {
        #region private stuff

        private const int defRefreshDelayMs = 6;
        private const double buttonWidth = 250.0;
        private readonly UserSettings userSettings;

        private readonly int refreshDelay = defRefreshDelayMs;
        private readonly Brush oldItemColor = Brushes.LightBlue, newItemColor = Brushes.LightGreen, visitedColor = Brushes.LightYellow;
        private Button infoButton;
        private readonly Typeface buttonTypeFace;

        private double animationDurationMs; //TODO implement
        private double animationStep = 1.0; // used to accelerate / slow down animation spee
        private bool animateOn = true; // toggles animation on / off
        private double refreshIntervalMin;
        private readonly double optWindowWidth = 280;
        private readonly double optWindowHeight = 210;
        private const double defaultRefreshMin = 5.0;
        private const int defaultNumberOfHeadlinesToDisplay = 20;
        private Dictionary<int, List<string>> allServicesPages;
        #endregion

        #region backer fields
        private ObservableCollection<DropDownCategory> categoriesList;
        private ObservableCollection<DropDownCategory> servicesList;

        private Visibility showOptionsWindow, showPauseButton, showResumeButton, showInfoButton = Visibility.Visible, showItemButtons = Visibility.Hidden, showSecDisplayRadioButton, isRefreshingNews = Visibility.Hidden;
        private bool usePrimaryDisplay, primaryIsCurrent, useTopTicker, topIsCurrent;
        private bool isTopMost;
        private double left = 0, top = 0, width = 0; //positions relative to desktop
        //options window
        private double optionsTop, optionsLeft;

        private readonly double primScreenWidth, secScreenWidth, primScreenHeight, secScreenHeight, taskBarHeight;
        private ObservableCollection<Button> newsButtons;
        private ObservableCollection<double> positions;
        private ObservableCollection<string> headlines;
        private ObservableCollection<(string, string, string)> urls;
        private ObservableCollection<Brush> itemColors;
        private ObservableCollection<Visibility> buttonVisibility;
        private double displayWidth;
        private bool canPause = true;
        private bool isDoingRefresh;
        private List<IContentItem> refreshedItems;
        private int selectedServiceIndex, selectedCategoryIndex;
        private bool isLogEnabled = false;
        private string logLocation;

        // store current setting in order to restore them if user selects "Cancel" in options window
        private int currentServiceIndex, currentCategoryIndex;
        private double currentRefreshMin;
        private bool currentLogging;
        #endregion

        #region simple bind targets
        public ICommand ButtonExitCommand { get; set; }
        public ICommand ButtonOptionsCommand { get; set; }
        public ICommand ButtonPauseCommand { get; set; }
        public ICommand ButtonResumeCommand { get; set; }
        public ICommand ButtonFasterCommand { get; set; }
        public ICommand ButtonSlowerCommand { get; set; }
        public ICommand OptionsSaveCommand { get; set; }
        public ICommand OptionsCancelCommand { get; set; }
        public ICommand OptionsDefaultsCommand { get; set; }
        public ObservableCollection<DropDownCategory> CategoriesList => categoriesList;
        public ObservableCollection<DropDownCategory> ServicesList => servicesList;
        public bool CanExecuteOptionsWindow => showOptionsWindow == Visibility.Hidden;
        public ObservableCollection<string> Headlines => headlines;
        public ObservableCollection<(string, string, string)> Urls => urls;
        public ObservableCollection<Brush> ItemColors => itemColors;

        public ObservableCollection<Visibility> ButtonVisibility => buttonVisibility;
        #endregion

        public TickerViewModel() : base()
        {
            // freeze brushes
            oldItemColor.Freeze();
            newItemColor.Freeze();
            visitedColor.Freeze();
            // Get monitor info
            primScreenWidth = SystemParameters.PrimaryScreenWidth;
            secScreenWidth = SystemParameters.VirtualScreenWidth - SystemParameters.PrimaryScreenWidth; // TODO handle more than 2 monitors?
            primScreenHeight = SystemParameters.WorkArea.Height;
            secScreenHeight = SystemParameters.VirtualScreenHeight;
            taskBarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;

            // Set defaults
            // Get saved settings, if any
            userSettings = UserSettings.LoadSettings();
            primaryIsCurrent = userSettings.Primary;
            UsePrimaryDisplay = primaryIsCurrent;
            topIsCurrent = userSettings.Top;
            refreshIntervalMin = userSettings.Refresh;
            currentServiceIndex = userSettings.Service;
            currentCategoryIndex = userSettings.Page;

            isTopMost = true;
            UsePrimaryDisplay = primaryIsCurrent;
            UseTopTicker = topIsCurrent;
            ShowSecDisplayRadioButton = secScreenWidth > 0 ? Visibility.Visible : Visibility.Hidden; // works for one gpu and 2 monitors
            if (ShowSecDisplayRadioButton == Visibility.Hidden && !primaryIsCurrent)
            {
                primaryIsCurrent = true;
                UsePrimaryDisplay = true;
            }
            ShowPauseButton = Visibility.Hidden;
            ShowResumeButton = Visibility.Hidden;
            ShowOptionsWindow = Visibility.Hidden;
            buttonTypeFace = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            isDoingRefresh = false;
            selectedCategoryIndex = currentCategoryIndex;
            selectedServiceIndex = currentServiceIndex;

            refreshIntervalMin = defaultRefreshMin;
            currentRefreshMin = refreshIntervalMin;

            SetupWindows(UsePrimaryDisplay);
            InitializeBindingCommands();
            InitializeItemsHandler();
            CreateInitialItems();
        }

        private void SetupWindows(bool usePrimary)
        {
            Width = usePrimary ? primScreenWidth - 2 : secScreenWidth - 2;
            ViewWidth = Width - 4 * 16 - 4;
            double currentHeight = primaryIsCurrent ? primScreenHeight : secScreenHeight;
            Top = topIsCurrent ? 1 : currentHeight - 32 - 1 - taskBarHeight;
            OptionsTop = topIsCurrent ? Top + 33 : Top - optWindowHeight;
            if (!topIsCurrent && !usePrimary)
            {
                OptionsTop = optionsTop - taskBarHeight;
            }
            if (usePrimary)
            {
                Left = 1;
                OptionsLeft = Width - optWindowWidth;
            }
            else
            {
                Left = primScreenWidth + 1;
                OptionsLeft = Left + Width - optWindowWidth;
            }

        }

        private void MoveWindowUpDown(bool toUp)
        {
            double currHeight = primaryIsCurrent ? primScreenHeight : secScreenHeight;
            if (toUp)
            {
                Top = 1;
                OptionsTop = Top + 33;
                IsTopmost = true;
            }
            else
            {
                Top = currHeight - 32 - 1;
                Top = usePrimaryDisplay ? top : top - taskBarHeight;
                OptionsTop = Top - optWindowHeight;
                IsTopmost = false;
            }
        }

        private void InitializeBindingCommands()
        {
            ButtonExitCommand = new RelayCommand(x => ExitButtonClick());
            ButtonOptionsCommand = new RelayCommand(param => OptionsButtonClick(), param => CanExecuteOptionsWindow);
            ButtonPauseCommand = new RelayCommand(param => PauseButtonClick(), param => CanPause);
            ButtonResumeCommand = new RelayCommand(param => ResumeButtonClick(), param => CanResume);
            ButtonSlowerCommand = new RelayCommand(param => SlowerButtonClick());
            ButtonFasterCommand = new RelayCommand(param => FasterButtonClick());
            OptionsCancelCommand = new RelayCommand(param => CancelOptionsClick(), param => true);
            OptionsSaveCommand = new RelayCommand(param => SaveOptionsClick(), param => true);
        }

        private void InitializeItemsHandler()
        {
            contentHandler = new ItemsHandler((int)refreshIntervalMin * 60, isLogEnabled, selectedServiceIndex, selectedCategoryIndex);
            allServicesPages = contentHandler.AllServicesPages;
            LogCheckTooltip = contentHandler.LogPath;
            if (contentHandler.AllCategories != null)
            {
                categoriesList = new ObservableCollection<DropDownCategory>();
                servicesList = new ObservableCollection<DropDownCategory>();// { new DropDownCategory(0, "Hacker News"), new DropDownCategory(1, "Reddit"), new DropDownCategory(2, "BBCNews") };                
                DropDownCategory aCat;
                foreach ((int, string) item in contentHandler.AllServices)
                {
                    aCat = new DropDownCategory(item.Item1, item.Item2);
                    ServicesList.Add(aCat);
                }
                PopulateCategories(selectedServiceIndex, selectedCategoryIndex);
                SelectedService = servicesList[selectedServiceIndex];
                selectedNews = selectedServiceIndex;
                selectedPage = selectedCategoryIndex;
            }
            contentHandler.ItemsRefreshCompletedHandler += ContentHandler_ItemsRefreshCompletedHandler;
            contentHandler.ItemsRefreshStartedHandler += ContentHandler_ItemsRefreshStartedHandler;
            refreshedItems = contentHandler.NewContent;
        }

        // Added to allow correct pages to display immediately after user selects new service
        private void PopulateCategories(int forWhichService, int whichPage = 0)
        {
            DropDownCategory aCat;
            int counter = 0;
            if (allServicesPages.TryGetValue(forWhichService, out List<string> pagesList))
            {
                categoriesList.Clear();
                foreach (string pageName in pagesList)
                {
                    aCat = new DropDownCategory(counter, pageName);
                    CategoriesList.Add(aCat);
                    counter++;
                }
                if (whichPage < categoriesList.Count) // just to make sure ...
                {
                    SelectedCategory = categoriesList[whichPage];
                }
                else
                {
                    SelectedCategory = categoriesList[categoriesList.Count - 1];
                }
            }
        }

        private async void CreateInitialItems()
        {
            newsButtons = new ObservableCollection<Button>();
            positions = new ObservableCollection<double>();
            headlines = new ObservableCollection<string>();
            itemColors = new ObservableCollection<Brush>();
            buttonVisibility = new ObservableCollection<Visibility>();
            urls = new ObservableCollection<(string, string, string)>();

            infoButton = new Button() { Content = "...", Width = buttonWidth, Height = 30, Background = newItemColor, Name = "B0", FontSize = 16, FontWeight = FontWeights.Bold };
            itemColors.Add(Brushes.LightGreen);
            headlines.Add("Fetching items...");
            positions.Add(displayWidth - buttonWidth);
            buttonVisibility.Add(Visibility.Visible);
            urls.Add((string.Empty, string.Empty, string.Empty));

            var binding = new Binding($"Positions[{0}]") { Mode = BindingMode.OneWay }; ;
            _ = infoButton.SetBinding(Canvas.LeftProperty, binding);
            binding = new Binding("ShowInfoButton") { Mode = BindingMode.OneWay };
            _ = infoButton.SetBinding(Button.VisibilityProperty, binding);
            NewsButtons.Add(infoButton); //Special button at position 0

            binding = new Binding("ShowItemButtons") { Mode = BindingMode.OneWay };

            Binding posBinding, contentBinding, toolTipBinding, backgroundBinding, visibilityBinding;
            Button but;
            //prepare fixed number of buttons
            //TODO make number of headlines configurable
            for (int i = 1; i < defaultNumberOfHeadlinesToDisplay + 1; i++)
            {
                positions.Add(displayWidth + i * buttonWidth + 2);
                headlines.Add(string.Empty);
                itemColors.Add(oldItemColor);
                buttonVisibility.Add(Visibility.Hidden);
                urls.Add((string.Empty, string.Empty, string.Empty));

                but = new Button() { Width = buttonWidth, Height = 30, Name = $"B{i}" };

                posBinding = new Binding($"Positions[{i}]") { Mode = BindingMode.OneWay };
                but.SetBinding(Canvas.LeftProperty, posBinding);
                but.SetBinding(Button.VisibilityProperty, binding);

                contentBinding = new Binding($"Headlines[{i}]") { Mode = BindingMode.OneWay };
                but.SetBinding(Button.ContentProperty, contentBinding);

                toolTipBinding = new Binding($"Urls[{i}]");
                but.SetBinding(Button.ToolTipProperty, toolTipBinding);

                backgroundBinding = new Binding($"ItemColors[{i}]") { Mode = BindingMode.Default };
                but.SetBinding(Button.BackgroundProperty, backgroundBinding);

                visibilityBinding = new Binding($"ButtonVisibility[{i}]") { Mode = BindingMode.Default };
                but.SetBinding(Button.VisibilityProperty, visibilityBinding);

                but.Click += But_Click;
                but.MouseRightButtonDown += But_MouseRightButtonDown;

                NewsButtons.Add(but);
            }

            _ = await Task.Run(() => DoAnimation()).ConfigureAwait(false); //Animation runs on its own thread
        }

        private void ResetPositions()
        {
            if (Positions != null)
            {
                animateOn = false;
                for (int i = 0; i < Positions.Count; i++)
                {
                    Positions[i] = displayWidth - (2 * buttonWidth) + (i * buttonWidth) + 2;
                    ButtonVisibility[i] = Visibility.Hidden;
                }
                animateOn = true;
            }
        }

        // The animation just updates the position values, bound to the button´s Canvas.Left property
        private async Task<bool> DoAnimation()
        {
            while (true)
            {
                //long begAnimTicks;
                //double ticksPerMs = Stopwatch.Frequency / 1000.0;
                //int counter = 0;
                double currStep;
                double pos;
                int tail;
                while (animateOn)
                {
                    //begAnimTicks = Stopwatch.GetTimestamp();
                    currStep = animationStep; //to avoid changes during the cycle
                    await Task.Delay(refreshDelay).ConfigureAwait(false);

                    for (int i = 0; i < Positions.Count; i++)
                    {
                        pos = Positions[i] - currStep;
                        tail = i != 0 ? i - 1 : Positions.Count - 1; // First item goes behind last, others behind previous
                        if (i == 0 && ShowInfoButton == Visibility.Visible)
                        {
                            Positions[i] = pos + buttonWidth >= 0 ? pos : displayWidth;
                            break;
                        }
                        else
                        {
                            pos = pos + buttonWidth >= 0 ? pos : Positions[tail] + buttonWidth + 2;
                            if (pos > displayWidth)
                            {
                                ButtonVisibility[i] = Visibility.Hidden;
                            }
                            else
                            {
                                ButtonVisibility[i] = Visibility.Visible;
                            }
                        }
                        Positions[i] = pos;
                    }
                    //counter++;
                    //animationMs = (Stopwatch.GetTimestamp() - begAnimTicks) / ticksPerMs;
                    //if(counter % 60 == 0)
                    //{
                    //    AnimationMS = animationMs;
                    //    counter = 0;
                    //}
                }
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        // The event signalling that a refresh cycle started, controls if "refresshing" icon is visible
        private void ContentHandler_ItemsRefreshStartedHandler(object sender, RefreshCompletedEventArgs e)
        {
            IsRefreshingNews = Visibility.Visible;
        }

        // The event signalling that a refresh cycle completed
        private void ContentHandler_ItemsRefreshCompletedHandler(object sender, RefreshCompletedEventArgs e)
        {
            CanPause = true;
            if (!isDoingRefresh)
            {
                ResetPositions();
                RefreshDisplayList();
            }
            ShowPauseButton = Visibility.Visible;
            ShowInfoButton = Visibility.Hidden;
            ShowItemButtons = Visibility.Visible;
            IsRefreshingNews = Visibility.Hidden;
        }

        // Put new headlines into buttons
        private void RefreshDisplayList()
        {
            isDoingRefresh = true;
            int newCount = refreshedItems.Count;
            if (newCount == 0)
            {
                for (int i = 1; i < itemColors.Count; i++)
                {
                    ItemColors[i] = ItemColors[i] == visitedColor ? visitedColor : oldItemColor;
                }
                isDoingRefresh = false;
                return;
            }

            newCount = newCount <= defaultNumberOfHeadlinesToDisplay ? newCount : defaultNumberOfHeadlinesToDisplay; //TODO make this configurable

            // Replace the first newCount items with fresh content, move the others down            
            int ceiling = headlines.Count - 1;
            if (newCount != defaultNumberOfHeadlinesToDisplay)
            {
                for (int i = ceiling; i > newCount; i--)
                {
                    Headlines[i] = Headlines[i - newCount];
                    Urls[i] = Urls[i - newCount];
                    ItemColors[i] = ItemColors[i - newCount] == visitedColor ? visitedColor : oldItemColor;
                }
            }
            // Add the new items to the front
            for (int i = 1; i <= newCount; i++)
            {
                Headlines[i] = FitString(refreshedItems[i - 1].ItemHeadline, buttonWidth);
                Urls[i] = (refreshedItems[i - 1].HasSummary ? refreshedItems[i - 1].ItemSummary : refreshedItems[i - 1].ItemHeadline, refreshedItems[i - 1].Link, refreshedItems[i - 1].SecondaryLink);
                ItemColors[i] = newItemColor;
            }
            isDoingRefresh = false;
        }

        // Cut off too long headlines
        private string FitString(string s, double width)
        {
            string resString = s;
            var fs = new FormattedText(s, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, buttonTypeFace, 12, Brushes.Black, 1.0);
            if (fs.Width > width)
            {
                double ratio = width / fs.Width;
                int newWidth = (int)(ratio * s.Length) - 8;
                resString = $"{resString.Substring(0, newWidth)} ...";
            }
            return resString;

        }

        #region Commands

        // Right click for headlines buttons
        private void But_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var but = sender as Button;
            if (int.TryParse(but.Name.Substring(1), out int butIndex))
            {
                itemColors[butIndex] = visitedColor;
            }

            (_, _, string secondary) = ((string, string, string))but.ToolTip;
            if (secondary.Length != 0)
            {
                // var p = Process.Start(secondary);
                // hack for net core
                string cleanUrl = secondary.Replace("&", "^&", StringComparison.InvariantCulture);
                var p = Process.Start(new ProcessStartInfo("cmd", $"/c start {cleanUrl}") { CreateNoWindow = true });
                p.Dispose();
            }
            e.Handled = true;
        }

        // Left click for headline buttons
        private void But_Click(object sender, RoutedEventArgs e)
        {
            var but = sender as Button;
            if (int.TryParse(but.Name.Substring(1), out int butIndex))
            {
                itemColors[butIndex] = visitedColor;
            }
            (_, string primary, _) = ((string, string, string))but.ToolTip;
            // hack for net core
            string cleanUrl = primary.Replace("&", "^&", StringComparison.InvariantCulture);
            var p = Process.Start(new ProcessStartInfo("cmd", $"/c start {cleanUrl}") { CreateNoWindow = true });
            p.Dispose();
            e.Handled = true;
        }

        private async void ExitButtonClick()
        {
            contentHandler.PauseRefresh();
            while (contentHandler.IsServiceRefreshing)
            {
                await Task.Delay(100);
            }
            contentHandler.Close();
            Application.Current.Shutdown();
        }

        private void PauseButtonClick()
        {
            animateOn = false;
            CanPause = false;
            ShowPauseButton = Visibility.Hidden;
            ShowResumeButton = Visibility.Visible;
            contentHandler.PauseRefresh();
        }

        private void FasterButtonClick()
        {
            animationStep += 0.25;
        }

        private void SlowerButtonClick()
        {
            animationStep -= 0.25;
            animationStep = animationStep < 0.25 ? 0.25 : animationStep;
        }

        private void ResumeButtonClick()
        {
            animateOn = true;
            CanPause = true;
            ShowPauseButton = Visibility.Visible;
            ShowResumeButton = Visibility.Hidden;
            contentHandler.ResumeRefreshing();
        }

        private void OptionsButtonClick()
        {
            if (showOptionsWindow == Visibility.Visible)
            {
                CancelOptionsClick();
                return;
            }
            ShowOptionsWindow = Visibility.Visible;
        }

        private async void SaveOptionsClick()
        {
            ShowOptionsWindow = Visibility.Hidden;
            if (refreshIntervalMin != currentRefreshMin)
            {
                currentRefreshMin = refreshIntervalMin;
                userSettings.Refresh = currentRefreshMin;
                contentHandler.ChangeRefreshInterval((int)refreshIntervalMin * 60);
            }
            if (selectedNews != SelectedService.Id)
            {
                selectedNews = SelectedService.Id;
                userSettings.Service = selectedNews;
                currentServiceIndex = selectedNews;
                ShowItemButtons = Visibility.Hidden;
                ShowInfoButton = Visibility.Visible;
                ResetPositions();
                selectedPage = SelectedCategory.Id;
                contentHandler.ChangeCurrentService(selectedNews, selectedPage, isLogEnabled);
                LogCheckTooltip = contentHandler.LogPath;
                PopulateCategories(selectedNews);
                refreshedItems = contentHandler.NewContent;
                SelectedCategory = categoriesList[selectedPage];
                SelectedCategoryIndex = selectedPage;
                currentCategoryIndex = selectedPage;
            }
            if (selectedPage != SelectedCategory.Id)
            {
                selectedPage = SelectedCategory.Id;
                userSettings.Page = selectedPage;
                contentHandler.ChangeCurrentCategory(selectedPage);
                ShowItemButtons = Visibility.Hidden;
                ShowInfoButton = Visibility.Visible;
                ResetPositions();
                currentCategoryIndex = selectedPage;
            }
            if (usePrimaryDisplay != primaryIsCurrent)
            {
                primaryIsCurrent = usePrimaryDisplay;
                userSettings.Primary = primaryIsCurrent;
                SetupWindows(usePrimaryDisplay);
                ResetPositions();
            }
            if (useTopTicker != topIsCurrent)
            {
                topIsCurrent = useTopTicker;
                userSettings.Top = topIsCurrent;
                MoveWindowUpDown(useTopTicker);
            }
            if (isLogEnabled != currentLogging)
            {
                currentLogging = isLogEnabled;
                ModifyLogging(isLogEnabled);
                LogCheckTooltip = contentHandler.LogPath;
            }
            _ = await UserSettings.SaveSettings(userSettings).ConfigureAwait(false);
        }

        private void CancelOptionsClick()
        {
            ShowOptionsWindow = Visibility.Hidden;
            if (currentRefreshMin != refreshIntervalMin)
            {
                NetworkRefresh = currentRefreshMin.ToString("N1", System.Globalization.CultureInfo.CurrentCulture);
            }
            if (currentServiceIndex != selectedServiceIndex)
            {
                SelectedServiceIndex = currentServiceIndex;
                SelectedService = servicesList[currentServiceIndex];
            }
            if (currentCategoryIndex != selectedCategoryIndex)
            {
                SelectedCategoryIndex = currentCategoryIndex;
            }
            if (primaryIsCurrent != usePrimaryDisplay)
            {
                UsePrimaryDisplay = primaryIsCurrent;
            }
            if (topIsCurrent != useTopTicker)
            {
                UseTopTicker = topIsCurrent;
            }
            if (isLogEnabled != currentLogging)
            {
                IsLogEnabled = currentLogging;
            }
        }

        private void ModifyLogging(bool doLog)
        {
            contentHandler.ControlLogging(doLog);
            LogCheckTooltip = contentHandler.LogPath;
        }
        #endregion

        #region Bound variables        

        // options window
        public double OptionsTop
        {
            get => optionsTop;
            set
            {
                if (optionsTop != value)
                {
                    optionsTop = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DropDownCategory SelectedCategory
        {
            get => selectedCategory;
            set
            {
                if (selectedCategory != value)
                {
                    selectedCategory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DropDownCategory SelectedService
        {
            get => selectedService;
            set
            {
                if (selectedService != value)
                {
                    selectedService = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int SelectedServiceIndex
        {
            get => selectedServiceIndex;
            set
            {
                if (selectedServiceIndex != value)
                {
                    selectedServiceIndex = value;
                    NotifyPropertyChanged();
                    PopulateCategories(selectedServiceIndex);
                }
            }
        }

        public int SelectedCategoryIndex
        {
            get => selectedCategoryIndex;
            set
            {
                selectedCategoryIndex = value;
                NotifyPropertyChanged();

            }
        }

        public double OptionsLeft
        {
            get => optionsLeft;
            set
            {
                if (optionsLeft != value)
                {
                    optionsLeft = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UsePrimaryDisplay
        {
            get => usePrimaryDisplay;
            set
            {
                if (usePrimaryDisplay != value)
                {
                    usePrimaryDisplay = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UseSecondaryDisplay
        {
            get => !usePrimaryDisplay;
            set
            {
                if (!usePrimaryDisplay != value)
                {
                    usePrimaryDisplay = !value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UseTopTicker
        {
            get => useTopTicker;
            set
            {
                if (useTopTicker != value)
                {
                    useTopTicker = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsTopmost
        {
            get => isTopMost;
            set
            {
                if (isTopMost != value)
                {
                    isTopMost = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsLogEnabled
        {
            get => isLogEnabled;
            set
            {
                if (isLogEnabled != value)
                {
                    isLogEnabled = value;
                    NotifyPropertyChanged();
                    ModifyLogging(isLogEnabled);
                    //LogCheckTooltip = contentHandler.LogPath;
                }
            }
        }

        public string LogCheckTooltip
        {
            get => logLocation;
            set
            {
                if (logLocation != value)
                {
                    logLocation = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double AnimationMS
        {
            get => animationDurationMs;
            set
            {
                if (animationDurationMs != value)
                {
                    animationDurationMs = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility IsRefreshingNews
        {
            get => isRefreshingNews;
            set
            {
                if (isRefreshingNews != value)
                {
                    isRefreshingNews = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility ShowOptionsWindow
        {
            get => showOptionsWindow;
            set
            {
                if (showOptionsWindow != value)
                {
                    showOptionsWindow = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility ShowSecDisplayRadioButton
        {
            get => showSecDisplayRadioButton;
            set
            {
                if (showSecDisplayRadioButton != value)
                {
                    showSecDisplayRadioButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // main window
        public Visibility ShowInfoButton
        {
            get => showInfoButton;
            set
            {
                if (showInfoButton != value)
                {
                    showInfoButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility ShowItemButtons
        {
            get => showItemButtons;
            set
            {
                if (showItemButtons != value)
                {
                    showItemButtons = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility ShowPauseButton
        {
            get => showPauseButton;
            set
            {
                if (showPauseButton != value)
                {
                    showPauseButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility ShowResumeButton
        {
            get => showResumeButton;
            set
            {
                if (showResumeButton != value)
                {
                    showResumeButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double ViewWidth
        {
            get => displayWidth;
            set
            {
                if (value != displayWidth)
                {
                    displayWidth = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<double> Positions => positions;

        public ObservableCollection<Button> NewsButtons => newsButtons;


        public double Left
        {
            get => left;
            set
            {
                if (left != value)
                {
                    left = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Top
        {
            get => top;
            set
            {
                if (top != value)
                {
                    top = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string NetworkRefresh
        {
            get => refreshIntervalMin.ToString("N1", System.Globalization.CultureInfo.CurrentCulture);

            set
            {
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out double refr))
                {
                    refreshIntervalMin = refr;
                    NotifyPropertyChanged();
                }
                else
                {
                    throw new ApplicationException(Localization.Resources.ErrorMustBeNumeric);

                }
            }
        }

        public bool CanPause
        {
            get => canPause;

            set
            {
                if (canPause != value)
                {
                    canPause = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool CanResume => !canPause;

        #endregion
    }
}
