using System.Collections.Generic;

namespace Fishery.Tools.ExtensionPackager
{
    public class MetaDependency
    {
        public string Name;
        public int VersionCode;
    }

    public class Dependency
    {
        public string Name;
        public int VersionCode;
    }

    public class UpdateLog
    {
        public int OriginalVersionCode;
        public int TargetVersionCode;
        public string OriginalVersion;
        public string TargetVersion;
        public string UpdateContent;
    }

    public class Installation
    {
        public string Id;
        public string Name;
        public string Author;
        public string Version;
        public int VersionCode;
        public string Summary;
        public string EntryPoint;
        public List<string> FileList;
        public List<MetaDependency> MetaDependencies;
        public List<Dependency> Dependencies;
        public Stack<UpdateLog> UpdateLog;
    }
}