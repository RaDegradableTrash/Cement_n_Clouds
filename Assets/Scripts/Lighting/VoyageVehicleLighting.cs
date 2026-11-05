using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Voyage.Lighting
{
    /// <summary>Connects H to the actual RV1.0 fixtures, preserving authored transforms.</summary>
    [DisallowMultipleComponent]
    public sealed class VoyageVehicleLighting : MonoBehaviour
    {
        readonly List<Light> headlights=new List<Light>();
        public IReadOnlyList<Light> Headlights => headlights;
        public bool HeadlightsOn { get; private set; }
        public void Bind()
        {
            headlights.Clear();
            foreach(var lamp in GetComponentsInChildren<Light>(true))
            {
                string fixture=lamp.transform.parent!=null ? lamp.transform.parent.name : "";
                bool primary=fixture=="HeadLightL" || fixture=="HeadLightR" || fixture=="HeadLightFix";
                bool auxiliary=fixture=="LightL" || fixture=="LightR";
                if(!primary && !auxiliary) continue;
                headlights.Add(lamp);
                lamp.color=new Color(1,.88f,.69f);
                lamp.intensity=fixture=="HeadLightFix" ? 90 : primary ? 45 : 15;
                lamp.range=primary ? 45 : 25;
                lamp.shadows=primary ? LightShadows.Soft : LightShadows.None;
                lamp.shadowBias=.05f; lamp.shadowNormalBias=.15f;
                lamp.GetUniversalAdditionalLightData().usePipelineSettings=false;
            }
            SetHeadlights(HeadlightsOn);
        }
        public void SetHeadlights(bool value)
        {
            HeadlightsOn=value;
            foreach(var light in headlights) if(light!=null) light.enabled=value;
        }
    }
}
