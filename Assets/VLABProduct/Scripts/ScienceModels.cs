using System;
using System.Collections.Generic;

namespace Vlab.Product
{
    [Serializable] public sealed class Parameter
    {
        public string key, vi, en, unit;
        public double min, max, initial, step;
        public double[] choices;public bool readOnly;
        public Parameter(string key,string vi,string en,string unit,double min,double max,double initial,double step)
        { this.key=key;this.vi=vi;this.en=en;this.unit=unit;this.min=min;this.max=max;this.initial=initial;this.step=step; }
    }
    [Serializable] public sealed class Reading
    {
        public string key, unit,labelVi,labelEn; public double value;
        public Reading(string key,double value,string unit,string labelVi=null,string labelEn=null){this.key=key;this.value=value;this.unit=unit;this.labelVi=labelVi;this.labelEn=labelEn;}
    }
    public sealed class ModelResult
    {
        public readonly List<Reading> readings=new List<Reading>();
        public string note="",detailJson="";
        public ModelResult Add(string key,double value,string unit,string vi=null,string en=null)
        {
            if(double.IsNaN(value)||double.IsInfinity(value))throw new ArithmeticException("Non-finite observation: "+key);
            readings.Add(new Reading(key,value,unit,vi,en));return this;
        }
        public double Get(string key){foreach(var r in readings)if(r.key==key)return r.value;throw new KeyNotFoundException(key);}
    }
    public sealed class LessonSpec
    {
        public string id,titleEn,goalEn,modelLabel;
        public string[] operationsVi,operationsEn;
        public Parameter[] parameters;
        public double duration;
        public LessonSpec(string id,string title,string goal,string label,double duration,string vi,string en,params Parameter[] p)
        {this.id=id;titleEn=title;goalEn=goal;modelLabel=label;this.duration=duration;operationsVi=vi.Split('|');operationsEn=en.Split('|');parameters=p;}
        static LessonSpec()
        {
            void Options(string id,string key,params double[] values){Array.Find(All[id].parameters,p=>p.key==key).choices=values;}
            Options("M04","d1",40,80);Options("M04","d2",40,80);Options("M01","driver",12,24);Options("M01","driven",12,24,36);Options("B01","objective",4,10,40);Options("C07","surface",1,2,4);Options("M03","strands",1,2,4);
            foreach(var pair in new[]{new[]{"M04","d2"},new[]{"P02","height"},new[]{"M07","size"},new[]{"B02","water"}})Array.Find(All[pair[0]].parameters,p=>p.key==pair[1]).readOnly=true;
        }

