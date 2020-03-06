using System.Collections.Generic;

namespace Juniper.VeldridIntegration
{
    public class Triangle<VertexT>
        : IFace<VertexT>
        where VertexT : struct
    {
        public VertexT A { get; }
        public VertexT B { get; }
        public VertexT C { get; }

        public int ElementCount => 3;

        public IEnumerable<VertexT> Elements
        {
            get
            {
                yield return A;
                yield return B;
                yield return C;
            }
        }

        public Triangle(VertexT a, VertexT b, VertexT c)
        {
            A = a;
            B = b;
            C = c;
        }
    }
}
