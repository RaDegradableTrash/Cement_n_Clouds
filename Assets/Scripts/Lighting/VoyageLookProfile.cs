using System;
using UnityEngine;

namespace Voyage.Lighting
{
    [CreateAssetMenu(menuName = "Voyage/Look Profile")]
    public sealed class VoyageLookProfile : ScriptableObject
    {
        public LookSettings settings = LookSettings.Default;
    }

    [Serializable]
    public struct LookSettings
    {
        [Range(-3,3)] public float exposure;
        [Range(-100,100)] public float saturation, contrast;
        [Range(0,2)] public float ambient, key, reflection;
        [Range(0,1)] public float transition, threshold, rim, bloom, vignette;
        public Color shadow, lightTint, fogTint, filter;
        public static LookSettings Default => new LookSettings {
            exposure = -.15f, saturation = -32, contrast = 16,
            ambient = .95f, key = 1.15f, reflection = 1,
            transition = .24f, threshold = .35f, rim = .12f, bloom = .18f, vignette = .15f,
            shadow = new Color(.82f,.86f,.94f), lightTint = Color.white,
            fogTint = new Color(.84f,.91f,1), filter = Color.white
        };
        public static LookSettings Lerp(LookSettings a, LookSettings b, float t) => new LookSettings {
            exposure=Mathf.Lerp(a.exposure,b.exposure,t), saturation=Mathf.Lerp(a.saturation,b.saturation,t),
            contrast=Mathf.Lerp(a.contrast,b.contrast,t), ambient=Mathf.Lerp(a.ambient,b.ambient,t),
            key=Mathf.Lerp(a.key,b.key,t), reflection=Mathf.Lerp(a.reflection,b.reflection,t),
            transition=Mathf.Lerp(a.transition,b.transition,t), threshold=Mathf.Lerp(a.threshold,b.threshold,t),
            rim=Mathf.Lerp(a.rim,b.rim,t), bloom=Mathf.Lerp(a.bloom,b.bloom,t), vignette=Mathf.Lerp(a.vignette,b.vignette,t),
            shadow=Color.Lerp(a.shadow,b.shadow,t), lightTint=Color.Lerp(a.lightTint,b.lightTint,t),
            fogTint=Color.Lerp(a.fogTint,b.fogTint,t), filter=Color.Lerp(a.filter,b.filter,t)
        };
    }
}
