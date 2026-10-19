using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class SafetyPresentation
    {
        public static void Instructions(VlabUi ui,RectTransform parent,bool vi)
        {
            string[] titles=vi?new[]{"Dọn khoảng trống","Cố định điện thoại và kính","Nghỉ khi khó chịu"}:new[]{"Clear your space","Secure your phone and viewer","Take a break"};
            string[] bodies=vi?new[]{"Dọn khoảng trống quanh bạn. Ngồi hoặc đứng ổn định.","Cố định điện thoại và kính. Chỉnh kính trước khi bắt đầu.","Dừng và nghỉ nếu chóng mặt hoặc khó chịu."}:new[]{"Clear the space around you. Sit or stand securely.","Secure your phone and viewer. Adjust the viewer before starting.","Stop and take a break if you feel dizzy or uncomfortable."};
            for(int i=0;i<3;i++)
            {
                var row=ui.Rect(parent,"Safety instruction "+i,40,125+i*120,780,110);
                var icon=ui.Rect(row,"Safety illustration",0,10,84,84);Draw(ui,icon,i);
                var title=ui.Text(row,(i+1)+" · "+titles[i],110,0,660,38,26);title.fontStyle=TMPro.FontStyles.Bold;
                ui.Text(row,bodies[i],110,43,660,72,23);
            }
        }
        static void Draw(VlabUi ui,RectTransform root,int kind)
        {
            Color ink=UiTheme.Readable(VlabUi.Hex("72AEFF"),VlabUi.PanelColor);
            void Line(float x,float y,float w,float h)=>ui.Rule(root,x,y,w,h,ink);
            void Ring(float x,float y,float size)
            {var image=ui.Rect(root,"Safety icon circle",x,y,size,size).gameObject.AddComponent<Image>();image.sprite=VlabUi.PointerRing();image.color=ink;image.raycastTarget=false;}
            if(kind==0)
            {
                foreach(float x in new[]{6f,65f}){Line(x,6,13,3);Line(x,75,13,3);}
                foreach(float y in new[]{6f,65f}){Line(6,y,3,13);Line(75,y,3,13);}
                Ring(32,20,20);Line(40,42,4,20);Line(25,48,34,3);Line(31,62,4,10);Line(49,62,4,10);
            }
            else if(kind==1)
            {
                Line(12,22,60,3);Line(12,61,60,3);Line(12,22,3,42);Line(69,22,3,42);
                Ring(23,35,16);Ring(45,35,16);Line(38,43,8,3);Line(3,33,9,3);Line(72,33,9,3);Line(3,33,3,22);Line(78,33,3,22);
            }
            else{Ring(6,6,72);Line(29,28,8,29);Line(47,28,8,29);}
        }
    }
}
