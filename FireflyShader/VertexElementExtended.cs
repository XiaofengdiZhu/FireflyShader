using Engine.Graphics;

namespace Game {
    public class VertexElementExtended : VertexElement {
        public int BufferIndex { get; set; }
        public int Divisor { get; set; }

        public VertexElementExtended(VertexElementFormat format, string semantic, int bufferIndex = 0, int divisor = 0) : base(format, semantic) {
            BufferIndex = bufferIndex;
            Divisor = divisor;
        }

        public VertexElementExtended(VertexElementFormat format, VertexElementSemantic semantic, int bufferIndex = 0, int divisor = 0) : base(format, semantic) {
            BufferIndex = bufferIndex;
            Divisor = divisor;
        }

        public VertexElementExtended(int offset, VertexElementFormat format, string semantic, int bufferIndex = 0, int divisor = 0) : base(offset, format, semantic) {
            BufferIndex = bufferIndex;
            Divisor = divisor;
        }

        public VertexElementExtended(int offset, VertexElementFormat format, VertexElementSemantic semantic, int bufferIndex = 0, int divisor = 0) : base(offset, format, semantic) {
            BufferIndex = bufferIndex;
            Divisor = divisor;
        }
    }
}