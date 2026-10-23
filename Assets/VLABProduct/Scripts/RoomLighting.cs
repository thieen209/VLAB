using UnityEngine;
using UnityEngine.Rendering;

namespace Vlab.Product
{
    public static class RoomLighting
    {
        // Neutral daylight and soft fill keep instruments readable without extra lights.
        // Keep exposure out of the shared camera so world-space text retains its colors.
        public static void Apply(int quality=1)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.72f,.76f,.80f);
            RenderSettings.ambientEquatorColor=new Color(.58f,.61f,.64f);
            RenderSettings.ambientGroundColor=new Color(.37f,.40f,.43f);
            RenderSettings.reflectionIntensity=.60f;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if(light.type==LightType.Directional)
                {light.intensity=1.05f;light.color=new Color(1f,.985f,.96f);light.shadowBias=.005f;light.shadowNormalBias=.008f;light.shadows=quality==0?LightShadows.None:quality>=2?LightShadows.Soft:LightShadows.Hard;light.shadowCustomResolution=quality<2?512:quality==2?1024:2048;}
                else if(light.name=="Window soft fill")
                {light.shadows=quality==0?LightShadows.None:quality==1?LightShadows.Hard:LightShadows.Soft;light.shadowBias=.01f;light.shadowNormalBias=.02f;}
                else if(light.name=="Ceiling illumination")
                {light.intensity=.6f;light.color=new Color(.98f,.99f,1f);light.shadowBias=.005f;light.shadowNormalBias=.008f;light.shadows=LightShadows.None;}
            }
            foreach(var shade in Object.FindObjectsByType<LabShade>())if(shade.daylight.Length>0)
            {RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.13f,.14f);shade.RefreshDaylight();}
            QualitySettings.anisotropicFiltering=quality>=2?AnisotropicFiltering.ForceEnable:AnisotropicFiltering.Disable;
        }
    }
}
