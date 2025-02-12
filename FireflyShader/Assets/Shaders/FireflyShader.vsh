#version 320 es
#extension GL_OES_standard_noise: enable
#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;
uniform float u_glymul;

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='COLOR' Attribute='a_color' />

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec4 a_color;

layout (std430, binding = 0) buffer OffsetBuffer {
    vec4 data[];
};

uniform mat4 u_worldViewProjectionMatrix;

out vec3 u_position;
out vec4 u_color;

void main()
{

    u_position = a_position;
    u_color = a_color;

    vec4 datum = data[gl_InstanceID];
    gl_Position = u_worldViewProjectionMatrix * vec4(a_position + datum.xyz, 1.0);
    float distance = abs(gl_Position.z);
    gl_PointSize = (sin(datum.w) * 25.0 + 75.0) / distance;//need correct
    OPENGL_POSITION_FIX;
}
