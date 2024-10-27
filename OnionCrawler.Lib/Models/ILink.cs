namespace OnionCrawler.Lib.Models
{
    public interface ILink
    {
        string Url { get; set; }
        LinkType Type { get; set; }
    }

}
