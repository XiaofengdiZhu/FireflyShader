using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;
using Shader = Engine.Graphics.Shader;

namespace Game {
    public class FireflyShaderShader : Shader {
        public struct VertexAttributeDataExtended {
            public int Size;
            public VertexAttribPointerType Type;
            public bool Normalize;
            public int Offset;
            public int BufferIndex;
            public int Divisor;
        }

        public string m_computeShaderCode;
        public int m_computeShader;
        public int m_computeProgram;
        public ShaderParameter m_worldViewProjectionMatrixParameter;
        public ShaderParameter m_attenuation;
        public Dictionary<VertexDeclaration, VertexAttributeDataExtended[]> m_vertexAttributeDataExtendedByDeclaration = [];

        public readonly ShaderTransforms Transforms;

        public float Attenuation {
            set => m_attenuation.SetValue(value);
        }

        public FireflyShaderShader(string vsh, string psh, string csh) : base(vsh, psh) {
            m_computeShaderCode = csh;
            Construct(vsh, psh);
            m_worldViewProjectionMatrixParameter = GetParameter("u_worldViewProjectionMatrix", true);
            m_attenuation = GetParameter("u_attenuation", true);
            Transforms = new ShaderTransforms(1);
        }

        public override void Construct(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros) {
            if (m_computeShaderCode == null) {
                return;
            }
            try {
                InitializeShader(vertexShaderCode, pixelShaderCode, shaderMacros);
                CompileShaders();
            }
            catch {
                Dispose();
                throw;
            }
        }

        public override void PrepareForDrawingOverride() {
            Transforms.UpdateMatrices(1, false, false, true);
            m_worldViewProjectionMatrixParameter.SetValue(Transforms.WorldViewProjection, 1);
        }

