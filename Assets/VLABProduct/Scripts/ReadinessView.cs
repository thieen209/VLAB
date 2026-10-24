using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Student-facing preparation, using the supplied supporting-UI/browser visual hierarchy.
    public static class ReadinessView
    {
        static readonly Dictionary<string,string[]> goals=new Dictionary<string,string[]>
        {
            ["P01"]=new[]{"Đo chu kỳ theo chiều dài và ước lượng gia tốc trọng trường.","Compare period with length and estimate gravity."},
            ["P02"]=new[]{"So góc phóng với tầm xa, ghi cả độ cao nòng thực.","Compare launch angle with range, retaining actual muzzle height."},
            ["P03"]=new[]{"Đo ngưỡng trượt và lực ma sát khi kéo đều.","Measure breakaway force and steady sliding friction."},
            ["P04"]=new[]{"Đo thời gian che cổng để tính vận tốc và gia tốc.","Use gate-blocking times to calculate speed and acceleration."},
            ["P05"]=new[]{"So độ cứng lò xo từ độ giãn và chu kỳ dao động.","Compare spring stiffness from extension and oscillation period."},
            ["P06"]=new[]{"So động lượng có dấu và động năng trước, sau va chạm.","Compare signed momentum and energy before and after collision."},
            ["P07"]=new[]{"Đo gia tốc qua cổng, so với lực và tổng khối lượng.","Measure gate-derived acceleration against force and total mass."},
            ["P08"]=new[]{"So thế năng mất với động năng tại vùng cổng.","Compare potential energy lost with kinetic energy at the gate."},
            ["C01"]=new[]{"Ghi ba lượt chuẩn độ hợp lệ gần nhau để tính nồng độ acid.","Use three concordant valid titres to find acid concentration."},
            ["C02"]=new[]{"Đo điện áp pin, so nồng độ và dấu khi đảo đầu đo.","Compare cell voltage with concentration and probe polarity."},
            ["C03"]=new[]{"So khối lượng đồng bám với điện lượng đã truyền.","Compare deposited copper mass with delivered charge."},
            ["C04"]=new[]{"Nhận biết chloride qua kết tủa và hai mẫu đối chứng.","Identify chloride using precipitation and two negative controls."},
            ["C05"]=new[]{"Pha loãng mẫu 10 mL và kiểm tra bảo toàn chất tan.","Dilute a 10 mL aliquot and check conservation of solute."},
            ["C06"]=new[]{"Đo pH bằng đầu dò và so với khoảng màu BTB.","Measure probe pH and compare it with the BTB colour range."},
            ["C07"]=new[]{"Ghi thể tích khí theo thời gian để so tốc độ phản ứng.","Record gas volume over time to compare reaction rates."},
            ["C08"]=new[]{"Đo nhiệt độ và tính nhiệt trung hòa, kể cả nhiệt dung cốc.","Measure neutralization heat, accounting for cup heat capacity."},
            ["B01"]=new[]{"Tự nhận diện ít nhất ba cấu trúc trên tiêu bản sơ đồ.","Identify at least three structures in the schematic specimen."},
            ["B02"]=new[]{"Theo dõi cùng tế bào khi co và phản co nguyên sinh.","Track the same cells through plasmolysis and recovery."},
            ["B03"]=new[]{"Cân ba mẫu đúng mã để tính phần trăm đổi khối lượng.","Weigh three identified samples and calculate mass-change percent."},
            ["B04"]=new[]{"Ghi khí theo thời gian, so nhiệt độ mẫu và đối chứng.","Record gas over time; compare sample temperatures and controls."},
            ["B05"]=new[]{"Tự ghi ba mốc khí để đọc đường lên men có pha trễ.","Record three gas points and interpret fermentation with a lag."},
            ["B06"]=new[]{"Đánh dấu mặt trước dung môi và tính Rf của các băng.","Mark the solvent front and calculate band Rf values."},
            ["B07"]=new[]{"So kết tủa thô giữa quy trình chiết và đối chứng.","Compare crude precipitate from extraction and controls."},
            ["B08"]=new[]{"Phân loại 100 mã tế bào rồi tính chỉ số phân bào.","Classify 100 cell IDs and calculate the mitotic index."},
            ["M01"]=new[]{"Đo tỉ số tốc độ và chiều quay của bộ bánh răng.","Measure speed ratio and rotation direction in the gear train."},
            ["M02"]=new[]{"So mômen quanh điểm tựa, kể cả trọng lượng thanh.","Compare moments about the pivot, including beam weight."},
            ["M03"]=new[]{"So lực kéo với quãng kéo và độ nâng của ròng rọc.","Compare effort, rope travel and load rise for pulley routes."},
            ["M04"]=new[]{"Đo tỉ số tốc độ và chiều quay của hai kiểu đai.","Measure speed ratio and direction for open and crossed belts."},
            ["M05"]=new[]{"Ghi vị trí theo góc và đo hành trình con trượt.","Record position against angle and measure slider stroke."},
            ["M06"]=new[]{"Đọc độ nâng theo góc và so hai loại con đội.","Read lift against cam angle and compare the two followers."},
            ["M07"]=new[]{"Đọc kích thước và hiệu chỉnh sai lệch zero của dụng cụ.","Read dimensions and correct the instrument zero error."},
            ["M08"]=new[]{"Đo độ võng có tải và kiểm tra số trở về khi tháo tải.","Measure loaded deflection and verify return after unloading."}
        };
        static string L(bool vi,string a,string b)=>vi?a:b;
        public static string Goal(string id,bool vi)=>goals.TryGetValue(id,out var pair)?pair[vi?0:1]:L(vi,"Cùng khám phá thí nghiệm này.","Explore this experiment.");
        public static string Preparation(string id,bool vi)
        {var spec=LessonSpec.All[id];return vi?spec.operationsVi[0]:spec.operationsEn[0];}

        public static RectTransform Render(VlabUi ui,RectTransform parent,ExperimentBrief brief,bool vi,Color accent,bool resume,Action back,Action help,Action enter)
        {
            var box=ui.Panel(parent,"Readiness view",200,120,1200,760);
            float readyAt=Time.unscaledTime+.22f;
            Action<Action> activate=action=>{if(Time.unscaledTime>=readyAt)action?.Invoke();};
            ui.Button(box,L(vi,"Quay lại","Back"),32,26,190,64,()=>activate(back)).name="Readiness back";
            Logo(ui,box);
            ui.Rule(box,32,108,1136,1,VlabUi.Stroke);

            string room=brief.id.StartsWith("P",StringComparison.Ordinal)?"Physics":brief.id.StartsWith("C",StringComparison.Ordinal)?"Chemistry":brief.id.StartsWith("B",StringComparison.Ordinal)?"Biology":"Engineering";
            string roomLabel=room=="Physics"?L(vi,"Phòng Vật lí","Physics laboratory"):room=="Chemistry"?L(vi,"Phòng Hóa học","Chemistry laboratory"):room=="Biology"?L(vi,"Phòng Sinh học","Biology laboratory"):L(vi,"Phòng Kỹ thuật","Engineering laboratory");
            var photo=ui.Panel(box,"Readiness room photograph",32,138,540,276,VlabUi.Raised);
            photo.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var texture=Resources.Load<Texture2D>("UI/Lessons/"+brief.id);
            if(texture)
            {
                var image=ui.Picture(photo,texture,0,0,540,276);float source=(float)texture.width/texture.height,target=540f/276;
                if(source>target){float width=target/source;image.uvRect=new Rect((1-width)/2,0,width,1);}else{float height=source/target;image.uvRect=new Rect(0,(1-height)/2,1,height);}
            }
            var caption=ui.Panel(photo,"Room photo caption",0,224,540,52,VlabUi.PanelColor);caption.GetComponent<VlabPanelOpacity>().Solid=true;caption.GetComponent<VlabPanelOpacity>().Apply(ui.canvas.renderMode==RenderMode.WorldSpace);
            ui.Text(caption,roomLabel,20,12,500,34,22);

            ui.Text(box,L(vi,"TRƯỚC KHI BẮT ĐẦU","BEFORE YOU BEGIN"),616,142,552,35,20,accent);
            var title=ui.Text(box,LessonGuide.ShortTitle(brief.id,vi),616,192,552,108,40);title.name="Readiness title";
            var goal=ui.Text(box,Goal(brief.id,vi),616,300,552,100,24,VlabUi.Secondary);goal.name="Readiness goal";goal.enableAutoSizing=true;goal.fontSizeMax=goal.fontSize;goal.fontSizeMin=ui.LargeText?24:22;
            ui.Text(box,resume?L(vi,"Số liệu đã lưu vẫn ở trong sổ tay.\nBắt đầu lại từ phần chuẩn bị.","Your saved readings remain in the journal.\nBegin again with preparation."):
                L(vi,"Làm từng bước, theo nhịp của bạn.","Take each step at your own pace."),616,412,552,40,22,VlabUi.Secondary);

            Tile(ui,box,32,"1","Play",L(vi,"Chuẩn bị dụng cụ","Prepare your kit"),Preparation(brief.id,vi),accent);
            Tile(ui,box,412,"2","Teleport",L(vi,"Trỏ và chọn","Point and select"),L(vi,"Trỏ vào nút muốn chọn.\nChạm hoặc bấm nút chọn.","Point at a button.\nClick, tap or trigger."),accent);
            Tile(ui,box,792,"3","Pause",L(vi,"Tạm dừng khi cần","Pause when needed"),L(vi,"Quay lại để tạm dừng.\nCăn lại nếu bảng lệch.","Back opens pause.\nRecenter if needed."),accent);
            ui.Rule(box,32,640,1136,1,VlabUi.Stroke);
            ui.Text(box,L(vi,"Chưa rõ? Chúng mình có hướng dẫn.","Need a hand? Guidance is always available."),32,674,520,48,22,VlabUi.Secondary);
            ui.Button(box,L(vi,"Trợ giúp","Help"),616,662,230,70,()=>activate(help)).name="Readiness help";
            ui.Button(box,L(vi,"Vào trạm","Enter station"),872,662,296,70,()=>activate(enter),accent).name="Readiness enter";
            return box;
        }
        static void Tile(VlabUi ui,RectTransform parent,float x,string number,string iconName,string title,string body,Color accent)
        {
            var tile=ui.Panel(parent,"Readiness setup "+number,x,464,356,154,VlabUi.Raised);
            var icon=Resources.Load<Texture2D>("UI/Icons/"+iconName);
            if(icon){var image=ui.Picture(tile,icon,22,20,36,36);image.color=UiTheme.On(VlabUi.Raised);}
            else ui.Text(tile,number,24,19,34,40,28,accent,TextAlignmentOptions.Center);
            ui.Text(tile,title,74,20,260,40,24);
            ui.Text(tile,body,24,76,308,68,22,VlabUi.Secondary);
        }
        static void Logo(VlabUi ui,RectTransform parent)
        {
            var texture=Resources.Load<Texture2D>("UI/vlab-banner-original");if(!texture)return;
            var image=ui.Picture(parent,texture,948,22,220,70);
            var crop=Resources.Load<TextAsset>("UI/logo-crop");if(!crop)return;
            var values=crop.text.Split(',');if(values.Length!=4)return;
            image.uvRect=new Rect(float.Parse(values[0],CultureInfo.InvariantCulture),float.Parse(values[1],CultureInfo.InvariantCulture),float.Parse(values[2],CultureInfo.InvariantCulture),float.Parse(values[3],CultureInfo.InvariantCulture));
        }
        public static string[] ValidateMetadata()
        {
            var missing=new List<string>();foreach(var id in LessonSpec.All.Keys)if(!goals.TryGetValue(id,out var pair)||pair.Length!=2||string.IsNullOrWhiteSpace(pair[0])||string.IsNullOrWhiteSpace(pair[1]))missing.Add(id);return missing.ToArray();
        }
    }
}
