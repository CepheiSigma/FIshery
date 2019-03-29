using System;
using System.Collections.Generic;
using Fishery.Core.Extension;
using Fishery.Core.Serialize;
using Fishery.Core.System;
using Fishery.Core.Utils;

namespace Fishery.Core.Data
{
    public class FileDBStorage : BaseStorage, IExtension
    {
        private Dictionary<string, Dictionary<string, object>> _sectionDictionary;
        private List<string> _tableList;

        public FileDBStorage()
        {
            try
            {
                Serializer serializer = new Serializer();
                if (IOManager.Exists(this, "table.json"))
                    _tableList =
                        serializer.DeSerializeFromString<List<string>>(IOManager.ReadFileString(this, "table.json")) ??
                        new List<string>();
                else
                    _tableList = new List<string>();
                _sectionDictionary = new Dictionary<string, Dictionary<string, object>>();
                foreach (var tableName in _tableList)
                {
                    if (IOManager.Exists(this, $"{tableName}_section.json"))
                    {
                        Dictionary<string, object> sectionList =
                            serializer.DeSerializeFromString<Dictionary<string, object>>(
                                IOManager.ReadFileString(this, $"{tableName}_section.json")) ??
                            new Dictionary<string, object>();
                        _sectionDictionary.Add(tableName, sectionList);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Error - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/FileDBStorage]:${ex.Message}\n${ex.StackTrace}");
            }
        }

        ~FileDBStorage()
        {
            Persistence();
        }

        public override void Save(string table, string section, object data)
        {
            try
            {
                Serializer serializer = new Serializer();
                if (String.IsNullOrEmpty(table) || String.IsNullOrEmpty(section))
                    return;
                if (!_sectionDictionary.ContainsKey(table))
                {
                    if (!_tableList.Contains(table))
                        _tableList.Add(table);
                    _sectionDictionary.Add(table, new Dictionary<string, object>());
                    IOManager.WriteFile(this, "table.json", serializer.SerializeToString(_tableList));
                }

                if (!_sectionDictionary[table].ContainsKey(section))
                    _sectionDictionary[table].Add(section, data);
                else
                {
                    _sectionDictionary[table][section] = data;
                }

                IOManager.WriteFile(this, $"{table}_section.json",
                    serializer.SerializeToString(_sectionDictionary[table]));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Error - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/FileDBStorage]:Error occurred when save {table}.{section}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void Save(string table, string section, object data, SaveOptimizer optimizer)
        {
            Serializer serializer = new Serializer();
            Save(table,section,serializer.DeSerializeFromString<object>(optimizer?.Convert(data)));
        }

        public override T Load<T>(string table, string section)
        {
            try
            {
                Serializer serializer = new Serializer();
                if (String.IsNullOrEmpty(table) || String.IsNullOrEmpty(section))
                    return default(T);
                if (_sectionDictionary.ContainsKey(table) && _sectionDictionary[table].ContainsKey(section))
                {
                    return serializer.DeSerializeFromString<T>(
                        serializer.SerializeToString(_sectionDictionary[table][section]));
                }

                return default(T);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        public override T Load<T>(string table, string section, LoadOptimizer<T> optimizer)
        {
            try
            {
                Serializer serializer = new Serializer();
                if (String.IsNullOrEmpty(table) || String.IsNullOrEmpty(section) || optimizer == null)
                    return default(T);
                if (_sectionDictionary.ContainsKey(table) && _sectionDictionary[table].ContainsKey(section))
                {
                    return optimizer.Convert(serializer.SerializeToString(_sectionDictionary[table][section]));
                }

                return default(T);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        public override void Persistence()
        {
            try
            {
                Serializer serializer = new Serializer();
                IOManager.WriteFile(this, "table.json", serializer.SerializeToString(_tableList));
                foreach (var tableName in _tableList)
                {
                    IOManager.WriteFile(this, $"{tableName}_section.json",
                        serializer.SerializeToString(_sectionDictionary[tableName]));
                }
            }
            catch
            {
            }
        }

        public void Initialize()
        {
        }

        public string GetExtensionName()
        {
            return "FileDB Storage";
        }

        public void Uninitialize()
        {
        }
    }
}