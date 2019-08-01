using System.IO;
using System.Net;
using System.Threading.Tasks;

using Juniper.Google.Maps.Tests;
using Juniper.Image;
using Juniper.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Juniper.Google.Maps.StreetView.Tests
{
    [TestClass]
    public class StreetViewTests : ServicesTests
    {
        [TestMethod]
        public async Task JPEGImageSize()
        {
            var imageRequest = new CrossCubeMapRequest(service, (PlaceName)"Alexandria, VA", 640, 640);
            if (!imageRequest.IsCached)
            {
                await imageRequest.Get();
            }

            var data = File.ReadAllBytes(imageRequest.CacheFile.FullName);
            var size = Image.JPEG.Factory.ReadDimensions(data);
            Assert.AreEqual(2560, size.width);
            Assert.AreEqual(1920, size.height);
        }

        [TestMethod]
        public async Task PNGImageSize()
        {
            var imageRequest = new CrossCubeMapRequest(service, (PlaceName)"Alexandria, VA", 640, 640);
            var img = await imageRequest.Get();
            var png = new Image.PNG.Factory();
            var data = png.Serialize(img);
            var size = Image.PNG.Factory.ReadDimensions(data);
            Assert.AreEqual(2560, size.width);
            Assert.AreEqual(1920, size.height);
        }

        [TestMethod]
        public async Task GetMetadata()
        {
            var metadataRequest = new MetadataRequest(service, (PlaceName)"Washington, DC");
            var metadata = await metadataRequest.Get();
            Assert.IsTrue(metadataRequest.IsCached);
            Assert.AreEqual(HttpStatusCode.OK, metadata.status);
            Assert.IsNotNull(metadata.copyright);
            Assert.IsNotNull("2016-07", metadata.date.ToString("yyyy-MM"));
            Assert.IsNotNull(metadata.location);
            Assert.IsNotNull(metadata.pano_id);
        }

        [TestMethod]
        public async Task GetImage()
        {
            var imageRequest = new ImageRequest(service, (PlaceName)"Alexandria, VA", 640, 640);
            var image = await imageRequest.Get();
            Assert.IsTrue(imageRequest.IsCached);
            Assert.AreEqual(640, image.dimensions.width);
            Assert.AreEqual(640, image.dimensions.height);
        }

        [TestMethod]
        public async Task GetCubeMap()
        {
            var cubeMapRequest = new CubeMapRequest(service, (PlaceName)"Washington, DC", 640, 640);
            var images = await cubeMapRequest.Get();
            Assert.IsTrue(cubeMapRequest.IsCached);
            foreach (var image in images)
            {
                Assert.AreEqual(640, image.dimensions.width);
                Assert.AreEqual(640, image.dimensions.height);
            }
        }

        [TestMethod]
        public async Task SaveCubeMap6PNG()
        {
            var cubeMapRequest = new CubeMapRequest(service, (PlaceName)"Washington, DC", 640, 640);
            var images = await cubeMapRequest.Get();
            var combined = await ImageData.Combine6Squares(images[0], images[1], images[2], images[3], images[4], images[5]);
            var outputFileName = Path.Combine(cacheDir.FullName, "dc6.png");
            var encoder = new Image.PNG.Factory();
            encoder.Serialize(outputFileName, combined);
            Assert.IsTrue(File.Exists(outputFileName));
        }

        [TestMethod]
        public async Task SaveCubeMap6JPEG()
        {
            var cubeMapRequest = new CubeMapRequest(service, (PlaceName)"Washington, DC", 640, 640);
            var images = await cubeMapRequest.Get();
            var combined = await ImageData.Combine6Squares(images[0], images[1], images[2], images[3], images[4], images[5]);
            var outputFileName = Path.Combine(cacheDir.FullName, "dc6.jpeg");
            var encoder = new Image.PNG.Factory();
            encoder.Serialize(outputFileName, combined);
            Assert.IsTrue(File.Exists(outputFileName));
        }

        [TestMethod]
        public async Task GetCubeMapCrossJPEG()
        {
            var cubeMapRequest = new CrossCubeMapRequest(service, (PlaceName)"Washington, DC", 640, 640);
            cubeMapRequest.CacheFile.Delete();
            var combined = await cubeMapRequest.Get();
            Assert.IsNotNull(combined);
        }
    }
}