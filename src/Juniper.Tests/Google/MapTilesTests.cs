using System;
using System.Text;
using System.Threading.Tasks;

using Juniper.Imaging;
using Juniper.IO;
using Juniper.World.GIS.Google.Tests;

using NUnit.Framework;

namespace Juniper.World.GIS.Google.MapTiles.Tests
{
    [TestFixture]
    public class MapTilesTests : ServicesTests
    {
        [Test]
        public void EncodeOnePart()
        {
            var sb = new StringBuilder();
            var first = 0;
            LinePathCollection.EncodePolylinePart(sb, -179.9832104, ref first);
            var encoded = sb.ToString();
            Assert.AreEqual("`~oia@", encoded);
        }

        [Test]
        public void EncodePair()
        {
            EncodePolylinePartTest(
                "_p~iF~ps|U",
                "38.5, -120.2");
        }

        [Test]
        public void EncodeString()
        {
            EncodePolylinePartTest(
                "_p~iF~ps|U_ulLnnqC_mqNvxq`@",
                "38.5, -120.2|40.7, -120.95|43.252, -126.453");
        }

        private static void EncodePolylinePartTest(string expected, string input)
        {
            var parts = input.SplitX('|');
            var encoded = LinePathCollection.EncodePolyline(parts);
            Assert.AreEqual(expected, encoded);
        }

        [Test]
        public async Task GetImageAsync()
        {
            var search = new TileRequest(apiKey, signingKey, new Size(640, 640))
            {
                Zoom = 20,
                Address = "4909 Rutland Pl, Alexandria, VA, 22304"
            };
            var decoder = new TranscoderCodec<Hjg.Pngcs.ImageLines, ImageData>(
                new HjgPngcsCodec(),
                new HjgPngcsImageDataTranscoder());
            var results = await cache
                .LoadAsync(decoder, search)
                .ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(640, results.Info.Dimensions.Width);
            Assert.AreEqual(640, results.Info.Dimensions.Height);
        }
    }
}