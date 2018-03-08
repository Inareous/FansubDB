using System;
using System.Collections.Generic;
using LiteDB;

namespace FansubDB
{
    internal class Program
    {
        public static List<Entry> Entries = new List<Entry>();

        private static void Main(string[] args)
        {
            //Development Mode
            Console.WriteLine("Opening/Creating Database!");
            var db = new LiteDatabase(@"MyData.db"); // unused for now
            Console.WriteLine("Starting Crawler");
            const int startIndex = 1;
            const int endIndex = 5;
            Scraper.Scrape("https://oploverz.id", startIndex,endIndex);
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
}