using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vlab.Product
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VlabWhiteboard:UnityEngine.UI.MaskableGraphic,IPointerDownHandler,IDragHandler,IPointerUpHandler,IPointerClickHandler
    {
        public const int StrokeLimit=NotebookPage.InkStrokeLimit,PointLimit=NotebookPage.InkPointLimit;
        internal sealed class Drawing
        {
            internal readonly List<NotebookStroke> strokes;
            internal List<Vector2> current;
            NotebookStroke erased;
            int erasedIndex;
            internal Drawing(List<NotebookStroke> strokes=null){this.strokes=strokes??new List<NotebookStroke>();}
            internal bool Add(Vector2 point,bool start)
            {
                if(!float.IsFinite(point.x)||!float.IsFinite(point.y))return false;
                point=new Vector2(Mathf.Clamp01(point.x),Mathf.Clamp01(point.y));
                if(start)current=null;
                if(current==null){if(strokes.Count>=StrokeLimit)return false;current=new List<Vector2>(PointLimit);strokes.Add(new NotebookStroke{points=current});}
                if(current.Count>=PointLimit)return false;
                if(current.Count>0&&(current[current.Count-1]-point).sqrMagnitude<.000004f)return false;
                erased=null;current.Add(point);return true;
            }
            internal bool Erase(Vector2 point,Vector2 size)
            {
                if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(size.x)||!float.IsFinite(size.y)||size.x<=0||size.y<=0)return false;
                var pixel=Vector2.Scale(point,size);
                for(int s=strokes.Count-1;s>=0;s--)
                {
                    var stroke=strokes[s].points;
                    for(int p=0;p<stroke.Count;p++)
                    {
                        var a=Vector2.Scale(stroke[Mathf.Max(0,p-1)],size);var b=Vector2.Scale(stroke[p],size);var ab=b-a;
                        var nearest=a+ab*(ab.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(pixel-a,ab)/ab.sqrMagnitude):0);
                        if((pixel-nearest).sqrMagnitude>225)continue;
                        erased=strokes[s];erasedIndex=s;strokes.RemoveAt(s);current=null;return true;
                    }
                }
                return false;
            }
            internal bool Undo()
            {
                current=null;
                if(erased!=null){strokes.Insert(erasedIndex,erased);erased=null;return true;}
                if(strokes.Count==0)return false;
                strokes.RemoveAt(strokes.Count-1);return true;
            }
            internal void Clear(){current=null;erased=null;strokes.Clear();}
        }
        static readonly Dictionary<string,Drawing> drawings=new Dictionary<string,Drawing>();
        Drawing drawing;
        bool erase,receivedDown,clearArmed;
        int activePointer=int.MinValue,downPointer=int.MinValue;
        TMPro.TextMeshProUGUI status;
        bool vietnamese,notebook;
        Action modified;
        Color paperColor=VlabUi.Hex("F4F8F4");
        internal static VlabWhiteboard RenderNotebook(VlabUi ui,RectTransform parent,NotebookPage note,bool vi,Action changed)
        {
            var paper=ui.Rect(parent,"Journal ink paper",0,80,1036,246);paper.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var ink=ui.Rect(paper,"Journal note ink",0,0,1036,246);
            var board=ink.gameObject.AddComponent<VlabWhiteboard>();board.drawing=new Drawing(note.inkStrokes);board.vietnamese=vi;board.notebook=true;board.modified=changed;
            board.color=JournalView.Ink;board.paperColor=JournalView.Paper;board.raycastTarget=true;
            ui.Button(parent,vi?"Bút / nét mới":"Pen / new stroke",0,0,242,64,()=>board.SetPen(false)).name="Journal ink pen";
            ui.Button(parent,vi?"Tẩy nét":"Erase stroke",258,0,190,64,()=>board.SetPen(true)).name="Journal ink erase";
            ui.Button(parent,vi?"Hoàn tác":"Undo",464,0,190,64,board.UndoStroke).name="Journal ink undo";
            board.status=ui.Text(parent,"",678,8,358,56,22,JournalView.Pencil);board.status.name="Journal ink mode";board.Changed();return board;
        }
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
            ui.Button(modal,vi?"Hoàn tác":"Undo",548,148,242,56,board.UndoStroke);
            ui.Button(modal,vi?"Xóa bảng":"Clear board",806,148,262,56,()=>{if(board.clearArmed){data.Clear();board.Changed();}else{board.clearArmed=true;board.status.text=vi?"Bấm Xóa bảng lần nữa để xác nhận. Xóa bảng không hoàn tác được.":"Select Clear board again to confirm. Clearing cannot be undone.";}});
            board.status=ui.Text(modal,"",32,466,1036,40,21);board.Changed();
            var entry=LearningGuide.Load(key);
            string suggestion=entry==null?(vi?"Vẽ sơ đồ bố trí. Ghi điều bạn dự đoán và so với điều bạn thực sự quan sát.":"Sketch the setup. Record your prediction and compare it with your own observation."):(vi?"GỢI Ý TỪ BÀI HỌC\n":"LESSON PROMPTS (Vietnamese source)\n")+entry.practiceVi+"\n\n"+(vi?"CHỖ DỄ NHẦM\n":"COMMON PITFALLS (Vietnamese source)\n")+entry.pitfallsVi;
            ui.Scroll(modal,suggestion,32,512,1036,194,22);
            ui.Text(modal,vi?"Bản nháp giữ trong phiên này; thoát ứng dụng sẽ mất. Không phải số đo đã lưu.":"Draft retained for this session; closing the app clears it. This is not saved measurement data.",32,718,770,58,20,VlabUi.Secondary);
            ui.Button(modal,vi?"Trở lại":"Back",832,718,236,58,close);
            return board;
        }
        void SetPen(bool eraser){erase=eraser;drawing.current=null;Changed();}
        void UndoStroke(){if(drawing.Undo())Changed(true);}
        void Changed(bool edit=false)
        {
            clearArmed=false;SetVerticesDirty();
            if(edit)modified?.Invoke();
            if(status)status.text=notebook?(erase?(vietnamese?"TẨY · chọn một nét":"ERASE · select a stroke"):(vietnamese?"BÚT · ":"PEN · ")+drawing.strokes.Count+" / "+StrokeLimit):erase?(vietnamese?"TẨY: chọn một nét để xóa cả nét. Hoàn tác để khôi phục nét vừa tẩy.":"ERASE: select a stroke to remove it. Undo restores the last erased stroke."):(vietnamese?"VẼ: giữ và kéo. VR: chọn các điểm để nối; Nét mới để ngắt. ":"DRAW: hold and drag. VR: select points to join; New stroke breaks the line. ")+drawing.strokes.Count+" / "+StrokeLimit;
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
            if(erase){if(drawing.Erase(point,rectTransform.rect.size))Changed(true);}
            else if(drawing.Add(point,start))Changed(true);
            else if(status&&(drawing.strokes.Count>=StrokeLimit||drawing.current?.Count>=PointLimit))status.text=notebook?(vietnamese?"Đầy nét · chọn Nét mới / Hoàn tác":"Limit reached · New stroke / Undo"):(vietnamese?"Nét tối đa 128 điểm, bảng tối đa 48 nét. Chọn Nét mới hoặc hoàn tác.":"Limit: 128 points per stroke, 48 strokes. Choose New stroke or Undo.");
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||activePointer!=int.MinValue||!Point(e,out var point))return;
            receivedDown=true;downPointer=activePointer=e.pointerId;Apply(point,true);
        }
        public void OnDrag(PointerEventData e){if(e.pointerId==activePointer&&Point(e,out var point))Apply(point,false);}
        public void OnPointerUp(PointerEventData e)
        {
            if(e.pointerId!=activePointer)return;
            activePointer=int.MinValue;receivedDown=e.eligibleForClick&&!e.dragging&&Point(e,out _);
        }
        public void OnPointerClick(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||activePointer!=int.MinValue)return;
            if(receivedDown&&e.pointerId==downPointer){receivedDown=false;return;}
            receivedDown=false;
            if(Point(e,out var point))Apply(point,false);
        }
        protected override void OnDisable(){activePointer=int.MinValue;receivedDown=false;if(drawing!=null)drawing.current=null;base.OnDisable();}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(drawing==null)return;var r=rectTransform.rect;
            // Keep an empty board visible to GraphicRaycaster before the first stroke.
            Color paper=paperColor;
            vh.AddVert(new Vector2(r.xMin,r.yMin),paper,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),paper,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),paper,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),paper,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
            foreach(var savedStroke in drawing.strokes)
            {
                var stroke=savedStroke.points;
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
}
