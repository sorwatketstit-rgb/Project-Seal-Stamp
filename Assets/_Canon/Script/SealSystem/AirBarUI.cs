using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Air meter: one Image set to Filled / Vertical. Fill = air / maxAir (5 bars total).
/// The Image needs a Source Image assigned (any sprite, even a white square) or Unity won't fill it.
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [SerializeField] SealSuction suction;
    [SerializeField] Image fill;

    void Awake()
    {
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
    }

    void Update()
    {
        fill.fillAmount = suction.Air01;
    }
}
