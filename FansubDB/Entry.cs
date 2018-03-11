using System;
using System.Collections.Generic;
using LiteDB;

namespace FansubDB
{
    class Entry
    {
        public Entry()
        {
            // Empty || Default constructor for LiteDB
            Id = ObjectId.NewObjectId();
            DateEntryCreated = Id.CreationTime;
        }

        public Entry(string website, string tnc, string pageUrl, DateTime date)
        {
            Website = website;
            TitleAndChapter = tnc;
            PageUrl = pageUrl;
            DatePageCreated = date;
            Id = ObjectId.NewObjectId();
            DateEntryCreated = Id.CreationTime;
        }


        [BsonId] public ObjectId Id { get; set; }

        public string Website { get; set; }
        public string TitleAndChapter { get; set; }
        public string PageUrl { get; set; }
        public DateTime DatePageCreated { get; set; }

        public DateTime DateEntryCreated { get; set; }


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

        public Link(string url, string site, bool isConverted)
        {
            Url = url;
            Site = site;
            IsConverted = isConverted;
        }

        public string Site { get; set; }
        public string Url { get; set; }
        public bool IsConverted { get; set; } = false;
    }
}