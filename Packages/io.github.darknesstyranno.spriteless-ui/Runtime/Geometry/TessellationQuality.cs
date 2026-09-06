using System;
using UnityEngine;

namespace SpriteLessUI.Geometry
{
    internal static class TessellationQuality
    {
        [ThreadStatic] internal static float Scale;
        internal static float Multiplier => Mathf.Max(1f, Scale);
    }
}
