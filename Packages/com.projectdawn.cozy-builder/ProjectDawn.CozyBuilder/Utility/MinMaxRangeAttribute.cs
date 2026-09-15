using System;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;

        public MinMaxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}