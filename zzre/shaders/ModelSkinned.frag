﻿#version 450
#extension GL_KHR_vulkan_glsl: enable

layout(location = 0) in vec2 fsin_uv;
layout(location = 1) in vec4 fsin_color;

layout(location = 0) out vec4 fsout_color;

layout(set = 0, binding = 0) uniform texture2D mainTexture;
layout(set = 0, binding = 1) uniform sampler mainSampler;
layout (set = 0, binding = 5) uniform MaterialUniforms
{
	vec4 tint;
	float vectorColorFactor;
	float tintFactor;
	float alphaReference;
};

vec4 weighColor(vec4 color, float factor)
{
	return color * factor + vec4(1,1,1,1) * (1 - factor);
}

void main()
{
	vec4 color =  texture(sampler2D(mainTexture, mainSampler), fsin_uv)
		* weighColor(fsin_color, vectorColorFactor)
		* weighColor(tint, tintFactor);
	if (color.a < alphaReference) // TODO: put alpha reference in uniforms
		discard;
	fsout_color = color;
}
