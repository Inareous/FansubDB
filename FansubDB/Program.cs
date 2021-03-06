﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
            collection.EnsureIndex("DatePageCreated");
//            db.DropCollection("entries"); // debugging purposes
            Console.WriteLine("Configuring Crawler");
            const string link = "https://samehadaku.tv";
            const string link2 = "https://oploverz.id";
            const string link3 = "https://awsubs.co";
            const int startIndex = 1;
            const int endIndex = 10;
            Console.WriteLine("Starting Crawler");
            var checkall = collection.FindAll().ToList();
            Scraper.DoScrape(link, startIndex, endIndex);
//            Scraper.DoScrape(link2, startIndex, endIndex);
//            Scraper.DoScrape(link3, startIndex, endIndex);
            Console.WriteLine("Creating NaN-API Call");

            #region Populating Links To Call
            ConvertEntries(updater);
//            ConvertDB(collection, updater);
            #endregion
            var linkCount = 0;
            var linknotConverted = 0;
            var linkConverted = 0;
            foreach (var entry in Entries)
            foreach (var type in entry.Download.FileType)
            foreach (var li in type.Link)
            {
                linkCount++;
            }

            var res = collection.FindAll();
            foreach (var entry in res)
            foreach (var type in entry.Download.FileType)
            foreach (var linkI in type.Link)
            {
                if (!linkI.IsConverted)
                {
                    linknotConverted++;
                }
                else
                {
                    linkConverted++;
                }
            }

            #region DebugZone

            Console.WriteLine($"Collecting Filter");
