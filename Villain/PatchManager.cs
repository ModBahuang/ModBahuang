using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Villain
{
    internal class PatchFormatException : FormatException
    {

        public static void ThrowWhenJsonReaderException(FileInfo conf, JsonReaderException e)
        {
            var b = new StringBuilder();
            b.Append($"The file(at {conf.FullName}) in your patch is invalid. ");
            b.Append("Your file must be valid Json format and only have an array element which contains only objects. ");
            b.AppendLine($"Error at path: {e.Path}, line: {e.LineNumber}, position: {e.LinePosition}");

            var message = b.ToString();

            throw new PatchFormatException(message, e);
        }

        public static void ThrowWhenInvalidToken(JToken token, FileInfo conf, Exception inner = null)
        {
            var lineInfo = (IJsonLineInfo)token;
            Logger.Assert(lineInfo.HasLineInfo(), "The token must have line info since it's loadded by `LineInfoHandling.Load`");

            var b = new StringBuilder();
            b.Append($"An inappropriate token found in your file({conf.FullName}).");
            b.AppendLine($"Error at line: {lineInfo.LineNumber} position: {lineInfo.LinePosition}");
            b.AppendLine($"Token type: {token.Type} path: {token.Path}");

            var message = b.ToString();

            throw new PatchFormatException(message, inner);
        }

        public static void ThrowWhenRecordNotFound(KeyNotFoundException e, FileInfo conf)
        {
            var b = new StringBuilder();
            b.Append($"The name of file({conf.FullName}) is an invalid game conf class name.");

            var message = b.ToString();

            throw new PatchFormatException(message, e);
        }

        private PatchFormatException(string message, Exception inner) : base(message, inner) { }
    }

    internal static class PatchManager
    {
        private class Processor
        {
            private readonly Dictionary<Record, Dictionary<int, Item>> items;

            private Dictionary<int, Item> current;
            private Record currentRecord;
            private FileInfo currentConf;

            public Processor(Dictionary<Record, Dictionary<int, Item>> buf)
            {
                items = buf;
            }

            private void UpdateState(FileInfo conf)
            {
                currentConf = conf;
                var name = Path.GetFileNameWithoutExtension(conf.Name);

                try
                {
                    currentRecord = ConfClassRecordStore.ByName(name);
                }
                catch (KeyNotFoundException e)
                {
                    PatchFormatException.ThrowWhenRecordNotFound(e, conf);
                }

                current = items.GetOrAdd(currentRecord, new Dictionary<int, Item>());
            }

            public void Process(FileInfo conf)
            {
                UpdateState(conf);

                JArray array = null;

                using (var reader = new JsonTextReader(currentConf.OpenText()))
                {
                    try
                    {
                        array = JArray.Load(reader, new JsonLoadSettings
                        {
                            LineInfoHandling = LineInfoHandling.Load
                        });
                    }
                    catch (JsonReaderException e)
                    {
                        PatchFormatException.ThrowWhenJsonReaderException(currentConf, e);
                    }
                }

                Logger.Assert(array != null, "Array must not be null since it whether be assigned or an exception has been thrown.");
                ProcessArray(array);
            }

            private void ProcessArray(JArray array)
            {
                foreach (var token in array)
                {
                    if (token.Type != JTokenType.Object)
                    {
                        PatchFormatException.ThrowWhenInvalidToken(token, currentConf);
                    }

                    // Checked token type above
                    ProcessObject(token.Value<JObject>());
                }
            }

            private void ProcessObject(JObject obj)
            {
                Item item = null;
                try
                {
                    item = Item.From(currentRecord, obj, currentConf);
                }
                catch (ArgumentException e)
                {
                    // FIXME
                    PatchFormatException.ThrowWhenInvalidToken(obj, currentConf, e);
                }

                var id = item?.Id ?? throw new Exception("Unreachable!");

                if (current.ContainsKey(id))
                {
                    var existed = current[id];

                    Logger.Warn($"Conflict between " +
                                $"Item1@({existed.Source})" +
                                " & " +
                                $"Item2@({item.Source})\n" +
                                "Item1 will be overriden/merged.");

                    existed.Merge(item);
                }
                else
                {
                    current.Add(id, item);
                }

            }
        }

        private static readonly Dictionary<Record, Dictionary<int, Item>> ITEMS_BY_RECORD = new Dictionary<Record, Dictionary<int, Item>>();

        public static void LoadAndApplyOnlyPatch(IEnumerable<DirectoryInfo> patches)
        {
            LoadAll(patches);
            foreach (var record in ITEMS_BY_RECORD.Keys)
            {
                ConfInitBridge.Erect(record);
            }
        }

        private static void LoadAll(IEnumerable<DirectoryInfo> patches)
        {
            var processor = new Processor(ITEMS_BY_RECORD);

            try
            {
                patches.ForEach(patch => patch.EnumerateFiles("*.json")
                    .ForEach(processor.Process));
            }
            catch (PatchFormatException e)
            {
                Logger.Warn($"{e}");
            }
        }

        public static void ApplyAll()
        {
            foreach (var record in ConfClassRecordStore.Records())
            {
                ConfInitBridge.Erect(record);
            }
        }

        public static Dictionary<int, Item> ByRecord(Record record)
        {
            return ITEMS_BY_RECORD[record];
        }
    }
}
