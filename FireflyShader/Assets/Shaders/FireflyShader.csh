#version 320 es

precision highp float;

layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;
layout (std430, binding = 0) buffer OffsetBuffer {
    vec4 data[];
};

layout (std430, binding = 1) buffer Noise {
    float noise[196608];
};

uniform vec4 a_delta;// x,y,z: delta player distance; w: delta time

float lengthSquared(vec3 v) {
    return dot(v, v);
}

void main() {
    uint size_x = gl_NumWorkGroups.x * gl_WorkGroupSize.x;
    uint size_y = gl_NumWorkGroups.y * gl_WorkGroupSize.y;
    uint index = gl_GlobalInvocationID.x
    + gl_GlobalInvocationID.y * size_x
    + gl_GlobalInvocationID.z * size_x * size_y;
    vec4 oldValue = data[index];
    float floatIndex = float(index);
    if (oldValue.w == 0.0 || lengthSquared(oldValue.xyz) > 4096.0) {
        data[index] = vec4(noise[index] * 64.0 - 32.0, noise[index + 1u] * 20.0 - 4.0, noise[index + 2u] * 64.0 - 32.0, floatIndex / 8192.0 + a_delta.w);
        return;
    }
    uint x = uint(fract(dot(oldValue.xyz, oldValue.wyz) + floatIndex / 256.0) * 196608.0);
    uint y = uint(fract(dot(oldValue.zwx, a_delta.zxy) + floatIndex / 2048.0) * 196608.0);
    uint z = uint(fract(dot(a_delta.yxw, a_delta.ywx) + floatIndex / 16384.0) * 196608.0);
    vec3 velocity = vec3(noise[x] - 0.5, noise[y] - 0.5, noise[z] - 0.5) * 2.0;
    data[index] = vec4(oldValue.xyz + velocity * a_delta.w - a_delta.xyz, oldValue.w + a_delta.w);
}
