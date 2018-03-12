using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiteDB;

namespace FansubDB
{
    class DBProcessor
    {
        private LiteDatabase Db { get; }

        public DBProcessor(LiteDatabase db)
        {
            Db = db;
        }

        public void CheckAndRemove(List<Entry> entries) // iteration, sync. VERY SLOW
        {
            var collection = Db.GetCollection<Entry>("entries");
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var index = i;
                var item = collection.Find(x =>
                    x.PageUrl.Equals(entries[index].PageUrl) && x.TitleAndChapter.Equals(entries[index].TitleAndChapter)).ToList();
                if (!item.Any()) continue;
                {
                    if (item.Any(x => x.IsFilled.Equals(true) &&
                                      x.IsConverted.Equals(true)))
                    {
                        entries.RemoveAt(i);
                    }
                }
            }
            collection.InsertBulk(entries);
        }

        public void Insert(List<Entry> entries) // iteration, sync. VERY SLOW
        {
            var collection = Db.GetCollection<Entry>("entries");
            for (var i = entries.Count - 1; i >= 0; i--)
            {
//              if (collection.Exists(Query.And(Query.EQ("PageUrl", entries[i].PageUrl), Query.EQ("TitleAndChapter", entries[i].TitleAndChapter))))
                var item = collection.Find(x =>
                    x.PageUrl.Equals(entries[i].PageUrl) && x.TitleAndChapter.Equals(entries[i].TitleAndChapter)).ToList();
                if (item.Any())
                {
                    foreach (var x in item)
                    {
                        if (x.IsFilled.Equals(false) && entries[i].IsFilled.Equals(true) ||
                            x.IsConverted.Equals(false) && entries[i].IsConverted.Equals(true))
                        {
                            collection.Update(x.Id, entries[i]);
                        }
                        else if (x.IsFilled.Equals(true) && entries[i].IsFilled.Equals(false) ||
                                 x.IsConverted.Equals(true) && entries[i].IsConverted.Equals(false))
                        {
                        }
                        else
                        {
                            var converted = false;
                            foreach (var links in entries[i].Download.FileType)
                            {
                                foreach (var link in links.Link)
                                {
                                    if (!link.IsConverted) continue;
                                    converted = true;
                                    break;
                                }

                                if (converted)
                                {
                                    break;
                                }
                            }

                            if (converted)
                            {
                                collection.Update(x.Id, entries[i]);
                            }
                        }

//                        else
//                        {
//                            Console.WriteLine("Id Missmatch, possibly a duplicate. . . Deleting duplicate");
//                            collection.Delete(x.Id);
//                        }
                    }

                    entries.RemoveAt(i);
                }
            }

            collection.InsertBulk(entries);
        }
    }
}