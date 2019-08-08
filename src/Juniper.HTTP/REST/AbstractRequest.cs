using System;
using System.Net;
using System.Threading.Tasks;

using Juniper.Progress;

namespace Juniper.HTTP.REST
{
    public abstract class AbstractRequest<ResponseType>
    {
        protected readonly AbstractRequestConfiguration api;

        protected AbstractRequest(AbstractRequestConfiguration api)
        {
            this.api = api;
        }

        public abstract bool IsCached { get; }

        public virtual Task<ResponseType> Get(IProgress prog = null)
        {
            throw new NotImplementedException();
        }

        public virtual Task<ResponseType> GetJPEG(IProgress prog = null)
        {
            throw new NotImplementedException();
        }

        public virtual Task Proxy(HttpListenerResponse response)
        {
            throw new NotImplementedException();
        }
    }
}