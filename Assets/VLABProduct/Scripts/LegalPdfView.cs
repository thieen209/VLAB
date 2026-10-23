using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Page images are rendered from bundled source PDFs; only the visible page is resident.
    public sealed class LegalPdfView:MonoBehaviour
    {
        [Serializable] public sealed class Document {public string key,sourceSha256,sourceFile;public int pageCount;}
        Document document;RawImage image;ScrollRect scroll;TMP_Text status;Texture2D texture;Button previous,next,smaller,larger;bool vi;float zoom=1;int page;
        public int PageIndex=>page;
        public int PageCount=>document?.pageCount??0;
        public float Zoom=>zoom;
        public Texture2D CurrentTexture=>texture;
        public bool Ready=>document!=null&&texture;
        public static Document Metadata(string key)
        {
            if(!new[]{"privacy_vi","privacy_en","terms_vi","terms_en"}.Contains(key))return null;
            var source=Resources.Load<TextAsset>("LegalPdf/"+key+"/index");if(!source)return null;
            try{var value=JsonUtility.FromJson<Document>(source.text);return value!=null&&value.key==key&&value.pageCount>0&&value.pageCount<=1000&&value.sourceSha256?.Length==64&&value.sourceSha256.All(Uri.IsHexDigit)?value:null;}
            catch(ArgumentException){return null;}
        }
        public bool Initialize(VlabUi ui,RectTransform parent,string key,bool vietnamese)
        {
            document=Metadata(key);vi=vietnamese;if(document==null)return false;
            var host=ui.Rect(parent,"PDF page viewport",48,158,1024,480);host.gameObject.AddComponent<Image>().color=Color.white;host.gameObject.AddComponent<RectMask2D>();
            scroll=host.gameObject.AddComponent<ScrollRect>();scroll.viewport=host;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=70;
            var paper=ui.Rect(host,"Original PDF page",0,0,1024,1448);paper.anchorMin=paper.anchorMax=paper.pivot=new Vector2(0,1);image=paper.gameObject.AddComponent<RawImage>();image.color=Color.white;image.raycastTarget=false;scroll.content=paper;
            float x=48;
            Button Control(string id,string label,float width,Action action){var b=ui.Button(parent,label,x,650,width,64,action);b.name="PDF "+id;x+=width+14;return b;}
            previous=Control("previous",vi?"Trang trước":"Previous",144,()=>Turn(-1));next=Control("next",vi?"Trang sau":"Next",144,()=>Turn(1));
            smaller=Control("zoom out","−",100,()=>SetZoom(zoom-.5f));larger=Control("zoom in","+",100,()=>SetZoom(zoom+.5f));
            Control("up",vi?"Lên":"Up",100,()=>Pan(0,1));Control("down",vi?"Xuống":"Down",100,()=>Pan(0,-1));Control("left",vi?"Trái":"Left",100,()=>Pan(-1,0));Control("right",vi?"Phải":"Right",100,()=>Pan(1,0));
            status=ui.Text(parent,"",48,722,1024,30,22);status.name="PDF page status";
            LoadPage(0);return Ready;
        }
        void Turn(int direction){LoadPage(Mathf.Clamp(page+direction,0,PageCount-1));}
        void LoadPage(int index)
        {
            Release();page=index;texture=Resources.Load<Texture2D>("LegalPdf/"+document.key+"/page-"+(page+1).ToString("D4"));image.texture=texture;
            if(!texture){previous.interactable=page>0;next.interactable=page<PageCount-1;smaller.interactable=larger.interactable=false;status.text=vi?"Thiếu trang PDF. Không thể đọc tài liệu đầy đủ.":"PDF page missing. The document is incomplete.";return;}
            Layout();scroll.horizontalNormalizedPosition=0;scroll.verticalNormalizedPosition=1;RefreshControls();
        }
        void SetZoom(float value){zoom=Mathf.Clamp(value,1,3);Layout();RefreshControls();}
        void Layout(){if(!texture)return;float width=scroll.viewport.rect.width*zoom;image.rectTransform.sizeDelta=new Vector2(width,width*texture.height/texture.width);scroll.horizontal=zoom>1;scroll.vertical=true;}
        void Pan(float x,float y)
        {
            Vector2 overflow=image.rectTransform.rect.size-scroll.viewport.rect.size;
            if(overflow.x>0)scroll.horizontalNormalizedPosition=Mathf.Clamp01(scroll.horizontalNormalizedPosition+x*scroll.viewport.rect.width*.8f/overflow.x);
            if(overflow.y>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+y*scroll.viewport.rect.height*.8f/overflow.y);
        }
        void RefreshControls(){previous.interactable=page>0;next.interactable=page<PageCount-1;smaller.interactable=zoom>1;larger.interactable=zoom<3;status.text=(vi?"Trang ":"Page ")+(page+1)+" / "+PageCount+" · "+Mathf.RoundToInt(zoom*100)+"%";}
        void Release(){if(image)image.texture=null;if(texture){Resources.UnloadAsset(texture);texture=null;}}
        void OnDisable(){Release();}
        void OnEnable(){if(document!=null&&image&&!texture)LoadPage(page);}
    }
}
