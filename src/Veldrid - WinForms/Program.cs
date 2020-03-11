using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Juniper.Imaging;
using Juniper.IO;
using Juniper.VeldridIntegration;

using Veldrid;

using VertexT = Juniper.VeldridIntegration.VertexPositionTexture;

namespace Juniper
{
    public static class Program
    {
        private static readonly Dictionary<MediaType, IImageCodec<ImageData>> decoders = new Dictionary<MediaType, IImageCodec<ImageData>>()
        {
            [MediaType.Image.Png] = new HjgPngcsCodec().Pipe(new HjgPngcsImageDataTranscoder()),
            [MediaType.Image.Jpeg] = new LibJpegNETCodec().Pipe(new LibJpegNETImageDataTranscoder(padAlpha: true))
        };

        private static readonly Dictionary<string, ImageData> images = new Dictionary<string, ImageData>();

        private static readonly Mesh<VertexT> texturedCube = new Mesh<VertexT>(
            new Quad<VertexT>[]{
                // Top
                new Quad<VertexT>(
                    new VertexT(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                    new VertexT(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                    new VertexT(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1)),
                    new VertexT(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1))),
                // Bottom
                new Quad<VertexT>(
                    new VertexT(new Vector3(-0.5f,-0.5f, +0.5f),  new Vector2(0, 0)),
                    new VertexT(new Vector3(+0.5f,-0.5f, +0.5f),  new Vector2(1, 0)),
                    new VertexT(new Vector3(-0.5f,-0.5f, -0.5f),  new Vector2(0, 1)),
                    new VertexT(new Vector3(+0.5f,-0.5f, -0.5f),  new Vector2(1, 1))),
                // Left
                new Quad<VertexT>(
                    new VertexT(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                    new VertexT(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                    new VertexT(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                    new VertexT(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 1))),
                // Right
                new Quad<VertexT>(
                    new VertexT(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                    new VertexT(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                    new VertexT(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
                    new VertexT(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1))),
                // Back
                new Quad<VertexT>(
                    new VertexT(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                    new VertexT(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                    new VertexT(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                    new VertexT(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1))),
                // Front
                new Quad<VertexT>(
                    new VertexT(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                    new VertexT(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                    new VertexT(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
                    new VertexT(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1)))
            });

        private static MainForm mainForm;
        private static Material<VertexT> material;
        private static MeshRenderer<VertexT> renderer;
        private static Texture surfaceTexture;
        private static DateTime start;

        private static async Task Main()
        {
            var imageDir = new DirectoryInfo("Images");
            if (imageDir.Exists)
            {
                foreach (var file in imageDir.EnumerateFiles())
                {
                    var name = Path.GetFileNameWithoutExtension(file.Name)
                        .ToLowerInvariant();
                    var type = (from t in MediaType.GuessByFile(file)
                                where t is MediaType.Image
                                select t)
                            .FirstOrDefault();
                    if (decoders.ContainsKey(type))
                    {
                        images[name] = decoders[type].Deserialize(file);
                    }
                }
            }

            material = await Material.LoadAsync<VertexT>(
                    "Shaders\\vert.glsl",
                    "Shaders\\frag.glsl")
                .ConfigureAwait(false);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;

            using var form = mainForm = new MainForm();
            mainForm.Panel.Ready += Panel_Ready;
            mainForm.Panel.CommandListUpdate += Panel_CommandListUpdate;
            mainForm.FormClosing += MainForm_FormClosing;
            Application.Run(mainForm);
        }

        private static void Panel_Ready(object sender, EventArgs e)
        {
            var device = mainForm.Device.VeldridDevice;
            var factory = device.ResourceFactory;

            var image = images["rock"];
            surfaceTexture = factory.CreateTexture(new TextureDescription(
                (uint)image.Info.Dimensions.Width, (uint)image.Info.Dimensions.Height, 1,
                1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D));

            var imageData = image.GetData();
            device.UpdateTexture(
                surfaceTexture,
                imageData,
                0, 0, 0,
                (uint)image.Info.Dimensions.Width, (uint)image.Info.Dimensions.Height, 1,
                0, 0);

            material.AddTexture("SurfaceTexture", surfaceTexture);
            material.CreateResources(device, factory);

            renderer = new MeshRenderer<VertexT>(
                device,
                mainForm.Panel.VeldridSwapChain.Framebuffer,
                material,
                texturedCube);

            start = DateTime.Now;
        }

        private static void Panel_CommandListUpdate(object sender, VeldridIntegration.WinFormsSupport.UpdateCommandListEventArgs e)
        {
            var time = (float)(DateTime.Now - start).TotalSeconds;
            var commandList = e.CommandList;
            var framebuffer = mainForm.Panel.VeldridSwapChain.Framebuffer;
            var width = framebuffer.Width;
            var height = framebuffer.Height;
            var aspectRatio = (float)width / height;

            commandList.Begin();

            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Units.Degrees.Radians(60), aspectRatio, 0.5f, 100);
            var viewMatrix = Matrix4x4.CreateLookAt(Vector3.UnitZ * 2.5f, Vector3.Zero, Vector3.UnitY);
            var worldMatrix = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, time)
                * Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, time / 3);

            material.UpdateMatrix("ProjectionBuffer", commandList, ref projectionMatrix);
            material.UpdateMatrix("ViewBuffer", commandList, ref viewMatrix);
            material.UpdateMatrix("WorldBuffer", commandList, ref worldMatrix);

            commandList.SetFramebuffer(framebuffer);
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.ClearDepthStencil(1);
            renderer.Draw(commandList);
            commandList.End();

            mainForm.Panel.Invalidate();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            mainForm.SetError(e.Exception);
        }

        private static void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            renderer?.Dispose();
            material?.Dispose();
            surfaceTexture?.Dispose();
        }
    }
}
