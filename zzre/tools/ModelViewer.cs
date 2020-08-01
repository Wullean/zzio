﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Veldrid;
using zzre.imgui;
using zzio.rwbs;
using zzre.core;
using zzio.utils;
using System.Numerics;

namespace zzre.tools
{
    public class ModelViewer : ListDisposable
    {
        private const float FieldOfView = 60.0f * 3.141592653f / 180.0f;

        private readonly ITagContainer diContainer;
        private readonly GraphicsDevice device;
        private readonly FramebufferWindowTag fbWindow;
        private readonly IBuiltPipeline builtPipeline;
        private readonly zzio.vfs.VirtualFileSystem vfs;
        private readonly DeviceBuffer geometryUniformBuffer;

        private RWGeometryBuffers? geometryBuffers;
        private ModelStandardUniforms geometryUniforms;
        private bool isGeometryUniformsDirty = true;
        private ResourceSet[] resourceSets = new ResourceSet[0];

        public Window Window { get; }

        public ModelViewer(ITagContainer diContainer)
        {
            this.diContainer = diContainer;
            device = diContainer.GetTag<GraphicsDevice>();
            vfs = diContainer.GetTag<zzio.vfs.VirtualFileSystem>();
            Window = diContainer.GetTag<WindowContainer>().NewWindow("Model Viewer");
            builtPipeline = diContainer.GetTag<StandardPipelines>().ModelStandard;
            fbWindow = new FramebufferWindowTag(Window, device);
            fbWindow.Pipeline = builtPipeline.Pipeline;
            fbWindow.OnRender += HandleRender;
            fbWindow.OnResize += HandleResize;

            geometryUniformBuffer = device.ResourceFactory.CreateBuffer(
                new BufferDescription(ModelStandardUniforms.Stride, BufferUsage.UniformBuffer));
            HandleResize(); // sets the projection matrix
            geometryUniforms.view = Matrix4x4.CreateTranslation(Vector3.UnitZ * -2.0f);
            geometryUniforms.world = Matrix4x4.Identity;
            geometryUniforms.tint = Vector4.One;
            AddDisposable(geometryUniformBuffer);
        }

        public void LoadModel(string pathText)
        {
            var path = new FilePath(pathText);
            var texturePath = GetTexturePathFromModel(path);

            using var contentStream = vfs.GetFileContent(pathText);
            if (contentStream == null)
                throw new FileNotFoundException($"Could not find model at {pathText}");
            var clump = Section.ReadNew(contentStream);
            if (clump.sectionId != SectionId.Clump)
                throw new InvalidDataException($"Expected a root clump section, got a {clump.sectionId}");
            var geometry = clump.FindChildById(SectionId.Geometry);
            if (geometry == null)
                throw new InvalidDataException("Could not find a geometry section in clump");

            geometryBuffers = new RWGeometryBuffers(diContainer, builtPipeline, (RWGeometry)geometry);
            AddDisposable(geometryBuffers);

            resourceSets = new ResourceSet[geometryBuffers.SubMeshes.Count];
            foreach (var (rwMaterial, index) in geometryBuffers.SubMeshes.Select(s => s.Material).Indexed())
            {
                var (texture, sampler) = LoadTextureFromMaterial(texturePath, rwMaterial);
                AddDisposable(texture);
                AddDisposable(sampler);
                resourceSets[index] = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription
                {
                    Layout = builtPipeline.ResourceLayouts.First(),
                    BoundResources = new BindableResource[]
                    {
                        texture,
                        device.PointSampler,
                        geometryUniformBuffer
                    }
                });
                AddDisposable(resourceSets[index]);
            }

            fbWindow.IsDirty = true;
        }

        private FilePath GetTexturePathFromModel(FilePath modelPath)
        {
            var modelDirPartI = modelPath.Parts.IndexOf(p => p.ToLowerInvariant() == "models");
            var context = modelPath.Parts[modelDirPartI + 1];
            return new FilePath("textures").Combine(context);
        }

        private (Texture, Sampler) LoadTextureFromMaterial(FilePath basePath, RWMaterial material)
        {
            var texSection = (RWTexture)material.FindChildById(SectionId.Texture, true);
            var addressModeU = ConvertAddressMode(texSection.uAddressingMode);
            var samplerDescription = new SamplerDescription()
            {
                AddressModeU = addressModeU,
                AddressModeV = ConvertAddressMode(texSection.vAddressingMode, addressModeU),
                Filter = ConvertFilterMode(texSection.filterMode)
            };

            var nameSection = (RWString)texSection.FindChildById(SectionId.String, true);
            using var textureStream = vfs.GetFileContent(basePath.Combine(nameSection.value + ".bmp").ToPOSIXString());
            var texture = new Veldrid.ImageSharp.ImageSharpTexture(textureStream, false);
            return (
                texture.CreateDeviceTexture(device, device.ResourceFactory),
                device.ResourceFactory.CreateSampler(samplerDescription));
        }

        private SamplerAddressMode ConvertAddressMode(TextureAddressingMode mode, SamplerAddressMode? altMode = null) => mode switch
        {
            TextureAddressingMode.Wrap => SamplerAddressMode.Wrap,
            TextureAddressingMode.Mirror => SamplerAddressMode.Mirror,
            TextureAddressingMode.Clamp => SamplerAddressMode.Clamp,
            TextureAddressingMode.Border => SamplerAddressMode.Border,

            TextureAddressingMode.NATextureAddress => altMode ?? throw new NotImplementedException(),
            TextureAddressingMode.Unknown => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

        private SamplerFilter ConvertFilterMode(TextureFilterMode mode) => mode switch
        {
            TextureFilterMode.Nearest => SamplerFilter.MinPoint_MagPoint_MipPoint,
            TextureFilterMode.Linear => SamplerFilter.MinLinear_MagLinear_MipPoint,
            TextureFilterMode.MipNearest => SamplerFilter.MinPoint_MagPoint_MipPoint,
            TextureFilterMode.MipLinear => SamplerFilter.MinLinear_MagLinear_MipPoint,
            TextureFilterMode.LinearMipNearest => SamplerFilter.MinPoint_MagPoint_MipLinear,
            TextureFilterMode.LinearMipLinear => SamplerFilter.MinLinear_MagLinear_MipLinear,

            TextureFilterMode.NAFilterMode => throw new NotImplementedException(),
            TextureFilterMode.Unknown => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

        private void HandleRender(CommandList cl)
        {
            if (geometryBuffers == null)
                return;
            if (isGeometryUniformsDirty)
            {
                isGeometryUniformsDirty = false;
                cl.UpdateBuffer(geometryUniformBuffer, 0, ref geometryUniforms);
            }

            geometryBuffers.SetBuffers(cl);
            foreach (var (subMesh, index) in geometryBuffers.SubMeshes.Indexed())
            {
                cl.SetGraphicsResourceSet(0, resourceSets[index]);
                cl.DrawIndexed(
                    indexStart: (uint)subMesh.IndexOffset,
                    indexCount: (uint)subMesh.IndexCount,
                    instanceCount: 1,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
        }

        private void HandleResize()
        {
            float ratio = fbWindow.Framebuffer.Width / (float)fbWindow.Framebuffer.Height;
            geometryUniforms.projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, ratio, 0.01f, 100.0f);
            isGeometryUniformsDirty = true;
        }
    }
}