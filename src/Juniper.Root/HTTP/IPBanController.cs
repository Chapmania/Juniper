using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Juniper.HTTP
{
    public sealed class IPBanController : AbstractRouteHandler
    {
        private readonly List<CIDRBlock> blocks = new List<CIDRBlock>();

        private readonly FileInfo banFile;

        public IPBanController()
        { }

        public IPBanController(IEnumerable<CIDRBlock> blocks)
            : base(null, int.MinValue, HttpProtocols.All, HttpMethods.All)
        {
            this.blocks.AddRange(blocks);
        }

        public IPBanController(Stream banFileStream)
            : this(CIDRBlock.Load(banFileStream))
        { }

        public IPBanController(FileInfo banFile)
            : this(CIDRBlock.Load(banFile))
        {
            this.banFile = banFile;
        }

        public IPBanController(string banFileName)
            : this(CIDRBlock.Load(banFileName))
        {
            banFile = new FileInfo(banFileName);
        }

        private CIDRBlock GetMatchingBlock(IPAddress address)
        {
            return blocks.Find(block => block.Contains(address));
        }

        public override bool IsMatch(HttpListenerRequest request)
        {
            var block = GetMatchingBlock(request.RemoteEndPoint.Address);
            if (block != null)
            {
                return true;
            }

            return false;
        }

        public override bool CanContinue(HttpListenerRequest request)
        {
            var block = GetMatchingBlock(request.RemoteEndPoint.Address);
            return base.CanContinue(request)
                && block is null;
        }

        public override Task InvokeAsync(HttpListenerContext context)
        {
            var block = GetMatchingBlock(context.Request.RemoteEndPoint.Address);
            OnInfo($"{context.Request.RemoteEndPoint} is banned by {block}.");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return Task.CompletedTask;
        }
    }
}
