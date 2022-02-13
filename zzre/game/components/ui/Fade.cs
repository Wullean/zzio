﻿using System;

namespace zzre.game.components.ui
{
    public record struct Fade(float From, float To, float Duration, float Time)
    {
        public float Value => MathEx.Lerp(From, To, Time / Duration);

        public static Fade In(float duration) => new Fade(0f, 1f, duration, 0f);
        public static Fade Out(float duration) => new Fade(1f, 0f, duration, 0f);

        public static readonly Fade StdIn = In(1.5f);
        public static readonly Fade StdOut = Out(0.8f);
    }
}