﻿using System;

namespace zzre.game.messages;

public record struct SetCameraMode(int Mode, DefaultEcs.Entity NPCEntity)
{
    public static readonly SetCameraMode Overworld = new(-1, default);
};
