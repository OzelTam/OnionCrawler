namespace OnionCrawler.Lib.Models
{
    public class QueuedLink : ILink
    {
        public string Url { get; set; }
        public int Depth { get; set; }
        public int RetryCount { get; set; } = 0;
        public LinkType Type { get; set; }
        public void IncrementRetryCount()
        {
            RetryCount++;
        }
        public QueuedLink(string url, int depth)
        {
            Url = url;
            Depth = depth;
        }
        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj is ILink link)
            {
                return link.Url == Url;
            }
            if (obj is string url)
            {
                return url == Url;
            }
            if (obj is WebPage wp)
            {
                return wp.Url == Url;
            }
            return false;
        }
    }

}
