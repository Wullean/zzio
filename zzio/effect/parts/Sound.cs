﻿using System;
using System.Collections.Generic;
using System.IO;
using zzio.utils;

namespace zzio.effect.parts
{
    [System.Serializable]
    public class Sound : IEffectPart
    {
        public EffectPartType Type { get { return EffectPartType.Sound; } }
        public string Name { get { return name; } }

        public uint
            volume = 100;
        public float
            minDist = 0.0f,
            maxDist = 0.0f;
        public bool
            isDisabled = false;
        public string
            fileName = "standard",
            name = "Sound Effect";

        public Sound() { }

        public void Read(BinaryReader r)
        {
            uint size = r.ReadUInt32();
            if (size != 84)
                throw new InvalidDataException("Invalid size of EffectPart Sound");

            volume = r.ReadUInt32();
            minDist = r.ReadSingle();
            maxDist = r.ReadSingle();
            r.BaseStream.Seek(4, SeekOrigin.Current);
            isDisabled = r.ReadBoolean();
            fileName = r.ReadSizedCString(32);
            name = r.ReadSizedCString(32);
            r.BaseStream.Seek(3, SeekOrigin.Current);
        }
    }
}
