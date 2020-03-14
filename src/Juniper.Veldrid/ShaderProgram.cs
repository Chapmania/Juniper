using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Veldrid;
using Veldrid.SPIRV;

namespace Juniper.VeldridIntegration
{
    public class ShaderProgram<VertexT>
        : IDisposable
        where VertexT : struct
    {
        private readonly Dictionary<string, DeviceBuffer> buffers = new Dictionary<string, DeviceBuffer>();
        private readonly Dictionary<string, Texture> textures;
        private readonly Mesh<VertexT> mesh;
        private readonly IndexFormat indexFormat;

        private Pipeline pipeline;
        private ResourceLayout[] layouts;
        private IDisposable[] resources;
        private ResourceSet[] resourceSets;

        private DeviceBuffer vertexBuffer;
        private DeviceBuffer indexBuffer;

        public ShaderProgram(GraphicsDevice device, Framebuffer framebuffer, Mesh<VertexT> mesh, ShaderProgramDescription<VertexT> material, params (string name, Texture texture)[] textures)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (framebuffer is null)
            {
                throw new ArgumentNullException(nameof(framebuffer));
            }

            this.mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
            indexFormat = mesh.IndexType.ToIndexFormat();

            if (material is null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var factory = device.ResourceFactory;

            if (factory is null)
            {
                throw new ArgumentException("GraphicsDevice is not ready. It has no ResourceFactory", nameof(device));
            }

            this.textures = textures.ToDictionary(x => x.name, x => x.texture);

            var vertex = material.VertexShader;
            var fragment = material.FragmentShader;

            var layoutsAndResources = vertex.Resources
                .Concat(fragment.Resources)
                .GroupBy(r => r.Set)
                .Select(g => g.ToArray())
                .Select(set =>
                {
                    var elements = set.Select(e => e.ToElementDescription()).ToArray();
                    var layout = factory.CreateResourceLayout(new ResourceLayoutDescription(elements));
                    var resources = set.Select(r => CreateResource(device, factory, r))
                        .ToArray();
                    return (layout, resources);
                })
                .ToArray();

            var pipelineOptions = material.PipelineOptions;

            pipelineOptions.ResourceLayouts = layouts = layoutsAndResources.Select(l => l.layout).ToArray();

            resources = layoutsAndResources.SelectMany(l => l.resources)
                .OfType<IDisposable>()
                .Where(r => r != device.Aniso4xSampler)
                .ToArray();

            resourceSets = layoutsAndResources
                .Select(l => factory.CreateResourceSet(new ResourceSetDescription(l.layout, l.resources)))
                .ToArray();

            (vertexBuffer, indexBuffer) = this.mesh.Prepare(device);

            var layout = material.VertexLayout;
            if (device.BackendType == GraphicsBackend.Direct3D11
                || material.UseSpirV)
            {
                for (var i = 0; i < material.VertexLayout.Elements.Length; ++i)
                {
                    material.VertexLayout.Elements[i].Semantic = VertexElementSemantic.TextureCoordinate;
                }
            }

            pipelineOptions.ShaderSet = new ShaderSetDescription
            {
                VertexLayouts = new VertexLayoutDescription[] { layout },
                Shaders = CreateShaders(factory, material)
            };

            pipelineOptions.Outputs = framebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineOptions);
        }

        private static Shader[] CreateShaders(ResourceFactory factory, ShaderProgramDescription<VertexT> material)
        {
            if (material.UseSpirV)
            {
                return factory.CreateFromSpirv(material.VertexShader.Description, material.FragmentShader.Description);
            }
            else
            {
                throw new InvalidOperationException("SPIR-V is the only supported shader format.");
            }
        }

        private BindableResource CreateResource(GraphicsDevice device, ResourceFactory factory, ShaderResource r)
        {
            return r.Kind switch
            {
                ResourceKind.Sampler => device.Aniso4xSampler,
                ResourceKind.TextureReadOnly => factory.CreateTextureView(textures[r.Name]),
                ResourceKind.TextureReadWrite => factory.CreateTextureView(textures[r.Name]),
                ResourceKind.UniformBuffer => CreateBuffer(factory, r),
                _ => throw new FormatException("Unknonw resource kind " + r.Kind)
            };
        }

        private DeviceBuffer CreateBuffer(ResourceFactory factory, ShaderResource r)
        {
            var buffer = factory.CreateBuffer(new BufferDescription(r.Size, BufferUsage.UniformBuffer));
            buffers[r.Name] = buffer;
            return buffer;
        }

        public void UpdateMatrix(string name, CommandList commandList, ref Matrix4x4 matrix)
        {
            if (commandList is null)
            {
                throw new ArgumentNullException(nameof(commandList));
            }

            commandList.UpdateBuffer(buffers[name], 0, ref matrix);
        }

        public void AddTexture(string name, Texture texture)
        {
            textures[name] = texture;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                pipeline?.Dispose();
                pipeline = null;
                indexBuffer?.Dispose();
                indexBuffer = null;
                vertexBuffer?.Dispose();
                vertexBuffer = null;

                foreach (var l in layouts)
                {
                    l.Dispose();
                }
                layouts = null;

                foreach (var r in resources)
                {
                    r.Dispose();
                }
                resources = null;

                foreach (var set in resourceSets)
                {
                    set.Dispose();
                }
                resourceSets = null;

            }
        }

        public void Draw(CommandList commandList)
        {
            if (commandList is null)
            {
                throw new ArgumentNullException(nameof(commandList));
            }

            commandList.SetPipeline(pipeline);
            for (var i = 0; i < resourceSets.Length; ++i)
            {
                commandList.SetGraphicsResourceSet((uint)i, resourceSets[i]);
            }
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, indexFormat);

            commandList.DrawIndexed(
                indexCount: mesh.IndexCount,
                instanceCount: mesh.FaceCount,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }
    }
}
