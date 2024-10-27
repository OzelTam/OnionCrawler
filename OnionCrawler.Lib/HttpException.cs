using System.Net;

namespace OnionCrawler.Lib
{
    public class FetchException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public HttpContent Content { get; set; }
        public string Url { get; set; }
        public FetchException(string message,string url, HttpStatusCode statusCode, HttpContent content) : base(message)
        {
            StatusCode = statusCode;
            Content = content;
            Url = url;
        }

    }

}

