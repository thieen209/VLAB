using System;
using TMPro;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class FrameSamples
    {
        readonly double[] values=new double[600],sorted=new double[600];int next,count;
        public int Count=>count;
        public double MeanMs{get;private set;}public double MedianMs{get;private set;}public double P95Ms{get;private set;}
        public double Fps=>MeanMs>0?1000/MeanMs:0;
        public void Clear(){next=count=0;MeanMs=MedianMs=P95Ms=0;}
        public double[] CopyMilliseconds(){var copy=new double[count];for(int i=0;i<count;i++)copy[i]=values[(next-count+i+values.Length)%values.Length];return copy;}
        public void AddSeconds(double seconds){if(!double.IsFinite(seconds)||seconds<=0)return;values[next]=seconds*1000;next=(next+1)%values.Length;count=Math.Min(count+1,values.Length);}
        public void Summarize()
        {
            if(count==0)return;double sum=0;for(int i=0;i<count;i++){sorted[i]=values[i];sum+=values[i];}Array.Sort(sorted,0,count);
            MeanMs=sum/count;MedianMs=sorted[(int)Math.Ceiling(count*.5)-1];P95Ms=sorted[(int)Math.Ceiling(count*.95)-1];
        }
    }
    // App frame intervals include waiting; neither these nor native CPU/GPU times measure motion-to-photon.
    public sealed class PerformanceOverlay:MonoBehaviour
    {
        public FrameSamples Samples{get;}=new FrameSamples();
        public double CpuMs{get;private set;}=double.NaN;public double GpuMs{get;private set;}=double.NaN;
        readonly FrameTiming[] timings=new FrameTiming[1];ulong timestamp;
        VlabUi ui;RectTransform panel;TextMeshProUGUI label;bool vi,capture;double last,nextRefresh;
        public void Initialize(VlabUi value)
        {
            ui=value;panel=ui.Panel(ui.surface,"Performance overlay",0,0,440,106,VlabUi.PanelColor);
            label=ui.Text(panel,"",12,8,416,90,20);label.lineSpacing=0;
            foreach(var graphic in panel.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.raycastTarget=false;
            var opacity=panel.GetComponent<VlabPanelOpacity>();opacity.Solid=true;opacity.Apply(false);Configure(false,true);
        }
        public void Configure(bool visible,bool vietnamese)
        {vi=vietnamese;if(enabled!=visible)ResetSamples();enabled=visible;if(panel)panel.gameObject.SetActive(visible);nextRefresh=0;}
        public void ResetSamples(){Samples.Clear();last=nextRefresh=0;timestamp=0;CpuMs=GpuMs=double.NaN;capture=FrameTimingManager.IsFeatureEnabled();}
        void OnEnable(){ResetSamples();if(panel)panel.gameObject.SetActive(true);}
        void OnDisable(){if(panel)panel.gameObject.SetActive(false);}
        void OnApplicationFocus(bool focused){ResetSamples();}
        void OnApplicationPause(bool paused){ResetSamples();}
        void LateUpdate()
        {
            if(!ui?.canvas||!panel)return;double now=Time.realtimeSinceStartupAsDouble;
            var app=VlabApp.Instance;bool activity=app&&(app.Route=="activity"||app.Route=="explore");
            bool visible=app&&(app.Route=="home"||activity)&&!app.HasOpenModal;
            if(panel.gameObject.activeSelf!=visible)panel.gameObject.SetActive(visible);
            if(last>0)Samples.AddSeconds(now-last);last=now;
            if(capture)FrameTimingManager.CaptureFrameTimings();
            if(now<nextRefresh)return;nextRefresh=now+.5;Samples.Summarize();
            CpuMs=GpuMs=double.NaN;
            if(capture&&FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].frameStartTimestamp!=timestamp)
            {var timing=timings[0];timestamp=timing.frameStartTimestamp;if(double.IsFinite(timing.cpuFrameTime)&&timing.cpuFrameTime>0)CpuMs=timing.cpuFrameTime;if(double.IsFinite(timing.gpuFrameTime)&&timing.gpuFrameTime>0)GpuMs=timing.gpuFrameTime;}
            bool world=ui.canvas.renderMode==RenderMode.WorldSpace;float unit=world?1:Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);
            float width=(world?440:Mathf.Min(370,ui.PhoneViewport.SafePixels.width-24))/unit,height=(world?90:68)/unit;
            var safe=ui.PhoneViewport.SafePixels;var content=ui.PhoneViewport.ContentPixels;
            float left=activity?safe.center.x-width*unit/2:safe.xMin+12;
            panel.anchoredPosition=world?new Vector2(activity?580:16,-16):new Vector2((left-content.xMin)/unit,-(content.yMax-safe.yMax+12)/unit);
            panel.sizeDelta=new Vector2(width,height);label.rectTransform.anchoredPosition=new Vector2(12/unit,-8/unit);label.rectTransform.sizeDelta=new Vector2(width-24/unit,height-16/unit);label.fontSize=(world?20:16)/unit;
            string scope=Application.isEditor?"Editor":(world?"VR":"Phone");
            label.text=Samples.Count<2?(vi?"Đang đo khung hình…":"Measuring frames…"):
                $"{scope} · {Samples.Fps:0} FPS · {Samples.MeanMs:0.0} ms · p95 {Samples.P95Ms:0.0} ms\nCPU {Measured(CpuMs)} · GPU {Measured(GpuMs)}";
            panel.SetAsLastSibling();
        }
        static string Measured(double value)=>double.IsFinite(value)?value.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+" ms":"—";
    }
}
