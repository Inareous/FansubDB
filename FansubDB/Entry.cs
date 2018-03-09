using System.Collections.Generic;
using LiteDB;

namespace FansubDB
{
    class Entry
    {
        public Entry()
        {
            // Empty || Default constructor for LiteDB
        }

        public Entry(string website, string tnc, string pageUrl)
        {
            Website = website;
            TitleAndChapter = tnc;
            PageUrl = pageUrl;
        }


        [BsonId] public ObjectId Id { get; set; }

        public string Website { get; set; }
        public string TitleAndChapter { get; set; }
        public string PageUrl { get; set; }

        public Download Download { get; set; } = new Download();

        public bool IsFilled { get; set; }
        public bool IsConverted { get; set; }
    }

    internal class Download
    {
        public List<FileType> FileType { get; set; } = new List<FileType>();
    }

    internal class FileType
    {
        public FileType()
        {
            // Empty || Default constructor for LiteDB
        }

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
        public Link()
        {
            // Empty || Default constructor for LiteDB
        }

        public Link(string url, string site)
        {
            Url = url;
            Site = site;
        }

        public string Site;
        public string Url;
    }
}