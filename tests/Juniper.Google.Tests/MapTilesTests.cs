using System.Net;
using System.Text;
using System.Threading.Tasks;

using Juniper.Google.Maps.Tests;
using Juniper.Image;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Juniper.Google.Maps.MapTiles.Tests
{
    [TestClass]
    public class MapTilesTests : ServicesTests
    {
        [TestMethod]
        public void EncodeOnePart()
        {
            var sb = new StringBuilder();
            var first = 0;
            LinePath.EncodePolylinePart(sb, -179.9832104, ref first);
            var encoded = sb.ToString();
            Assert.AreEqual("`~oia@", encoded);
        }

        [TestMethod]
        public void EncodePair()
        {
            var input = "38.5, -120.2";
            var expected = "_p~iF~ps|U";
            EncodePolylinePartTest(expected, input);
        }

        [TestMethod]
        public void EncodeString()
        {
            var input = "38.5, -120.2|40.7, -120.95|43.252, -126.453".Split('|');
            var expected = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";
            EncodePolylinePartTest(expected, input);
        }

        private static void EncodePolylinePartTest(string expected, params string[] input)
        {
            var encoded = LinePath.EncodePolyline(input);
            Assert.AreEqual(expected, encoded);
        }

        [TestMethod]
        public async Task GetImage()
        {
        }
    }
}