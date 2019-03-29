namespace Fishery.Core.Resource
{
    public interface IResourceFetcher
    {
        BaseResource FetchResourceMetadata(BaseResource resource);
        BaseResource FetchResource(BaseResource resource);
        string GetFetcherName();
    }
}
