using System;
using System.Collections.Generic;
using System.IO;

namespace Elympics
{
    public static class BinaryReaderExtensions
    {
        public static Dictionary<int, byte[]> ReadDictionaryIntToByteArray(this BinaryReader br)
        {
            var dataDictCount = br.ReadInt32();
            var dict = new Dictionary<int, byte[]>(dataDictCount);
            for (var i = 0; i < dataDictCount; i++)
            {
                var id = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                dict.Add(id, data);
            }

            return dict;
        }

        public static Dictionary<string, string> ReadDictionaryStringToString(this BinaryReader br)
        {
            var dataDictCount = br.ReadInt32();
            var dict = new Dictionary<string, string>(dataDictCount);
            for (var i = 0; i < dataDictCount; i++)
            {
                var key = br.ReadString();
                var val = br.ReadString();
                dict.Add(key, val);
            }

            return dict;
        }

        public static List<KeyValuePair<int, byte[]>> ReadListWithKvpIntToByteArray(this BinaryReader br)
        {
            var dataListCount = br.ReadInt32();
            var list = new List<KeyValuePair<int, byte[]>>(dataListCount);
            for (var i = 0; i < dataListCount; i++)
            {
                var id = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                list.Add(new KeyValuePair<int, byte[]>(id, data));
            }

            return list;
        }

        public static void ReadIntoDictionaryIntToByteArray(this BinaryReader br, Dictionary<int, byte[]> dict)
        {
            var dataDictCount = br.ReadInt32();
            for (var i = 0; i < dataDictCount; i++)
            {
                var id = br.ReadInt32();
                var dataLength = br.ReadInt32();
                var data = br.ReadBytes(dataLength);
                dict.Add(id, data);
            }
        }

        public static List<T> ReadList<T>(this BinaryReader br, Func<BinaryReader, T> read)
        {
            var dataListCount = br.ReadInt32();
            if (dataListCount < 0)
                return null;
            var list = new List<T>(dataListCount);
            for (var i = 0; i < dataListCount; i++)
                list.Add(read(br));

            return list;
        }
    }
}
