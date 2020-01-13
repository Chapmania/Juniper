using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Juniper.HTTP.Server.Administration.NetSH.Tests
{
    [TestClass]
    public class NetSHTests
    {
        private const string testAddressString1 = "192.160.0.1";
        private static readonly IPAddress testAddress1 = IPAddress.Parse(testAddressString1);

        private static Task<bool> ExistsAsync()
        {
            var command = new GetFirewallRule("Test Ban");
            return command.ExistsAsync();
        }

        private async Task MaybeAddRuleAsync()
        {
            var exists = await ExistsAsync()
                .ConfigureAwait(false);
            if (!exists)
            {
                await AddRuleAsync()
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AddRuleAsync()
        {
            var command = new AddFirewallRule("Test Ban", FirewallRuleDirection.Out, FirewallRuleAction.Block, new CIDRBlock(testAddress1));
            var retCode = await command.RunAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(0, retCode);
            Assert.IsTrue(command.TotalStandardOutput.Length > 0);
        }

        [TestMethod]
        public async Task DeleteRuleAsync()
        {
            await MaybeAddRuleAsync()
                .ConfigureAwait(false);

            var command = new DeleteFirewallRule("Test Ban");
            var deleteCount = await command.RunAsync()
                .ConfigureAwait(false);
            Assert.IsTrue(deleteCount >= 1);
        }

        [TestMethod]
        public async Task RuleExistsAsync()
        {
            await MaybeAddRuleAsync()
                .ConfigureAwait(false);

            var command = new GetFirewallRule("Test Ban");
            Assert.IsTrue(await command.ExistsAsync()
                .ConfigureAwait(false));
        }

        [TestMethod]
        public async Task GetRulesAsync()
        {
            await MaybeAddRuleAsync()
                .ConfigureAwait(false);

            var command = new GetFirewallRule("Test Ban");
            var blocks = await command.GetRangesAsync()
                .ConfigureAwait(false);
            Assert.IsTrue(blocks.Length > 0);
            Assert.AreEqual(testAddress1, blocks[0].Start);
        }
    }
}
