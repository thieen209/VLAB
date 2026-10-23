using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vlab.Product
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VlabWhiteboard:UnityEngine.UI.MaskableGraphic,IPointerDownHandler,IDragHandler,IPointerUpHandler,IPointerClickHandler
    {
        public const int StrokeLimit=48,PointLimit=128;
        internal sealed class Drawing
        {
            internal readonly List<List<Vector2>> strokes=new List<List<Vector2>>();
            internal List<Vector2> current;
            internal bool Add(Vector2 point,bool start)
            {
                if(!float.IsFinite(point.x)||!float.IsFinite(point.y))return false;
                point=new Vector2(Mathf.Clamp01(point.x),Mathf.Clamp01(point.y));
                if(start||current==null){if(strokes.Count>=StrokeLimit)return false;current=new List<Vector2>(PointLimit);strokes.Add(current);}
                if(current.Count>=PointLimit)return false;
                if(current.Count>0&&(current[current.Count-1]-point).sqrMagnitude<.000004f)return false;
                current.Add(point);return true;
            }
            internal void Undo(){current=null;if(strokes.Count>0)strokes.RemoveAt(strokes.Count-1);}
            internal void Clear(){current=null;strokes.Clear();}
        }
        static readonly Dictionary<string,Drawing> drawings=new Dictionary<string,Drawing>();
        Drawing drawing;
        bool erase,receivedDown,clearArmed;
        int activePointer=int.MinValue;
        TMPro.TextMeshProUGUI status;
        bool vietnamese;
        public static VlabWhiteboard Render(VlabUi ui,RectTransform modal,string experimentId,bool vi,Action close)
        {
            var key=experimentId!=null&&LessonSpec.All.ContainsKey(experimentId)?experimentId:"general";
            if(!drawings.TryGetValue(key,out var data)){data=new Drawing();drawings.Add(key,data);}data.current=null;
            ui.Text(modal,vi?"Phác thảo, đặt giả thuyết, rồi tự kiểm tra bằng thí nghiệm.":"Sketch a hypothesis, then check it with your experiment.",32,96,1036,42,23);
            var paper=ui.Rect(modal,"Whiteboard paper",32,216,1036,238);
            var background=paper.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=VlabUi.Hex("F4F8F4");background.raycastTarget=false;
            paper.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var ink=ui.Rect(paper,"Whiteboard ink",0,0,1036,238);ink.anchorMin=Vector2.zero;ink.anchorMax=Vector2.one;ink.offsetMin=ink.offsetMax=Vector2.zero;
            var board=ink.gameObject.AddComponent<VlabWhiteboard>();board.drawing=data;board.vietnamese=vi;board.color=VlabUi.Hex("164F43");board.raycastTarget=true;
            ui.Button(modal,vi?"Nét mới":"New stroke",32,148,242,56,()=>board.SetPen(false));
            ui.Button(modal,vi?"Tẩy nét":"Erase stroke",290,148,242,56,()=>board.SetPen(true));
            ui.Button(modal,vi?"Hoàn tác nét":"Undo stroke",548,148,242,56,()=>{data.Undo();board.Changed();});
            ui.Button(modal,vi?"Xóa bảng":"Clear board",806,148,262,56,()=>{if(board.clearArmed){data.Clear();board.Changed();}else{board.clearArmed=true;board.status.text=vi?"Bấm Xóa bảng lần nữa để xác nhận. Hoàn tác chỉ xóa nét vẽ cuối.":"Select Clear board again to confirm. Undo removes the last drawn stroke.";}});
            board.status=ui.Text(modal,"",32,466,1036,40,21);board.Changed();
            var entry=LearningGuide.Load(key);
            string suggestion=entry==null?(vi?"Vẽ sơ đồ bố trí. Ghi điều bạn dự đoán và so với điều bạn thực sự quan sát.":"Sketch the setup. Record your prediction and compare it with your own observation."):(vi?"GỢI Ý TỪ BÀI HỌC\n":"LESSON PROMPTS (Vietnamese source)\n")+entry.practiceVi+"\n\n"+(vi?"CHỖ DỄ NHẦM\n":"COMMON PITFALLS (Vietnamese source)\n")+entry.pitfallsVi;
            ui.Scroll(modal,suggestion,32,512,1036,194,22);
            ui.Text(modal,vi?"Bản nháp giữ trong phiên này; thoát ứng dụng sẽ mất. Không phải số đo đã lưu.":"Draft retained for this session; closing the app clears it. This is not saved measurement data.",32,718,770,58,20,VlabUi.Secondary);
            ui.Button(modal,vi?"Trở lại":"Back",832,718,236,58,close);
            return board;
        }
        void SetPen(bool eraser){erase=eraser;drawing.current=null;Changed();}
        void Changed()
        {
            clearArmed=false;SetVerticesDirty();
            if(status)status.text=erase?(vietnamese?"TẨY: chọn một nét để xóa cả nét. Thao tác tẩy không hoàn tác được.":"ERASE: select a stroke to remove it. Erasing cannot be undone."):(vietnamese?"VẼ: giữ và kéo. VR: chọn các điểm để nối; Nét mới để ngắt. ":"DRAW: hold and drag. VR: select points to join; New stroke breaks the line. ")+drawing.strokes.Count+" / "+StrokeLimit;
        }
        bool Point(PointerEventData e,out Vector2 point)
        {
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:(e.pressEventCamera?e.pressEventCamera:canvas.worldCamera);
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,camera,out var local)){point=default;return false;}
            var r=rectTransform.rect;if(r.width<=0||r.height<=0||!r.Contains(local)){point=default;return false;}
            point=new Vector2((local.x-r.xMin)/r.width,(local.y-r.yMin)/r.height);return true;
        }
        void Apply(Vector2 point,bool start)
        {
            if(erase)
            {
                var size=rectTransform.rect.size;var pixel=Vector2.Scale(point,size);
                for(int s=drawing.strokes.Count-1;s>=0;s--){var stroke=drawing.strokes[s];for(int p=0;p<stroke.Count;p++){var a=Vector2.Scale(stroke[Mathf.Max(0,p-1)],size);var b=Vector2.Scale(stroke[p],size);var ab=b-a;var nearest=a+ab*(ab.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(pixel-a,ab)/ab.sqrMagnitude):0);if((pixel-nearest).sqrMagnitude<=225){drawing.strokes.RemoveAt(s);drawing.current=null;Changed();return;}}}
            }
            else if(drawing.Add(point,start))Changed();
            else if(status&&(drawing.strokes.Count>=StrokeLimit||drawing.current?.Count>=PointLimit))status.text=vietnamese?"Nét tối đa 128 điểm, bảng tối đa 48 nét. Chọn Nét mới hoặc hoàn tác.":"Limit: 128 points per stroke, 48 strokes. Choose New stroke or Undo.";
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||activePointer!=int.MinValue||!Point(e,out var point))return;
            receivedDown=true;activePointer=e.pointerId;Apply(point,true);
        }
        public void OnDrag(PointerEventData e){if(e.pointerId==activePointer&&Point(e,out var point))Apply(point,false);}
        public void OnPointerUp(PointerEventData e){if(e.pointerId==activePointer)activePointer=int.MinValue;}
        public void OnPointerClick(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return;
            if(receivedDown){receivedDown=false;return;}
            if(Point(e,out var point))Apply(point,false);
        }
        protected override void OnDisable(){activePointer=int.MinValue;receivedDown=false;base.OnDisable();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(drawing==null)return;var r=rectTransform.rect;
            // Keep an empty board visible to GraphicRaycaster before the first stroke.
            Color paper=VlabUi.Hex("F4F8F4");
            vh.AddVert(new Vector2(r.xMin,r.yMin),paper,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),paper,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),paper,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),paper,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
            foreach(var stroke in drawing.strokes)
            for(int p=0;p<stroke.Count;p++)
            {
                Vector2 a=r.min+Vector2.Scale(stroke[Mathf.Max(0,p-1)],r.size),b=r.min+Vector2.Scale(stroke[p],r.size);
                var direction=b-a;if(direction.sqrMagnitude<.001f)b=a+Vector2.right*2;
                var side=new Vector2(-(b-a).y,(b-a).x).normalized*2.5f;int offset=vh.currentVertCount;
                vh.AddVert(a-side,color,Vector2.zero);vh.AddVert(a+side,color,Vector2.zero);vh.AddVert(b+side,color,Vector2.zero);vh.AddVert(b-side,color,Vector2.zero);
                vh.AddTriangle(offset,offset+1,offset+2);vh.AddTriangle(offset,offset+2,offset+3);
            }
        }
    }
}
