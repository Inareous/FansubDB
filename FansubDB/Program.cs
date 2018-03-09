using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using LiteDB;
using Newtonsoft.Json;

namespace FansubDB
{
    internal class Program
    {
        public static List<Entry> Entries = new List<Entry>();

        private static void Main()
        {
            //Development Mode
            Console.WriteLine("Opening/Creating Database!");
            var db = new LiteDatabase(@"Database.db"); // unused for now
            var updater = new DBProcessor(db);
            var collection = db.GetCollection<Entry>("entries");
            db.DropCollection("entries"); // debugging purposes
            Console.WriteLine("Configuring Crawler");
            const string link = "https://oploverz.id";
            const int startIndex = 1;
            const int endIndex = 10;
            Console.WriteLine("Starting Crawler");
            Scraper.Scrape(link, startIndex, endIndex);
            Console.WriteLine("Creating Nyan-API Call");

            #region Populating Links To Call

//            jsonOBJ json = new jsonOBJ();
//            foreach (var entry in Entries)
//            foreach (var type in entry.Download.FileType)
//            foreach (var link in type.Link)
//            json.urlArray.Add(link.Url);
//            string JSON_Data = JsonConvert.SerializeObject(json);
//            var result = jsonOBJ.NyanApiCall(JSON_Data);
//            var resultOBJ = JsonConvert.DeserializeObject<jsonOBJ>(result);
//            Console.WriteLine(resultOBJ);

            #endregion

            Console.WriteLine("Updating Links\n");
            updater.Insert(Entries);
            var linkCount = 0;
            foreach (var entry in Entries)
            foreach (var type in entry.Download.FileType)
            foreach (var dummy in type.Link)
                linkCount++;
            Console.WriteLine("Result :\n");
            Console.WriteLine($"Total entry added : {Entries.Count}");
            Console.WriteLine($"Total new link added : {linkCount}");
            Console.WriteLine($"Total collection not filled : {collection.Count(x => x.IsFilled.Equals(false))}");
//            var notFilled = collection.Find(x => x.IsFilled.Equals(false)); // debug
            Console.WriteLine($"Current collection count : {collection.Count()}");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadLine();
            Console.WriteLine("Exiting . . .");
            db.Dispose();
            Thread.Sleep(3000); // 3 sec
        }
    }

    internal class JsonObj
    {
        public List<string> UrlArray = new List<string>();

        public static string NyanApiCall(string json)
        {
            var httpWebRequest = (HttpWebRequest) WebRequest.Create("https://mafuyu-bypasser.herokuapp.com/bypass");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
            using (var streamReader =
                new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }
    }
}