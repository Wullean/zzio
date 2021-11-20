﻿using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs.System;
using Veldrid;
using zzio;
using zzre.rendering;
using zzre.materials;
using zzio.scn;
using DefaultEcs;
using System.Numerics;

namespace zzre.game.systems
{
    [With(typeof(components.Visibility))]
    public partial class ModelRenderer : AEntityMultiMapSystem<CommandList, ClumpBuffers>
    {
        private struct ClumpCount
        {
            public readonly ClumpBuffers Clump;
            public readonly IReadOnlyList<BaseModelInstancedMaterial> Materials;
            public readonly uint Count;

            public ClumpCount(ClumpBuffers clump, IReadOnlyList<BaseModelInstancedMaterial> materials, uint count = 1)
            {
                Clump = clump;
                Materials = materials;
                Count = count;
            }

            public ClumpCount Increment() => new ClumpCount(Clump, Materials, Count + 1);
        }

        private readonly ITagContainer diContainer;
        private readonly IDisposable sceneLoadedSubscription;
        private readonly components.RenderOrder responsibility;

        private readonly List<ClumpCount> clumpCounts = new List<ClumpCount>();
        private readonly List<ModelInstance> instances = new List<ModelInstance>();
        private DeviceBuffer? instanceBuffer; // not owned
        private uint instanceStart;

        public ModelRenderer(ITagContainer diContainer, components.RenderOrder responsibility) :
            base(diContainer.GetTag<World>(), CreateEntityContainer, useBuffer: true)
        {
            this.diContainer = diContainer;
            this.responsibility = responsibility;
            sceneLoadedSubscription = World.Subscribe<messages.SceneLoaded>(HandleSceneLoaded);
        }

        public override void Dispose()
        {
            base.Dispose();
            sceneLoadedSubscription.Dispose();
        }

        private void HandleSceneLoaded(in messages.SceneLoaded message)
        {
            // only get the tag now, as it was only just created for us
            var modelInstanceBuffer = diContainer.GetTag<ModelInstanceBuffer>();
            var totalCount = MultiMap.Keys.Sum(key => MultiMap.Count(key));
            instanceBuffer = modelInstanceBuffer.DeviceBuffer;
            instanceStart = modelInstanceBuffer.Reserve(totalCount);
            clumpCounts.Capacity = MultiMap.Keys.Count();
            instances.Capacity = totalCount;
        }

        [WithPredicate]
        private bool Filter(in components.RenderOrder order) => order == responsibility;

        protected override void PreUpdate(CommandList state)
        {
            clumpCounts.Clear();
            instances.Clear();
        }

        [Update]
        private void Update(
            CommandList cl,
            in DefaultEcs.Entity entity,
            in ClumpBuffers clumpBuffers,
            List<BaseModelInstancedMaterial> materials,
            Location location,
            in components.ClumpMaterialInfo materialInfo)
        {
            if (clumpCounts.LastOrDefault().Clump != clumpBuffers)
                clumpCounts.Add(new(clumpBuffers, materials));
            else
                clumpCounts[^1] = clumpCounts[^1].Increment();

            instances.Add(new()
            {
                tint = materialInfo.Color,
                world = location.LocalToWorld,
                texShift = entity.Has<components.TexShift>()
                    ? entity.Get<components.TexShift>().Matrix
                    : Matrix3x2.Identity
            });
        }

        protected override void PostUpdate(CommandList cl)
        {
            if (instanceBuffer == null)
                throw new InvalidOperationException("Model instance buffer was never set");

            var instanceSpan = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instances);
            cl.UpdateBuffer(instanceBuffer,
                ModelInstance.Stride * instanceStart,
                ref instanceSpan[0],
                ModelInstance.Stride * (uint)instances.Count);

            bool isFirstDraw;
            bool isFirstClump = true;
            var curInstanceStart = instanceStart;
            foreach (var clumpCount in clumpCounts)
            {
                var (clump, materials, count) = (clumpCount.Clump, clumpCount.Materials, clumpCount.Count);

                isFirstDraw = true;
                foreach (var (subMesh, material) in clump.SubMeshes.Zip(materials))
                {
                    (material as IMaterial).Apply(cl);
                    if (isFirstDraw)
                    {
                        isFirstDraw = false;
                        clump.SetBuffers(cl);
                    }
                    if (isFirstClump)
                    {
                        isFirstClump = false;
                        cl.SetVertexBuffer(1, instanceBuffer);
                    }
                    cl.DrawIndexed(
                        vertexOffset: 0,
                        indexStart: (uint)subMesh.IndexOffset,
                        indexCount: (uint)subMesh.IndexCount,
                        instanceStart: curInstanceStart,
                        instanceCount: count);
                }

                curInstanceStart += count;
            }
        }
    }
}