        public override void CompileShaders() {
            DeleteShaders();
            Dictionary<string, string> dictionary = [];
            Dictionary<string, string> dictionary2 = [];
            ParseShaderMetadata(m_vertexShaderCode, dictionary, dictionary2);
            ParseShaderMetadata(m_pixelShaderCode, dictionary, dictionary2);
            uint vertexShader = GLWrapper.GL.CreateShader(ShaderType.VertexShader);
            m_vertexShader = (int)vertexShader;
            GLWrapper.GL.ShaderSource(vertexShader, m_vertexShaderCode);
            GLWrapper.GL.CompileShader(vertexShader);
            GLWrapper.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int @params);
            if (@params != 1) {
                string shaderInfoLog = GLWrapper.GL.GetShaderInfoLog(vertexShader);
                throw new InvalidOperationException($"Error compiling vertex shader.\n{shaderInfoLog}");
            }
            uint pixelShader = GLWrapper.GL.CreateShader(ShaderType.FragmentShader);
            m_pixelShader = (int)pixelShader;
            GLWrapper.GL.ShaderSource(pixelShader, m_pixelShaderCode);
            GLWrapper.GL.CompileShader(pixelShader);
            GLWrapper.GL.GetShader(pixelShader, ShaderParameterName.CompileStatus, out int params2);
            if (params2 != 1) {
                string shaderInfoLog2 = GLWrapper.GL.GetShaderInfoLog(pixelShader);
                throw new InvalidOperationException($"Error compiling pixel shader.\n{shaderInfoLog2}");
            }
            uint computeShader = GLWrapper.GL.CreateShader(ShaderType.ComputeShader);
            m_computeShader = (int)computeShader;
            GLWrapper.GL.ShaderSource(computeShader, m_computeShaderCode);
            GLWrapper.GL.CompileShader(computeShader);
            GLWrapper.GL.GetShader(computeShader, ShaderParameterName.CompileStatus, out int params22);
            if (params22 != 1) {
                string shaderInfoLog22 = GLWrapper.GL.GetShaderInfoLog(computeShader);
                throw new InvalidOperationException($"Error compiling compute shader.\n{shaderInfoLog22}");
            }
            uint program = GLWrapper.GL.CreateProgram();
            m_program = (int)program;
            GLWrapper.GL.AttachShader(program, vertexShader);
            GLWrapper.GL.AttachShader(program, pixelShader);
            GLWrapper.GL.LinkProgram(program);
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out int params3);
            if (params3 != 1) {
                string programInfoLog = GLWrapper.GL.GetProgramInfoLog(program);
                throw new InvalidOperationException($"Error linking program.\n{programInfoLog}");
            }
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.ActiveAttributes, out int params4);
            for (int i = 0; i < params4; i++) {
                GLWrapper.GL.GetActiveAttrib(
                    program,
                    (uint)i,
                    256u,
                    out uint _,
                    out int _,
                    out AttributeType _,
                    out string stringBuilder
                );
                int attribLocation = GLWrapper.GL.GetAttribLocation(program, stringBuilder);
                if (!dictionary.TryGetValue(stringBuilder, out string value)) {
                    continue;
                    //throw new InvalidOperationException($"Attribute \"{stringBuilder}\" has no semantic defined in shader metadata.");
                }
                m_shaderAttributeData.Add(new ShaderAttributeData { Location = attribLocation, Semantic = value });
            }
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.ActiveUniforms, out int params5);
            List<ShaderParameter> list = [];
            Dictionary<string, ShaderParameter> dictionary3 = [];
            for (int j = 0; j < params5; j++) {
                GLWrapper.GL.GetActiveUniform(
                    program,
                    (uint)j,
                    256u,
                    out uint _,
                    out int size2,
                    out UniformType type2,
                    out string stringBuilder2
                );
                int uniformLocation = GLWrapper.GL.GetUniformLocation(program, stringBuilder2);
                ShaderParameterType shaderParameterType = GLWrapper.TranslateActiveUniformType(type2);
                int num = stringBuilder2.IndexOf('[');
                if (num >= 0) {
                    stringBuilder2 = stringBuilder2.Remove(num, stringBuilder2.Length - num);
                }
                ShaderParameter shaderParameter = new(this, stringBuilder2, shaderParameterType, size2) { Location = uniformLocation };
                dictionary3.Add(shaderParameter.Name, shaderParameter);
                list.Add(shaderParameter);
                if (shaderParameterType == ShaderParameterType.Texture2D) {
                    if (!dictionary2.TryGetValue(shaderParameter.Name, out string value2)) {
                        throw new InvalidOperationException($"Texture \"{shaderParameter.Name}\" has no sampler defined in shader metadata.");
                    }
                    ShaderParameter shaderParameter2 = new(this, value2, ShaderParameterType.Sampler2D, 1) { Location = int.MaxValue };
                    dictionary3.Add(value2, shaderParameter2);
                    list.Add(shaderParameter2);
                }
            }
            if (m_parameters != null) {
                foreach (KeyValuePair<string, ShaderParameter> item in dictionary3) {
                    if (m_parametersByName.TryGetValue(item.Key, out ShaderParameter value3)) {
                        value3.Location = item.Value.Location;
                    }
                }
                ShaderParameter[] parameters = m_parameters;
                for (int k = 0; k < parameters.Length; k++) {
                    parameters[k].IsChanged = true;
                }
            }
            else {
                m_parameters = list.ToArray();
                m_parametersByName = dictionary3;
            }
            m_glymulParameter = GetParameter("u_glymul");
            if (m_glymulParameter.Type != 0) {
                throw new InvalidOperationException("u_glymul parameter has invalid type.");
            }
            uint computeProgram = GLWrapper.GL.CreateProgram();
            m_computeProgram = (int)computeProgram;
            GLWrapper.GL.AttachShader(computeProgram, computeShader);
            GLWrapper.GL.LinkProgram(computeProgram);
            GLWrapper.GL.GetProgram(computeProgram, ProgramPropertyARB.LinkStatus, out int params6);
            if (params6 != 1) {
                string programInfoLog2 = GLWrapper.GL.GetProgramInfoLog(computeProgram);
                throw new InvalidOperationException($"Error linking compute program.\n{programInfoLog2}");
            }
        }

        public virtual VertexAttributeDataExtended[] GetVertexAttribDataExtended(VertexDeclaration vertexDeclaration) {
            if (!m_vertexAttributeDataExtendedByDeclaration.TryGetValue(vertexDeclaration, out VertexAttributeDataExtended[] value)) {
                value = new VertexAttributeDataExtended[8];
                foreach (ShaderAttributeData shaderAttributeDatum in m_shaderAttributeData) {
                    VertexElement vertexElement = null;
                    for (int i = 0; i < vertexDeclaration.m_elements.Length; i++) {
                        if (vertexDeclaration.m_elements[i].Semantic == shaderAttributeDatum.Semantic) {
                            vertexElement = vertexDeclaration.m_elements[i];
                            break;
                        }
                    }
                    if (vertexElement == null) {
                        throw new InvalidOperationException($"VertexElement not found for shader attribute \"{shaderAttributeDatum.Semantic}\".");
                    }
                    int bufferIndex = 0;
                    int divisor = 0;
                    if (vertexElement is VertexElementExtended vertexElementExtended) {
                        bufferIndex = vertexElementExtended.BufferIndex;
                        divisor = vertexElementExtended.Divisor;
                    }
                    value[shaderAttributeDatum.Location] = new VertexAttributeDataExtended { Size = vertexElement.Format.GetElementsCount(), Offset = vertexElement.Offset, BufferIndex = bufferIndex, Divisor = divisor };
                    GLWrapper.TranslateVertexElementFormat(vertexElement.Format, out value[shaderAttributeDatum.Location].Type, out value[shaderAttributeDatum.Location].Normalize);
                }
                m_vertexAttributeDataExtendedByDeclaration.Add(vertexDeclaration, value);
            }
            return value;
        }

        public virtual void Compute(Vector3 deltaPosition, float deltaTime, uint numGroupsX, uint numGroupsY = 1, uint numGroupsZ = 1) {
            GLWrapper.UseProgram(m_computeProgram);
            GLWrapper.GL.Uniform4(
                0,
                deltaPosition.X,
                deltaPosition.Y,
                deltaPosition.Z,
                deltaTime
            );
            GLWrapper.GL.DispatchCompute(numGroupsX, numGroupsY, numGroupsZ);
            GLWrapper.GL.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
        }

        public static void ApplyShaderAndBuffers(FireflyShaderShader shader, VertexDeclaration vertexDeclaration, IntPtr vertexOffset, VertexBuffers buffers, int? elementArrayBuffer) {
            shader.PrepareForDrawing();
            int activeBufferIndex = 0;
            VertexBuffers.Buffer activeBuffer = buffers.Buffers[0];
            GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, (int)buffers.Buffers[0].m_buffer);
            if (elementArrayBuffer.HasValue) {
                GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, elementArrayBuffer.Value);
            }
            GLWrapper.UseProgram(shader.m_program);
            if (shader != GLWrapper.m_lastShader
                || vertexOffset != GLWrapper.m_lastVertexOffset
                || activeBuffer.m_buffer != GLWrapper.m_lastArrayBuffer
                || vertexDeclaration.m_elements != GLWrapper.m_lastVertexDeclaration.m_elements) {
                VertexAttributeDataExtended[] vertexAttribData = shader.GetVertexAttribDataExtended(vertexDeclaration);
                for (int i = 0; i < vertexAttribData.Length; i++) {
                    VertexAttributeDataExtended data = vertexAttribData[i];
                    if (data.Size != 0) {
                        if (data.BufferIndex != activeBufferIndex) {
                            activeBufferIndex = data.BufferIndex;
                            activeBuffer = buffers.Buffers[activeBufferIndex];
                            GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, (int)activeBuffer.m_buffer);
                        }
                        GLWrapper.GL.VertexAttribPointer(
                            (uint)i,
                            data.Size,
                            data.Type,
                            data.Normalize,
                            activeBuffer.Stride,
                            vertexOffset + data.Offset
                        );
                        int? divisor = data.Divisor > 0 ? data.Divisor : null;
                        VertexAttribArray(i, true, divisor);
                    }
                    else {
                        VertexAttribArray(i, false);
                    }
                }
                GLWrapper.m_lastShader = shader;
                GLWrapper.m_lastVertexDeclaration = vertexDeclaration;
                GLWrapper.m_lastVertexOffset = vertexOffset;
                GLWrapper.m_lastArrayBuffer = (int)activeBuffer.m_buffer;
            }
            int num = 0;
            int num2 = 0;
            ShaderParameter shaderParameter;
            while (true) {
                if (num2 >= shader.m_parameters.Length) {
                    return;
                }
                shaderParameter = shader.m_parameters[num2];
                if (shaderParameter.IsChanged) {
                    switch (shaderParameter.Type) {
                        case ShaderParameterType.Float:
                            GLWrapper.GL.Uniform1(shaderParameter.Location, (uint)shaderParameter.Count, shaderParameter.Value);
                            shaderParameter.IsChanged = false;
                            break;
                        case ShaderParameterType.Vector2:
                            GLWrapper.GL.Uniform2(shaderParameter.Location, (uint)shaderParameter.Count, shaderParameter.Value);
                            shaderParameter.IsChanged = false;
                            break;
                        case ShaderParameterType.Vector3:
                            GLWrapper.GL.Uniform3(shaderParameter.Location, (uint)shaderParameter.Count, shaderParameter.Value);
                            shaderParameter.IsChanged = false;
                            break;
                        case ShaderParameterType.Vector4:
                            GLWrapper.GL.Uniform4(shaderParameter.Location, (uint)shaderParameter.Count, shaderParameter.Value);
                            shaderParameter.IsChanged = false;
                            break;
                        case ShaderParameterType.Matrix:
                            GLWrapper.GL.UniformMatrix4(shaderParameter.Location, (uint)shaderParameter.Count, false, shaderParameter.Value);
                            shaderParameter.IsChanged = false;
                            break;
                        default: throw new InvalidOperationException("Unsupported shader parameter type.");
                        case ShaderParameterType.Texture2D:
                        case ShaderParameterType.Sampler2D: break;
                    }
                }
                if (shaderParameter.Type == ShaderParameterType.Texture2D) {
                    if (num >= 8) {
                        throw new InvalidOperationException("Too many simultaneous textures.");
                    }
                    GLWrapper.ActiveTexture((TextureUnit)(33984 + num));
                    if (shaderParameter.IsChanged) {
                        GLWrapper.GL.Uniform1(shaderParameter.Location, num);
                    }
                    ShaderParameter obj = shader.m_parameters[num2 + 1];
                    Texture2D texture2D = (Texture2D)shaderParameter.Resource;
                    SamplerState samplerState = (SamplerState)obj.Resource;
                    if (texture2D != null) {
                        if (samplerState == null) {
                            break;
                        }
                        if (GLWrapper.m_activeTexturesByUnit[num] != texture2D.m_texture) {
                            GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, true);
                        }
                        if (!GLWrapper.m_textureSamplerStates.TryGetValue(texture2D.m_texture, out SamplerState value)
                            || value != samplerState) {
                            GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                            if (GLWrapper.GL_EXT_texture_filter_anisotropic) {
                                GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, samplerState.FilterMode == TextureFilterMode.Anisotropic ? samplerState.MaxAnisotropy : 1f);
                            }
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLWrapper.TranslateTextureFilterModeMin(samplerState.FilterMode, texture2D.MipLevelsCount > 1));
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLWrapper.TranslateTextureFilterModeMag(samplerState.FilterMode));
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLWrapper.TranslateTextureAddressMode(samplerState.AddressModeU));
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLWrapper.TranslateTextureAddressMode(samplerState.AddressModeV));
#if !ANDROID
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, samplerState.MinLod);
                            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, samplerState.MaxLod);
