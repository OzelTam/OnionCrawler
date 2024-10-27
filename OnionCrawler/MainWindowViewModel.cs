
using OnionCrawler.Lib;
using OnionCrawler.Lib.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace OnionCrawler.WPF
{

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _rootUrl = String.Empty;
        private string _proxyHostIP = "127.0.0.1";
        private int _port = 9050;
        private int _maxRetry = 2;
        private int _maxThreads = (Environment.ProcessorCount / 2) < 1 ? 1 : (Environment.ProcessorCount / 2);
        private int _timeout = 20;
        private bool _includeClearnet = false;
        private bool _includeOnion = true;
        private bool _includeI2P = false;
        private bool _includeIP = false;

        public ICommand StartCrawlingCommand { get; }
        private bool _isCrawling = false;
        public string CrawlingStatus => _isCrawling ? "Crawling..." : "Idle";
        public string CrawlingButtonContent => _isCrawling ? "Stop Crawling" : "Start Crawling";
        private CancellationTokenSource tokenSource = new();


        public ObservableCollection<QueuedLink> queuedLinks = new();
        public ObservableCollection<QueuedLink> processingLinks = new();
        public ObservableCollection<WebPage> fetchedPages = new();
        public bool EditingEnabled => !_isCrawling; 
        private void ExecuteStartCrawling(object parameter)
        {
            if (!_isCrawling)
            {
                ProxyHostIP = ProxyHostIP.Trim();
                var host = ProxyHostIP.StartsWith("socks5://") ? ProxyHostIP : "socks5://" + ProxyHostIP;
                var crawler = new Crawler(host, Port);
                crawler.Configure(o =>
                {
                    o.MaxRetry = MaxRetry;
                    o.MaxThreads = MaxThreads;
                    o.Timeout = TimeSpan.FromSeconds(Timeout);
                    o.IncludeClearnet = IncludeClearnet;
                    o.IncludeOnion = IncludeOnion;
                    o.IncludeI2P = IncludeI2P;
                    o.IncludeIP = IncludeIP;
                });

                crawler.OnQueued += (sender, link) =>
                {
                    link.ForEach(l => queuedLinks.Add(l));
                };

                crawler.OnProcessing += (sender, link) =>
                {

                    queuedLinks.Remove(queuedLinks.First(o => o.Equals(link)));
                    processingLinks.Add(link);
                };

                crawler.OnPageFetched += (sender, page) =>
                {
                    processingLinks.Remove(processingLinks.First(o => o.Equals(page)));
                    fetchedPages.Add(page);
                };

                crawler.OnCrawlingFinished += (sender, pages) =>
                {
                    _isCrawling = false;
                    OnPropertyChanged(nameof(CrawlingStatus));
                    OnPropertyChanged(nameof(CrawlingButtonContent));
                    OnPropertyChanged(nameof(EditingEnabled));
                };

                var rootUrl = RootUrl;
                tokenSource = new();
                Task.Run(()=> crawler.Crawl(rootUrl, tokenSource.Token));
                _isCrawling = true;
                OnPropertyChanged(nameof(CrawlingButtonContent));
                OnPropertyChanged(nameof(EditingEnabled));

            }
            else
            {
                _isCrawling = false;
                tokenSource.Cancel();
                OnPropertyChanged(nameof(EditingEnabled));
                OnPropertyChanged(nameof(CrawlingButtonContent));
            }
        }
        public MainWindowViewModel()
        {
            StartCrawlingCommand = new RelayCommand(ExecuteStartCrawling);
        }
        public string RootUrl
        {
            get => _rootUrl;
            set { _rootUrl = value; OnPropertyChanged(nameof(RootUrl)); }
        }

        public string ProxyHostIP
        {
            get => _proxyHostIP;
            set { _proxyHostIP = value; OnPropertyChanged(nameof(ProxyHostIP)); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(nameof(Port)); }
        }

        public int MaxRetry
        {
            get => _maxRetry;
            set { _maxRetry = value; OnPropertyChanged(nameof(MaxRetry)); }
        }

        public int MaxThreads
        {
            get => _maxThreads;
            set { _maxThreads = value; OnPropertyChanged(nameof(MaxThreads)); }
        }

        public int Timeout
        {
            get => _timeout;
            set { _timeout = value; OnPropertyChanged(nameof(Timeout)); }
        }

        public bool IncludeClearnet
        {
            get => _includeClearnet;
            set { _includeClearnet = value; OnPropertyChanged(nameof(IncludeClearnet)); }
        }

        public bool IncludeOnion
        {
            get => _includeOnion;
            set { _includeOnion = value; OnPropertyChanged(nameof(IncludeOnion)); }
        }

        public bool IncludeI2P
        {
            get => _includeI2P;
            set { _includeI2P = value; OnPropertyChanged(nameof(IncludeI2P)); }
        }

        public bool IncludeIP
        {
            get => _includeIP;
            set { _includeIP = value; OnPropertyChanged(nameof(IncludeIP)); }
        }

        // Validation logic
        public string Error => null;
        public string this[string columnName]
        {

            get
            {
                string onionPattern = @"\bhttps?://[a-z2-7]{16}\.onion\b|\bhttps?://[a-z2-7]{56}\.onion\b";
                string error = null;
                switch (columnName)
                {
                    case nameof(RootUrl):
                        if (!Regex.IsMatch(RootUrl, onionPattern))
                            error = "Please enter a valid URL.";
                        break;
                    case nameof(ProxyHostIP):
                        if (!IPAddress.TryParse(ProxyHostIP, out _))
                            error = "Please enter a valid IP address.";
                        break;
                    case nameof(Port):
                        if (Port < 1 || Port > 65535)
                            error = "Port must be between 1 and 65535.";
                        break;
                    case nameof(MaxRetry):
                        if (MaxRetry < 0)
                            error = "Max Retry cannot be negative.";
                        break;
                    case nameof(MaxThreads):
                        if (MaxThreads < 1)
                            error = "Max Threads must be at least 1.";
                        break;
                    case nameof(Timeout):
                        if (Timeout < 1)
                            error = "Timeout must be at least 1 second.";
                        break;
                }
                return error;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
