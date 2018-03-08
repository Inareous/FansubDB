using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using LiteDB;

namespace FansubBDTest
{
    internal class Program
    {
        public static List<Entry> Entries = new List<Entry>();

        private static void Main(string[] args)
        {
            Console.WriteLine("Opening/Creating Database!");
            var db = new LiteDatabase(@"MyData.db");
            Console.WriteLine("Starting Crawler");
            Scraper.Scrape("https://awsubs.co", 1, 40);
            Console.WriteLine("Updating Database");
            var linkCount = 0;
            foreach (var entry in Entries)
            foreach (var type in entry.Download.FileType)
            foreach (var dummy in type.Link)
                linkCount++;
            Console.WriteLine($"Total Link : {linkCount}");
            Console.ReadLine();
        }
    }

    internal class Scraper
    {
        public static readonly List<string> SiteList = new List<string>(new string[]
        {
            "samehadaku.tv",
            "oploverz.id",
            "awsubs.co"
        });

        private static readonly HttpClientHandler Handler = new HttpClientHandler()
        {
            UseProxy = false,
            Proxy = null,
            PreAuthenticate = true,
            UseDefaultCredentials = false,
            MaxAutomaticRedirections = 4,
            MaxRequestContentBufferSize = Int32.MaxValue,
        };

        private static readonly HttpClient Connection = new HttpClient(Handler);


        public static void Scrape(string baseurl, int startIndex, int lastIndex)
        {
            //set Connection SPM
            ServicePointManager.DefaultConnectionLimit = 20;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            //set Accept headers for HttpClient
            Connection.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml,application/json");
            //set User agent for HttpClient
            Connection.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; EN; rv:11.0) like Gecko");
            Connection.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");
            var Tasks = new List<Task>();
            for (var i = startIndex; i <= lastIndex; i++)
            {
                var index = i.ToString();
                Tasks.Add(Task.Run(() => StartPageCrawlerAsync(baseurl, index)));
            }

            Task.WaitAll(Tasks.ToArray());
            //StartPageCrawlerAsync(baseurl, lastIndex.ToString()).ConfigureAwait(true); // For debugging purpose
        }

