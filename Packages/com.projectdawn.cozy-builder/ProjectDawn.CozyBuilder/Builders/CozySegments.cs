using Unity.Collections;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    public interface ICozySegmentsModifier : ICozyHashable
    {
        void Modify(ref CozyBuilderContext context, NativeList<Segment> segments, CozySpline spline);
    }

    [AddComponentMenu("Cozy Builder/Builder/Cozy Segments")]
    public class CozySegments : MonoBehaviour, ICozyBuilder
    {
        [Tooltip("Spacing between each segment.")]
        public float Spacing = 0.17f;

        CozySpline Spline => GetComponent<CozySpline>() ?? transform.parent?.GetComponent<CozySpline>();

        void ICozyBuilder.Build(ref CozyBuilderContext context)
        {
            float length = Spline.CalculateLength();
            float count = (int)(length / Spacing);

            float offset = 0.5f / count;

            using NativeList<Segment> segments = new NativeList<Segment>(Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                float t = (float)(i + 0.5f) / (count);

                segments.Add(new Segment
                {
                    X = t - offset,
                    Width = offset * 2,
                    Index = i,
                    Depth = 1f,
                });
            }

            if (segments.Length == 0)
                return;

            using (var modifiers = this.GetComponentsScoped<ICozySegmentsModifier>(true))
                foreach (var modifier in modifiers.List)
                    if (modifier is MonoBehaviour component && component.isActiveAndEnabled && modifier.CalculateHash() != 0)
                        modifier.Modify(ref context, segments, Spline);
        }

        uint ICozyHashable.CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (Spline == null)
                return 0;
            HashBuffer hash = new HashBuffer();
            using (var modifiers = this.GetComponentsScoped<ICozySegmentsModifier>(true))
                foreach (var modifier in modifiers.List)
                    if (modifier is MonoBehaviour component && component.isActiveAndEnabled)
                        hash.Write(modifier.CalculateHash());
            hash.Write(Spline.CalculateHash());
            hash.Write(Spacing);
            return hash.GetHash();
        }
    }
}