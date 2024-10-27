namespace OnionCrawler.Lib.Models
{
    public class WebPage
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Host { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public int Depth { get; set; }
        public int ExtractedLinkCount { get; set; }
        public LinkType Type { get; set; }

        public WebPage(string url, string title, string description, int depth)
        {
            Url = url;
            Id = Url.GetHashCode().ToString();
            Title = title;
            Host = new Uri(url).Host;
            Description = description;
            Depth = depth;
        }

        public override int GetHashCode()
        {
            return Url.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is WebPage page)
            {
                return page.Url == Url;
            }
            if (obj is string url)
            {
                return url == Url;
            }
            if (obj is ILink link)
            {
                return link.Url == Url;
            }

            return false;
        }

    }

}