//            var find = collection.Find(x => x.DatePageCreated <= DateTime.Today.AddDays(-15) && x.DatePageCreated >= DateTime.Today.AddDays(-20)).ToList();
//            var notConverted = collection.Find(x => !x.IsConverted).ToList();
//            ConvertManualList(notConverted, updater, out var resOut);
            #endregion
            Console.WriteLine("Result :\n");
            Console.WriteLine($"Total entry added : {Entries.Count}");
            Console.WriteLine($"Total new link added : {linkCount}");
            Console.WriteLine($"Total link converted in db : {linkConverted}");
            Console.WriteLine($"Total link not converted in db : {linknotConverted}");
            Console.WriteLine($"Total collection not filled : {collection.Count(x => x.IsFilled.Equals(false))}");
            Console.WriteLine($"Total collection not converted : {collection.Count(x => x.IsConverted.Equals(false))}");
            //            var notFilled = collection.Find(x => x.IsFilled.Equals(false)); // debug
            //            var tryFilter = updater.FilterBySite(false, true, false); // debug
            Console.WriteLine($"Current collection count : {collection.Count()}");
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadLine();
            db.Dispose();
            Console.WriteLine("Exiting . . .");
            //            Thread.Sleep(3000); // 3 sec
        }

        public static void ConvertEntries(DBProcessor updater) // CHANGE SO ONLY SEND AFTER CHECKED
        {
            updater.CheckAndRemove(Entries);
            List<json> populatedLink = new List<json>();
            foreach (var entry in Entries)
            foreach (var type in entry.Download.FileType)
            foreach (var linkI in type.Link)
                populatedLink.Add(new json(linkI.Url, linkI.IsConverted));
            string json = JsonConvert.SerializeObject(populatedLink);
            JsonObj.DoCall(json, out populatedLink);
            Console.WriteLine("Finished");
            var indexing = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                var resConverted = true;
                for (int j = 0; j < Entries[i].Download.FileType.Count; j++)
                {
                    var fileTypeConverted = true;
                    for (int k = 0; k < Entries[i].Download.FileType[j].Link.Count; k++)
                    {
                        Entries[i].Download.FileType[j].Link[k] = new Link(populatedLink[indexing].url, Entries[i].Download.FileType[j].Link[k].Site, populatedLink[indexing].isConverted);
                        indexing++;
                        if (!Entries[i].Download.FileType[j].Link[k].IsConverted)
                        {
                            fileTypeConverted = false;
                        }
                    }
                    if (!fileTypeConverted)
                    {
                        resConverted = false;
                    }
                }
                Entries[i].IsConverted = resConverted;
            }
            updater.Insert(Entries);
        }

        public static void ConvertManualList(List<Entry> res, DBProcessor updater, out List<Entry> resOut)
        {
            resOut = res;
            List<json> populatedLink = new List<json>();
            for (int i = 0; i < resOut.Count; i++)
            {
                for (int j = 0; j < resOut[i].Download.FileType.Count; j++)
                {
                    for (int k = 0; k < resOut[i].Download.FileType[j].Link.Count; k++)
                    {
                        populatedLink.Add(new json(resOut[i].Download.FileType[j].Link[k].Url, resOut[i].Download.FileType[j].Link[k].IsConverted));
                    }
                }
            }
            string json = JsonConvert.SerializeObject(populatedLink);
            JsonObj.DoCall(json, out populatedLink);
            int indexing = 0;
            for (int i = 0; i < resOut.Count; i++)
            {
                var resConverted = true;
                for (int j = 0; j < resOut[i].Download.FileType.Count; j++)
                {
                    var fileTypeConverted = true;
                    for (int k = 0; k < resOut[i].Download.FileType[j].Link.Count; k++)
                    {
                        resOut[i].Download.FileType[j].Link[k] = new Link(populatedLink[indexing].url, resOut[i].Download.FileType[j].Link[k].Site, populatedLink[indexing].isConverted);
                        indexing++;
                        if (!resOut[i].Download.FileType[j].Link[k].IsConverted)
                        {
                            fileTypeConverted = false;
                        }
                    }
                    if (!fileTypeConverted)
                    {
                        resConverted = false;
                    }
                }
                resOut[i].IsConverted = resConverted;
            }

            updater.Insert(resOut);
            Console.WriteLine("Finished");
        }

        public static void ConvertDB(LiteCollection<Entry> collection, DBProcessor updater)
        {
            var res = collection.FindAll().ToList();
//            var resOut = collection.Find(x => x.DatePageCreated <= DateTime.Today.AddDays(-15) && x.DatePageCreated >= DateTime.Today.AddDays(-20)).ToList();
            List<json> populatedLink = new List<json>();
            for (int i = 0; i < res.Count; i++)
            {
                for (int j = 0; j < res[i].Download.FileType.Count; j++)
                {
                    for (int k = 0; k < res[i].Download.FileType[j].Link.Count; k++)
                    {
                        populatedLink.Add(new json(res[i].Download.FileType[j].Link[k].Url, res[i].Download.FileType[j].Link[k].IsConverted));
                    }
                }
            }
            string json = JsonConvert.SerializeObject(populatedLink);
            JsonObj.DoCall(json, out populatedLink);
            int indexing = 0;
            for (int i = 0; i < res.Count; i++)
            {
                var resConverted = true;
                for (int j = 0; j < res[i].Download.FileType.Count; j++)
                {
                    var fileTypeConverted = true;
                    for (int k = 0; k < res[i].Download.FileType[j].Link.Count; k++)
                    {
                        res[i].Download.FileType[j].Link[k] = new Link(populatedLink[indexing].url, res[i].Download.FileType[j].Link[k].Site, populatedLink[indexing].isConverted);
                        indexing++;
                        if (!res[i].Download.FileType[j].Link[k].IsConverted)
                        {
                            fileTypeConverted = false;
                        }
                    }
                    if (!fileTypeConverted)
                    {
                        resConverted = false;
                    }
                }
                res[i].IsConverted = resConverted;
            }
            updater.Insert(res);
            
            Console.WriteLine("Finished");
        }
    }

    internal class JsonObj
    {
        public static void DoCall(string json, out List<json> jsonOut)
        {
            jsonOut = JsonConvert.DeserializeObject<List<json>>(json);
            var x = jsonOut;
            Task task;
            Console.Write("Waiting for response");
            task = Task.Run(async () => { NaNApiCall(json, out x); });
            while (!task.IsCompleted)
            {
                Console.Write(". ");
                Thread.Sleep(5000); // 5 sec
            }
            Console.Write("\n");
            Task.WaitAll(task);
            jsonOut = x;
        }

        public static void NaNApiCall(string json, out List<json> jsonOut)
        {
            jsonOut = JsonConvert.DeserializeObject<List<json>>(json);
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://localhost:29839/bypass");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.KeepAlive = true;
                httpWebRequest.Timeout = 20 * 60 * 1000;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader =
                    new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
                {
                    var result = streamReader.ReadToEnd();
                    jsonOut = JsonConvert.DeserializeObject<List<json>>(result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    internal class json
    {
        public json(string url, bool isConverted)
        {
            this.url = url;
            this.isConverted = isConverted;
        }
        public string url { get; set; }
        public bool isConverted { get; set; }
    }
}