﻿using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    [Serializable, NetSerializable]
    public enum TetrisPlayerAction
    {
        NewGame,
        StartLeft,
        EndLeft,
        StartRight,
        EndRight,
        Rotate,
        CounterRotate,
        SoftdropStart,
        SoftdropEnd,
        Harddrop,
        Pause,
        Unpause,
        Hold
    }
}
