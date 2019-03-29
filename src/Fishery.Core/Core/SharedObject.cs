using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fishery.Core.Serialize;

namespace Fishery.Core
{
    public class SharedObject : MarshalByRefObject
    {
        private Serializer serializer;

        public SharedObject()
        {
            serializer = new Serializer();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public virtual void Dispose()
        {
        }

        public List<string> GetRuntimeFields()
        {
            List<string> fieldNameList = new List<string>();
            var fieldInfoList = GetType().GetRuntimeFields();
            foreach (var fieldInfo in fieldInfoList)
            {
                fieldNameList.Add(fieldInfo.Name);
            }

            return fieldNameList;
        }

        public object GetValue(string name)
        {
            Dictionary<string, object> GetSharedObjectValue(SharedObject value)
            {
                Dictionary<string, object> innerFieldList = new Dictionary<string, object>();
                foreach (string innerName in (value as SharedObject).GetRuntimeFields())
                {
                    try
                    {
                        innerFieldList.Add(innerName, (value as SharedObject).GetValue(innerName));
                    }
                    catch (Exception)
                    {
                        innerFieldList.Add(innerName, "this value was not been shared");
                    }
                }

                return innerFieldList;
            }


            foreach (var fieldInfo in GetType().GetRuntimeFields())
            {
                if (fieldInfo.Name == name)
                {
                    var value = fieldInfo.GetValue(this);

                    if (value is IDictionary)
                    {
                        Dictionary<object, object> innerDictionary = new Dictionary<object, object>();
                        foreach (DictionaryEntry v in value as IDictionary)
                        {
                            if (v.Value is SharedObject)
                                innerDictionary.Add(v.Key, GetSharedObjectValue((SharedObject) v.Value));
                            else
                                innerDictionary.Add(v.Key, v.Value);
                        }

                        return innerDictionary;
                    }

                    if (value is ICollection)
                    {
                        List<object> innerList = new List<object>();
                        foreach (var v in value as ICollection)
                        {
                            if (v is SharedObject)
                                innerList.Add(GetSharedObjectValue((SharedObject) v));
                            innerList.Add(v);
                        }

                        return innerList;
                    }

                    if (value is SharedObject)
                        return GetSharedObjectValue((SharedObject) value);
                    return value;
                }
            }

            return null;
        }

        public object Invoke(string name, BindingFlags flags, object[] parameters)
        {
            return GetType().GetMethod(name, flags).Invoke(this, parameters);
        }
    }
}