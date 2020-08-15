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
using ImGuiNET;
using zzio.vfs;
using zzre.rendering;
using zzre.materials;
using zzio.primitives;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp;

namespace zzre.tools
{
    /// <summary>
    /// A simple editor/viewer layout with an info section and
    /// a framebuffer area with orbit controls
    /// </summary>
    public class SimpleEditorTag : ListDisposable
    {
        private const float FieldOfView = 60.0f * 3.141592653f / 180.0f;

        private readonly GraphicsDevice device;
        private readonly MouseEventArea mouseArea;
        private readonly FramebufferArea fbArea;

        private float distance = 2.0f;
        private Vector2 cameraAngle = Vector2.Zero;
        private bool didSetColumnWidth = false;
        private List<(string name, Action content, bool defaultOpen)> infoSections = new List<(string, Action, bool)>();

        public Window Window { get; }
        public UniformBuffer<Matrix4x4> Projection { get; }
        public UniformBuffer<Matrix4x4> View { get; }
        public UniformBuffer<Matrix4x4> World { get; }

        public SimpleEditorTag(Window window, ITagContainer diContainer)
        {
            device = diContainer.GetTag<GraphicsDevice>();
            Window = window;
            Window.AddTag(this);
            Window.OnContent += HandleContent;
            fbArea = new FramebufferArea(Window, device);
            Window.AddTag(fbArea);
            fbArea.OnRender += HandleRender;
            fbArea.OnResize += HandleResize;
            AddDisposable(fbArea);
            mouseArea = new MouseEventArea(Window);
            mouseArea.OnDrag += HandleDrag;
            mouseArea.OnScroll += HandleScroll;

            Projection = new UniformBuffer<Matrix4x4>(device.ResourceFactory);
            View = new UniformBuffer<Matrix4x4>(device.ResourceFactory);
            World = new UniformBuffer<Matrix4x4>(device.ResourceFactory);
            World.Ref = Matrix4x4.Identity;
            ResetView();
            HandleResize();
            AddDisposable(Projection);
            AddDisposable(View);
            AddDisposable(World);
        }

        public void AddInfoSection(string name, Action content, bool defaultOpen = true) =>
            infoSections.Add((name, content, defaultOpen));

        private void HandleContent()
        {
            ImGui.Columns(2, null, true);
            if (!didSetColumnWidth)
            {
                ImGui.SetColumnWidth(0, ImGui.GetWindowSize().X * 0.3f);
                didSetColumnWidth = true;
            }
            ImGui.BeginChild("LeftColumn", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.HorizontalScrollbar);
            foreach (var (name, content, isDefaultOpen) in infoSections)
            {
                var flags = isDefaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : 0;
                if (!ImGui.CollapsingHeader(name, flags))
                    continue;
                
                ImGui.BeginGroup();
                ImGui.Indent();
                content();
                ImGui.EndGroup();

            }
            ImGui.EndChild();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.NextColumn();
            mouseArea.Content();
            fbArea.Content();
            ImGui.PopStyleVar(1);

        }

        private void HandleRender(CommandList cl)
        {
            Projection.Update(cl);
            View.Update(cl);
            World.Update(cl);
        }

        private void HandleResize()
        {
            Projection.Ref = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, fbArea.Ratio, 0.01f, 100.0f);
        }

        private void HandleDrag(ImGuiMouseButton button, Vector2 delta)
        {
            if (button != ImGuiMouseButton.Right)
                return;

            cameraAngle += delta * 0.01f;
            while (cameraAngle.X > MathF.PI) cameraAngle.X -= 2 * MathF.PI;
            while (cameraAngle.X < -MathF.PI) cameraAngle.X += 2 * MathF.PI;
            cameraAngle.Y = Math.Clamp(cameraAngle.Y, -MathF.PI / 2.0f, MathF.PI / 2.0f);
            UpdateCamera();
        }

        private void HandleScroll(float scroll)
        {
            distance = distance * MathF.Pow(2.0f, -scroll * 0.1f);
            UpdateCamera();
        }

        public void ResetView()
        {
            distance = 2.0f;
            cameraAngle = Vector2.Zero;
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            View.Ref = Matrix4x4.CreateRotationY(cameraAngle.X) * Matrix4x4.CreateRotationX(cameraAngle.Y) * Matrix4x4.CreateTranslation(0.0f, 0.0f, -distance);
            fbArea.IsDirty = true;
        }
    }
}