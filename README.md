# Onion Crawler
Re-Implementation of an old project of mine in .NET 8 C#.

Project Contains

- Library
- Console Interface
- Web (Blazor) Interface

Requires [`Tor Expert Bundle`](https://www.torproject.org/download/tor/) to be used as SOCKS 5 Proxy. 

## Console Interface Guide
After compiling the console project it can be used from command-line with following arguments
```
  -SHORT NAME --LONG NAME      EXPLAINATION                                               DEFAULT VALUE
  -r, --root                   Required. Root URL to start crawling.                      <REQUIRED>
  -p, --proxy                  Proxy host IP.                                             "socks5://127.0.0.1"
  -o, --port                   Proxy port.                                                 9050
  -m, --max-retry              Max retry count.                                            1
  -t, --max-threads            Max threads.                                                3
  -C, --force-thread-count     Force thread count. (Default limits is Processor Count -1)  false
  -T, --timeout                Timeout in seconds.                                         20
  -c, --include-clearnet       Include clearnet.                                           false
  -n, --include-onion          Include onion.                                              true
  -i, --include-i2p            Include I2P.                                                false
  -I, --include-ip             Include IP.                                                 false
  -d, --max-depth              Max depth.                                                  99999
  -g, --max-pages              Max pages.                                                  999999999
  -u, --ping-url               Ping URL to check proxy is up.                              "https://check.torproject.org/api/ip"
  -f, --filepath               Filepath to extract Fetched WebPages as csv.                "results.csv"
  -b, --batch-size             Batch size to extract fetched WebPages as csv file.         40
  -q, --max-in-queue           Max links in queue.                                         1000
  -F, --failed-links-path      Filepath to save failed links.                              "failed.csv"
  -e, --external-links-path    Filepath to save external links.                            null
  --help                       Display this help screen.                                   -
  --version                    Display version information.                                -
```

## Web UI (Blazor Guide)
After compilng and running the web server you are good to go (Considering Tor Expert Bundle is runniing)
UI Is pretty self-explanatory and easy to use but has limited capabilities compared to Console Interface.
