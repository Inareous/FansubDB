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

        public void Insert(List<Entry> entries) // iteration, sync. VERY SLOW
        {
            var collection = Db.GetCollection<Entry>("entries");
            for (var i = entries.Count - 1; i >= 0; --i)
            {
//              if (collection.Exists(Query.And(Query.EQ("PageUrl", entries[i].PageUrl), Query.EQ("TitleAndChapter", entries[i].TitleAndChapter))))
                var item = collection.FindOne(x =>
                    x.PageUrl.Equals(entries[i].PageUrl) && x.TitleAndChapter.Equals(entries[i].TitleAndChapter));
                if (item != null)
                {
                    if (item.IsFilled.Equals(false) && entries[i].IsFilled.Equals(true) ||
                        item.IsConverted.Equals(false) && entries[i].IsConverted.Equals(true))
                    {
                        collection.Update(item.Id, entries[i]);
                    }

                    entries.RemoveAt(i);
                }
            }

            collection.InsertBulk(entries);
        }
    }
}