using System;

namespace Fishery.Core.System
{
    public class Info
    {
        public static string ProductName => "Fishery OpenSource";
        public static string CoverVersion => "0.0.1";
        public static int CoreVersionCode => 1;
        public static string HostOperationSystem =>
            $"{Environment.OSVersion.Platform.ToString()}_{Environment.OSVersion.Version}";
    }
}