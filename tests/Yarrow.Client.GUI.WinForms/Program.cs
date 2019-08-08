using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using Juniper.Google.Maps;
using Juniper.Google.Maps.StreetView;
using Juniper.Imaging.Windows;
using Juniper.World.GIS;

namespace Yarrow.Client.GUI.WinForms
{
    internal static class Program
    {
        private static readonly string MY_PICTURES = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        private static ImageViewer form;
        private static YarrowClient<Image> yarrow;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;

            var uri = new Uri(Properties.Settings.Default.YarrowServerHost);
            var decoder = new GDIImageDecoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            var yarrowCacheDirName = Path.Combine(MY_PICTURES, "Yarrow");
            var yarrowCacheDir = new DirectoryInfo(yarrowCacheDirName);
            var gmapsCacheDirName = Path.Combine(MY_PICTURES, "GoogleMaps");
            var gmapsCacheDir = new DirectoryInfo(gmapsCacheDirName);
            var gmapsKeyFileName = Path.Combine(gmapsCacheDirName, "keys.txt");
            var gmapsKeyFile = new FileInfo(gmapsKeyFileName);
            using (var fileStream = gmapsKeyFile.OpenRead())
            using (var reader = new StreamReader(fileStream))
            {
                var apiKey = reader.ReadLine();
                var signingKey = reader.ReadLine();
                yarrow = new YarrowClient<Image>(uri, decoder, yarrowCacheDir, apiKey, signingKey, gmapsCacheDir);
            }
            form = new ImageViewer();
            form.LocationSubmitted += Form_LocationSubmitted;
            form.LatLngSubmitted += Form_LatLngSubmitted;
            form.PanoSubmitted += Form_PanoSubmitted;
            using (form)
            {
                Application.Run(form);
            }
        }

        private static async Task GetImageData(MetadataResponse metadata)
        {
            if (metadata.status == System.Net.HttpStatusCode.OK)
            {
                var geo = await yarrow.ReverseGeocode(metadata.location);
                var imageFile = await yarrow.GetImage(metadata.pano_id);
                form.SetImage(metadata, geo, imageFile);
            }
            else
            {
                form.ShowError();
            }
        }

        private static void Form_LocationSubmitted(object sender, string location)
        {
            Task.Run(async () =>
            {
                var metadata = await yarrow.GetMetadata((PlaceName)location);
                await GetImageData(metadata);
            });
        }

        private static void Form_LatLngSubmitted(object sender, string latlng)
        {
            Task.Run(async () =>
            {
                var metadata = await yarrow.GetMetadata(LatLngPoint.ParseDecimal(latlng));
                await GetImageData(metadata);
            });
        }

        private static void Form_PanoSubmitted(object sender, string pano)
        {
            Task.Run(async () =>
            {
                var metadata = await yarrow.GetMetadata((PanoID)pano);
                await GetImageData(metadata);
            });
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}