        private static async Task StartPageCrawlerAsync(string baseurl, string urlString)
        {
            try
            {
                #region Select Sites

                var checksite = 99;
                foreach (var site in SiteList)
                {
                    if (!baseurl.Contains(site)) continue;
                    checksite = SiteList.IndexOf(site);
                    break;
                }

                IHtmlDocument doc;
                HtmlParser parser;
                string append;
                IHtmlCollection<IElement> node;
                string urlText;
                switch (checksite)
                {
                    #region samehadaku

                    case 0: //samehadaku
                        append = "page";
                        urlText = $"{baseurl}/{append}/{urlString}";
                        using (var response = Connection.GetAsync(urlText).Result
                        ) // sync call. Async ver : "var response = await Connection.SendAsync(request).ConfigureAwait(true)"
                        {
                            parser = new HtmlParser();
                            var content = response.Content.ReadAsStringAsync().Result; //sync call
                            doc = await parser.ParseAsync(content).ConfigureAwait(true);
                            node = doc.QuerySelectorAll("div[id^='tie-block_2598']>" +
                                                        "div[class^='container-wrapper']>" +
                                                        "div[class^='mag-box-container clearfix']>" +
                                                        "ul[class^='posts-items posts-list-container']>" +
                                                        "li");
                            foreach (var list in node)
                            {
                                var tnc = list.QuerySelector("a").TextContent;
                                var urlPage = list.QuerySelector("a").GetAttribute("href");
                                var startEntry = new Entry(baseurl, tnc, urlPage);
                                await StartDownloadLinkCrawlerAsync(baseurl, startEntry).ConfigureAwait(true);
                                Program.Entries.Add(startEntry);
                            }
                        }

                        return;

                    #endregion

                    #region oplovers

                    case 1: //oplovers
                        return;

                    #endregion

                    #region awsubs

                    case 2: //awsubs
                        append = "page";
                        //                            html = web.DownloadString($"{baseurl}/{append}/{urlString}");
                        urlText = $"{baseurl}/{append}/{urlString}";
                        using (var response = await Connection.GetAsync(urlText).ConfigureAwait(true)) // sync call
                        {
                            parser = new HtmlParser();
                            var content = response.Content.ReadAsStringAsync().Result; //sync call
                            doc = await parser.ParseAsync(content).ConfigureAwait(true);
                            node = doc.QuerySelectorAll("div[class^='aztanime'] " +
                                                        "div[class^='chan']");
                            foreach (var list in node)
                            {
                                var tnc = list.QuerySelector("a").TextContent;
                                var urlPage = list.QuerySelector("a").GetAttribute("href");
                                var startEntry = new Entry(baseurl, tnc, urlPage);
                                await StartDownloadLinkCrawlerAsync(baseurl, startEntry).ConfigureAwait(true);
                                Program.Entries.Add(startEntry);
                            }
                        }

                        return;

                    #endregion

                    case 99: // Default
                        return;
                    default:
                        return;
                }

                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e.InnerException != null) Console.WriteLine(e.InnerException.Message);
            }
        }

        private static async Task StartDownloadLinkCrawlerAsync(string baseurl, Entry startEntry)
        {
            try
            {
                var checksite = 99;
                foreach (var site in SiteList)
                {
                    if (baseurl.Contains(site))
                    {
                        checksite = SiteList.IndexOf(site);
                    }
                }

                IHtmlCollection<IElement> node;
                switch (checksite)
                {
                    #region samehadaku

                    case 0:
                        using (var response = await Connection.GetAsync(startEntry.PageUrl).ConfigureAwait(true))
                        {
                            var parser = new HtmlParser();

                            var content = response.Content.ReadAsStringAsync().Result;
                            var doc = await parser.ParseAsync(content).ConfigureAwait(true);


                            node = doc.QuerySelectorAll("div[class^='download-eps']>" +
                                                        "ul>" +
                                                        "li");
                            foreach (var actualList in node)
                            {
                                var fileType = actualList.QuerySelector("strong").TextContent;
                                var links = new List<Link>();
                                var spanContainer = actualList.QuerySelectorAll("span");
                                try
                                {
                                    links.AddRange(
                                        from span in spanContainer
                                        from link in span.QuerySelectorAll("a")
                                        let dl = link.GetAttribute("href")
                                        let site = link.TextContent
                                        select new Link(dl, site));
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }

                                startEntry.Download.FileType.Add(new FileType(fileType, links));
                                startEntry.IsFilled = true;
                            }
                        }

                        return;

                    #endregion

                    #region oplovers

                    case 1: //oplovers
                        return;

                    #endregion

                    #region awsubs

                    case 2: //awsubs - this thing sucks. Connection always closed by remote host and idk how to fix
                        using (var response = await Connection.GetAsync(startEntry.PageUrl).ConfigureAwait(true))
                        {
                            var parser = new HtmlParser();

                            var content = response.Content.ReadAsStringAsync().Result;
                            var doc = await parser.ParseAsync(content).ConfigureAwait(true);
                            var nodeLink = doc.QuerySelectorAll("div[class^='dl-box']>" +
                                                                "div[class^='dl-item']");
                            var nodeTitle = doc.QuerySelectorAll("div[class^='dl-box']>" +
                                                                 "div[class^='dl-title']");

                            for (var listIndex = 0; listIndex < nodeLink.Length; listIndex++)
                            {
                                var fileType = nodeTitle[listIndex].TextContent;
                                var links = new List<Link>();
                                var aLink = nodeLink[listIndex].QuerySelectorAll("a");
                                try
                                {

                                    links.AddRange(
                                        from link in aLink
                                        let dl = link.GetAttribute("href")
                                        let site = link.TextContent
                                        select new Link(dl, site));
                                }
                                catch (Exception)
                                { 
                                  //ignored
                                }


                                startEntry.Download.FileType.Add(new FileType(fileType, links));
                                startEntry.IsFilled = true;
                            }
                        }
                        return;

                    #endregion

                    case 99: // Default
                        return;
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e.InnerException != null) Console.WriteLine(e.InnerException.Message);
            }
        }
    }


    internal class Entry
    {
        public Entry(string website, string tnc, string pageUrl)
        {
            Website = website;
            TitleAndChapter = tnc;
            PageUrl = pageUrl;
        }

        public string Website { get; set; }
        public string TitleAndChapter { get; set; }
        public string PageUrl { get; set; }

        public Download Download { get; set; } = new Download();

        public bool IsFilled { get; set; }
    }

    internal class Download
    {
        public List<FileType> FileType { get; set; } = new List<FileType>();
    }

    internal class FileType
    {
        public FileType(string file, List<Link> links)
        {
            File = file;
            Link = links;
        }

        public string File { get; set; }
        public string Resolution { get; set; }
        public List<Link> Link { get; set; }
    }

    internal class Link
    {
        public string Site;
        public string Url;

        public Link(string url, string site)
        {
            Url = url;
            Site = site;
        }
    }
}