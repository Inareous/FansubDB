using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace FansubDB
{
    class Scraper
    {
        #region Supported Sites
        public static readonly List<string> SiteList = new List<string>(new[]
        {
            "samehadaku.tv",
            "oploverz.id",
            "awsubs.co"
        });
        #endregion


        private static readonly HttpClientHandler Handler = new HttpClientHandler()
        {
            UseProxy = false,
            Proxy = null,
            PreAuthenticate = true,
            UseDefaultCredentials = true,
            MaxAutomaticRedirections = 4,
            MaxRequestContentBufferSize = Int32.MaxValue,
        };

        private static readonly HttpClient Connection = new HttpClient(Handler);


        public static void DoScrape(string baseurl, int startIndex, int lastIndex)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            Task task;
            Console.Write("Crawling");
            task = Task.Run(async () => { Scrape(baseurl, startIndex, lastIndex); });
            while (!task.IsCompleted)
            {
                Console.Write(". ");
                Thread.Sleep(5000); // 5 sec
            }
            Console.Write("\n");
            Task.WaitAll(task);
        }


        public static async Task Scrape(string baseurl, int startIndex, int lastIndex)
        {

            #region Connection Configuration
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
            #endregion
            var tasks = new List<Task>();
            for (var i = startIndex; i <= lastIndex; i++)
            {
                var index = i.ToString();
                tasks.Add(Task.Run(() => StartPageCrawlerAsync(baseurl, index)));
            }

            Task.WaitAll(tasks.ToArray());
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
                        using (var response = await Connection.GetAsync(urlText).ConfigureAwait(true)) // sync call. Async ver : "var response = await Connection.SendAsync(request).ConfigureAwait(true)"
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
                                var x = list.QuerySelectorAll("span[class^='date meta-item'] span");
                                var dateHolder = x[x.Length-1].TextContent;
                                var date = DateTime.ParseExact(dateHolder, "MMMM d, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
//                                DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                                var tnc = list.QuerySelector("a").TextContent;
                                var urlPage = list.QuerySelector("a").GetAttribute("href");
                                var startEntry = new Entry(baseurl, tnc, urlPage, date);
                                await StartDownloadLinkCrawlerAsync(baseurl, startEntry).ConfigureAwait(true);
                                Program.Entries.Add(startEntry);
                            }
                        }

                        return;

                    #endregion

                    #region oplovers

                    case 1: //oplovers
                        append = "page";
                        urlText = $"{baseurl}/{append}/{urlString}";
                        using (var response = await Connection.GetAsync(urlText).ConfigureAwait(true)) // sync call. Async ver : "var response = await Connection.SendAsync(request).ConfigureAwait(true)"
                        {
                            parser = new HtmlParser();
                            var content = response.Content.ReadAsStringAsync().Result; //sync call
                            doc = await parser.ParseAsync(content).ConfigureAwait(true);
                            node = doc.QuerySelectorAll("div[class^='td-ss-main-content'] " +
                                                        "div[class^='td-block-span6'] " +
                                                        "div[class^='item-details']");
                            foreach (var list in node)
                            {
                                var x = list.QuerySelector("time").TextContent;
                                var date = DateTime.ParseExact(x, "MMMM d, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                                var tnc = list.QuerySelector("a").GetAttribute("title");
                                var urlPage = list.QuerySelector("a").GetAttribute("href");
                                var startEntry = new Entry(baseurl, tnc, urlPage,date);
                                await StartDownloadLinkCrawlerAsync(baseurl, startEntry).ConfigureAwait(true);
                                Program.Entries.Add(startEntry);
                            }
                        }
                        return;

                    #endregion

                    #region awsubs

                    case 2: //awsubs - this thing sucks. Connection always closed by remote host and idk how to fix
                        append = "page";
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
                                var date = DateTime.Today;
                                try
                                {
                                    var x = list.QuerySelector("div[class^='details'] div[class^='kategori']").TextContent;
                                    var xSplit = x.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    var dateholder = xSplit[0].Replace(" Released on ", "");
                                    var datesplit = dateholder.Split(' ');
                                    datesplit[1] = Regex.Replace(datesplit[1],@"\D+", string.Empty);
                                    dateholder = String.Join(" ", datesplit);
                                    date = DateTime.ParseExact(dateholder, "MMMM d yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                                }
                                catch(Exception)
                                {
                                }
                                var tnc = list.QuerySelector("a").TextContent;
                                var urlPage = list.QuerySelector("a").GetAttribute("href");
                                var startEntry = new Entry(baseurl, tnc, urlPage,date);
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
                Console.WriteLine($"\n{e.Message}");
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
                                var linkContainer = actualList.QuerySelectorAll("a");
                                try
                                {
                                    links.AddRange(
                                        from link in linkContainer
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
                        using (var response = await Connection.GetAsync(startEntry.PageUrl).ConfigureAwait(true))
                        {
                            var parser = new HtmlParser();

                            var content = response.Content.ReadAsStringAsync().Result;
                            var doc = await parser.ParseAsync(content).ConfigureAwait(true);


                            node = doc.QuerySelectorAll("div[class^='download-eps']>" +
                                                        "ul>" +
                                                        "li");
                            var pagetype = 1;
                            if (!node.Any())
                            {
                                pagetype = 2;
                            }
                            switch (pagetype)
                            {
                                case 1:
                                    foreach (var actualList in node)
                                    {
                                        var fileType = actualList.QuerySelector("span").TextContent;
                                        var links = new List<Link>();
                                        var linkContainer = actualList.QuerySelectorAll("a");
                                        try
                                        {
                                            links.AddRange(
                                                from link in linkContainer
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
                                    break;
                                case 2:
                                    var nodeLink = doc.QuerySelectorAll("div[class^='soraddl op-download']>" +
                                                                        "div[class^='soraurl list-download']");
                                    var nodeTitle = doc.QuerySelectorAll("div[class^='soraddl op-download']>" +
                                                                         "div[class^='sorattl title-download']");
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

                                    break;
                            }
                            
                        }
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
                Console.WriteLine($"\n{e.Message}");
                if (e.InnerException != null) Console.WriteLine(e.InnerException.Message);
            }
        }
    }
}
