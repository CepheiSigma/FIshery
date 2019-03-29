using System.Collections.Generic;

namespace Fishery.Core.Resource
{
    public interface IResourceLocator
    {
        List<BaseResource> GetTargetList(List<object> condition=null);
        BaseResource CheckResource(BaseResource target,List<BaseResource> resourceList);
        BaseResource MaintainResource(BaseResource resource);
        string GetLocatorName();
    }
}