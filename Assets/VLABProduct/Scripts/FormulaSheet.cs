using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    // Build-time mathematical typesetting; no runtime TeX or scientific evaluation.
    public static class FormulaSheet
    {
        [Serializable] public sealed class Entry {public string id,sourceSha256;public int pages;public string[] equations;}
        [Serializable] sealed class Catalogue {public Entry[] items;}
        static Dictionary<string,Entry> entries;
        public static IReadOnlyDictionary<string,Entry> All
        {
            get
            {
                if(entries==null){var source=Resources.Load<TextAsset>("Catalog/formula-sheets");var data=source?JsonUtility.FromJson<Catalogue>(source.text):null;entries=(data?.items??Array.Empty<Entry>()).ToDictionary(item=>item.id,StringComparer.Ordinal);}
                return entries;
            }
        }
        public static Texture2D Image(string id,int page)=>Resources.Load<Texture2D>("UI/Formulas/"+id+(page==0?"":"_"+(page+1)));
        public static void Render(VlabUi ui,RectTransform parent,string id,bool vi,JournalState state,Action refresh)
        {
            if(!All.TryGetValue(id,out var entry)){ui.Text(parent,vi?"Trang công thức chưa có trong bản cài đặt.":"Formula sheet unavailable in this installation.",32,260,1036,160,28);return;}
            state.contentPage=Mathf.Clamp(state.contentPage,0,entry.pages-1);
            var note=ui.Text(parent,vi?"Ký hiệu và điều kiện áp dụng: xem Kiến thức.":"Definitions and validity conditions: see Knowledge.",32,248,1036,44,24);note.color=JournalView.Ink;
            var texture=Image(id,state.contentPage);
            // Exact 3:1 source aspect; opaque paper and ink retain contrast at any menu opacity.
            var image=ui.Picture(parent,texture,32,296,1036,244);image.name="Journal formula image";
            if(texture){float width=Mathf.Min(1036,244f*texture.width/texture.height);image.rectTransform.sizeDelta=new Vector2(width,width*texture.height/texture.width);image.rectTransform.anchoredPosition=new Vector2(32+(1036-width)*.5f,-296);}
            var previous=ui.Button(parent,vi?"‹ Trang trước":"‹ Previous page",32,552,240,64,()=>{state.contentPage--;refresh();});previous.name="Journal content previous";previous.interactable=state.contentPage>0;
            var label=ui.Text(parent,(vi?"Công thức ":"Formulas ")+(state.contentPage+1)+" / "+entry.pages,302,568,432,44,24,VlabUi.Secondary,TMPro.TextAlignmentOptions.Center);label.color=JournalView.Pencil;
            var next=ui.Button(parent,vi?"Trang sau ›":"Next page ›",828,552,240,64,()=>{state.contentPage++;refresh();});next.name="Journal content next";next.interactable=state.contentPage+1<entry.pages;
        }
    }
}
