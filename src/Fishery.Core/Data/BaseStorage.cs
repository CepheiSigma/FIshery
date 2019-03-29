using System;
using Fishery.Core.Serialize;

namespace Fishery.Core.Data
{
    public abstract class BaseStorage: SharedObject
    {
        public abstract void Save(string table,string section,object data);
        public abstract void Save(string table,string section,object data, SaveOptimizer optimizer);
        public abstract T Load<T>(string table, string section);
        public abstract T Load<T>(string table, string section, LoadOptimizer<T> optimizer);
        public abstract void Persistence();
    }

    public class SaveOptimizer : SharedObject
    {
        public string Convert(object o)
        {
            Serializer serializer = new Serializer();
            return serializer.SerializeToString(o);
        }
    }

    public class LoadOptimizer<T>:SharedObject
    {

        public T Convert(string serialized)
        {
            try
            {
                Serializer serializer = new Serializer();
                return serializer.DeSerializeFromString<T>(serialized);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}