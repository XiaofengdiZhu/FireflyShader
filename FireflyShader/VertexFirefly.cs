using Engine;
using Engine.Graphics;

namespace Game {
    public struct VertexFirefly {
        public static readonly VertexDeclaration VertexDeclaration = new(false, new VertexElement(0, VertexElementFormat.Vector3, "POSITION"), new VertexElement(12, VertexElementFormat.NormalizedByte4, "COLOR"));

        public Vector3 Position;
        public Color Color;
        //public Vector3 Offset; //因为存在另一个数组

        public VertexFirefly(Vector3 position, Color color) {
            Position = position;
            Color = color;
        }
    }
}