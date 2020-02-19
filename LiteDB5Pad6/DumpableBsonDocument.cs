using LINQPad;
using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDBPad
{
    public class DumpableBsonDocument : BsonDocument, ICustomMemberProvider
    {
        public static void RegisterSerializer()
        {
            BsonMapper.Global.RegisterType<DumpableBsonDocument>
            (
                serialize: (doc) => doc,
                deserialize: (bson) => new DumpableBsonDocument(bson.AsDocument.ToDictionary(_=>_.Key, _=>_.Value))
            );
        }

        Dictionary<string, BsonValue> _dict;
        public DumpableBsonDocument(Dictionary<string, BsonValue> dict)
            :base(dict)
        {
            _dict = dict;
        }

        public IEnumerable<string> GetNames()
        {
            return _dict.Keys;
        }

        public IEnumerable<Type> GetTypes()
        {
            foreach (var key in _dict.Keys)
            {
                var item = _dict[key];
                if (item.IsDocument)
                    yield return typeof(DumpableBsonDocument);
                else if (item.IsArray)
                    yield return typeof(IEnumerable);
                else if (item.IsBinary)
                    yield return typeof(byte[]);
                else if (item.IsBoolean)
                    yield return typeof(bool);
                else if (item.IsDateTime)
                    yield return typeof(DateTime);
                else if (item.IsDecimal)
                    yield return typeof(decimal);
                else if (item.IsDouble)
                    yield return typeof(double);
                else if (item.IsGuid)
                    yield return typeof(Guid);
                else if (item.IsInt32)
                    yield return typeof(int);
                else if (item.IsInt64)
                    yield return typeof(long);
                else if (item.IsString)
                    yield return typeof(string);
                else if (item.IsObjectId)
                    yield return typeof(ObjectId);
                else if (item.IsNull)
                    yield return typeof(object);
                else
                    yield return typeof(BsonValue);

            }
        }

        public IEnumerable<object> GetValues()
        {
            foreach (var key in _dict.Keys)
            {
                var item = _dict[key];

                yield return GetValueFromBson(item);
            }
        }

        private static object GetValueFromBson(BsonValue item)
        {
            if (item.IsDocument)
                return new DumpableBsonDocument(item.AsDocument.ToDictionary(_ => _.Key, _ => _.Value));
            else if (item.IsArray)
                return item.AsArray.Select(_ => GetValueFromBson(_));
            else if (item.IsBinary)
                return item.AsBinary;
            else if (item.IsBoolean)
                return item.AsBoolean;
            else if (item.IsDateTime)
                return item.AsDateTime;
            else if (item.IsDecimal)
                return item.AsDecimal;
            else if (item.IsDouble)
                return item.AsDouble;
            else if (item.IsGuid)
                return item.AsGuid;
            else if (item.IsInt32)
                return item.AsInt32;
            else if (item.IsInt64)
                return item.AsInt64;
            else if (item.IsString)
                return item.AsString;
            else if (item.IsObjectId)
                return item.AsObjectId;
            else if (item.IsNull)
                return null;
            else
                return item;
        }
    }
}
