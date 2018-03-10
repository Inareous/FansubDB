using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace FansubDB
{
    class DBFIlter
    {
        #region Supported Sites

        private static readonly List<string> SiteList = new List<string>(new[]
        {
            "samehadaku.tv",
            "oploverz.id",
            "awsubs.co"
        });

        #endregion


        public List<Entry> result = new List<Entry>();

        private LiteDatabase Db { get; }

        public DBFIlter(LiteDatabase db)
        {
            Db = db;
        }

        public void FilterBySite(bool siteA, bool siteB, bool siteC)
        {
            var collection = Db.GetCollection<Entry>("entries");

            if (siteA)
            {
                result.AddRange(collection.Find(x => x.Website.Contains(SiteList[0])));
            }

            if (siteB)
            {
                result.AddRange(collection.Find(x => x.Website.Contains(SiteList[1])));
            }

            if (siteC)
            {
                result.AddRange(collection.Find(x => x.Website.Contains(SiteList[2])));
            }
        }

        public void FilterByTitle(string name)
        {
            var collection = Db.GetCollection<Entry>("entries");
            result.AddRange(collection.Find(x => x.TitleAndChapter.Contains(name)));
        }

        public void FilterByURLExist(string link) // add last
        {
            result = (List<Entry>) result.Where(
                x => x.Download.FileType.All(y => y.Link.All(z => z.Url.Contains(link))));
        }

        public void FilterByDLSiteExist(string site) // add last
        {
            result = (List<Entry>)result.Where(
                x => x.Download.FileType.All(y => y.Link.All(z => z.Site.Contains(site))));
        }

        public void FilterByFileTypeExist(string fileType) // add last
        {
            result = (List<Entry>)result.Where(
                x => x.Download.FileType.All(y=> y.File.Contains(fileType)));
        }

        public void DeleteSiteFromResult(string site)
        {
            foreach (var entry in result)
            {
                foreach (var download in entry.Download.FileType)
                {
                    for (var i = download.Link.Count - 1; i >= 0; --i)
                    {
                        if (download.Link[i].Site.Contains("site"))
                        {
                            download.Link.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void OnlySiteFromResult(string url)
        {
            foreach (var entry in result)
            {
                foreach (var download in entry.Download.FileType)
                {
                    for (var i = download.Link.Count - 1; i >= 0; --i)
                    {
                        if (!download.Link[i].Site.Contains("site"))
                        {
                            download.Link.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void DeleteURLFromResult(string url)
        {
            foreach (var entry in result)
            {
                foreach (var download in entry.Download.FileType)
                {
                    for (var i = download.Link.Count - 1; i >= 0; --i)
                    {
                        if (download.Link[i].Url.Contains("url"))
                        {
                            download.Link.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void OnlyURLFromResult(string url)
        {
            foreach (var entry in result)
            {
                foreach (var download in entry.Download.FileType)
                {
                    for (var i = download.Link.Count - 1; i >= 0; --i)
                    {
                        if (!download.Link[i].Url.Contains("url"))
                        {
                            download.Link.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void FlushFilter()
        {
            result.Clear();
        }

        public void NoFilter(bool isNoFilter)
        {
            FlushFilter();
            var collection = Db.GetCollection<Entry>("entries");
            if (isNoFilter)
            {
                result.AddRange(collection.FindAll());
            }
        }
    }
}