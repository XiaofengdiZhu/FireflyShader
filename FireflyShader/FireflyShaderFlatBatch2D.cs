using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;
using PrimitiveType = Silk.NET.OpenGLES.PrimitiveType;

namespace Game {
    public class FireflyShaderFlatBatch : BaseBatch {
        public static FireflyShaderShader FireflyShaderShader;

        public readonly List<VertexFirefly> FireflyVertices = [];

        public readonly VertexBuffers FireflyBuffer = new(
            VertexFirefly.VertexDeclaration,
            [
                new VertexBuffers.BufferInitiateInfo(1),
                new VertexBuffers.BufferInitiateInfo(
                    (uint)SubsystemFireflyShader.FireflyCount,
                    (uint)VertexElementFormat.Vector4.GetSize(),
                    BufferTargetARB.ShaderStorageBuffer,
                    BufferUsageARB.DynamicDraw,
                    0
                ),
                new VertexBuffers.BufferInitiateInfo(
                    196608,
                    (uint)VertexElementFormat.Single.GetSize(),
                    BufferTargetARB.ShaderStorageBuffer,
                    BufferUsageARB.StaticDraw,
                    1
                )
            ]
        );

        public Vector3 m_deltaPosition;
        public float m_deltaTime;

        public FireflyShaderFlatBatch() {
            Layer = 0;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullNoneScissor;
            BlendState = BlendState.AlphaBlend;
            if (FireflyShaderShader == null) {
                string vsh = ContentManager.Get<string>("Shaders/FireflyShader", ".vsh");
                string psh = ContentManager.Get<string>("Shaders/FireflyShader", ".psh");
                string csh = ContentManager.Get<string>("Shaders/FireflyShader", ".csh");
                FireflyShaderShader = new FireflyShaderShader(vsh, psh, csh);
                FireflyShaderShader.Attenuation = 5f;
            }
        }

        public override bool IsEmpty() => FireflyVertices.Count == 0;

        public void QueueFirefly(Vector3 position, Color color) {
            FireflyVertices.Add(new VertexFirefly(position, color));
        }

        public override void Clear() {
            FireflyVertices.Clear();
        }

        public void SetDelta(Vector3 deltaPosition, float deltaTime) {
            m_deltaPosition = deltaPosition;
            m_deltaTime = deltaTime;
        }

        public override void Flush(Matrix matrix, Vector4 color, bool clearAfterFlush = true) {
            FireflyShaderShader.Transforms.World[0] = matrix;
            if (FireflyVertices.Count > 0) {
                Display.DepthStencilState = DepthStencilState;
                Display.RasterizerState = RasterizerState;
                Display.BlendState = BlendState;
                if (FireflyVertices.Count > FireflyBuffer.Buffers[0].Count) {
                    FireflyBuffer.DeleteBuffer(0);
                    FireflyBuffer.Buffers[0].Count = (uint)FireflyVertices.Count;
                    FireflyBuffer.AllocateBuffer(0);
                }
                FireflyBuffer.SetData(0, FireflyVertices.ToArray(), 0, FireflyVertices.Count);
                FireflyShaderShader.Compute(m_deltaPosition, m_deltaTime, (uint)(SubsystemFireflyShader.FireflyCount / 1024 / 32), 32);
                //用于获取前三个计算结果
                /*uint bufferSize = 16u * 3u;
                GLWrapper.GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, FireflyBuffer.Buffers[1].m_buffer);
                void* mappedBuffer = GLWrapper.GL.MapBufferRange(BufferTargetARB.ShaderStorageBuffer, IntPtr.Zero, new UIntPtr(bufferSize), MapBufferAccessMask.ReadBit);
                float[] data = new float[bufferSize / sizeof(float)];
                Marshal.Copy(new IntPtr(mappedBuffer), data, 0, data.Length);
                GLWrapper.GL.UnmapBuffer(BufferTargetARB.ShaderStorageBuffer);
                //FireflyBuffer.SetData(1, data, 0, 48);
                Console.WriteLine(string.Join(",", data));*/
                DrawArrayInstanced(
                    PrimitiveType.Points,
                    FireflyShaderShader,
                    FireflyBuffer,
                    0,
                    FireflyVertices.Count,
                    SubsystemFireflyShader.FireflyCount
                );
            }
            if (clearAfterFlush) {
                Clear();
            }
        }

        public static void DrawArrayInstanced(PrimitiveType primitiveType, FireflyShaderShader shader, VertexBuffers vertexBuffers, int startVertex, int verticesCount, int instanceCount = 1) {
            //Display.VerifyParametersDraw(primitiveType, shader, vertexBuffer, startVertex, verticesCount);
            GLWrapper.ApplyRenderTarget(Display.RenderTarget);
            GLWrapper.ApplyViewportScissor(Display.Viewport, Display.ScissorRectangle, Display.RasterizerState.ScissorTestEnable);
            FireflyShaderShader.ApplyShaderAndBuffers(
                shader,
                vertexBuffers.VertexDeclaration,
                IntPtr.Zero,
                vertexBuffers,
                null
            );
            GLWrapper.ApplyRasterizerState(Display.RasterizerState);
            GLWrapper.ApplyDepthStencilState(Display.DepthStencilState);
            GLWrapper.ApplyBlendState(Display.BlendState);
            if (instanceCount <= 1) {
                GLWrapper.GL.DrawArrays(primitiveType, startVertex, (uint)verticesCount);
            }
            else {
                GLWrapper.GL.DrawArraysInstanced(primitiveType, startVertex, (uint)verticesCount, (uint)instanceCount);
            }
        }
    }
}