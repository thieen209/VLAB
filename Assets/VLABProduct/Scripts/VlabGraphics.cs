using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Vlab.Product
{
    public static class VlabGraphics
    {
        public static string Label(int tier,bool vi)
        {return (vi?new[]{"Thấp","Trung bình","Cao","Rất cao"}:new[]{"Low","Medium","High","Ultra"})[Math.Clamp(tier,0,3)];}
        // shortcut: conservative RAM/GPU estimate; use measured device profiles when hardware data is available. Ultra is manual.
        public static int Recommend(int memoryMb,int graphicsMb)
        {if((memoryMb>0&&memoryMb<3000)||(graphicsMb>0&&graphicsMb<512))return 0;if(memoryMb>=8000&&graphicsMb>=2048)return 2;return 1;}
        public static int RecommendedTier=>Recommend(SystemInfo.systemMemorySize,SystemInfo.graphicsMemorySize);
        public static void Apply(UniversalRenderPipelineAsset pipeline,int tier)
        {
            if(tier<0||tier>3)throw new ArgumentOutOfRangeException(nameof(tier));
            pipeline.renderScale=tier==0?.75f:tier==1?.85f:1;
            pipeline.shadowDistance=tier==0?8:tier==1?15:tier==2?20:25;
            pipeline.mainLightShadowmapResolution=tier<2?512:tier==2?1024:2048;
            pipeline.msaaSampleCount=tier<2?2:4;pipeline.shadowCascadeCount=tier<2?1:tier==2?2:4;
            RoomLighting.Apply(tier);
        }
    }
}