#endif
                            GLWrapper.m_textureSamplerStates[texture2D.m_texture] = samplerState;
                        }
                    }
                    else if (GLWrapper.m_activeTexturesByUnit[num] != 0) {
                        GLWrapper.BindTexture(TextureTarget.Texture2D, 0, true);
                    }
                    num++;
                    shaderParameter.IsChanged = false;
                }
                num2++;
            }
            throw new InvalidOperationException($"Associated SamplerState is not set for texture \"{shaderParameter.Name}\".");
        }

        public static void VertexAttribArray(int index, bool enable, int? divisor = null) {
            if (enable && (!GLWrapper.m_vertexAttribArray[index].HasValue || !GLWrapper.m_vertexAttribArray[index].Value)) {
                GLWrapper.GL.EnableVertexAttribArray((uint)index);
                GLWrapper.m_vertexAttribArray[index] = true;
                if (divisor.HasValue) {
                    GLWrapper.GL.VertexAttribDivisor((uint)index, (uint)divisor.Value);
                }
            }
            else if (!enable
                && (!GLWrapper.m_vertexAttribArray[index].HasValue || GLWrapper.m_vertexAttribArray[index].Value)) {
                GLWrapper.GL.DisableVertexAttribArray((uint)index);
                GLWrapper.m_vertexAttribArray[index] = false;
            }
        }
    }
}