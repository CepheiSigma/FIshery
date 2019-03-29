
namespace Fishery.Core.Extension
{
    public abstract class SharedExtension : SharedObject, IExtension
    {
        public abstract void Initialize();
        public abstract string GetExtensionName();
        public abstract void Uninitialize();
    }
}