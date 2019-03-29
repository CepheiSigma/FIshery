using System;

namespace Fishery.Core.Resource
{
    public class BaseResource: SharedObject
    {
        public string Id;
        public string Name;
        public long Size;
        public DateTime CreatedTime;
        public DateTime ModifiedTime;
        public string PhysicPath;
        public string NetworkPath;
        public bool NeedMaintenance;
        public string LocatorName;
        public string ForeignId;


        public BaseResource()
        {
            
        }
        public BaseResource(string id,string name, long size, string physicPath, string networkPath, DateTime createdTime,
            DateTime modifiedTime)
        {
            Id = id;
            Name = name;
            Size = size;
            CreatedTime = createdTime;
            ModifiedTime = modifiedTime;
            PhysicPath = physicPath;
            NetworkPath = networkPath;
            NeedMaintenance = false;
            LocatorName = "";
            ForeignId = "";
        }
    }
}