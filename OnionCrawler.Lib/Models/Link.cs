namespace OnionCrawler.Lib.Models
{
    public struct Link : ILink
    {
        public string Url { get; set; }
        public LinkType Type { get; set; }
        public Link(string url, LinkType linkType)
        {
            Url = url;
            Type = linkType;
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
            if(obj is string url)
            {
                return url == Url;
            }
            if(obj is WebPage wp)
            {
               return wp.Url == Url;
            }
            return false;
        }

    }

}
