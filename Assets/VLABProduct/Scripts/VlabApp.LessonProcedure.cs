using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        void ShowLessonProcedure()
        {
            if(selected==null)return;
            var box=Modal(L("Quy trình · ","Procedure · ")+LessonGuide.ShortTitle(selected.id,Vi),1120,800);
            ui.Scroll(box,UiFormula.RichText(LessonGuide.FullProcedure(selected.id,Vi,selected)),40,118,1040,570,26).name="Lesson procedure content";
            Button(box,L("Đã hiểu · Quay lại","Got it · Back"),40,706,1040,64,CloseModal,Accent).name="Lesson procedure close";
        }
    }
}
