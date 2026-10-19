using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class NotebookKeyboard
    {
        public static void Render(VlabUi ui,RectTransform box,TMP_InputField target,bool vi,Action done)
        {
            if(!target)throw new ArgumentNullException(nameof(target));
            var backing=box.GetComponent<VlabPanelOpacity>();backing.Solid=true;backing.Apply(ui.canvas.renderMode==RenderMode.WorldSpace);
            var state=box.gameObject.AddComponent<NotebookKeyboardState>();state.Target=target;state.Vietnamese=vi;state.Done=done;state.CaptureSelection();
            var preview=ui.Panel(box,"Notebook keyboard preview",40,108,1360,170,VlabUi.Raised);
            var scroll=preview.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=ui.Rect(preview,"Notebook preview viewport",16,14,1090,142);viewport.gameObject.AddComponent<RectMask2D>();
            var body=ui.Text(viewport,"",0,0,1070,142,30);body.richText=false;body.overflowMode=TextOverflowModes.Overflow;body.name="Notebook keyboard live text";
            scroll.viewport=viewport;scroll.content=body.rectTransform;var geometry=preview.gameObject.AddComponent<VlabScrollContent>();geometry.Body=body;geometry.Scroll=scroll;state.Preview=body;state.Geometry=geometry;
            geometry.Up=ui.Button(preview,vi?"Lên trang":"Page up",1120,10,220,70,()=>geometry.Move(1));geometry.Up.name="Notebook key Page up";
            geometry.Down=ui.Button(preview,vi?"Xuống trang":"Page down",1120,90,220,70,()=>geometry.Move(-1));geometry.Down.name="Notebook key Page down";
            ui.Text(box,vi?"Thêm dấu ngay sau nguyên âm · | là vị trí con trỏ":"Add a tone immediately after its vowel · | marks the caret",40,286,1360,42,24,VlabUi.Secondary);
            state.Status=ui.Text(box,"",40,824,1360,42,24,VlabUi.Secondary);state.Status.name="Notebook keyboard status";
            void Key(string key,string label,float x,float y,float w=106)
            {
                var button=ui.Button(box,label,x,y,w,70,()=>state.Press(key));button.name="Notebook key "+key;state.Buttons[key]=button;
                var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.fontSize=key.Length==1?(ui.LargeText?33.6f:30):24;
                if(key.Length==1&&char.IsLetter(key[0]))state.Letters[key]=caption;
            }
            for(int i=0;i<10;i++)Key("1234567890"[i].ToString(),"1234567890"[i].ToString(),40+i*114,342);
            Key("Backspace",vi?"Xóa lùi":"Backspace",1180,342,220);
            const string row1="qwertyuiop";for(int i=0;i<row1.Length;i++)Key(row1[i].ToString(),row1[i].ToString(),40+i*114,422);
            Key("Shift",vi?"Chữ hoa":"Shift",1180,422,220);
            const string row2="asdfghjkl";for(int i=0;i<row2.Length;i++)Key(row2[i].ToString(),row2[i].ToString(),40+i*114,502);
            Key("Newline",vi?"Xuống dòng":"New line",1070,502,330);state.Buttons["Newline"].interactable=target.lineType!=TMP_InputField.LineType.SingleLine;
            const string row3="zxcvbnm.,?";for(int i=0;i<row3.Length;i++)Key(row3[i].ToString(),row3[i].ToString(),40+i*114,582);
            Key("Left","<",1180,582);Key("Right",">",1294,582);
            const string vowels="ăâêôơưđ";for(int i=0;i<vowels.Length;i++)Key(vowels[i].ToString(),vowels[i].ToString(),40+i*114,662);
            Key("Space",vi?"Dấu cách":"Space",842,662,558);
            string[] tones={"acute","grave","hook","tilde","dot","clear-tone"},labels=vi?new[]{"Sắc","Huyền","Hỏi","Ngã","Nặng","Bỏ dấu"}:new[]{"Acute","Grave","Hook","Tilde","Dot","Clear tone"};
            for(int i=0;i<tones.Length;i++)Key(tones[i],labels[i],40+i*178,742,170);
            Key("Done",vi?"Xong":"Done",1112,742,288);state.Refresh();
        }
        static int[] Boundaries(string value)=>StringInfo.ParseCombiningCharacters(value??"").Concat(new[]{(value??"").Length}).ToArray();
        static int Floor(string value,int index){index=Math.Max(0,Math.Min(value.Length,index));return Boundaries(value).Last(x=>x<=index);}
        static int Ceiling(string value,int index){index=Math.Max(0,Math.Min(value.Length,index));return Boundaries(value).First(x=>x>=index);}
        static void Range(string value,int anchor,int focus,out int first,out int last)
        {
            first=Floor(value,Math.Min(anchor,focus));last=anchor==focus?first:Ceiling(value,Math.Max(anchor,focus));
        }
        public static string Insert(string value,int anchor,int focus,string insertion,int limit,out int caret,out bool accepted)
        {
            value=value??"";insertion=insertion??"";Range(value,anchor,focus,out int first,out int last);caret=first;
            if(limit>0&&value.Length-(last-first)+insertion.Length>limit){accepted=false;caret=Floor(value,focus);return value;}
            accepted=true;caret=first+insertion.Length;return value.Substring(0,first)+insertion+value.Substring(last);
        }
        public static string Backspace(string value,int anchor,int focus,out int caret)
        {
            value=value??"";Range(value,anchor,focus,out int first,out int last);
            if(first==last&&first>0)first=Boundaries(value).Last(x=>x<first);
            caret=first;return value.Remove(first,last-first);
        }
        public static int Move(string value,int anchor,int focus,bool right)
        {
            value=value??"";Range(value,anchor,focus,out int first,out int last);if(first!=last)return right?last:first;
            return right?Boundaries(value).Where(x=>x>last).DefaultIfEmpty(value.Length).First():Boundaries(value).Where(x=>x<first).DefaultIfEmpty(0).Last();
        }
        public static string Tone(string value,int anchor,int focus,string tone,int limit,out int caret,out bool accepted)
        {
            value=value??"";caret=Floor(value,focus);accepted=false;if(anchor!=focus||caret==0)return value;
            int position=caret,start=Boundaries(value).Last(x=>x<position);string previous=value.Substring(start,position-start),decomposed=previous.Normalize(NormalizationForm.FormD);
            if(decomposed.Length==0||"aeiouyAEIOUY".IndexOf(decomposed[0])<0)return value;
            char mark;switch(tone){case "acute":mark='\u0301';break;case "grave":mark='\u0300';break;case "hook":mark='\u0309';break;case "tilde":mark='\u0303';break;case "dot":mark='\u0323';break;case "clear-tone":mark='\0';break;default:return value;}
            string clean=new string(decomposed.Where(x=>x!='\u0300'&&x!='\u0301'&&x!='\u0303'&&x!='\u0309'&&x!='\u0323').ToArray());
            string replacement=(clean+(mark=='\0'?"":mark.ToString())).Normalize(NormalizationForm.FormC);
            return Insert(value,start,caret,replacement,limit,out caret,out accepted);
        }
    }
    public sealed class NotebookKeyboardState:MonoBehaviour
    {
        public TMP_InputField Target;public bool Vietnamese,Shift;public Action Done;public TextMeshProUGUI Preview,Status;public VlabScrollContent Geometry;
        public readonly Dictionary<string,Button> Buttons=new Dictionary<string,Button>();public readonly Dictionary<string,TextMeshProUGUI> Letters=new Dictionary<string,TextMeshProUGUI>();
        int anchor,focus;string lastText="",message="";bool finished;
        public void CaptureSelection(){if(Target){anchor=Target.selectionStringAnchorPosition;focus=Target.selectionStringFocusPosition;}}
        void LateUpdate(){if(!Target)return;if(Target.isFocused)CaptureSelection();if(lastText!=Target.text)Refresh();}
        public void Press(string key)
        {
            if(!Target||finished)return;
            if(key=="Done"){finished=true;Done?.Invoke();return;}
            if(key=="Shift"){Shift=!Shift;foreach(var pair in Letters)pair.Value.text=Shift?pair.Key.ToUpperInvariant():pair.Key;Refresh();return;}
            string value=Target.text??"",next=value;int caret=focus;bool accepted=true;message="";
            if(key=="Left"||key=="Right"){anchor=focus=NotebookKeyboard.Move(value,anchor,focus,key=="Right");ApplyCaret();Refresh();return;}
            if(key=="Backspace")next=NotebookKeyboard.Backspace(value,anchor,focus,out caret);
            else if(new[]{"acute","grave","hook","tilde","dot","clear-tone"}.Contains(key))
            {
                next=NotebookKeyboard.Tone(value,anchor,focus,key,Target.characterLimit,out caret,out accepted);
                if(!accepted)message=Vietnamese?"Đặt con trỏ ngay sau nguyên âm để thêm dấu.":"Place the caret immediately after a vowel to add its tone.";
            }
            else
            {
                if(key=="Newline"&&Target.lineType==TMP_InputField.LineType.SingleLine)return;
                string insertion=key=="Space"?" ":key=="Newline"?"\n":Shift?key.ToUpperInvariant():key;
                next=NotebookKeyboard.Insert(value,anchor,focus,insertion,Target.characterLimit,out caret,out accepted);
                if(!accepted)message=Vietnamese?"Đã đạt giới hạn ký tự; nội dung vẫn được giữ.":"Character limit reached; your writing is retained.";
            }
            if(accepted){anchor=focus=caret;Target.text=next;ApplyCaret();}Refresh();
        }
        void ApplyCaret(){Target.selectionStringAnchorPosition=anchor;Target.selectionStringFocusPosition=focus;Target.ForceLabelUpdate();}
        public void Refresh()
        {
            if(!Target||!Preview)return;string value=Target.text??"";focus=Mathf.Clamp(focus,0,value.Length);anchor=Mathf.Clamp(anchor,0,value.Length);
            Preview.text=value.Insert(focus,"|");lastText=value;
            Status.text=!string.IsNullOrEmpty(message)?message:(Vietnamese?"Đang viết":"Writing")+" · "+value.Length+(Target.characterLimit>0?" / "+Target.characterLimit:"")+(Shift?" · Shift":"");
            Geometry.Refresh();Preview.ForceMeshUpdate();var characters=Preview.textInfo.characterInfo;
            for(int i=0;i<Preview.textInfo.characterCount;i++)if(characters[i].index==focus)
            {
                float top=-characters[i].ascender,bottom=-characters[i].descender,offset=Preview.rectTransform.anchoredPosition.y,height=Geometry.Scroll.viewport.rect.height;
                if(top<offset)offset=top;if(bottom>offset+height)offset=bottom-height+12;
                var p=Preview.rectTransform.anchoredPosition;p.y=Mathf.Clamp(offset,0,Mathf.Max(0,Preview.rectTransform.rect.height-height));Preview.rectTransform.anchoredPosition=p;break;
            }
        }
    }
}
