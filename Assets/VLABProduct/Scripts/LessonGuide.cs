using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class GuideTask
    {
        public string kind,key,control,action,success;
        public GuideTask(string kind,string key,string control,string action,string success){this.kind=kind;this.key=key;this.control=control;this.action=action;this.success=success;}
    }
    // Guidance observes real state. It never calls Prepare, Operate, Measure or classifies an image.
    public sealed class LessonGuide
    {
        sealed class Entry
        {
            public string vi,en,runVi,runEn;public string[] targets;
            public Entry(string vi,string en,string targets,string runVi,string runEn){this.vi=vi;this.en=en;this.targets=targets.Split(',');this.runVi=runVi;this.runEn=runEn;}
        }
        static readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>
        {
            ["P01"]=new Entry("Con lắc đơn","Pendulum","stand,pendulum,pendulum,pendulum","Đếm đủ chu kỳ cùng chiều qua mốc cân bằng.","Count full same-direction crossings at equilibrium."),
            ["P02"]=new Entry("Ném xiên","Projectile","target,launcher,ball","Quan sát lần chạm đầu; đọc dấu trên tấm hứng.","Observe first impact and the mark on the target."),
            ["P03"]=new Entry("Ma sát","Friction","meter,block,meter","Tăng lực kéo đến khi khối trượt, rồi giữ kéo đều.","Raise pull until the block slides, then maintain steady sliding."),
            ["P04"]=new Entry("Cổng quang","Photogates","track,flag,timer","Chờ bản chắn đi qua cả hai cổng.","Wait for the flag to pass both gates."),
            ["P05"]=new Entry("Lò xo","Spring","stand,spring,load","Quan sát dao động lò xo và các mốc thời gian.","Observe spring oscillations and timing samples."),
            ["P06"]=new Entry("Va chạm xe","Cart collision","track,cart,bumperA","Quan sát hai xe trước và sau va chạm.","Observe both carts before and after contact."),
            ["P07"]=new Entry("Định luật Newton","Newton's law","track,load,cart","Chờ xe qua hai cổng, tải treo chưa chạm đáy.","Wait for both gates before the hanger reaches its stop."),
            ["P08"]=new Entry("Cơ năng trên dốc","Slope energy","track,cart,cart","Quan sát vật trượt và số đo hai cổng.","Observe sliding motion and both gate readings."),
            ["C01"]=new Entry("Chuẩn độ giấm","Vinegar titration","burette,burette,flask,flask","Mở khóa ít, lắc bình, đóng khóa rồi đọc mặt khum.","Deliver slowly, swirl, close the valve and read the meniscus."),
            ["C02"]=new Entry("Pin Daniell","Daniell cell","zn,bridge,meter","Đọc điện áp; có thể đảo đầu đo để so sánh dấu.","Read voltage; optionally reverse probes to compare its sign."),
            ["C03"]=new Entry("Điện phân đồng","Copper electrolysis","balance,cathode,supply","Đủ thời gian: tắt nguồn, rửa, làm khô rồi cân cathode.","At target time: switch off, rinse, dry and weigh the cathode."),
            ["C04"]=new Entry("Nhận biết chloride","Chloride test","reagent,nitrate,nacl","So sánh NaCl với hai mẫu đối chứng.","Compare NaCl with both negative controls."),
            ["C05"]=new Entry("Pha loãng","Dilution","pipette,vol50,vol50","Đọc nồng độ mẫu có nhãn sau khi trộn.","Read the labelled sample concentration after mixing."),
            ["C06"]=new Entry("Đo pH","pH measurement","test,probe,paper","Đọc đầu dò ngập trong mẫu và màu giấy BTB riêng.","Read the immersed probe and the separate BTB colour."),
            ["C07"]=new Entry("Tốc độ tạo khí","Gas reaction rate","chips,tube,flask","Theo dõi thể tích khí; không mở cổng khi đang phản ứng.","Observe gas volume; keep the reaction ports sealed."),
            ["C08"]=new Entry("Nhiệt trung hòa","Neutralization heat","acidCup,thermometer,cal","Theo dõi nhiệt kế ít nhất 20 giây.","Observe the immersed thermometer for at least 20 seconds."),
            ["B01"]=new Entry("Biểu bì hành","Onion cells","onion,coverslip,microscope,microscope","Chọn hình rồi tự gọi tên ít nhất 3 cấu trúc.","Select image regions and identify at least three structures."),
            ["B02"]=new Entry("Co nguyên sinh","Plasmolysis","microscope,microscope,dropper","Theo dõi cùng 20–30 mã tế bào qua ba trạng thái.","Track the same 20–30 cell IDs through all three states."),
            ["B03"]=new Entry("Thẩm thấu khoai","Potato osmosis","balance,solution,sample0,paper","Đủ thời gian: thấm từng mẫu rồi cân cuối đúng mã.","After soaking, blot and weigh each identified sample."),
            ["B04"]=new Entry("Catalase","Catalase","aliquot,bath,tube","Chờ cân bằng nhiệt, trộn và tự ghi số đo khí.","Wait for thermal equilibrium, mix and manually read gas."),
            ["B05"]=new Entry("Lên men","Fermentation","yeast,bath,tube","Trộn khi mẫu đủ nhiệt, ghi ít nhất 3 mốc khí.","Mix at the target temperature and record at least three gas points."),
            ["B06"]=new Entry("Sắc ký lá","Leaf chromatography","mortar,filter,capillary,strip","Chờ ít nhất 30 giây rồi đánh dấu mặt trước dung môi.","Develop for at least 30 seconds, then mark the solvent front."),
            ["B07"]=new Entry("Tách DNA thô","Crude DNA","mortar,extract,filter,alcohol,receiver","Quan sát ít nhất 10 giây; có vật liệu thô mới cuốn nhẹ.","Observe for at least 10 seconds; spool only visible crude material."),
            ["B08"]=new Entry("Đếm kỳ nguyên phân","Mitosis count","microscope,microscope,specimen","Tự phân loại 100 mã toàn cục; ô chưa rõ chọn Chưa rõ.","Classify 100 global IDs; retain unclear cells as unclear."),
            ["M01"]=new Entry("Truyền bánh răng","Gears","board,gears,gears","Quan sát chiều quay và đọc tỉ số tốc độ.","Observe rotation direction and read the speed ratio."),
            ["M02"]=new Entry("Cân bằng đòn bẩy","Lever balance","fulcrum,lever,lever","Quan sát mômen hai phía quanh lỗ điểm tựa.","Observe moments about the actual pivot bore."),
            ["M03"]=new Entry("Ròng rọc","Pulleys","fixedA,cord,meter","Kéo đầu dây từng 10 mm tới độ nâng đã đặt.","Pull the free end in 10 mm steps to the selected lift."),
            ["M04"]=new Entry("Truyền đai","Belt drive","pulleys,belt,motor","Quan sát đai và đọc tốc độ trục ra.","Observe the belt and read output shaft speed."),
            ["M05"]=new Entry("Tay quay – con trượt","Slider–crank","crank,rod100,slider","Quan sát một chu kỳ quay và hành trình con trượt.","Observe a drive cycle and the slider travel."),
            ["M06"]=new Entry("Cam và con đội","Cam follower","cam,follower,gauge","Quan sát nâng, dừng và hạ; đọc đồng hồ so.","Observe rise, dwell and return on the dial gauge."),
            ["M07"]=new Entry("Thước kẹp và panme","Caliper / micrometer","micrometer,samples,micrometer","Đọc số chỉ, trừ sai lệch zero đã ghi.","Read the scale and subtract the recorded zero error."),
            ["M08"]=new Entry("Uốn dầm","Beam bending","supports,beam,gauge","Đọc độ võng có tải, tháo tải và kiểm tra hồi phục.","Read loaded deflection, then unload and check recovery.")
        };
        // Observable checks support independent judgement; they are not model answers or measurements.
        static readonly Dictionary<string,string[]> preparationChecks=new Dictionary<string,string[]>
        {
            ["P01"]=new[]{"Giá đứng chắc, kẹp gắn đúng điểm lắp ở đầu tay ngang.|Dây nối từ điểm treo tới quả; quả không vướng bàn.|Xác định vị trí cân bằng và cùng chiều qua mốc.|Quả ở góc thả đã chọn; đồng hồ chờ lần đầu qua vị trí cân bằng theo chiều đã chọn.","The stand is stable; the clamp uses the mounting point on its arm.|The string joins pivot and bob; the bob clears the bench.|Identify equilibrium and one crossing direction.|The bob is at the chosen release angle; timing waits for the first selected-direction equilibrium crossing."},
            ["P02"]=new[]{"Tấm hứng và thước ở vùng nhận viên bi.|Kiểm tra góc, tốc độ và độ cao nòng thực.|Viên bi nằm trong bộ phóng, chưa bay.","Target and ruler are in the receiving area.|Check angle, speed and actual muzzle height.|The ball is seated in the launcher, not yet flying."},
            ["P03"]=new[]{"Dây lực kế nối với khối theo phương ngang.|Ghi khối lượng và hai hệ số ma sát của lượt.|Lực kế trở về không trước khi kéo.","The meter cord pulls the block horizontally.|Retain mass and both declared friction coefficients.|The force meter is zero before pulling."},
            ["P04"]=new[]{"Xe nằm trên ray, đường đi qua hai cổng thông thoáng.|Bản chắn được gắn vào xe.|Hai cổng chưa bị chắn và thời gian cũ đã được xóa.","The cart sits on the track with a clear route through both gates.|The flag is attached to the cart.|Both gates are clear and old intervals have been reset."},
            ["P05"]=new[]{"Đầu lò xo gắn vào kẹp trên giá.|Đọc chiều dài khi chưa treo tải.|Tải đã treo; độ giãn và biên độ nằm trong hành trình cho phép.","The spring top is seated in the stand clamp.|Read the length without a hanging load.|The load is fitted; extension and amplitude fit the allowed travel."},
            ["P06"]=new[]{"Hai xe ở cùng ray và còn khoảng chạy trước khi chạm.|Đầu va chạm phù hợp kiểu đàn hồi hoặc dính đã chọn.|Ghi vận tốc có dấu theo cùng một chiều dương.","Both carts share the track and have travel before contact.|Attachments match the chosen elastic or sticking case.|Retain signed velocities in one common positive direction."},
            ["P07"]=new[]{"Xe trên ray, ròng rọc ở đầu ray.|Dây nối xe với tải treo, tải còn khoảng rơi.|Cả hai cổng nằm trước vùng tải chạm đáy.","The cart is on the track and pulley at its end.|The cord joins cart and hanger with available fall.|Both gates are reached before the hanger bottom stop."},
            ["P08"]=new[]{"Ray nằm đúng góc nghiêng đã chọn.|Đọc chênh cao thẳng đứng, không lấy chiều dài dốc thay thế.|Xe ở mốc thả; không thêm xung đẩy.","The track matches the chosen incline angle.|Read vertical height difference, not distance along the slope.|The glider is at the release mark without an extra push."},
            ["C01"]=new[]{"Burette đã có dung dịch bazơ, cốc thải nhận phần tráng.|Đầu khóa được đuổi bọt; đóng khóa trước khi đọc.|Số đọc đầu đã giữ; phần mẫu nằm trong bình chuẩn độ.|Bình có chỉ thị và ở dưới đầu burette.","The burette contains base and waste receives the rinse.|The tip is primed; close the valve before reading.|The initial reading is retained and aliquot is in the flask.|Indicator is present and the flask is below the burette outlet."},
            ["C02"]=new[]{"Zn và Cu ở đúng hai dung dịch tương ứng.|Hai đầu cầu muối ngập trong hai cốc.|Đầu đo nối đúng điện cực; giữ rõ quy ước dấu.","Zn and Cu are in their matching half-cell solutions.|Both salt-bridge ends are immersed.|Meter leads contact the electrodes; keep the sign convention clear."},
            ["C03"]=new[]{"Khối lượng đầu lấy từ điện cực khô trên cân.|Hai điện cực ngập dung dịch và không chạm nhau.|Dây nối đúng cực, nguồn tắt trước khi bắt đầu lượt.","Initial mass is read from the dry electrode on the balance.|Both electrodes are immersed without touching.|Leads have correct polarity; power is off before starting the trial."},
            ["C04"]=new[]{"Phân biệt rõ cốc NaCl, NaNO3 và nước.|Hai đối chứng đã nhận đúng liều AgNO3 theo đơn vị mô phỏng.|NaCl cũng được thử; so hiện tượng với cả hai đối chứng.","NaCl, NaNO3 and water cups are identified.|Both controls received their AgNO3 simulation-unit doses.|NaCl is also tested; compare its appearance with both controls."},
            ["C05"]=new[]{"Pipette được tráng bằng đúng dung dịch nguồn.|Phần mẫu nằm trong bình định mức đã chọn.|Đã tới vạch và đảo trộn; mẫu có nhãn riêng.","The pipette is rinsed with the intended stock.|The aliquot is in the selected volumetric flask.|The solution reaches the mark and is mixed with its own label."},
            ["C06"]=new[]{"Mẫu có nhãn và điều kiện pha loãng rõ ràng.|Đã đọc hai dung dịch đệm và rửa đầu dò giữa các lần.|Đầu dò ngập mẫu; giấy BTB đã được thử riêng.","The sample has a label and declared dilution conditions.|Both buffers were read and the probe rinsed between them.|The probe is immersed; BTB paper was tested separately."},
            ["C07"]=new[]{"Đã cân nhóm hạt và đong lượng acid của lượt.|Bình, nút và syringe nối thành đường khí kín.|Hạt tiếp xúc acid; bắt đầu theo dõi khí khi chạy lượt.","Chip batch and acid amount are prepared.|Flask, stopper and syringe form a sealed gas route.|Chips contact acid; monitor gas when the trial runs."},
            ["C08"]=new[]{"Thể tích hai phần không vượt sức chứa cốc.|Nhiệt độ đầu acid và base đã được đọc riêng.|Dung dịch trong cốc cách nhiệt, đầu dò được đặt đúng.","Combined volumes fit the calorimeter.|Initial acid and base temperatures were read separately.|The solutions are in the calorimeter with the probe positioned."},
            ["B01"]=new[]{"Lớp biểu bì được trải trên lam có nước.|Lamen đã hạ nghiêng và phần dư được thấm.|Lam nằm trong bàn kính và hai kẹp đóng.|Ở vật kính thấp, chỉnh đến khi cấu trúc trên hình đủ rõ.","An epidermis layer is spread on the wet slide.|The coverslip is lowered at an angle and excess blotted.|The slide is on the stage with both clips closed.|At low power, adjust until the illustrated structures are clear."},
            ["B02"]=new[]{"Lam đã gắn và thấy được hình ban đầu.|Cùng nhóm mã đã được phân loại ban đầu và giữ cố định.|Dung dịch đã thay; tiếp tục quan sát đúng nhóm mã cũ.","The slide is mounted and the baseline view is visible.|The same IDs have baseline classifications and are locked.|The solution is exchanged; retain the original tracked IDs."},
            ["B03"]=new[]{"Cả ba mã mẫu có số cân đầu riêng.|Phân biệt dung dịch thử và nước đối chứng.|Mẫu có nhãn nằm trong dung dịch; giữ thời điểm bắt đầu ngâm.|Cách thấm trước cân cuối đã thống nhất cho cả nhóm.","All three sample IDs have separate initial masses.|Test solution and water control are identified.|Labelled samples are immersed with soak start times retained.|One blotting protocol is agreed for all final weighings."},
            ["B04"]=new[]{"Giữ rõ điều kiện mẫu enzyme hoặc đối chứng của lượt.|Mẫu đặt trong bể; còn phải chờ mẫu đạt gần nhiệt độ bể.|Đường thu khí đã kín; trộn bằng thao tác riêng khi đủ nhiệt.","Retain the enzyme or control conditions for this trial.|The sample is in the bath; allow its temperature to approach the bath.|The gas route is sealed; mix separately once temperature is ready."},
            ["B05"]=new[]{"Giữ rõ mẫu nấm men và điều kiện đối chứng.|Ghi lượng đường, bể nhiệt và điều kiện mẫu.|Đường khí đã kín; chờ mẫu đủ nhiệt rồi tự chọn Trộn.","Retain yeast identity and control conditions.|Record sugar amount, bath and sample conditions.|The gas route is sealed; wait for temperature, then choose Mix."},
            ["B06"]=new[]{"Dịch chiết đã được chuẩn bị từ lá.|Giữ phần dịch qua giấy lọc.|Mẫu đã chấm lên vạch và khô trước khi chạy.|Vạch mẫu ở trên mặt dung môi.","Leaf extract is prepared.|Retain the liquid passing through the filter.|The origin spot is applied and dry before development.|The sample origin is above the solvent surface."},
            ["B07"]=new[]{"Mô quả đã được nghiền.|Đệm đã thêm vào mô nghiền theo điều kiện của lượt.|Giữ dịch lọc, không lấy phần bã làm mẫu chiết.|Alcohol thành lớp hoặc điều kiện bỏ alcohol đã được xác định.|Giữ rõ điều kiện mẫu/đối chứng trước khi quan sát kết tủa thô.","Fruit tissue is crushed.|Buffer is added according to the trial conditions.|Retain filtrate rather than tissue residue.|Alcohol is layered, or its omission control is declared.|Keep sample/control conditions clear before observing crude precipitate."},
            ["B08"]=new[]{"Tiêu bản đã gắn, hình đủ rõ để tự phân loại.|Trường nhìn có mã, nhận ra vùng ảnh trùng nhau.|Quy tắc đếm giữ mỗi mã một lần và cho phép Chưa rõ.","The slide is mounted with a view suitable for classification.|The field has IDs and overlapping crops can be recognized.|The counting rule retains each ID once and allows Unclear."},
            ["M01"]=new[]{"Trục và bộ bánh răng ngồi đúng vị trí lắp.|Xác định bánh vào, bánh ra và cặp răng được chọn.|Khoảng tâm phù hợp bộ răng, chưa chạy motor.","Shafts and gear train are seated at their mounts.|Identify input, output and the selected tooth pair.|The stock centre distance is consistent before motor operation."},
            ["M02"]=new[]{"Nhận đúng lỗ điểm tựa đang chọn.|Hai lực đặt ở các vạch, cánh tay đo từ điểm tựa.|Dùng mốc ngang để đọc mômen, kể cả trọng lượng thanh.","Identify the selected pivot bore.|Loads occupy indexed marks; arms are measured from the pivot.|Use the horizontal reference and include beam weight in moments."},
            ["M03"]=new[]{"Phân biệt cụm ròng rọc cố định và cụm chuyển động.|Đếm các nhánh dây thực sự đỡ tải.|Tải và lực kế đã nối; ghi độ nâng cần đạt.","Distinguish fixed pulleys from moving pulleys.|Count the strands actually supporting the moving load.|Load and meter are attached; retain the target lift."},
            ["M04"]=new[]{"Nhận đúng pulley chủ động và bị động.|Đai thuộc đúng bộ chiều dài và kiểu mở/chéo đã chọn.|Motor nối vào trục vào; giữ rõ tốc độ và điều kiện trượt.","Identify the input and output pulleys.|The belt matches its stock length and open/crossed route.|The motor drives the input; retain speed and declared slip."},
            ["M05"]=new[]{"Tay quay nối trục motor, dùng đúng lỗ bán kính.|Thanh truyền đang dùng đúng chiều dài đã chọn và nối hai chốt.|Con trượt theo ray; cơ cấu không báo lỗi căn chỉnh. Quan sát khi chạy.","The crank joins the motor shaft at the selected radius hole.|The selected rigid rod joins both pins.|The slider follows its guide without an alignment error. Observe it when running."},
            ["M06"]=new[]{"Cam nối với motor.|Con đội đang chọn tiếp xúc cam và theo dẫn hướng.|Giữ mốc góc và độ nâng ban đầu để so khi cam quay.","The cam is connected to the motor.|The selected follower contacts the cam along its guide.|Retain the initial angle and lift reference before rotation."},
            ["M07"]=new[]{"Hai mặt đo đóng; sai lệch zero được giữ riêng.|Đúng mã mẫu nằm giữa các mặt đo.|Tiếp xúc nhẹ đã có; đọc số thô rồi hiệu chỉnh zero.","The faces are closed and zero error retained separately.|The identified sample is between the measuring faces.|Gentle contact is obtained; read raw indication then correct zero."},
            ["M08"]=new[]{"Dầm tựa đúng hai gối.|Ghi nhịp và tiết diện của cấu hình đang dùng.|Đã có mốc không tải và số chỉ có tải để so sau khi tháo.","The beam rests on both supports.|Retain the current span and section.|Unloaded zero and loaded indication are available for the return check."}
        };
        readonly ExperimentSession session;readonly HashSet<string> applied=new HashSet<string>();readonly List<string> actions=new List<string>();
        public LessonGuide(ExperimentSession session){this.session=session;}
        internal bool NeedsFinalProbeRinse=>actions.LastIndexOf("rinseProbe")<actions.LastIndexOf("buffer7");
        public void Applied(string id)
        {if(id=="fresh"){applied.Clear();actions.Clear();}if(id=="resetPull"){applied.Remove("pull+");applied.Remove("steady");}if(id=="serial"){applied.Remove("waterMark");applied.Remove("invert");}applied.Add(id);actions.Add(id);}
        public void Variable(string key,double value){if(key=="flow"&&value>0){applied.Remove("swirl");applied.Remove("final");applied.Remove("titre");}}
        public static string ShortTitle(string id,bool vi)=>entries.TryGetValue(id,out var entry)?(vi?entry.vi:entry.en):id;
        public static string[] ValidateMetadata()=>LessonSpec.All.Where(pair=>!entries.ContainsKey(pair.Key)||entries[pair.Key].targets.Length!=pair.Value.operationsEn.Length||!preparationChecks.TryGetValue(pair.Key,out var checks)||checks.Length!=2||checks.Any(text=>text.Split('|').Length!=pair.Value.operationsEn.Length)).Select(pair=>pair.Key).ToArray();
        public string Explanation(GuideTask task,bool vi)
        {
            if(task==null)return "";
            var authored=LearningGuide.Load(session.spec.id);
            if(vi&&!string.IsNullOrWhiteSpace(authored?.practiceVi))return authored.practiceVi;
            return session.spec.goalEn;
        }
        public string Next(ApparatusRig rig,GuideTask task,bool vi,int preparationReference=-1)
        {
            if(task==null)return "";
            var entry=entries[session.spec.id];var operations=vi?session.spec.operationsVi:session.spec.operationsEn;
            string observation=vi?entry.runVi:entry.runEn;
            if(preparationReference>=0)
            {
                int next=preparationReference+1;
                return next<operations.Length?L(vi,"Sau khi bước tham khảo này hoàn thành: ","After this reference step is completed: ")+operations[next]
                    :L(vi,"Sau khi chuẩn bị đủ, tự bắt đầu lượt và quan sát: ","After preparation, start the trial yourself and observe: ")+observation;
            }
            if(task.kind=="pause"||session.Phase==ExperimentPhase.Invalid)return task.action;
            if(task.kind=="resume")return L(vi,"Khi bạn chọn Tiếp tục: ","When you choose Resume: ")+observation;
            if(session.Phase==ExperimentPhase.Measured)return L(vi,"Đọc lượt vừa ghi trong Sổ tay trước khi quyết định làm lượt tiếp theo.","Review the recorded trial in Notebook before choosing another trial.");
            if(task.kind=="record")return L(vi,"Sau khi bạn chọn Ghi số đo: ","After you choose Record measurement: ")+task.success;
            if(task.kind=="install")
            {
                var next=rig?.items.FirstOrDefault(item=>!item.installed&&item.assetId!=task.key&&(!rig.InstallSupport(item)||rig.InstallSupport(item).installed||rig.InstallSupport(item).assetId==task.key));
                if(next)return L(vi,"Sau khi lắp món hiện tại, chọn: ","After fitting the current item, select: ")+rig.EquipmentLabel(next,vi);
                return L(vi,"Khi mọi dụng cụ đã lắp đúng, bắt đầu chuẩn bị: ","Once all equipment is fitted, begin preparation: ")+operations[0];
            }
            if(session.Phase==ExperimentPhase.Preparing)
            {
                int current=Mathf.Clamp(session.Operation,0,operations.Length-1),next=current+1;
                string prerequisite=task.kind=="prepare"?"":L(vi,"Hoàn thành thao tác dụng cụ rồi kiểm tra bước ","Complete the instrument action, then check step ")+(current+1)+": "+operations[current]+". ";
                return prerequisite+(next<operations.Length?L(vi,"Khi bước này được xác nhận, tiếp theo: ","Once this step is confirmed, next: ")+operations[next]
                    :L(vi,"Khi chuẩn bị đủ, tự bắt đầu lượt và quan sát: ","Once preparation is complete, start the trial yourself and observe: ")+observation);
            }
            if(task.kind=="start")return L(vi,"Sau khi bắt đầu: ","After starting: ")+observation;
            if(task.kind=="image")return task.success;
            return observation+" "+L(vi,"Chỉ chọn Ghi số đo khi điều kiện quan sát của lượt đã đủ.","Choose Record measurement only when this trial's observation conditions are met.");
        }
        string L(bool vi,string a,string b)=>vi?a:b;
        GuideTask Task(bool vi,string kind,string key,string control,string actionVi,string actionEn,string successVi,string successEn)=>new GuideTask(kind,key,control,L(vi,actionVi,actionEn),L(vi,successVi,successEn));
        GuideTask Control(ApparatusRig rig,bool vi,string key,string id,string successVi="Nút thao tác báo đã thực hiện; trạng thái dụng cụ thay đổi.",string successEn="The control succeeds and the instrument state changes.")
        {
            var control=rig.Driver?.Controls(key).FirstOrDefault(c=>c.id==id||c.id.StartsWith(id,StringComparison.Ordinal));
            var item=rig.items.FirstOrDefault(x=>x.assetId==key);string name=item?(rig.EquipmentLabel(item,vi)??key):key;
            string label=control==null?L(vi,"chọn phần quan sát","select the observation"):vi?control.vi:control.en;
            return new GuideTask("instrument",key,control?.id??id,name+" → "+label,L(vi,successVi,successEn));
        }
        bool Enabled(ApparatusRig rig,string key,string id)=>rig.Driver?.Controls(key).Any(c=>c.id==id&&c.enabled)==true;
        bool Seen(string id)=>applied.Contains(id);
        string Target(ApparatusRig rig,Entry entry,int index)
        {
            index=Mathf.Clamp(index,0,entry.targets.Length-1);
            string key=entry.targets[index];
            // Guide the fitted variant: the other stock rods/follower remain inactive.
            if(session.spec.id=="M05"&&key=="rod100")key="rod"+(int)session.parameters["rod"];
            if(session.spec.id=="M06"&&key=="follower"&&rig.items.Any(x=>x.assetId=="roller"&&x.gameObject.activeInHierarchy))key="roller";
            if(session.spec.id=="P06"&&key=="bumperA"&&session.parameters["elastic"]<.5)key="latch";
            if(session.spec.id=="C05"&&key=="vol50"&&session.parameters["final"]>=75)key="vol100";
            if(session.spec.id=="M07"&&key=="micrometer"&&session.parameters["micrometer"]<.5)key="caliper";
            if(session.spec.id=="P08"&&index==1)key="ruler";
            if(!rig.items.Any(x=>x.assetId==key))key=rig.items.FirstOrDefault()?.assetId??"";
            return key;
        }
        public GuideTask Preparation(ApparatusRig rig,bool vi,int operation)
        {
            var entry=entries[session.spec.id];int index=Mathf.Clamp(operation,0,entry.targets.Length-1);string key=Target(rig,entry,index);
            string successVi="Dụng cụ xác nhận bước chuẩn bị; kiểm tra lại bố trí trước khi tiếp tục.",successEn="The instrument validates this step; inspect the setup before continuing.";
            if(preparationChecks.TryGetValue(session.spec.id,out var checks)){var viChecks=checks[0].Split('|');var enChecks=checks[1].Split('|');if(index<viChecks.Length&&index<enChecks.Length){successVi=viChecks[index];successEn=enChecks[index];}}
            return Task(vi,"prepare",key,"",session.spec.operationsVi[index],session.spec.operationsEn[index],successVi,successEn);
        }
        public GuideTask Current(ApparatusRig rig,bool vi,bool suspendedRunning=false)
        {
            string id=session.spec.id;var entry=entries[id];
            if(!rig.Ready)
            {
                var item=rig.Selected&&!rig.Selected.installed?rig.Selected:rig.NextInstallableItem();
                var support=rig.InstallSupport(item);if(support&&!support.installed)item=support;
                string name=item?(rig.EquipmentLabel(item,vi)??item.assetId):"";
                return Task(vi,"install",item?.assetId??"","",name+": chọn dụng cụ rồi tự đặt vào vị trí lắp. Có thể trả lại khay khi chưa muốn lắp.",name+": select the instrument, then place it at its installation anchor. Return it to the tray if needed.","Tên món hiện Đã lắp; kiểm tra chỗ tiếp xúc và bố trí trước khi chuẩn bị. Chưa có số đo được ghi.","Its label changes to Fitted. Inspect its contact and arrangement before preparation. No measurement is recorded.");
            }
            if(session.Phase==ExperimentPhase.Invalid)return Task(vi,"pause","","","Lượt này chưa hợp lệ. Mở Tạm dừng → Làm lại lượt.","This trial is invalid. Open Pause → Restart attempt.","Bắt đầu lại ở chuẩn bị; các lượt đã ghi vẫn còn.","Preparation restarts; existing notebook entries are retained.");
            if(session.Phase==ExperimentPhase.Paused&&!suspendedRunning)return Task(vi,"resume","","","Đang tạm dừng. Nhấn Tiếp tục khi đã sẵn sàng.","Paused. Select Resume when ready.","Đồng hồ và thí nghiệm chạy tiếp.","The clock and experiment continue.");
            int op=session.Operation;
            if(session.Phase==ExperimentPhase.Preparing)
            {
                if(id=="C01"&&op==2&&!Seen("initial"))return Control(rig,vi,"burette","initial","Mặt khum đầu được giữ cho lượt này; sau đó quay lại Thực hiện bước này.","The initial meniscus is retained; then return to Perform current action.");
                if(id=="C05"&&op==2){string key=session.parameters["final"]<75?"vol50":"vol100";if(Enabled(rig,key,"waterMark"))return Control(rig,vi,key,"waterMark");if(Enabled(rig,key,"invert"))return Control(rig,vi,key,"invert","Mẫu có nhãn sau khi trộn; quay lại Thực hiện bước này.","A mixed labelled sample is retained; return to Perform current action.");}
                if(id=="C06"&&op==1)
                {
                    if(!Seen("buffer4"))return Control(rig,vi,"probe","buffer4");
                    int count=actions.Count(x=>x=="rinseProbe");if(count<1)return Control(rig,vi,"probe","rinseProbe");
                    if(!Seen("buffer7"))return Control(rig,vi,"probe","buffer7");if(NeedsFinalProbeRinse)return Control(rig,vi,"probe","rinseProbe");
                }
                if(id=="C06"&&op==2){if(!Seen("probeSample"))return Control(rig,vi,"probe","probeSample");if(!Seen("paper"))return Control(rig,vi,"paper","paper");}
                if(id=="C08"&&op==1){if(!Seen("acidT"))return Control(rig,vi,"thermometer","acidT");if(!Seen("baseT"))return Control(rig,vi,"thermometer","baseT");}
                if(id=="C04"&&(op==1||op==2))
                {string target=op==2?"nacl":!Seen("dose:nitrate")?"nitrate":"water";if(!Seen("dose:"+target)){if(actions.LastOrDefault()!="selectAg")return Control(rig,vi,"reagent","selectAg");return Control(rig,vi,target,"dose:"+target);}}
                var biology=Biology(rig);
                if(id=="B01"&&op==3&&biology!=null&&Math.Abs(biology.focus-6.5)>.7)return Control(rig,vi,"microscope",biology.focus>6.5?"focus-":"focus+");
                if(id=="B02"&&op==1&&biology!=null&&biology.cells.Count(x=>x.baseline>=0)<20)return CellTask(rig,vi,biology,0);
                if(id=="B03"&&op==0&&biology!=null){int index=biology.samples.FindIndex(x=>!x.initialRead);if(biology.samples.Count==0)index=0;if(index>=0)return Control(rig,vi,"balance","weighInitial:"+index,"Khối lượng đầu của đúng mã mẫu được giữ. Làm đủ ba mẫu.","The initial mass is retained for that sample ID. Repeat for all three.");}
                if(id=="B06"&&op==2){if(!Seen("loadCapillary"))return Control(rig,vi,"capillary","loadCapillary");if(!Seen("deposit"))return Control(rig,vi,"strip","deposit");if(!Seen("dry"))return Control(rig,vi,"strip","dry");}
                return Preparation(rig,vi,op);
            }
            if(id=="P02"&&session.Phase==ExperimentPhase.Ready)return Task(vi,"start","target","","Xem tấm hứng dưới sàn, rồi nhấn Bắt đầu lượt đo.","Locate the floor mat, then select Start trial.","Viên bi bay về phía tấm hứng; chưa tự ghi kết quả.","The ball flies toward the mat; no result is saved automatically.");
            if(id=="P02"&&session.Authority?.CanRecord(out _)==true)return Task(vi,"record","target","","Nhìn dấu trên tấm hứng dưới sàn, rồi chọn Ghi số đo.","Inspect the mark on the floor mat, then Record measurement.","Lượt đo xuất hiện trong Sổ tay.","The trial appears in Notebook.");
            if(session.Phase==ExperimentPhase.Ready)return Task(vi,"start",Target(rig,entry,entry.targets.Length-1),"",id=="P01"?"Giữ quả nặng, kéo lệch nhẹ rồi thả tay để con lắc dao động.":"Nhấn Bắt đầu lượt đo. Sau khi quan sát đủ, chọn Ghi số đo để lưu.",id=="P01"?"Hold the bob, pull it gently to one side, then release it.":"Select Start trial. After completing the observations, select Record measurement to save.","Trạng thái chuyển sang Đang quan sát, đồng hồ chạy.","The phase becomes Observing and its clock advances.");
            if(session.Authority?.CanRecord(out _)==true||(session.Pendulum!=null&&session.Pendulum.Cycles>=session.parameters["cycles"]))return Task(vi,"record","","","Nhấn Ghi số đo để lưu lượt thực hành.","Select Record measurement to save this trial.","Màn hình kết quả mở và lượt xuất hiện trong Sổ tay.","Results opens and the attempt appears in Notebook.");
            var s=Biology(rig);
            if(s!=null)
            {
                if(id=="B01")return CellTask(rig,vi,s,-1);
                if(id=="B08")return CellTask(rig,vi,s,3);
                if(id=="B02")
                {
                    if(s.clock-s.saltAt<session.parameters["duration"])return Wait(vi,"microscope","Chờ đủ thời gian ngâm muối; vẫn theo dõi các mã cũ.","Wait for salt exposure; retain the original cell IDs.");
                    if(s.cells.Any(x=>x.salt<0))return CellTask(rig,vi,s,1);
                    if(!s.waterAdded)return Control(rig,vi,"microscope","waterReplace");
                    if(s.clock-s.waterAt<10)return Wait(vi,"microscope","Chờ ít nhất 10 giây sau khi thay nước.","Wait at least ten seconds after replacing water.");
                    return CellTask(rig,vi,s,2);
                }
                if(id=="B03")for(int i=0;i<s.samples.Count;i++)if(!s.samples[i].finalRead){if(s.clock-s.samples[i].soakStart<session.parameters["duration"])return Wait(vi,"timer","Đang ngâm mẫu · còn "+Math.Ceiling(session.parameters["duration"]-(s.clock-s.samples[i].soakStart))+" giây. Chưa lấy mẫu để cân cuối.","Soaking · "+Math.Ceiling(session.parameters["duration"]-(s.clock-s.samples[i].soakStart))+" seconds remaining before final weighing.");return Control(rig,vi,"balance",(s.samples[i].blotted?"weighFinal:":"blot:")+i);}
                if(id=="B04"||id=="B05")
                {if(!s.reacting)return Enabled(rig,"flask","mix")?Control(rig,vi,"flask","mix"):Wait(vi,"bath","Chờ nhiệt độ mẫu gần nhiệt độ bể; chưa trộn vội.","Wait for the sample to reach the bath temperature before mixing.");if(s.gas.Count>0&&s.clock-s.gas.Last().seconds<10)return Wait(vi,"syringe","Đợi khoảng 10 giây rồi tự ghi mốc khí tiếp theo.","Wait about ten seconds before manually reading the next gas point.");if(s.gas.Count<3||s.gas.Last().seconds-s.gas.First().seconds<20)return Control(rig,vi,"syringe","gasRead","Mốc khí mới xuất hiện. Ghi ≥3 mốc trải ít nhất 20 giây; hai lần cách ≥1 giây.","A new gas point appears. Collect ≥3 points spanning 20 s, separated by ≥1 s.");}
                if(id=="B06"&&!s.frontMarked)return s.clock>=30?Control(rig,vi,"strip","markFront"):Wait(vi,"strip",entry.runVi,entry.runEn);
                if(id=="B07"&&!s.spooled&&Enabled(rig,"rod","spool"))return Control(rig,vi,"rod","spool");
            }
            if(id=="P02")return Wait(vi,"target","Quay về phía sàn theo hướng bộ phóng; quan sát dấu trên tấm hứng.","Look toward the floor along the launch direction; observe the target mark.");
            if(id=="P03")
            {
                if(Enabled(rig,"meter","resetPull"))return Control(rig,vi,"meter","resetPull","Khối đã tới cuối mặt trượt. Đưa về đầu để đo lại; khi vừa bắt đầu trượt, chọn Kéo chậm và đều ngay trong bảng lực kế.","The block reached the tile end. Return it to repeat; select Pull slowly and steadily in the meter panel as soon as sliding starts.");
                return Control(rig,vi,"meter",Enabled(rig,"meter","steady")?"steady":"pull+","Giữ bảng lực kế mở. Tăng lực từng bước; khi khối bắt đầu trượt, chọn Kéo chậm và đều, rồi quan sát ít nhất 0,5 giây.","Keep the meter panel open. Raise pull step by step; when sliding starts, select Pull slowly and steadily, then observe at least 0.5 seconds.");
            }
            if(id=="M03")return Control(rig,vi,"meter","pull-rope");
            if(id=="M08"&&Enabled(rig,"gauge","unload"))return Control(rig,vi,"gauge","unload");
            if(id=="C03"&&session.Elapsed>=session.parameters["time"]){if(!Seen("off"))return Control(rig,vi,"supply","off");if(!Seen("rinseCathode"))return Control(rig,vi,"cathode","rinseCathode");if(!Seen("dryCathode"))return Control(rig,vi,"cathode","dryCathode");return Control(rig,vi,"balance","weighFinal");}
            if(id=="C01")
            {
                if(Seen("titre"))return Control(rig,vi,"burette","fresh");
                if(Enabled(rig,"burette","initial")&&!Seen("initial"))return Control(rig,vi,"burette","initial");
                if(Enabled(rig,"flask","aliquot"))return Control(rig,vi,"flask","aliquot");
                if(Enabled(rig,"flask","indicator"))return Control(rig,vi,"flask","indicator");
                if(session.parameters["flow"]>0)return Control(rig,vi,"burette","close");
                if(!Seen("swirl"))return Control(rig,vi,"flask","swirl");
                string note=session.Observe().note;
                if(note.StartsWith("Pale pink:",StringComparison.Ordinal))return Wait(vi,"flask","Giữ khóa đóng, quan sát màu hồng nhạt bền 30 giây.","Keep the valve closed; observe pale pink persistence for 30 seconds.");
                if(note.StartsWith("Persistent",StringComparison.Ordinal)){if(!Seen("final"))return Control(rig,vi,"burette","final");return Control(rig,vi,"burette","titre");}
                if(note.StartsWith("Deep pink",StringComparison.Ordinal))return Control(rig,vi,"burette","titre","Lượt quá điểm cuối được giữ là bị loại; chọn Mẫu mới để làm lại.","The overshot titre is retained as rejected; choose Fresh aliquot.");
                return Task(vi,"variables","burette","","Mở Thông số, tăng Độ mở khóa ít; lắc bình, đóng khóa để đọc lại.","Open Variables, increase Valve opening slightly; swirl and close before rereading.","Chỉ Giữ lượt chuẩn độ khi đã tới điểm cuối; cần ba lượt hợp lệ gần nhau.","Retain titre only at the endpoint; three concordant valid titres are required.");
            }
            return Wait(vi,Target(rig,entry,entry.targets.Length-1),entry.runVi,entry.runEn);
        }
        BiologyLessonState Biology(ApparatusRig rig)
        {if(!(rig.Driver is BiologyDriver))return null;try{return JsonUtility.FromJson<BiologyLessonState>(rig.Driver.SaveState());}catch(ArgumentException){return null;}}
        GuideTask CellTask(ApparatusRig rig,bool vi,BiologyLessonState s,int phase)
        {
            bool needsChoice=phase==-1?s.selectedCell>0&&!s.structures.Any(x=>x.cell==s.selectedCell&&x.region==s.selectedStructure):phase==3?s.selectedCell>0&&!s.cells.Any(x=>x.id==s.selectedCell):s.selectedCell>0&&s.cells.Any(x=>x.id==s.selectedCell&&(phase==0?x.baseline:phase==1?x.salt:x.recovery)<0);
            if(needsChoice)return Task(vi,"instrument","microscope",phase==-1?"structure:":phase==3?"stage:":"cell",phase<0?"Chọn tên cấu trúc vừa quan sát, như Thành tế bào hoặc Nhân.":phase==3?"Chọn nút tên kỳ vừa quan sát; chưa chắc thì chọn Chưa rõ.":"Chọn Chưa co hoặc Đã co cho mã ô đang được đánh dấu.",phase<0?"Choose the observed structure name, such as Cell wall or Nucleus.":phase==3?"Choose the observed stage button; select Unclear when uncertain.":"Choose Not plasmolysed or Plasmolysed for the marked cell ID.","Số ô/cấu trúc đã phân loại tăng; cùng mã không bị đếm thêm lần nữa.","The classified count advances; repeating a global ID edits it without duplicating it.");
            return Task(vi,"image","microscope","","Chọn một ô chưa phân loại trên hình. Mã toàn cục giữ nguyên khi đổi nửa ảnh.","Select an unclassified cell in the image. Its global ID stays the same across crops.","Mã ô đã chọn hiện ra; tiếp theo bạn tự chọn nhãn, ứng dụng không đoán hộ.","The selected ID is shown; you choose its label, without automatic classification.");
        }
        GuideTask Wait(bool vi,string key,string a,string b)=>Task(vi,"observe",key,"",a,b,"Chỉ Ghi số đo sáng khi trạng thái dụng cụ và quan sát đã đủ điều kiện.","Record measurement enables only when apparatus and observation checks pass.");
        public static string ExplainAction(string raw,bool okay,bool vi)
        {
            if(!okay)return ExplainFailure(raw,vi);
            if(string.IsNullOrWhiteSpace(raw))return vi?"Đã thực hiện.":"Action applied.";
            if(!vi)return raw;
            switch(raw.Trim())
            {
                case "Structure matches source schematic.":return "Cấu trúc bạn chọn khớp sơ đồ tham chiếu.";
                case "Reconsider its position and boundary in this schematic; this is not a microscopy-resolution claim.":return "Chưa khớp sơ đồ. Xem lại vị trí và đường bao của cấu trúc rồi chọn lại. Đây là hình minh họa, không phải ảnh đo độ phân giải kính hiển vi.";
                case "Your classification matches source atlas.":return "Phân loại của bạn khớp atlas tham chiếu.";
                case "Reconsider chromosome arrangement. Your answer is retained until you edit it.":return "Xem lại cách sắp xếp nhiễm sắc thể rồi cân nhắc chọn lại. Câu trả lời hiện tại vẫn được giữ cho đến khi bạn sửa.";
                case "Source cell is unscorable; retain unclear and state why.":return "Ô này không đủ căn cứ để chấm theo atlas. Chọn Chưa rõ và ghi lý do quan sát của bạn.";
                case "Water control: pH 7.00 at 25 °C. Rinse before changing samples.":return "Mẫu nước đối chứng: pH 7,00 ở 25 °C. Rửa đầu dò trước khi chuyển sang mẫu khác.";
                default:return raw;
            }
        }
        public static string ExplainFailure(string raw,bool vi)
        {
            if(!vi)return raw+" Check the setup and required conditions; open Guidance if needed.";
            if(!string.IsNullOrEmpty(raw)&&raw.Any(c=>c>127&&char.IsLetter(c)))return raw;
            switch((raw??"").Trim())
            {
                case "Select actual source structure first.":return "Chạm vào cấu trúc cần nhận diện trên hình trước, rồi chọn tên cấu trúc.";
                case "Select tracked source cell.":return "Chạm chọn một tế bào trong nhóm đang theo dõi rồi phân loại trạng thái của chính mã ô đó.";
                case "Select actual source cell.":return "Chạm chọn một tế bào trên atlas trước, rồi chọn nhãn kỳ phân bào.";
                case "Classify cell first.":return "Chọn nhãn kỳ phân bào cho ô đang chọn trước khi kiểm tra. Nếu chưa rõ, giữ Chưa rõ và tiếp tục quan sát.";
                case "Focus mounted specimen first.":return "Lắp lam lên bàn kính và chỉnh ảnh đủ nét trước khi chọn tế bào trên hình.";
                case "Mount and focus slide with both clips closed.":return "Lắp lam lên bàn kính, đóng cả hai ngàm giữ và lấy nét ảnh rồi bắt đầu.";
                case "Focus at actual 4× objective first using microscope controls.":return "Lấy nét ở vật kính nhỏ trước khi tăng độ phóng đại.";
                case "Focus at the 10× objective before changing to 40×.":return "Chọn vật kính 10×, lấy nét rõ rồi mới chuyển sang 40×.";
                case "Complete focused observation at 10× before changing to 40×.":return "Hoàn thành quan sát rõ nét ở vật kính 10× trước khi chuyển sang 40×.";
                case "At 40× use the fine-focus knob; coarse movement can hit the slide.":return "Ở vật kính 40×, chỉ dùng núm chỉnh tinh; chỉnh thô có thể làm vật kính chạm lam.";
                case "Place the slide on the microscope stage before closing the clips.":return "Đặt lam lên bàn kính trước, rồi chạm kẹp để giữ lam.";
                case "Mount, secure and focus the slide before recording.":return "Đặt lam đúng vị trí, đóng kẹp và lấy nét rõ trước khi ghi nhận.";
                case "Place the slide on the stage, then close the clips on the microscope.":return "Đưa lam kính lên giữa bàn kính, sau đó chạm kẹp để giữ lam.";
                case "Move the slide onto the centre of the microscope stage.":return "Kéo lam kính vào giữa bàn kính, đúng vị trí được đánh dấu.";
                case "Place the complete item over a clear bench surface.":return "Đưa toàn bộ dụng cụ lên phần bàn trống rồi bấm Đặt xuống. Bấm × để trả về chỗ cũ.";
                case "Keep the vessel upright before setting it down.":return "Giữ bình thẳng đứng trước khi đặt xuống bàn.";
                case "Find and focus mounted specimen at low power first.":return "Tìm và lấy nét mẫu đã lắp ở vật kính nhỏ trước khi chuyển sang vật kính lớn hơn.";
                case "Focus travel rejected.":return "Kính chưa nhận được vị trí lấy nét này. Kiểm tra kính đã lắp và đang hoạt động, rồi chỉnh lại trong hành trình cho phép.";
                case "Objective change rejected.":return "Chưa đổi được vật kính. Kiểm tra lam đã lắp, vị trí lấy nét và giới hạn cơ cấu rồi thử lại.";
                case "Install apparatus first.":return "Lắp đủ dụng cụ ở trạm trước khi dùng nút thao tác của bài.";
                case "Apparatus is disconnected.":return "Bộ dụng cụ chưa được lắp đầy đủ hoặc kết nối chưa còn nguyên. Kiểm tra lại bố trí trước khi ghi số đo.";
                case "Switch off before removing the cathode.":return "Chờ điện phân tạo lớp bám rồi tắt nguồn trước khi tháo cathode để rửa và cân.";
                case "Start the prepared experiment before switching on the valid circuit.":return "Hoàn tất chuẩn bị, bắt đầu lượt và kiểm tra mạch đúng trước khi bật nguồn. Lượt đã cân cuối không được bật điện lại.";
                case "Gas port opened after contact: collected gas volume is invalid.":return "Cổng khí đã mở sau khi hai chất tiếp xúc nên thể tích khí thu được không còn hợp lệ. Làm lại lượt với đường khí kín trước khi cho phản ứng.";
                case "Run prepared experiment before recording.":return "Hoàn tất chuẩn bị và bắt đầu thí nghiệm trước khi ghi số đo.";
                case "Point inside a cell structure.":return "Chạm vào bên trong cấu trúc tế bào trên hình; tránh nền trống và đường phân cách.";
                case "Point inside a source atlas cell.":return "Chạm vào bên trong một tế bào của atlas, tránh khoảng trống giữa các ô.";
                case "Wait for sample temperature to approach bath setting.":return "Chờ nhiệt độ mẫu gần nhiệt độ bể, với độ chênh dưới 0,5 °C, rồi mới trộn để bắt đầu phản ứng.";
            }
            string text=(raw??"").ToLowerInvariant();
            if(text.Contains("select a loose instrument first"))return "Chọn một dụng cụ chưa lắp trong danh sách hoặc trên bàn trước.";
            if(text.Contains("already fitted"))return "Dụng cụ đã được lắp. Chọn Quan sát hoặc Thao tác để sử dụng; không cần lấy lại.";
            if(text.Contains("only during setup"))return "Chỉ lắp dụng cụ trong giai đoạn chuẩn bị. Dừng lượt đang chạy trước khi đổi bố trí.";
            if(text.Contains("fit supporting instrument first"))return "Lắp dụng cụ đỡ trước, rồi chọn lại món cần gắn lên giá. Các món đã lắp vẫn được giữ.";
            if(text.Contains("this part is stored"))return "Món này còn trong khay dự trữ. Chọn Đặt vào vị trí lắp trong danh sách dụng cụ.";
            if(text.Contains("predicted shot leaves the supported room area"))return "Quỹ đạo vượt vùng đo hoặc chạm vật cản. Đổi vận tốc hoặc góc phóng rồi thử lại; thông số của bạn vẫn được giữ.";
            if(text.Contains("delivered configurable beam"))return "Cấu hình vượt giới hạn dầm. Giữ nhịp tối đa 30 cm; giảm tải hoặc giảm nhịp để độ võng không quá 1% nhịp, rồi thử lại. Số đo đã lưu vẫn được giữ.";
            if((text.Contains("receiver")||text.Contains("receiving"))&&text.Contains("capacity"))return "Bình hứng chưa đủ chỗ. Chuẩn bị lượt mới với bình hứng trống rồi thử lọc lại. Lần bị từ chối chưa chuyển dung dịch; số đo đã lưu vẫn được giữ.";
            if(text.Contains("funnel")&&text.Contains("outlet"))return "Đầu phễu phải hướng xuống giữa bình hứng và cao hơn mặt dung dịch. Lắp lại phễu rồi thử lọc; số đo đã lưu vẫn được giữ.";
            if(text.Contains("holder")||text.Contains("jaws")||text.Contains("fixture")||(text.Contains("support")&&text.Contains("rim")))return "Kẹp hoặc giá đỡ chưa tiếp xúc đúng. Đặt lại dụng cụ về vị trí lắp theo hướng dẫn, kiểm tra điểm tựa rồi thử lại. Số đo đã lưu vẫn được giữ.";
            if(text.Contains("initial meniscus"))return "Chưa giữ số đọc đầu. Mở burette → Đọc mặt khum đầu, rồi quay lại bước chuẩn bị.";
            if(text.Contains("buffer")||text.Contains("calibrat"))return "Chưa hiệu chuẩn đủ. Dùng máy pH: buffer 4 → rửa → buffer 7 → rửa, rồi thử lại.";
            if(text.Contains("mark")&&text.Contains("mix"))return "Chưa đủ nước hoặc chưa trộn. Mở bình định mức → Thêm nước tới vạch → Đậy và đảo trộn.";
            if(text.Contains("temperature")&&text.Contains("acid"))return "Chưa đo đủ nhiệt độ đầu. Mở nhiệt kế → Đo T acid, rồi Đo T base.";
            if(text.Contains("focus"))return "Kiểm tra lam đã đặt đúng và đóng kẹp. Chỉnh lấy nét từng bước; ở vật kính 40× chỉ dùng chỉnh tinh. Hình sơ đồ không phải phép đo kích thước đã hiệu chuẩn.";
            if(text.Contains("overshot")||text.Contains("overshoot"))return "Đã quá điểm cuối. Trong burette, chọn Giữ lượt chuẩn độ để giữ lượt bị loại và lý do; sau đó chọn Mẫu mới và chuẩn độ chậm hơn. Lượt bị loại không tính vào ba lượt hợp lệ.";
            if(text.Contains("20")&&text.Contains("cell"))return "Chưa đủ nhóm ô. Chọn và phân loại cùng 20–30 mã tế bào; không đổi nhóm khi thay dung dịch.";
            if(text.Contains("100")&&text.Contains("id"))return "Chưa đủ 100 mã khác nhau. Tiếp tục phân loại; mã lặp chỉ sửa đáp án, không tăng số ô.";
            if(text.Contains("wait")||text.Contains("continue observing")||text.Contains("at least"))return "Quan sát chưa đủ thời gian hoặc số lần. Tiếp tục bước đang hướng dẫn; đồng hồ chỉ chạy khi không tạm dừng.";
            return "Thao tác chưa đạt điều kiện. Kiểm tra bố trí và yêu cầu của bước này; có thể mở Hướng dẫn khi cần. Dữ liệu đã ghi vẫn được giữ.";
        }
    }
}