        static Parameter P(string k,string vi,string en,string u,double lo,double hi,double x,double s)=>new Parameter(k,vi,en,u,lo,hi,x,s);
        public static readonly Dictionary<string,LessonSpec> All=new Dictionary<string,LessonSpec>
        {
            ["P01"]=new LessonSpec("P01","Simple pendulum","Measure repeated periods, compare lengths and estimate gravity.","Nonlinear ideal pendulum; no air resistance",0,
                "Gắn giá và kẹp|Gắn dây và quả nặng|Đặt mốc qua vị trí cân bằng|Kéo lệch rồi thả không đẩy","Mount stand and clamp|Attach string and bob|Set the equilibrium timing mark|Displace and release without a push",
                P("length","Chiều dài tới tâm quả","Pivot-to-centre length","m",.2,1.0,.6,.05),P("angle","Góc thả","Release angle","°",2,60,8,1),P("mass","Khối lượng","Bob mass","kg",.05,.5,.1,.05),P("cycles","Số chu kỳ đo","Timed cycles","",1,20,10,1)),
            ["P02"]=new LessonSpec("P02","Projectile motion","Compare first-impact range for launch angle, speed and height.","Analytic trajectory; no air resistance",3,
                "Đặt thước và tấm hứng|Chỉnh hướng bộ phóng|Nạp viên bi","Place ruler and target|Aim the launcher|Load the ball",
                P("angle","Góc phóng","Launch angle","°",5,85,45,5),P("speed","Vận tốc đầu","Launch speed","m/s",1,10,5,.5),P("height","Độ cao đầu","Launch height","m",0,1.2,0,.1)),
            ["P03"]=new LessonSpec("P03","Static and kinetic friction","Find the static threshold and steady sliding force.","Controlled horizontal pull; Coulomb friction",8,
                "Gắn lực kế vào khối|Chọn hệ số ma sát cho lượt đo|Đặt lực kế về không","Attach force meter|Set declared friction coefficients|Zero the meter",
                P("mass","Tổng khối lượng","Total mass","kg",.1,2,.5,.1),P("muS","Hệ số nghỉ","Static coefficient","",.1,.8,.4,.05),P("muK","Hệ số trượt","Kinetic coefficient","",.05,.7,.3,.05)),
            ["P04"]=new LessonSpec("P04","Photogate timing","Measure gate blocking times and derive speed and acceleration.","Analytic constrained cart motion",3,
                "Lắp ray và xe|Gắn bản chắn|Căn hai cổng và nối đồng hồ","Mount track and cart|Attach interrupt flag|Align gates and connect timer",
                P("flag","Rộng bản chắn","Flag width","m",.05,.05,.05,.01),P("spacing","Khoảng cách cổng","Gate spacing","m",.5,.5,.5,.1),P("speed","Vận tốc tại A","Speed at A","m/s",.2,2,.5,.1),P("accel","Gia tốc","Acceleration","m/s²",0,2,.75,.05)),
            ["P05"]=new LessonSpec("P05","Spring stiffness and oscillation","Compare static extension with oscillation-based stiffness.","Hooke model within elastic travel",7,
                "Gắn lò xo vào giá|Đo chiều dài tự nhiên|Treo tải và kéo nhẹ","Attach spring|Measure unloaded length|Attach load and displace gently",
                P("mass","Khối lượng treo","Hanging mass","kg",.01,.05,.02,.01),P("k","Độ cứng","Spring stiffness","N/m",10,50,20,5),P("amplitude","Biên độ","Amplitude","m",.005,.02,.005,.005)),
            ["P06"]=new LessonSpec("P06","Cart collisions","Compare signed momentum and energy before and after a collision.","One-dimensional elastic or sticking collision",3,
                "Lắp hai xe lên ray|Gắn đầu va chạm|Xác định chiều dương","Mount both carts on the track|Fit collision attachments|Set positive direction",
                P("m1","Khối lượng xe A","Cart A mass","kg",.1,1,.2,.1),P("m2","Khối lượng xe B","Cart B mass","kg",.1,1,.2,.1),P("u1","Vận tốc A","A initial speed","m/s",-.9,2,1,.1),P("u2","Vận tốc B","B initial speed","m/s",-1,1,0,.1),P("elastic","Đàn hồi 1 / dính 0","Elastic 1 / sticking 0","",0,1,1,1)),
            ["P07"]=new LessonSpec("P07","Newton’s second law","Separate force and total-mass investigations using a cart and hanging load.","Massless taut string and ideal pulley",2,
                "Lắp xe và ròng rọc|Luồn dây, gắn tải|Căn cổng trước vùng tải chạm đáy","Mount cart and pulley|Thread string and attach load|Align gates before the bottom stop",
                P("cart","Khối lượng xe","Cart mass","kg",.1,1,.4,.05),P("hanger","Tải treo","Hanging mass","kg",.01,.15,.05,.01),P("travel","Quãng tải rơi","Available fall","m",.1,.6,.4,.05)),
            ["P08"]=new LessonSpec("P08","Work and energy on a slope","Compare potential energy, kinetic energy and frictional loss.","Sliding block; no rolling energy",3,
                "Chỉnh độ nghiêng ray|Đo chiều cao đứng|Đặt vật ở mốc thả","Set track incline|Measure vertical height|Place block at release mark",
                P("mass","Khối lượng","Mass","kg",.1,1,.3,.1),P("height","Chiều cao đứng","Vertical height","m",.01,.35,.2,.01),P("angle","Góc nghiêng","Incline angle","°",10,30,30,5),P("mu","Hệ số ma sát","Friction coefficient","",0,.8,.1,.05)),
            ["C01"]=new LessonSpec("C01","Vinegar titration","Prepare the burette, approach the endpoint and compare repeat titres.","Acetic acid / strong base equilibrium; ideal dilute solution",0,
                "Tráng và nạp burette|Đuổi bọt đầu khóa|Pipette mẫu vào bình|Thêm phenolphthalein","Rinse and fill burette|Prime the tip|Pipette sample into flask|Add phenolphthalein",
                P("acid","Nồng độ mẫu","Sample concentration","mol/L",.05,.2,.1,.01),P("base","Nồng độ NaOH","NaOH concentration","mol/L",.05,.2,.1,.01),P("sample","Thể tích mẫu","Sample volume","mL",10,10,10,1),P("flow","Độ mở khóa","Valve opening","%",0,100,0,5)),
            ["C02"]=new LessonSpec("C02","Daniell cell","Assemble the cell and investigate concentration and polarity.","Nernst equation; activities approximated by concentrations",2,
                "Đặt Zn trong ZnSO4, Cu trong CuSO4|Lắp cầu muối|Nối vôn kế","Match electrodes to half-cells|Fit salt bridge|Connect voltmeter",
                P("zinc","Nồng độ Zn²⁺","Zinc concentration","mol/L",.01,2,1,.1),P("copper","Nồng độ Cu²⁺","Copper concentration","mol/L",.01,2,1,.1),P("temperature","Nhiệt độ","Temperature","°C",15,45,25,5),P("polarity","Chiều dây 1 / đảo -1","Polarity 1 / reversed -1","",-1,1,1,2)),
            ["C03"]=new LessonSpec("C03","Copper electrolysis","Relate charge to deposited copper and distinguish electrode types.","Faraday model; 100% current efficiency within reagent limit",20,
                "Cân điện cực khô|Lắp điện cực không chạm nhau|Nối nguồn đúng cực","Weigh dry electrodes|Mount separated electrodes|Connect power with correct polarity",
                P("current","Dòng điện","Current","A",.1,2,.5,.1),P("time","Thời gian mục tiêu","Target time","s",20,600,120,20),P("copperAnode","Anode đồng 1 / trơ 0","Copper anode 1 / inert 0","",0,1,1,1),P("moles","Cu²⁺ ban đầu","Initial copper ions","mol",.001,.05,.01,.001)),
            ["C04"]=new LessonSpec("C04","Chloride identification","Compare a blank control and chloride sample using silver nitrate.","Qualitative 2-unit transfers; NaCl, NaNO3 and water controls",3,
                "Dán nhãn NaCl, NaNO3 và nước|Thử hai mẫu đối chứng|Thử NaCl với AgNO3 đã chọn","Label NaCl, NaNO3 and water|Test both negative controls|Test NaCl with selected AgNO3"),
            ["C05"]=new LessonSpec("C05","Dilution and concentration series","Transfer a measured aliquot, fill to the mark and conserve solute.","Dilute aqueous solution; explicit final volume",2,
                "Tráng pipette bằng dung dịch gốc|Chuyển aliquot vào bình định mức|Thêm nước tới vạch và trộn","Rinse pipette with stock|Transfer aliquot to volumetric flask|Fill to mark and mix",
                P("stock","Nồng độ gốc","Stock concentration","mol/L",.1,2,1,.1),P("aliquot","Aliquot","Aliquot volume","mL",10,10,10,1),P("final","Thể tích cuối","Final volume","mL",50,100,50,50)),
            ["C06"]=new LessonSpec("C06","pH and dilution","Compare indicator ranges with a probe and investigate dilution.","Strong monoprotic acid/base with water autoionization at 25 °C",2,
                "Pha dung dịch và dán nhãn|Hiệu chuẩn, rửa đầu dò|Nhúng đầu dò và thử giấy BTB","Prepare and label solution|Calibrate and rinse probe|Immerse probe and test BTB paper",
                P("concentration","Nồng độ ban đầu","Initial concentration","mol/L",.001,.1,.01,.001),P("dilution","Hệ số pha loãng","Dilution factor","",1,1000,1,10),P("base","Base 1 / acid 0","Base 1 / acid 0","",0,1,0,1)),
            ["C07"]=new LessonSpec("C07","Calcium carbonate reaction rate","Measure a gas-volume curve and identify the limiting reagent.","Illustrative finite-substrate kinetics; ideal gas at 1 atm",30,
                "Cân CaCO3 và đong acid|Nối kín bình với syringe|Cho tiếp xúc và bắt đầu đồng hồ","Weigh carbonate and measure acid|Seal flask to gas syringe|Mix and start clock",
                P("batches","Số nhóm hạt","Equal-mass batches","",1,3,2,1),P("acid","Nồng độ HCl","HCl concentration","mol/L",.1,2,1,.1),P("volume","Thể tích HCl","Acid volume","mL",5,50,20,5),P("surface","Diện tích tương đối","Relative surface area","",1,4,1,1)),
            ["C08"]=new LessonSpec("C08","Neutralization calorimetry","Measure temperature rise and account for the cup heat capacity.","Delivered calorimeter: 57000 J/mol, explicit solution volumes and temperatures",20,
                "Đong acid và base riêng|Đo nhiệt độ đầu|Trộn trong cốc cách nhiệt và đặt đầu dò","Measure acid and base separately|Measure initial temperatures|Mix in insulated cup and immerse probe",
                P("acidMl","Thể tích acid","Acid volume","mL",20,80,25,5),P("baseMl","Thể tích base","Base volume","mL",20,80,25,5),P("acidM","Nồng độ acid","Acid concentration","mol/L",.01,1,.5,.05),P("baseM","Nồng độ base","Base concentration","mol/L",.01,1,.5,.05),P("acidC","Nhiệt độ acid","Acid temperature","°C",0,50,25,1),P("baseC","Nhiệt độ base","Base temperature","°C",0,50,25,1),P("cupC","Nhiệt độ cốc","Cup temperature","°C",0,50,25,1),P("cup","Nhiệt dung cốc","Cup heat capacity","J/K",0,100,20,10),P("loss","Cho phép thoát nhiệt","Heat loss enabled","",0,1,0,1)),
            ["B01"]=new LessonSpec("B01","Onion epidermis microscopy","Prepare a single layer and focus before changing objectives.","Supplied schematic specimen; focus and magnification are illustrative",2,
                "Trải biểu bì lên lam có nước|Hạ lamen nghiêng, thấm dư|Đặt lam và đóng hai kẹp|Lấy nét 4× trước khi đổi vật kính","Spread epidermis on wet slide|Lower coverslip at an angle and blot|Mount slide and close both clips|Focus at 4× before changing objective",
                P("objective","Vật kính","Objective","×",4,40,4,1),P("focus","Vị trí lấy nét","Focus position","mm",0,13,6.5,.5),P("bubble","Bọt do hạ lamen","Trapped air bubble","",0,1,0,1)),
            ["B02"]=new LessonSpec("B02","Plasmolysis and recovery","Track the same cells during salt exposure and water replacement.","Illustrative osmotic response; not a calibrated biological rate",30,
                "Chuẩn bị lam và lấy nét|Đánh dấu cùng trường nhìn|Thay dung dịch ở mép lamen","Prepare and focus slide|Mark the tracked field|Exchange liquid at coverslip edge",
                P("salt","Nồng độ muối","Salt concentration","%",0,5,2,.5),P("duration","Thời gian tiếp xúc","Exposure duration","s",10,120,30,10),P("water","Thay nước phục hồi","Replace with water","",0,1,0,1)),
            ["B03"]=new LessonSpec("B03","Potato osmosis","Compare mass changes across a concentration series with controlled blotting.","Seeded illustrative water-response model; not experimental calibration",30,
                "Cắt mẫu đồng cỡ và cân đầu|Chuẩn bị sucrose và đối chứng|Ngâm mẫu có nhãn|Thống nhất cách thấm mẫu trước khi cân cuối","Cut matched samples and weigh|Prepare sucrose series and control|Immerse labelled samples|Agree blotting protocol before final weighing",
                P("mass","Khối lượng đầu","Initial mass","g",1,5,2,.1),P("concentration","Sucrose","Sucrose concentration","mol/L",0,.6,.2,.05),P("duration","Thời gian ngâm","Soak duration","s",60,1800,600,60),P("blot","Đã thấm nước","Blotted","",0,1,1,1)),
            ["B04"]=new LessonSpec("B04","Catalase and temperature","Compare oxygen production with inactive-enzyme and water controls.","Illustrative enzyme kinetics and irreversible heat inactivation",30,
                "Chuẩn bị enzyme, mẫu bất hoạt và nước|Cân bằng nhiệt mẫu|Nối hệ thu khí và thêm H2O2","Prepare enzyme and controls|Equilibrate sample temperature|Seal collector and add peroxide",
                P("temperature","Nhiệt độ mẫu","Sample temperature","°C",10,90,35,5),P("enzyme","Enzyme tương đối","Relative enzyme amount","",0,2,1,.25),P("peroxide","H2O2 ban đầu","Initial peroxide","mmol",0,5,2,.5),P("inactive","Mẫu đã bất hoạt","Heat-inactivated sample","",0,1,0,1)),
            ["B05"]=new LessonSpec("B05","Yeast fermentation","Record carbon dioxide over time and compare a no-sugar control.","Illustrative lag and substrate-limited fermentation; accelerated clock",30,
                "Chuẩn bị nấm men và đối chứng|Đong đường, đặt bể nhiệt|Nối syringe và trộn","Prepare yeast and controls|Measure sugar and set bath|Connect syringe and mix",
                P("sugar","Glucose","Glucose mass","g",0,.5,.1,.05),P("temperature","Nhiệt độ","Temperature","°C",10,60,30,5),P("yeast","Nấm men tương đối","Relative yeast amount","",0,2,1,.25),P("inactive","Mẫu bất hoạt","Inactive sample","",0,1,0,1)),
            ["B06"]=new LessonSpec("B06","Leaf pigment chromatography","Prepare an extract and measure bands relative to the solvent front.","Illustrative bands for two configured solvent systems",30,
                "Nghiền lá với dung môi|Lọc dịch chiết|Chấm mẫu trên vạch chì|Đặt giấy, vạch trên dung môi","Grind leaf with solvent|Filter extract|Spot above pencil origin|Suspend paper above solvent level",
                P("solvent","Hệ dung môi","Solvent system","",0,1,0,1),P("front","Chiều dài mặt trước","Solvent travel limit","mm",20,88,70,1),P("submerged","Vạch chấm bị ngập","Origin submerged","",0,1,0,1)),
            ["B07"]=new LessonSpec("B07","Fruit DNA extraction","Follow extraction and compare omitted-step controls qualitatively.","Qualitative crude precipitate; not pure DNA or a mass assay",10,
                "Nghiền mô quả|Thêm đệm và trộn nhẹ|Lọc giữ dịch chiết|Rót alcohol thành lớp|Xác nhận mẫu và đối chứng","Mash fruit tissue|Add buffer and mix gently|Filter and retain extract|Layer cold alcohol|Confirm the sample/control setup",
                P("detergent","Có chất tẩy","Detergent included","",0,1,1,1),P("salt","Có muối","Salt included","",0,1,1,1),P("temperature","Nhiệt độ alcohol","Alcohol temperature","°C",0,40,5,5),P("alcohol","Có alcohol","Alcohol included","",0,1,1,1)),
            ["B08"]=new LessonSpec("B08","Mitosis and mitotic index","Classify distinct cells, retain uncertain cells and calculate the index.","Supplied schematic atlas; no same-cell time-progression claim",2,
                "Đặt tiêu bản và lấy nét|Chọn trường nhìn có mã|Phân loại tế bào không đếm trùng","Mount and focus slide|Select an identified field|Classify distinct cells without duplicates",
                P("field","Mã trường nhìn","Field identifier","",1,3,1,1)),
            ["M01"]=new LessonSpec("M01","Gear transmission","Assemble matching gears and measure signed speed ratios.","Ideal external gears; optional efficiency affects torque only",5,
                "Lắp trục và gối đỡ|Gắn bánh răng cùng module|Kiểm tra khoảng tâm","Mount shafts and bearings|Fit gears of matching module|Check centre distance",
                P("driver","Răng bánh chủ động","Driver teeth","",12,24,12,12),P("driven","Răng bánh bị động","Driven teeth","",12,36,24,12),P("rpm","Tốc độ vào","Input speed","rpm",10,120,60,10),P("module","Module","Module","mm",2,2,2,1)),
            ["M02"]=new LessonSpec("M02","Lever and moment balance","Change force positions and compare signed moments about the pivot.","Lever with declared 0.5 N beam weight; actual pivot and support reaction",3,
                "Đặt điểm tựa|Gắn hai tải đúng vị trí|Đặt thanh về mốc ngang","Place fulcrum|Attach loads at marked positions|Set horizontal reference",
                P("force1","Lực trái","Left force","N",1,10,5,.5),P("arm1","Cánh tay trái","Left arm","m",.1,.4,.4,.1),P("force2","Lực phải","Right force","N",1,10,10,.5),P("arm2","Cánh tay phải","Right arm","m",.1,.4,.2,.1)),
            ["M03"]=new LessonSpec("M03","Pulley mechanical advantage","Route supporting strands and compare force with travel.","Ideal strand topology plus declared efficiency",5,
                "Gắn ròng rọc cố định/động|Luồn dây đúng sơ đồ|Gắn tải và lực kế","Mount fixed/moving pulleys|Route the supporting strands|Attach load and force meter",
                P("strands","Nhánh chịu tải","Supporting strands","",1,4,2,1),P("load","Trọng lượng tải","Load weight","N",1,10,10,1),P("height","Độ nâng","Lift height","m",.05,.2,.2,.05),P("efficiency","Hiệu suất","Efficiency","%",50,100,100,5)),
            ["M04"]=new LessonSpec("M04","Belt transmission","Compare open/crossed belts, pitch diameters and slip.","Kinematic belt model with declared fractional slip",5,
                "Lắp hai pulley|Luồn đai và kiểm độ căng|Nối motor và đo tốc độ","Mount pulleys|Route and tension belt|Connect motor and tachometer",
                P("d1","Đường kính chia vào","Driver pitch diameter","mm",40,80,40,40),P("d2","Đường kính chia ra","Driven pitch diameter","mm",40,80,80,40),P("rpm","Tốc độ vào","Input speed","rpm",10,120,60,10),P("crossed","Đai chéo","Crossed belt","",0,1,0,1),P("slip","Trượt","Slip","%",0,30,0,5)),
            ["M05"]=new LessonSpec("M05","Slider–crank","Change crank radius and rod length and measure slider travel.","Inline kinematic linkage; connecting rod longer than crank",5,
                "Lắp trục tay quay|Gắn thanh truyền và chốt|Căn con trượt trên ray","Mount crank shaft|Fit connecting rod and pins|Align slider on guide",
                P("radius","Bán kính tay quay","Crank radius","mm",20,40,30,10),P("rod","Dài thanh truyền","Rod length","mm",80,120,100,20),P("rpm","Tốc độ quay","Drive speed","rpm",5,60,20,5)),
            ["M06"]=new LessonSpec("M06","Cam and follower","Measure rise, dwell and return from the configured cam profile.","Simple-harmonic rise/return with explicit dwell sectors",5,
                "Lắp cam và motor|Căn con đội theo dẫn hướng|Đặt mốc góc và đồng hồ","Mount cam and motor|Align follower and guide|Zero angle and displacement gauge",
                P("lift","Độ nâng","Lift","mm",10,10,10,5),P("rise","Góc nâng","Rise sector","°",120,120,120,15),P("rpm","Tốc độ","Drive speed","rpm",5,60,20,5)),
            ["M07"]=new LessonSpec("M07","Caliper and micrometer","Zero the tool, contact the sample and correct the raw reading.","Geometric sample with virtual contact/ratchet; no physical force sensing",2,
                "Đóng dụng cụ và đo sai số không|Đặt mẫu đúng mặt đo|Tiếp xúc nhẹ và đọc thang","Close tool and record zero error|Place sample at measurement faces|Contact gently and read scale",
                P("size","Kích thước mẫu","Sample dimension","mm",12.1,20,12.1,.1),P("zero","Sai số không","Zero error","mm",-.1,.1,.02,.01),P("micrometer","Panme 1 / thước kẹp 0","Micrometer 1 / caliper 0","",0,1,1,1)),
            ["M08"]=new LessonSpec("M08","Beam bending","Compare deflection as load, span and section height change.","Simply supported rectangular beam; small elastic deflection",3,
                "Đặt hai gối tựa|Đo tiết diện và khẩu độ|Đặt đồng hồ giữa nhịp, thêm tải","Place supports|Measure span and section|Zero midspan gauge and apply load",
                P("load","Lực giữa nhịp","Midspan force","N",0,5,1,1),P("span","Khẩu độ","Support span","m",.2,.3,.3,.05),P("height","Chiều cao tiết diện","Section height","mm",2,8,5,1),P("modulus","Môđun Young","Young modulus","GPa",1,200,2,1))
        };
    }

