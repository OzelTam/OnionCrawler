using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnionCrawler.Lib.Models;


namespace OnionCrawler.Lib
{
    public static class Common
    {
        public const string torPattern = @"\bhttps?://[a-z2-7]{16,56}\.onion\b(?:/[^\s\""><]*)?";
        public const string clearNetPattern = @"\bhttps?://(?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9-]+\.(com|org|net|gov|edu|io|co|biz|info|me|mil|us|uk|ca|de|jp|fr|au|cn|ru|br|it|in|es|nl|se|no|ch|fi|pl|gr|pt|tr|mx|ar|za|nz|sg|kr|hk|my|th|vn|cz|ro|bg|lt|lv|ee|sk|hu|be|is|dk|at|sa|ae|il|ir|pk|bd|tw|ph)\b(?:/[^\s\""><]*)?";
        public const string i2pPattern = @"\bhttps?://[a-zA-Z0-9-]+\.i2p\b(?:/[^\s\""><]*)?";
        public const string ipPattern = @"\bhttps?://(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b(?:/[^\s\""><]*)?";
        public static List<Link> ExtractLinks(string html, bool onion, bool clearNet, bool i2p, bool ip)
        {

            var torMatches = onion ? Regex.Matches(html, torPattern) : null;
            var httpHttpsMatches = clearNet ? Regex.Matches(html, clearNetPattern) : null;
            var i2pMatches = i2p ? Regex.Matches(html, i2pPattern) : null;
            var ipMatches = ip ? Regex.Matches(html, ipPattern) : null;

            var links = new List<Link>();

            if (torMatches != null)
                links.AddRange(torMatches.Select(m => new Link(m.Value, LinkType.Onion)));

            if (httpHttpsMatches != null)
                links.AddRange(httpHttpsMatches.Select(m => new Link(m.Value, LinkType.Clearnet)));

            if (i2pMatches != null)
                links.AddRange(i2pMatches.Select(m => new Link(m.Value, LinkType.I2P)));

            if (ipMatches != null)
                links.AddRange(ipMatches.Select(m => new Link(m.Value, LinkType.Unknown)));

            return links;
        }

        public static LinkType UrlType(this string url)
        {
            if (Regex.IsMatch(url, torPattern))
            {
                return LinkType.Onion;
            }

            if (Regex.IsMatch(url, clearNetPattern))
            {
                return LinkType.Clearnet;
            }

            if (Regex.IsMatch(url, i2pPattern))
            {
                return LinkType.I2P;
            }

            if (Regex.IsMatch(url, ipPattern))
            {
                return LinkType.IP;
            }

            return LinkType.Unknown;
        }

       public static Link ToLink(this string url)
        {
            return new Link(url, url.UrlType());
        }

        public static Link ToLink(this QueuedLink queuedLink)
        {
            return new Link(queuedLink.Url, queuedLink.Url.UrlType());
        }
        public static string ExtractTitle(string html)
        {
            var titleMatch = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
            return titleMatch.Success ? titleMatch.Groups[1].Value : String.Empty;
        }

        public static string ExtractMetaDescription(string html)
        {
            var descriptionMatch = Regex.Match(html, @"<meta\s+name=[""']description[""']\s+content=[""'](.*?)[""']", RegexOptions.IgnoreCase);
            return descriptionMatch.Success ? descriptionMatch.Groups[1].Value : String.Empty;
        }

        public static StringBuilder ToCsv(this IEnumerable<ILink> links, bool includeHeaders = true)
        {
            if (links == null || !links.Any())
            {
                return new StringBuilder();
            }

            var csv = new StringBuilder();
            if (links.First() is QueuedLink)
            {
                if(includeHeaders)
                    csv.AppendLine("Url,Depth,Type");
                foreach (var link in links.Cast<QueuedLink>())
                {
                    csv.AppendLine($"{link.Url.Replace(",", " ")},{link.Depth},{link.Type}");
                }
            }
            else
            {
                if (includeHeaders)
                   csv.AppendLine("Url,Type");
                foreach (var link in links)
                {
                    csv.AppendLine($"{link.Url.Replace(",", " ")},{link.Type}");
                }
            }
            return csv;
        }

        public static StringBuilder ToCsv(this IEnumerable<WebPage> pages, bool includeHeaders = true)
        {
            if (pages == null || !pages.Any())
            {
                return new StringBuilder();
            }

            var csv = new StringBuilder();
            if(includeHeaders)
            csv.AppendLine("Url,Title,Description,Type");
            foreach (var page in pages)
            {
                csv.AppendLine($"{page.Url.Replace(",", " ")},{page.Title.Replace(",", " ")},{page.Description.Replace(",", " ")},{page.Type}");
            }
            return csv;
        }
    }
}
