using UnityEngine;
using UnityEngine.Rendering;

namespace Vlab.Product
{
    public static class RoomLighting
    {
        // A warmer daylight key and restrained cool fill give original surfaces depth.
        // Keep exposure out of the shared camera so world-space text retains its colors.
        public static void Apply()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.36f,.41f,.47f);
            RenderSettings.ambientEquatorColor=new Color(.22f,.25f,.29f);
            RenderSettings.ambientGroundColor=new Color(.13f,.14f,.16f);
            RenderSettings.reflectionIntensity=.40f;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(light.name=="Soft daylight")
                {light.intensity=1.05f;light.color=new Color(1f,.965f,.91f);light.shadowBias=.015f;light.shadowNormalBias=.03f;}
                else if(light.name=="Ceiling illumination")
                {light.intensity=.50f;light.color=new Color(.94f,.97f,1f);light.shadowBias=.015f;light.shadowNormalBias=.03f;}
            }
        }
    }
}