    public static class ScienceModels
    {
        public const double G=9.81, F=96485.33212;
        public static double PendulumPeriod(double length,double angleDegrees)
        {
            if(length<=0||Math.Abs(angleDegrees)>=180)throw new ArgumentOutOfRangeException();
            double a=1,b=Math.Cos(angleDegrees*Math.PI/360);
            for(int i=0;i<12;i++){double old=a;a=(a+b)/2;b=Math.Sqrt(old*b);}
            return 2*Math.PI*Math.Sqrt(length/G)/a;
        }
        public static ModelResult Evaluate(string id,Dictionary<string,double> p,double elapsed,double dose=0,int seed=1)
        {
            if(!LessonSpec.All.TryGetValue(id,out var spec))throw new ArgumentException("Unknown experiment "+id);
            foreach(var x in spec.parameters)
                if(!p.TryGetValue(x.key,out var v)||double.IsNaN(v)||double.IsInfinity(v)||v<x.min-1e-9||v>x.max+1e-9)throw new ArgumentOutOfRangeException(x.key);
            if(elapsed<0||dose<0||double.IsNaN(elapsed)||double.IsInfinity(elapsed))throw new ArgumentOutOfRangeException(nameof(elapsed));
            double V(string key)=>p[key];
            double Rad(double degrees)=>degrees*Math.PI/180;
            var r=new ModelResult();double t=elapsed;
            switch(id)
            {
                case "P01": {double T=PendulumPeriod(V("length"),V("angle"));r.Add("period",T,"s").Add("tenCycleTime",T*V("cycles"),"s").Add("gravityEstimate",4*Math.PI*Math.PI*V("length")/(T*T),"m/s²").Add("mass",V("mass"),"kg");break;}
                case "P02": {double vx=V("speed")*Math.Cos(Rad(V("angle"))),vy=V("speed")*Math.Sin(Rad(V("angle")));double tf=(vy+Math.Sqrt(vy*vy+2*G*V("height")))/G;r.Add("flightTime",tf,"s").Add("range",vx*tf,"m").Add("maxHeight",V("height")+vy*vy/(2*G),"m");break;}
                case "P03": {if(V("muK")>V("muS"))throw new InvalidOperationException("Kinetic coefficient must not exceed the static coefficient in this lesson.");double N=V("mass")*G;r.Add("normalForce",N,"N").Add("staticPeak",N*V("muS"),"N").Add("slidingForce",N*V("muK"),"N").Add("currentPull",t<4?N*V("muS")*t/4:N*V("muK"),"N");break;}
                case "P04": {double u=V("speed"),a=V("accel"),s=V("spacing"),d=V("flag"),v=Math.Sqrt(u*u+2*a*s);r.Add("gateA",GateTime(u,a,d),"s").Add("gateB",GateTime(v,a,d),"s").Add("speedA",u,"m/s").Add("speedB",v,"m/s").Add("acceleration",(v*v-u*u)/(2*s),"m/s²");break;}
                case "P05": {double extension=V("mass")*G/V("k");if(extension+V("amplitude")>.05)throw new InvalidOperationException("Elastic travel exceeded.");r.Add("extension",extension,"m").Add("period",2*Math.PI*Math.Sqrt(V("mass")/V("k")),"s").Add("stiffness",V("k"),"N/m");break;}
                case "P06": {double m=V("m1"),n=V("m2"),u=V("u1"),v=V("u2");if(u<=v)throw new InvalidOperationException("Cart A must approach cart B in the chosen arrangement.");double a,b;if(V("elastic")>.5){a=((m-n)*u+2*n*v)/(m+n);b=((n-m)*v+2*m*u)/(m+n);}else a=b=(m*u+n*v)/(m+n);r.Add("velocityA",a,"m/s").Add("velocityB",b,"m/s").Add("momentumBefore",m*u+n*v,"kg·m/s").Add("momentumAfter",m*a+n*b,"kg·m/s").Add("energyBefore",.5*m*u*u+.5*n*v*v,"J").Add("energyAfter",.5*m*a*a+.5*n*b*b,"J");break;}
                case "P07": {double a=V("hanger")*G/(V("cart")+V("hanger"));r.Add("acceleration",a,"m/s²").Add("externalForce",V("hanger")*G,"N").Add("tension",V("cart")*a,"N").Add("bottomTime",Math.Sqrt(2*V("travel")/a),"s");break;}
                case "P08": {double angle=Rad(V("angle")),distance=V("height")/Math.Sin(angle),ep=V("mass")*G*V("height"),loss=Math.Min(ep,V("mu")*V("mass")*G*Math.Cos(angle)*distance),ke=Math.Max(0,ep-loss);r.Add("potentialEnergy",ep,"J").Add("kineticEnergy",ke,"J").Add("frictionLoss",loss,"J").Add("speed",Math.Sqrt(2*ke/V("mass")),"m/s");break;}
                case "C01": {double n=V("acid")*V("sample")/1000,b=V("base")*dose/1000,total=(V("sample")+dose)/1000,eq=n/V("base")*1000,ph;const double ka=1.8e-5;
                    if(b<n-1e-10){if(b<1e-10){double c=n/total;ph=-Math.Log10(2*ka*c/(ka+Math.Sqrt(ka*ka+4*ka*c)));}else{double acid=(n-b)/total,salt=b/total;double h=2*ka*acid/(salt+ka+Math.Sqrt((salt+ka)*(salt+ka)+4*ka*acid));ph=-Math.Log10(h);}}
                    else if(b<=n+1e-10){double kb=1e-14/ka,c=n/total,oh=2*kb*c/(kb+Math.Sqrt(kb*kb+4*kb*c));ph=14+Math.Log10(oh);}else{double c=(b-n)/total;ph=14+Math.Log10((c+Math.Sqrt(c*c+4e-14))/2);}
                    r.Add("delivered",dose,"mL").Add("pH",ph,"").Add("endpointTarget",eq,"mL").Add("remainingAcid",Math.Max(0,n-b),"mol");break;}
                case "C02": {if(Math.Abs(V("polarity"))<.99)throw new InvalidOperationException("Select normal or reversed leads.");double E=1.1+8.314462618*(V("temperature")+273.15)/(2*F)*Math.Log(V("copper")/V("zinc"));r.Add("voltage",E*V("polarity"),"V");break;}
                case "C03": {double charge=V("current")*Math.Min(t,V("time")),mol=charge/(2*F);if(V("copperAnode")<.5)mol=Math.Min(mol,V("moles"));r.Add("charge",Math.Min(charge,V("copperAnode")>.5?double.MaxValue:V("moles")*2*F),"C").Add("copperMass",mol*63.546,"g").Add("anodeMassLoss",V("copperAnode")>.5?mol*63.546:0,"g");break;}
                case "C04": {r.Add("blankPrecipitate",0,"").Add("samplePrecipitate",1,"");r.note="Qualitative presence/absence; not a quantitative chloride assay.";break;}
                case "C05": {if(V("aliquot")>V("final"))throw new InvalidOperationException("Final volume is smaller than aliquot.");r.Add("solute",V("stock")*V("aliquot")/1000,"mol").Add("finalConcentration",V("stock")*V("aliquot")/V("final"),"mol/L").Add("dilutionFactor",V("final")/V("aliquot"),"");break;}
                case "C06": {double c=V("concentration")/V("dilution"),h=(c+Math.Sqrt(c*c+4e-14))/2,ph=-Math.Log10(h);if(V("base")>.5)ph=14-ph;r.Add("pH",ph,"").Add("indicatorLower",Math.Floor(ph),"").Add("indicatorUpper",Math.Ceiling(ph),"").Add("concentration",c,"mol/L");break;}
                case "C07": {double maximum=Math.Min(V("batches")*.047977484110161756/100.0869,V("acid")*V("volume")/2000),n=maximum*(1-Math.Exp(-.04*V("surface")*V("acid")*t)),volume=n*.082057*298.15*1000;r.Add("gasVolume",Math.Min(100,volume),"mL").Add("gasLimit",maximum*.082057*298.15*1000,"mL").Add("rate",maximum*.04*V("surface")*V("acid")*Math.Exp(-.04*V("surface")*V("acid")*t)*.082057*298.15*1000,"mL/s");if(volume>=100)r.note="Gas syringe capacity reached; later volume is unavailable.";break;}
                case "C08": {double n=Math.Min(V("acidMl")*V("acidM"),V("baseMl")*V("baseM"))/1000,capacity=(V("acidMl")+V("baseMl"))*4.18+V("cup"),q=n*57000,initial=(V("acidMl")*4.18*V("acidC")+V("baseMl")*4.18*V("baseC")+V("cup")*V("cupC"))/capacity,delta=q/capacity*(1-Math.Exp(-t/2))*Math.Exp(-(V("loss")>.5?.15/capacity:0)*t);r.Add("temperature",initial+delta,"°C").Add("temperatureRise",delta,"K").Add("releasedHeat",q*(1-Math.Exp(-t/2)),"J").Add("reactedAmount",n*(1-Math.Exp(-t/2)),"mol");break;}
                case "B01": {double sharp=Math.Exp(-Math.Pow((V("focus")-6.5)/1.2,2));r.Add("magnification",10*V("objective"),"×").Add("focusQuality",sharp,"").Add("bubblePresent",V("bubble"),"");break;}
                case "B02": {double exposed=Math.Min(t,V("duration")),fraction=(1-Math.Exp(-V("salt")*.04*exposed));if(V("water")>.5)fraction*=Math.Exp(-Math.Max(0,t-V("duration"))*.08);int co=(int)Math.Round(30*fraction);r.Add("trackedCells",30,"").Add("plasmolysedCells",co,"").Add("plasmolysisPercent",co/30.0*100,"%");break;}
                case "B03": {double noise=((seed%17)/16.0-.5)*.015,change=(.18*Math.Tanh((.25-V("concentration"))*5)+noise)*(1-Math.Exp(-Math.Min(t,V("duration"))/500));double m=V("mass")*(1+change)+(V("blot")<.5?.08:0);r.Add("initialMass",V("mass"),"g").Add("finalMass",m,"g").Add("massChange",(m-V("mass"))/V("mass")*100,"%");break;}
                case "B04": {double temp=V("temperature"),activity=V("inactive")>.5?0:Math.Exp(-Math.Pow((temp-38)/22,2))*Math.Exp(-Math.Max(0,temp-55)*.12),limit=V("peroxide")*.5*24.465,volume=limit*(1-Math.Exp(-.015*V("enzyme")*activity*t));r.Add("oxygenVolume",volume,"mL").Add("initialRate",limit*.015*V("enzyme")*activity,"mL/s").Add("oxygenLimit",limit,"mL");break;}
                case "B05": {double activity=V("inactive")>.5?0:Math.Exp(-Math.Pow((V("temperature")-32)/17,2))*Math.Exp(-Math.Max(0,V("temperature")-45)*.15),limit=V("sugar")/180.156*2*24465,volume=limit*(1-Math.Exp(-.006*V("yeast")*activity*Math.Max(0,t-15)));r.Add("carbonDioxide",Math.Min(100,volume),"mL").Add("gasLimit",limit,"mL");if(volume>=100)r.note="Syringe capacity reached.";break;}
                case "B06": {if(V("submerged")>.5)throw new InvalidOperationException("Sample origin is submerged; prepare a fresh strip above the solvent.");double front=V("front")*(1-Math.Exp(-t/20)),rf=V("solvent")>.5?.68:.55;r.Add("solventFront",front,"mm").Add("bandA",front*rf,"mm").Add("rfA",rf,"").Add("bandB",front*.85,"mm").Add("rfB",.85,"");break;}
                case "B07": {double present=V("detergent")>.5&&V("salt")>.5&&V("alcohol")>.5&&t>=8?1:0;r.Add("crudePrecipitate",present,"presence");r.note="Visible crude precipitate may contain DNA and other material; no purity or yield is inferred.";break;}
                case "B08": {int field=(int)V("field"),interphase=field==1?21:field==2?18:24,pro=field==1?4:field==2?5:3,meta=2,ana=field==2?3:1,telo=30-interphase-pro-meta-ana;r.Add("totalCells",30,"").Add("interphase",interphase,"").Add("prophase",pro,"").Add("metaphase",meta,"").Add("anaphase",ana,"").Add("telophase",telo,"").Add("mitoticIndex",(30-interphase)/30.0*100,"%");break;}
                case "M01": {double ratio=-V("driver")/V("driven");r.Add("inputSpeed",V("rpm"),"rpm").Add("outputSpeed",V("rpm")*ratio,"rpm").Add("signedRatio",ratio,"").Add("centreDistance",V("module")*(V("driver")+V("driven"))/2,"mm");break;}
                case "M02": {double a=V("force1")*V("arm1"),b=V("force2")*V("arm2");r.Add("leftMoment",a,"N·m").Add("rightMoment",b,"N·m").Add("netMoment",a-b,"N·m").Add("balanced",Math.Abs(a-b)<.005?1:0,"");break;}
                case "M03": {double n=V("strands"),eta=V("efficiency")/100,force=V("load")/(n*eta),s=n*V("height");r.Add("effortForce",force,"N").Add("pullDistance",s,"m").Add("loadWork",V("load")*V("height"),"J").Add("inputWork",force*s,"J").Add("efficiency",eta*100,"%");break;}
                case "M04": {double ratio=V("d1")/V("d2")*(V("crossed")>.5?-1:1)*(1-V("slip")/100);r.Add("outputSpeed",V("rpm")*ratio,"rpm").Add("signedRatio",ratio,"");break;}
                case "M05": {double a=V("radius"),l=V("rod");if(l<=a)throw new InvalidOperationException("Connecting rod must be longer than the crank radius.");double theta=t*V("rpm")*2*Math.PI/60,x=a*Math.Cos(theta)+Math.Sqrt(l*l-a*a*Math.Pow(Math.Sin(theta),2));r.Add("angle",(t*V("rpm")*6)%360,"°").Add("sliderPosition",x,"mm").Add("stroke",2*a,"mm");break;}
                case "M06": {double theta=(t*V("rpm")*6)%360,beta=V("rise"),h=V("lift"),y=theta<beta?h*.5*(1-Math.Cos(Math.PI*theta/beta)):theta<180?h:theta<180+beta?h*.5*(1+Math.Cos(Math.PI*(theta-180)/beta)):0;r.Add("angle",theta,"°").Add("lift",y,"mm").Add("maximumLift",h,"mm");break;}
                case "M07": {double res=V("micrometer")>.5?.01:.02,raw=Math.Round((V("size")+V("zero"))/res)*res;r.Add("rawReading",raw,"mm").Add("zeroError",V("zero"),"mm").Add("correctedReading",raw-V("zero"),"mm").Add("resolution",res,"mm");break;}
                case "M08": {double b=.02,h=V("height")/1000,I=b*h*h*h/12,delta=V("load")*Math.Pow(V("span"),3)/(48*V("modulus")*1e9*I);if(delta>V("span")*.05)throw new InvalidOperationException("Outside this model’s small-deflection range; reduce load/span or increase section height.");r.Add("deflection",delta*1000,"mm").Add("sectionMoment",I,"m⁴");break;}
                default:throw new ArgumentException(id);
            }
            return r;
        }
        static double GateTime(double centreSpeed,double accel,double width)
        {
            // symmetric flag edges at +/- width/2 around gate-centre event
            if(accel<1e-10)return width/centreSpeed;
            double left=centreSpeed*centreSpeed-accel*width;
            if(left<=0)throw new InvalidOperationException("Flag is too long for the chosen gate-A speed.");
            return (Math.Sqrt(centreSpeed*centreSpeed+accel*width)-Math.Sqrt(left))/accel;
        }
        public static Dictionary<string,double> Defaults(string id)
        {var p=new Dictionary<string,double>();foreach(var x in LessonSpec.All[id].parameters)p[x.key]=x.initial;return p;}
    }
}
