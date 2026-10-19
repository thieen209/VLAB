using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vlab.Product
{
    // The illustration is a physical-location guide, not a BLE packet/hand-ID claim.
    public static class ControllerDiagramView
    {
        public const string TextureResource="UI/ControllerPair-v3",SideTextureResource="UI/ControllerSide-v1";
        public const float Width=1036,Height=450;
        public static void Render(VlabUi ui,RectTransform parent,GameSettings draft,bool vi,bool reduced,Action<int> pick,bool rightHand=false,bool sideView=false)
        {
            var host=ui.Rect(parent,"Controller diagram",0,0,Width,Height);
            var state=host.gameObject.AddComponent<ControllerDiagramState>();
            state.Initialize(ui,draft,vi,reduced,pick,rightHand,sideView);
        }
    }
    public sealed class ControllerDiagramState:MonoBehaviour
    {
        VlabUi ui;GameSettings draft;bool vi,reduced,right,side;Action<int> pick;RectTransform body;
        static readonly string[] physical={"A","B","MENU","GRIP","TRIGGER","STICK"};
        // Actual 1536x1024 illustration button centers, top-left source coordinates.
        static readonly Vector2[] leftPoints={new Vector2(475,358),new Vector2(584,284),new Vector2(539,111),new Vector2(490,575),new Vector2(520,43),new Vector2(350,219)};
        static readonly Vector2[] rightPoints={new Vector2(1060,358),new Vector2(951,284),new Vector2(997,111),new Vector2(1040,575),new Vector2(1003,43),new Vector2(1185,219)};
        public bool RightHand=>right;
        public bool SideView=>side;
        public void Initialize(VlabUi ui,GameSettings draft,bool vi,bool reduced,Action<int> pick,bool rightHand=false,bool sideView=false)
        {this.ui=ui;this.draft=draft;this.vi=vi;this.reduced=reduced;this.pick=pick;right=rightHand;side=sideView;Build();}
        public void SetHand(bool rightHand){if(right!=rightHand){right=rightHand;Build();}}
        void Build()
        {
            if(body){body.gameObject.SetActive(false);Destroy(body.gameObject);}
            body=ui.Rect(transform,"Controller diagram body",0,0,1036,450);
            var left=ui.Button(body,vi?"Tay trái":"Left hand",0,0,164,64,()=>{right=false;Build();},right?VlabUi.Raised:VlabUi.Stroke);left.name="Controller hand left";
            var other=ui.Button(body,vi?"Tay phải":"Right hand",180,0,164,64,()=>{right=true;Build();},right?VlabUi.Stroke:VlabUi.Raised);other.name="Controller hand right";
            ui.Button(body,vi?"Mặt trước":"Front",692,0,164,64,()=>{side=false;Build();},side?VlabUi.Raised:VlabUi.Stroke).name="Controller angle front";
            ui.Button(body,vi?"Mặt bên":"Side",872,0,164,64,()=>{side=true;Build();},side?VlabUi.Stroke:VlabUi.Raised).name="Controller angle side";
            var texture=Resources.Load<Texture2D>(side?ControllerDiagramView.SideTextureResource:ControllerDiagramView.TextureResource);
            const float px=300,py=78,pw=436,ph=332;
            if(texture)
            {
                // Preserve the generated pair's actual proportions, never stretch the hand.
                // GPU crop only. Bounds include the opaque silhouette (measured from
                // final source alpha), with a little padding; the handle is cropped
                // below the grip to make the physical controls legible at VR distance.
                float sourceLeft=right?812:203,sourceTop=side?4:14,sourceWidth=530,sourceHeight=side?710:700;
                Rect uv=new Rect(sourceLeft/1536f,(1024-sourceTop-sourceHeight)/1024f,sourceWidth/1536f,sourceHeight/1024f);
                float aspect=texture.width*uv.width/(texture.height*uv.height),w=pw,h=w/aspect;
                if(h>ph){h=ph;w=h*aspect;}
                var photo=ui.Picture(body,texture,px+(pw-w)/2,py+(ph-h)/2,w,h);photo.name="Controller hand photograph";photo.uvRect=uv;
                var points=right?rightPoints:leftPoints;
                for(int i=0;i<6;i++)
                {
                    if(side&&i!=3&&i!=4)continue;
                    Vector2 source=points[i];if(side)source=i==3?(right?new Vector2(1045,570):new Vector2(490,570)):(right?new Vector2(884,333):new Vector2(650,333));
                    float x=px+(pw-w)/2+(source.x-sourceLeft)/sourceWidth*w,y=py+(ph-h)/2+(source.y-sourceTop)/sourceHeight*h;
                    float by=CalloutTop(i);bool onLeft=CalloutLeft(i);
                    Line(new Vector2(onLeft?264:772,by+45),new Vector2(x,y));
                    int option=Option(i);var hot=ui.Button(body,"",x-15,y-15,30,30,()=>pick?.Invoke(option),Color.clear);hot.name="Controller hotspot "+option;
                    var face=hot.GetComponent<Image>();face.color=Color.clear;var opacity=hot.GetComponent<VlabPanelOpacity>();opacity.FlatAlpha=0;opacity.Solid=false;
                    foreach(var effect in hot.GetComponents<Shadow>())effect.enabled=false;
                    var ring=ui.Rect(hot.transform,"Physical button ring",1,1,28,28).gameObject.AddComponent<Image>();ring.sprite=VlabUi.PointerRing();ring.color=VlabUi.TextColor;ring.raycastTarget=false;
                    var focus=hot.gameObject.AddComponent<ControllerDiagramCue>();focus.Reduced=reduced;focus.Edge=hot.GetComponent<Outline>();focus.Ring=ring;focus.Accent=VlabUi.Hex(right?"B87551":"3E7E8A");
                }
            }
            else ui.Text(body,vi?"Chưa có ảnh tay cầm":"Controller illustration unavailable",px,176,pw,110,26,null,TextAlignmentOptions.Center);
            for(int i=0;i<6;i++)
            {
                if(side&&i!=3&&i!=4)continue;
                int option=Option(i);float x=CalloutLeft(i)?0:772,y=CalloutTop(i);
                var button=ui.Button(body,"",x,y,264,90,()=>pick?.Invoke(option));button.name="Controller physical "+option;
                string name=physical[i];if(i==4)name=vi?"Cò bóp":"Trigger";if(i==5)name=vi?"Nhấn cần":"Stick press";
                var heading=ui.Text(button.transform,name,18,5,228,38,24);heading.fontStyle=FontStyles.Bold;
                int owner=ControllerBindings.OwnerOfPhysical(draft,option);string[] actions=vi?new[]{"Chọn / xác nhận","Cầm / thả","Mở menu","Căn hướng","Quay lại / hủy"}:new[]{"Select / confirm","Hold / release","Open menu","Recenter","Back / cancel"};
                var caption=ui.Text(button.transform,owner>=0?actions[owner]:(vi?"Chưa gán":"Unassigned"),18,46,228,38,24,VlabUi.Secondary);caption.name="Controller assigned action";
                var cue=button.gameObject.AddComponent<ControllerDiagramCue>();cue.Reduced=reduced;cue.Edge=button.GetComponent<Outline>();cue.Accent=VlabUi.Hex(right?"B87551":"3E7E8A");
            }
            ui.Text(body,vi?"Hình minh họa · Gán nút HID. BLE riêng chưa xác minh.":"Illustration · Standard HID mappings. Custom BLE is unverified.",0,414,1036,36,22,VlabUi.Secondary).name="Controller mapping scope";
        }
        int Option(int i)=>i==0?4:i==1?5:i==2?2:i==3?(right?9:8):i==4?(right?11:10):(right?13:12);
        bool CalloutLeft(int i)=>side?i==3:i==0||i==2||i==5;
        float CalloutTop(int i)=>side?(i==3?258:128):84+Row(i)*108;
        static int Row(int i)=>i==2||i==4?0:i==1||i==5?1:2;
        void Line(Vector2 start,Vector2 end)
        {
            var line=ui.Rect(body,"Controller callout line",start.x,start.y,(end-start).magnitude,2.5f);line.pivot=new Vector2(0,.5f);line.anchoredPosition=new Vector2(start.x,-start.y);
            line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);var image=line.gameObject.AddComponent<Image>();image.color=VlabUi.Stroke;image.raycastTarget=false;
        }
    }
    public sealed class ControllerDiagramCue:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public bool Reduced;public Outline Edge;public Image Ring;public Color Accent;bool hover;float amount;
        public void OnPointerEnter(PointerEventData data){hover=true;}
        public void OnPointerExit(PointerEventData data){hover=false;}
        public void OnSelect(BaseEventData data){hover=true;}
        public void OnDeselect(BaseEventData data){hover=false;}
        void Update()
        {
            float next=Reduced?(hover?1:0):Mathf.MoveTowards(amount,hover?1:0,Time.unscaledDeltaTime/0.16f);
            if(Mathf.Approximately(next,amount))return;amount=next;
            transform.localScale=Vector3.one*(Reduced?1:1+.025f*amount);
            if(Edge){Edge.enabled=amount>0;Edge.effectColor=Color.Lerp(VlabUi.Stroke,Accent,amount);}
            if(Ring)Ring.color=Color.Lerp(VlabUi.TextColor,Accent,amount);
        }
    }
}
