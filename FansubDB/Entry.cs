using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FansubDB
{
    class Entry
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
