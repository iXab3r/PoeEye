﻿using System;
using System.Drawing;

namespace PoeShared.Scaffolding;

public static class PointExtensions
{
    public static bool InRange(this Point point, Point other, int allowedDeltaX = 0, int allowedDeltaY = 0)
    {
        return Math.Abs(point.X - other.X) <= allowedDeltaX && Math.Abs(point.Y - other.Y) <= allowedDeltaY;
    }
}