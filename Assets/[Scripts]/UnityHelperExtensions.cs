using System;
using UnityEngine;

public static class UnityHelperExtensions
{
    public static Tuple<float, float> ToTuple(this Vector2 vector)
    {
        return new Tuple<float, float>(vector.x, vector.y);
    }
}