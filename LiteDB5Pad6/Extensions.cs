using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LiteDBPad
{
    public static class Extensions
    {
        public static DumpableBsonDocument Dump(this BsonDocument document)
        {
            return new DumpableBsonDocument(document.ToDictionary(_ => _.Key, _ => _.Value));
        }

        public static IEnumerable<DumpableBsonDocument> Dump(this IEnumerable<BsonDocument> documents)
        {
            foreach (var doc in documents)
                yield return new DumpableBsonDocument(doc.ToDictionary(_ => _.Key, _ => _.Value));
        }

        public static string ElementValue(this XElement element, string name, string defaultValue = null)
        {
            var child = element.Element(name);
            if (child != null)
                return child.Value;

            return defaultValue;
        }

        public static T ElementValue<T>(this XElement element, string name, Func<string, T> parseFunc, T defaultValue = default(T))
        {
            var child = element.Element(name);
            if (child != null)
                return parseFunc(child.Value);

            return defaultValue;
        }
    } 
}
