using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using LiteDB;
using Newtonsoft.Json;

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
            const int endIndex = 2;
            Scraper.Scrape("https://oploverz.id", startIndex,endIndex);
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
            Console.WriteLine("Updating Links");
            var linkCount = 0;
            foreach (var entry in Entries)
            foreach (var type in entry.Download.FileType)
            foreach (var dummy in type.Link)
                linkCount++;
            Console.WriteLine($"Total Link : {linkCount}");
            Console.ReadLine();
        }
    }

    internal class jsonOBJ
    {
        public List<string> urlArray = new List<string>();

        public static string NyanApiCall(string json)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://mafuyu-bypasser.herokuapp.com/bypass");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }
    }
}