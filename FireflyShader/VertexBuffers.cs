using System;
using System.Runtime.InteropServices;
using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;

namespace Game {
    public class VertexBuffers : GraphicsResource {
        public struct BufferInitiateInfo {
            public uint Count;
            public uint? Stride;
            public BufferTargetARB Target;
            public BufferUsageARB Usage;
            public uint? BindBase;

            public BufferInitiateInfo(uint count, uint? stride = null, BufferTargetARB target = BufferTargetARB.ArrayBuffer, BufferUsageARB usage = BufferUsageARB.StaticDraw, uint? bindBase = null) {
                Count = count;
                Stride = stride;
                Target = target;
                Usage = usage;
                BindBase = bindBase;
            }
        }

        public class Buffer {
            public uint m_buffer;
            public uint m_count;

            public uint Count {
                get => m_count;
                set => m_count = value;
            }

            public uint Stride;
            public BufferTargetARB Target;
            public BufferUsageARB Usage;
            public uint? BindBase;

            public Buffer(uint count, uint stride, BufferTargetARB target, BufferUsageARB usage, uint? bindBase = null) {
                Count = count;
                Stride = stride;
                Target = target;
                Usage = usage;
                BindBase = bindBase;
            }

            public void DeleteBuffer() {
                if (m_buffer != 0) {
                    GLWrapper.DeleteBuffer(Target, (int)m_buffer);
                    m_buffer = 0;
                }
            }

            public unsafe void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct {
                VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
                GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
                try {
                    int num = Utilities.SizeOf<T>();
                    GLWrapper.BindBuffer(Target, (int)m_buffer);
                    GLWrapper.GL.BufferSubData(Target, new IntPtr(targetStartIndex * Stride), new UIntPtr((uint)(num * sourceCount)), (gCHandle.AddrOfPinnedObject() + sourceStartIndex * num).ToPointer());
                    if (BindBase.HasValue) {
                        GLWrapper.GL.BindBufferBase(Target, BindBase.Value, m_buffer);
                    }
                }
                finally {
                    gCHandle.Free();
                }
            }

            public unsafe uint AllocateBuffer() {
                GLWrapper.GL.GenBuffers(1, out uint buffer);
                GLWrapper.BindBuffer(Target, (int)buffer);
                GLWrapper.GL.BufferData(Target, new UIntPtr(Stride * Count), null, Usage);
                if (BindBase.HasValue) {
                    GLWrapper.GL.BindBufferBase(Target, BindBase.Value, buffer);
                }
                m_buffer = buffer;
                return buffer;
            }

            public void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct {
                int num = Utilities.SizeOf<T>();
                ArgumentNullException.ThrowIfNull(source);
                if (sourceStartIndex < 0
                    || sourceCount < 0
                    || sourceStartIndex + sourceCount > source.Length) {
                    throw new ArgumentException("Range is out of source bounds.");
                }
                if (targetStartIndex < 0
                    || (Count != uint.MaxValue && targetStartIndex * Stride + sourceCount * num > Count * Stride)) {
                    throw new ArgumentException("Range is out of target bounds.");
                }
            }
        }

        public Buffer[] Buffers;

        public VertexDeclaration VertexDeclaration { get; set; }

        public VertexBuffers(VertexDeclaration vertexDeclaration, BufferInitiateInfo[] infos) {
            InitializeVertexBuffers(vertexDeclaration, infos);
            AllocateBuffers();
        }

        public override void Dispose() {
            base.Dispose();
            DeleteBuffers();
        }

        public void SetData<T>(int bufferIndex, T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct {
            Buffers[bufferIndex].SetData(source, sourceStartIndex, sourceCount, targetStartIndex);
        }

        public override void HandleDeviceLost() {
            DeleteBuffers();
        }

        public override void HandleDeviceReset() {
            AllocateBuffers();
        }

        public void AllocateBuffers() {
            foreach (Buffer buffer in Buffers) {
                buffer.AllocateBuffer();
            }
        }

        public void AllocateBuffer(int bufferIndex) {
            Buffers[bufferIndex].AllocateBuffer();
        }

        public void DeleteBuffers() {
            foreach (Buffer buffer in Buffers) {
                buffer.DeleteBuffer();
            }
        }

        public void DeleteBuffer(int bufferIndex) {
            Buffers[bufferIndex].DeleteBuffer();
        }

        public override int GetGpuMemoryUsage() {
            int result = 0;
            foreach (Buffer buffer in Buffers) {
                result += (int)(buffer.Count * buffer.Stride);
            }
            return result;
        }

        public void InitializeVertexBuffers(VertexDeclaration vertexDeclaration, BufferInitiateInfo[] infos) {
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            int length = infos.Length;
            if (length <= 0) {
                throw new ArgumentException("Argument \"info\" is empty.");
            }
            uint[] strides = new uint[length];
            foreach (VertexElement element in vertexDeclaration.VertexElements) {
                int bufferIndex = element is VertexElementExtended elementExtended ? elementExtended.BufferIndex : 0;
                if (bufferIndex >= length) {
                    throw new ArgumentException("BufferIndex of VertexElementExtended must be less than verticesCounts.Length.");
                }
                strides[bufferIndex] += (uint)element.Format.GetSize();
            }
            Buffers = new Buffer[length];
            for (int i = 0; i < length; i++) {
                BufferInitiateInfo info = infos[i];
                if (info.Count <= 0) {
                    throw new ArgumentException("The count of buffer must be greater than zero.");
                }
                Buffers[i] = new Buffer(
                    info.Count,
                    info.Stride ?? strides[i],
                    info.Target,
                    info.Usage,
                    info.BindBase
                );
            }
            VertexDeclaration = vertexDeclaration;
        }
    }
}