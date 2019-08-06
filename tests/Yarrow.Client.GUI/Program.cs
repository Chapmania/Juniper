using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using Juniper.Google.Maps;

namespace Yarrow.Client.GUI
{
    internal static class Program
    {
        private static ImageViewer form;
        private static YarrowClient yarrow;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;

            yarrow = new YarrowClient();
            form = new ImageViewer();
            form.VisibleChanged += Form_Activated;

            using (form)
            {
                Application.Run(form);
            }
        }

        private static void Form_Activated(object sender, EventArgs e)
        {
            if (form.Visible)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    var metadata = await yarrow.GetMetadata((PlaceName)"Alexandria, VA");
                    var pano = metadata.pano_id;
                    var image = await yarrow.GetImage(pano);
                    form.SetImage(image);
                });
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}