﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace zzre.rendering
{
    public class LocationBuffer : BaseDisposable
    {
        private const uint MatrixStride = 4 * 4 * sizeof(float);

        private WeakReference<Location>?[] locations;
        private Matrix4x4[] matrices;
        private int nextFreeIndex = 0;
        private DeviceBuffer buffer;

        public int Capacity => matrices.Length;
        public int Count { get; private set; } = 0;
        public bool IsFull => Capacity == Count;

        public LocationBuffer(ResourceFactory factory, int capacity = 64)
        {
            locations = new WeakReference<Location>?[capacity];
            matrices = new Matrix4x4[capacity];
            buffer = factory.CreateBuffer(new BufferDescription(
                (uint)capacity * MatrixStride, BufferUsage.UniformBuffer));
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();
            buffer.Dispose();
        }

        public DeviceBufferRange Add(Location location)
        {
            if (IsFull && !FindSlotToCleanup())
                throw new InvalidOperationException("LocationBuffer has no available slot free");

            Count++;
            int usedIndex = nextFreeIndex++;
            locations[usedIndex] = new WeakReference<Location>(location);
            if (!IsFull)
            {
                while (locations[nextFreeIndex]?.TryGetTarget(out _) ?? false)
                    nextFreeIndex++;
                if (locations[nextFreeIndex] != null)
                    Remove(nextFreeIndex);
            }
            else
                nextFreeIndex = Capacity;

            return new DeviceBufferRange(buffer, (uint)usedIndex * MatrixStride, MatrixStride);
        }

        public void Remove(DeviceBufferRange range) => Remove((int)(range.Offset / MatrixStride));

        private void Remove(int freeIndex)
        {
            if (locations[freeIndex] == null)
                return;
            Count--;
            locations[freeIndex] = null;
            nextFreeIndex = Math.Min(nextFreeIndex, freeIndex);
#if DEBUG
            matrices[freeIndex] = new Matrix4x4() * float.NaN; // as canary value
#endif
        }

        private bool FindSlotToCleanup()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!(locations[i]?.TryGetTarget(out _) ?? false))
                {
                    Remove(i);
                    return true;
                }
            }
            return false;
        }

        private (int min, int max) UpdateMatrixArray()
        {
            int minIndex = -1, maxIndex = -1;
            int found = 0;
            for (int i = 0; found < Count && i < Capacity; i++)
            {
                Location? location = null;
                if (!(locations[i]?.TryGetTarget(out location) ?? false))
                {
                    Remove(i);
                    continue;
                }
                found++;
                if (minIndex < 0)
                    minIndex = i;
                maxIndex = i;
                matrices[i] = location!.WorldToLocal;
            }
            return (minIndex, maxIndex);
        }

        public void Update(CommandList cl)
        {
            var (minI, maxI) = UpdateMatrixArray();
            if (minI < 0 || maxI < 0)
                return;
            cl.UpdateBuffer(buffer, (uint)minI * MatrixStride, ref matrices[0], (uint)(maxI - minI + 1) * MatrixStride);
        }

        public void Update(GraphicsDevice device)
        {
            var (minI, maxI) = UpdateMatrixArray();
            if (minI < 0 || maxI < 0)
                return;
            device.UpdateBuffer(buffer, (uint)minI * MatrixStride, ref matrices[0], (uint)(maxI - minI + 1) * MatrixStride);
        }
    }
}