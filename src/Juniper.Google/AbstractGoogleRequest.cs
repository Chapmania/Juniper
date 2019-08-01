using System;
using Juniper.HTTP.REST;
using Juniper.Serialization;

namespace Juniper.Google
{
    public abstract class AbstractGoogleRequest<ResultType> : AbstractSingleRequest<ResultType>
    {
        private readonly bool signRequests;

        protected AbstractGoogleRequest(AbstractEndpoint api, Uri baseServiceURI, IDeserializer<ResultType> deserializer, string path, string cacheLocString, bool signRequests)
            : base(api, baseServiceURI, deserializer, path, cacheLocString)
        {
            this.signRequests = signRequests;
        }

        protected override Uri AuthenticatedURI
        {
            get
            {
                var uri = base.AuthenticatedURI;
                if (api is Maps.Endpoint google)
                {
                    uri = google.AddKey(uri);
                    if (signRequests)
                    {
                        uri = google.AddSignature(uri);
                    }
                }
                return uri;
            }
        }
    }
}