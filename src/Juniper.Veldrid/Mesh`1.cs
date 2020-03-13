using System;
using System.Linq;

using Veldrid;

using IndexT = System.UInt16;

namespace Juniper.VeldridIntegration
{
    public class Mesh<VertexT> : Mesh
        where VertexT : struct
    {
        private readonly IFace<VertexT>[] faces;
        private readonly VertexT[] vertices;
        private readonly uint vertexSizeInBytes;
        private readonly IndexT[] indices;

        public uint FaceCount => (uint)faces.Length;

        public uint VertexCount => (uint)vertices.Length;

        public uint IndexCount => (uint)indices.Length;

        public Type IndexType => typeof(IndexT);

        private Mesh(IFace<VertexT>[] faces, (VertexT[] vertices, IndexT[] indices) unpacked)
            : this(faces, unpacked.vertices, unpacked.indices)
        { }

        public Mesh(params IFace<VertexT>[] faces)
            : this(faces, faces.ToVerts())
        { }

        internal Mesh(IFace<VertexT>[] faces, VertexT[] vertices, IndexT[] indices)
        {
            this.faces = faces ?? throw new ArgumentNullException(nameof(faces));
            this.vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            this.indices = indices ?? throw new ArgumentNullException(nameof(indices));
            var info = VertexTypeCache.GetDescription<VertexT>();
            vertexSizeInBytes = info.size;
        }

        internal (DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer) Prepare(GraphicsDevice device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (device.ResourceFactory is null)
            {
                throw new InvalidOperationException("Device is not ready (no ResourceFactory)");
            }

            var vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(
                sizeInBytes: (uint)vertices.Length * vertexSizeInBytes,
                usage: BufferUsage.VertexBuffer));

            device.UpdateBuffer(vertexBuffer, 0, vertices);

            var indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(
                sizeInBytes: (uint)indices.Length * sizeof(IndexT),
                usage: BufferUsage.IndexBuffer));

            device.UpdateBuffer(indexBuffer, 0, indices);

            return (vertexBuffer, indexBuffer);
        }

        public static Mesh<VertexT> operator+(Mesh<VertexT> a, Mesh<VertexT> b)
        {
            if(a is null)
            {
                return b;
            }

            if(b is null)
            {
                return a;
            }

            var combined = a.faces
                .Concat(b.faces)
                .ToArray();

            return new Mesh<VertexT>(faces: combined);
        }
    }
}
