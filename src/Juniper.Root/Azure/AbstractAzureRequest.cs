using System;

using Juniper.HTTP;
using Juniper.HTTP.REST;

namespace Juniper.Azure
{
    public abstract class AbstractAzureRequest<MediaTypeT> : AbstractRequest<MediaTypeT>
        where MediaTypeT : MediaType
    {
        private static Uri MakeURI(string region, string component)
        {
            return new Uri($"https://{region}.{component}.microsoft.com/");
        }

        protected AbstractAzureRequest(HttpMethod method, string region, string component, string path, MediaTypeT contentType)
            : base(method, AddPath(MakeURI(region, component), path), contentType)
        { }
    }
}
