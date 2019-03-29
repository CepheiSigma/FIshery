using System;

namespace Fishery.Core.Extension
{
    public interface IExtension
    {
        void Initialize();
        string GetExtensionName();
        void Uninitialize();
    }
}