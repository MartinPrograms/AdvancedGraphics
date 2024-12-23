﻿using System.Numerics;

namespace Common;

public static class MathUtils
{
    public static float DegToRad(float deg)
    {
        return deg * (float)Math.PI / 180.0f;
    }

    public static float[] ToArray(this Matrix4x4 matrix)
    {
        return new float[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
    }
}