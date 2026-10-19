using UnityEngine;
using UnityEngine.Rendering;

namespace Vlab.Product
{
    public static class RoomLighting
    {
        // Runtime-only daylight balance. Keep the supplied room and model materials intact.
        // World-space text shares this camera: avoid applying film exposure to interface colors.
        public static void Apply()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.42f,.46f,.50f);
            RenderSettings.ambientEquatorColor=new Color(.24f,.27f,.30f);
            RenderSettings.ambientGroundColor=new Color(.12f,.14f,.16f);
            RenderSettings.reflectionIntensity=.45f;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(light.name=="Soft daylight")
                {light.intensity=.9f;light.color=Color.white;light.shadowBias=.015f;light.shadowNormalBias=.03f;}
                else if(light.name=="Ceiling illumination")
                {light.intensity=.6f;light.color=new Color(.97f,.985f,1);light.shadowBias=.015f;light.shadowNormalBias=.03f;}
            }
        }
    }
}
