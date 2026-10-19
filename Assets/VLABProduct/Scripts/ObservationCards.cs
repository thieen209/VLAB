using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Vlab.Product
{
    // The same readable instrument values appear live and in the saved notebook.
    public static class ObservationCards
    {
        public static string FormatValue(double value,bool vi)
        {
            var culture=CultureInfo.GetCultureInfo(vi?"vi-VN":"en-US");
            if(value!=0&&Math.Abs(value)<.001)return value.ToString("0.###E+0",culture);
            string text=value.ToString("0.###",culture);return text.Length>9?value.ToString("0.###E+0",culture):text;
        }
#if UNITY_EDITOR
        public static void CheckFormatting(Action<string,bool> check)
        {
            check("Small positive amounts never display as zero",FormatValue(.0001,true)=="1E-4");
            check("Small negative amounts preserve their sign",FormatValue(-.0001,true)=="-1E-4");
            check("Exact zero remains zero",FormatValue(0,true)=="0");
            check("Vietnamese decimal comma is readable at threshold",FormatValue(.001,true)=="0,001");
            check("English decimal support remains available",FormatValue(.001,false)=="0.001");
        }
#endif
        public static int PageCount(IEnumerable<Reading> readings,int pageSize=4)=>Math.Max(1,((readings?.Count()??0)+pageSize-1)/pageSize);
        public static void Render(VlabUi ui,Transform parent,IEnumerable<Reading> readings,bool vi,int page,float x,float y,float width,float height,int pageSize=4)
        {
            var values=(readings??Enumerable.Empty<Reading>()).ToArray();page=Mathf.Clamp(page,0,PageCount(values,pageSize)-1);
            int rows=pageSize<=2?1:2;float gap=20,cardWidth=(width-gap)/2,cardHeight=(height-gap*(rows-1))/rows;
            var selected=values.Skip(page*pageSize).Take(pageSize).ToArray();
            if(selected.Length==0){ui.Text(parent,vi?"Lắp dụng cụ và làm bước hướng dẫn để xem số đo.":"Fit the equipment and follow the guide to see readings.",x+24,y+40,width-48,height-60,30,VlabUi.Secondary);return;}
            for(int i=0;i<selected.Length;i++)
            {
                var reading=selected[i];var card=ui.Panel(parent,"Observation card "+reading.key,x+i%2*(cardWidth+gap),y+i/2*(cardHeight+gap),cardWidth,cardHeight,VlabUi.Raised);
                ui.Rule(card,0,20,4,cardHeight-40,VlabUi.Stroke);
                var binding=card.gameObject.AddComponent<ObservationCardBinding>();binding.Key=reading.key;
                binding.Label=ui.Text(card,VlabApp.ReadingCaption(reading,vi),22,14,cardWidth-44,76,26,VlabUi.Secondary);binding.Label.name="Observation label";
                binding.Value=ui.Text(card,"",22,cardHeight-82,cardWidth-180,70,48);binding.Value.fontWeight=FontWeight.SemiBold;binding.Value.name="Observation value";
                binding.Unit=ui.Text(card,reading.unit??"",cardWidth-150,cardHeight-57,128,40,26,VlabUi.Secondary);binding.Unit.name="Observation unit";
                binding.Show(reading,vi);
            }
        }
        public static void Refresh(Transform parent,IEnumerable<Reading> readings,bool vi)
        {
            if(!parent)return;var values=(readings??Enumerable.Empty<Reading>()).ToArray();
            foreach(var card in parent.GetComponentsInChildren<ObservationCardBinding>()){var reading=values.FirstOrDefault(r=>r.key==card.Key);if(reading!=null)card.Show(reading,vi);else{card.Value.text="—";card.Unit.text="";}}
        }
    }
    public sealed class ObservationCardBinding:MonoBehaviour
    {
        public string Key;public TextMeshProUGUI Label,Value,Unit;
        public void Show(Reading reading,bool vi)
        {
            Label.text=VlabApp.ReadingCaption(reading,vi);Unit.text=reading.unit??"";
            if(reading.unit=="presence"){Value.text=reading.value>.5?(vi?"Có":"Present"):(vi?"Không":"Absent");Unit.text="";}
            else if(reading.key=="BTBstate"&&reading.unit=="interval")
            {Value.text=reading.value==1?(vi?"≤ 6,0":"≤ 6.0"):reading.value==2?(vi?"6,0–7,6":"6.0–7.6"):reading.value==3?(vi?"≥ 7,6":"≥ 7.6"):(vi?"Chưa đo":"Unread");Unit.text="pH";}
            else Value.text=ObservationCards.FormatValue(reading.value,vi);
            float width=((RectTransform)transform).rect.width;
            float unitWidth=string.IsNullOrEmpty(Unit.text)?0:Unit.GetPreferredValues(Unit.text,float.PositiveInfinity,40).x+4;
            Unit.rectTransform.sizeDelta=new Vector2(unitWidth,40);
            Unit.rectTransform.anchoredPosition=new Vector2(width-22-unitWidth,Unit.rectTransform.anchoredPosition.y);
            Value.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,width-44-(unitWidth>0?unitWidth+22:0)),70);
        }
    }
}
