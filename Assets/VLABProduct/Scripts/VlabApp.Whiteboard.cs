using System.Linq;
using TMPro;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        int boardFormulaPage;
        void ShowWhiteboard()
        {
            string id=Session?.spec.id??selected?.id??"P01";
            var box=Modal(L("Bảng bài học · ","Lesson board · ")+LessonGuide.ShortTitle(id,Vi),1100,880);box.name="Lesson board";
            box.GetComponent<PhoneDialogLayout>().enabled=false;box.gameObject.AddComponent<WorkbenchPanelLayout>().Initialize(ui,new Vector2(1100,880),true,true);
            ui.Text(box,ReadinessView.Goal(id,Vi),32,100,1036,100,28);
            int count=FormulaSheet.All.TryGetValue(id,out var entry)?entry.pages:1;boardFormulaPage=Mathf.Clamp(boardFormulaPage,0,count-1);
            var texture=FormulaSheet.Image(id,boardFormulaPage);
            var image=ui.Picture(box,texture,32,212,1036,300);image.name="Lesson board formula";
            if(texture){float width=Mathf.Min(1036,300f*texture.width/texture.height);image.rectTransform.sizeDelta=new Vector2(width,width*texture.height/texture.width);image.rectTransform.anchoredPosition=new Vector2(32+(1036-width)*.5f,-212);}
            if(count>1){Button(box,"‹",32,514,80,56,()=>{boardFormulaPage--;ShowWhiteboard();}).interactable=boardFormulaPage>0;ui.Text(box,(boardFormulaPage+1)+" / "+count,122,522,856,42,23,VlabUi.Secondary,TextAlignmentOptions.Center);Button(box,"›",988,514,80,56,()=>{boardFormulaPage++;ShowWhiteboard();}).interactable=boardFormulaPage+1<count;}
            string knowledge=LearningGuide.Load(id)?.learnVi??"";
            string units=LessonSpec.All.TryGetValue(id,out var spec)?string.Join(" · ",spec.parameters.Where(p=>!string.IsNullOrEmpty(p.unit)).Select(p=>(Vi?p.vi:p.en)+" ("+p.unit+")")):"";
            float notesY=count>1?590:548;
            ui.Scroll(box,UiFormula.RichText((Vi?"":"Vietnamese source material\n")+knowledge+"\n\n"+L("ĐƠN VỊ TRONG BÀI\n","LESSON UNITS\n")+units),32,notesY,1036,760-notesY,25);
            Button(box,L("Phác thảo của tôi","My sketch"),32,792,496,64,ShowSketch).name="Open sketch page";
            Button(box,L("Trở lại bàn","Return to bench"),552,792,516,64,CloseModal).name="Lesson board close";
        }
        void ShowSketch()
        {
            var box=Modal(L("Bảng phác thảo","Sketch board"),1100,800);
            VlabWhiteboard.Render(ui,box,Session?.spec.id??selected?.id??"P01",Vi,ShowWhiteboard);
        }
    }
}
