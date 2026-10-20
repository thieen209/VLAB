using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public sealed class NotebookAxis
    {
        public string key,vi,en,unit;
        public NotebookAxis(string key,string vi,string en,string unit=""){this.key=key;this.vi=vi;this.en=en;this.unit=unit;}
        public string Label(bool vietnamese)=>(vietnamese?vi:en)+(string.IsNullOrEmpty(unit)?"":" ("+unit+")");
    }
    public sealed class NotebookPoint {public double x,y;public string attemptId,sampleId;}
    public sealed class NotebookSummary {public double x,mean,min,max;public int n;}
    public sealed class NotebookFit {public bool eligible,smallAngle;public double slope,intercept,rSquared,gravity;public string vi,en;}
    public sealed class NotebookCurve {public string attemptId;public readonly List<NotebookPoint> points=new List<NotebookPoint>();}
    public sealed class NotebookGroup
    {
        public string key,vi,en;public readonly List<AttemptRecord> records=new List<AttemptRecord>();
        public readonly List<NotebookPoint> points=new List<NotebookPoint>();public readonly List<NotebookSummary> summaries=new List<NotebookSummary>();
        public readonly List<NotebookCurve> curves=new List<NotebookCurve>();public NotebookFit fit;
    }
    public sealed class NotebookData
    {
        public string experimentId,independentKey,dependentKey,noticeVi="",noticeEn="";public bool tableOnly,curveMode;
        public NotebookAxis x,y;public readonly List<NotebookAxis> xChoices=new List<NotebookAxis>(),yChoices=new List<NotebookAxis>();
        public readonly List<NotebookGroup> groups=new List<NotebookGroup>();public readonly List<AttemptRecord> rows=new List<AttemptRecord>();public int excluded;
    }
    public static class ExperimentNotebook
    {
        static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
        static readonly Dictionary<string,string[]> Defaults=new Dictionary<string,string[]>
        {
            ["P01"]=new[]{"length","periodSquared"},["P02"]=new[]{"angle","range"},["P03"]=new[]{"normalForce","slidingForce"},["P04"]=new[]{"spacing","acceleration"},
            ["P05"]=new[]{"mass","periodSquared"},["P06"]=new[]{"m1","momentumAfter"},["P07"]=new[]{"externalForceFromMass","acceleration"},["P08"]=new[]{"height","kineticEnergy"},
            ["C01"]=new[]{"base","meanTitre"},["C02"]=new[]{"copper","voltage"},["C03"]=new[]{"time","massChange"},["C04"]=new[]{"",""},["C05"]=new[]{"stock","concentration"},
            ["C06"]=new[]{"dilutionFactor","pH"},["C07"]=new[]{"@time","gasVolume"},["C08"]=new[]{"@time","temperature"},
            ["B01"]=new[]{"",""},["B02"]=new[]{"salt","plasmolysisPercent"},["B03"]=new[]{"concentration","meanMassChange"},["B04"]=new[]{"@time","gasVolume"},
            ["B05"]=new[]{"@time","gasVolume"},["B06"]=new[]{"solventFront","rf1"},["B07"]=new[]{"",""},["B08"]=new[]{"",""},
            ["M01"]=new[]{"driven","signedRatio"},["M02"]=new[]{"arm1","netMoment"},["M03"]=new[]{"strands","effortForce"},["M04"]=new[]{"slip","outputSpeed"},
            ["M05"]=new[]{"@angle","sliderPosition"},["M06"]=new[]{"@angle","lift"},["M07"]=new[]{"",""},["M08"]=new[]{"load","deflection"}
        };
        [Serializable] sealed class Detail
        {public double reactionAt;public GasPoint[] gas;public ChemPoint[] curve;public MotionPoint[] series;public Potato[] samples;public string sampleId,beltConfiguration,followerType;public bool reversedLeads,driveLarge;public int pivot,belt,sample,gearDriverTeeth,gearDrivenTeeth;public GateDetail gateA,gateB;}
        [Serializable] sealed class GateDetail{public bool complete;public double blockingSeconds,meanSpeed,midpointPosition;}
        [Serializable] sealed class GasPoint{public double seconds,volumeMl;public string sampleId;}
        [Serializable] sealed class ChemPoint{public double seconds,value;}
        [Serializable] sealed class MotionPoint{public double t,a,b,c;}
        [Serializable] sealed class Potato{public string id;public double initial,final,concentration;public bool initialRead,finalRead,blotted;}
        static Detail ReadDetail(AttemptRecord a)
        {if(string.IsNullOrEmpty(a.detailJson)||a.detailJson.Length>1000000)return null;try{return JsonUtility.FromJson<Detail>(a.detailJson);}catch{return null;}}
        public static NotebookData BuildData(string id,List<AttemptRecord> attempts,string independentKey=null,string dependentKey=null)
        {
            var d=new NotebookData{experimentId=id};
            if(!LessonSpec.All.TryGetValue(id,out var spec)||!Defaults.TryGetValue(id,out var defaults))
            {d.tableOnly=true;d.noticeEn="No experiment mapping is available.";d.noticeVi="Chưa có ánh xạ dữ liệu cho bài này.";return d;}
            d.rows.AddRange((attempts??new List<AttemptRecord>()).Where(a=>a!=null&&a.experimentId==id));
            d.tableOnly=defaults[0]==""||id=="B02";
            foreach(var p in spec.parameters)d.xChoices.Add(new NotebookAxis(p.key,p.vi,p.en,p.unit));
            if(defaults[0]=="@time")d.xChoices.Insert(0,new NotebookAxis("@time","Thời gian từ lúc trộn","Time since mixing","s"));
            if(defaults[0]=="@angle")d.xChoices.Insert(0,new NotebookAxis("@angle","Góc đã lấy mẫu","Sampled angle","°"));
            foreach(var r in d.rows.SelectMany(a=>a.readings??new List<Reading>()).Where(r=>r!=null&&Finite(r.value)).GroupBy(r=>r.key).Select(g=>g.First()))
                d.yChoices.Add(new NotebookAxis(r.key,r.labelVi??r.key,r.labelEn??r.key,r.unit));
            if(id=="P01"||id=="P05")d.yChoices.Insert(0,new NotebookAxis("periodSquared","Bình phương chu kỳ đo","Squared measured period","s²"));
            if(id=="P02")d.xChoices.Insert(0,new NotebookAxis("initialSpeedSquared","Bình phương vận tốc đầu","Squared initial speed","m²/s²"));
            // Supplied research/experiments.py P03: plot measured steady force against N, not merely mass.
            if(id=="P03")d.xChoices.Insert(0,new NotebookAxis("normalForce","Phản lực đã ghi","Recorded normal force","N"));
            if(id=="P07")
            {d.xChoices.Insert(0,new NotebookAxis("externalForceFromMass","Ngoại lực m tải × g","Hanging-weight external force","N"));d.xChoices.Insert(1,new NotebookAxis("inverseTotalMass","Nghịch đảo tổng khối lượng","Inverse total system mass","kg⁻¹"));}
            if(id=="C06")d.xChoices.Insert(0,new NotebookAxis("dilutionFactor","Hệ số pha loãng đã ghi","Recorded total dilution factor"));
            if(id=="B06")d.xChoices.Insert(0,new NotebookAxis("solventFront","Quãng mặt trước đã đánh dấu","Marked solvent-front travel","mm"));
            d.independentKey=independentKey??defaults[0];d.dependentKey=dependentKey??defaults[1];
            d.curveMode=d.independentKey=="@time"||d.independentKey=="@angle";
            // A saved trace has one declared channel. Other saved scalar results are selectable after choosing a comparison X.
            if(d.curveMode){d.dependentKey=defaults[1];d.yChoices.RemoveAll(a=>a.key!=d.dependentKey);}
            d.x=d.xChoices.FirstOrDefault(x=>x.key==d.independentKey)??new NotebookAxis(d.independentKey,d.independentKey,d.independentKey);
            d.y=d.yChoices.FirstOrDefault(x=>x.key==d.dependentKey)??new NotebookAxis(d.dependentKey,d.dependentKey,d.dependentKey);
            foreach(var a in d.rows)
            {
                var signature=Controls(spec,a,d.independentKey,out string vi,out string en);
                var group=d.groups.FirstOrDefault(g=>g.key==signature);
                if(group==null){group=new NotebookGroup{key=signature,vi=vi,en=en};d.groups.Add(group);}group.records.Add(a);
                if(d.tableOnly)continue;
                if(d.curveMode)
                {
                    var curve=ActualCurve(id,a,d.independentKey,d.dependentKey);if(curve.points.Count>=2)group.curves.Add(curve);else d.excluded++;continue;
                }
                if(!TryX(spec,a,d.independentKey,out double x)){d.excluded++;continue;}
                // B03's three independently identified samples are replicates; repeated final balance reads are not.
                if(id=="B03"&&d.dependentKey=="meanMassChange")
                {
                    var details=ReadDetail(a);var samples=details?.samples;
                    if(samples!=null)
                    {
                        foreach(var sample in samples.Where(p=>p!=null&&p.initialRead&&p.finalRead&&p.blotted&&p.initial>0&&Finite(p.initial)&&Finite(p.final)&&!string.IsNullOrEmpty(p.id)).GroupBy(p=>p.id).Select(g=>g.First()))
                        {double y=100*(sample.final-sample.initial)/sample.initial;if(Finite(y))group.points.Add(new NotebookPoint{x=x,y=y,attemptId=a.attemptId,sampleId=sample.id});}
                        continue;
                    }
                    // Legacy records only contain an attempt mean: retain one point, never invent three replicates.
                }
                if(id=="P07"&&d.dependentKey=="acceleration"&&!HasGateObservation(a)){d.excluded++;continue;}
                if(!TryY(a,d.dependentKey,out double value)){d.excluded++;continue;}
                group.points.Add(new NotebookPoint{x=x,y=value,attemptId=a.attemptId,sampleId=a.attemptId});
            }
            foreach(var g in d.groups)
            {
                // A repeated saved ID is displayed in rows but cannot inflate replication statistics.
                var unique=g.points.GroupBy(p=>(p.sampleId??"")+"|"+p.x.ToString("R",Inv)).Select(p=>p.First()).ToList();g.points.Clear();g.points.AddRange(unique);
                foreach(var bucket in unique.GroupBy(p=>p.x).OrderBy(b=>b.Key))g.summaries.Add(new NotebookSummary{x=bucket.Key,n=bucket.Count(),mean=bucket.Average(p=>p.y),min=bucket.Min(p=>p.y),max=bucket.Max(p=>p.y)});
                if(id=="P01"&&d.independentKey=="length"&&d.dependentKey=="periodSquared")g.fit=PendulumFit(g,spec);
            }
            if(d.tableOnly){d.noticeVi="Bảng kết quả định tính/nhận dạng đã lưu. Không tạo đồ thị số hoặc kết luận từ mã phân loại.";d.noticeEn="Saved qualitative/identification results. No numerical trend or scientific conclusion is inferred from category IDs.";}
            else if(d.curveMode){d.noticeVi="Chỉ các mốc thực sự đã lưu trong từng lượt; đường nối chỉ nối các mốc, không phải đường lý thuyết. Mỗi lượt giữ riêng.";d.noticeEn="Only saved time/angle samples are plotted. Lines connect saved points; they are not theoretical curves. Attempts remain separate.";}
            else{d.noticeVi="Chọn một nhóm giữ nguyên các tham số khác. Mỗi điểm là số đo đã lưu; n là số mẫu/lượt riêng biệt, khoảng là min–max, không phải khoảng tin cậy.";d.noticeEn="Select a group holding every other parameter fixed. Points are saved observations; n counts distinct samples/attempts. Range is min–max, not a confidence interval.";}
            if(id=="P02")
            {d.noticeVi+=" Độ cao là từ nòng thật tới tấm hứng, không phải h₀=0. Khi đổi góc, độ cao có thể đổi theo; xem Độ cao nòng thực trong từng lượt. Đồ thị góc mô tả cấu hình đã lắp, không chứng minh hai góc bù cho cùng tầm xa. Trục v₀² giữ góc và độ cao.";d.noticeEn+=" Height is the actual muzzle-to-mat distance, not h₀=0. Changing angle can change that height; inspect Actual muzzle height in each record. The angle plot describes the assembled geometry and cannot establish equal complementary-angle ranges. The v₀² axis holds angle and height fixed.";}
            if(id=="P07")
            {d.noticeVi+=" Trục ngoại lực giữ M = m xe + m tải cố định; trục 1/M giữ m tải cố định. Chỉ gia tốc có hai khoảng che cổng đã lưu mới được vẽ; dự đoán mô hình không thay số đo.";d.noticeEn+=" The force axis holds M = cart + hanger fixed; 1/M holds hanger mass fixed. Only accelerations backed by two saved blocking intervals are plotted; model predictions never replace measurements.";}
            if(id=="P03")
            {d.noticeVi+=" Trục phản lực dùng số N đã lưu cùng lực kéo đều; thiếu số N thì không vẽ điểm. Giữ riêng các hệ số ma sát và mô hình; dụng cụ hiện dùng mặt gỗ, kéo ngang.";d.noticeEn+=" The normal-force axis uses saved N readings paired with steady pull; missing N excludes a point. Friction coefficients and models remain separate. The current apparatus uses wood and a horizontal pull.";}
            if(id=="B02")
            {d.noticeVi="Sơ đồ nguồn có bốn trạng thái do người học chọn, không có quan hệ nồng độ–thời gian được hiệu chuẩn. Giữ bảng số đếm cùng mã trước/muối/sau thay nước; không vẽ đường đáp ứng nồng độ từ các ảnh này.";d.noticeEn="The supplied images are four learner-selected illustrative states, without a calibrated concentration–time response. Retain same-ID baseline/salt/recovery counts in the table; no concentration-response curve is inferred from these images.";}
            if(d.excluded>0){d.noticeVi+=" "+d.excluded+" lượt thiếu dữ liệu hợp lệ cho trục đã chọn; vẫn giữ trong bảng.";d.noticeEn+=" "+d.excluded+" records lack valid selected-axis data and remain in the table.";}
            return d;
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        static bool Reading(AttemptRecord a,string key,out double value)
        {var r=a.readings?.FirstOrDefault(x=>x!=null&&x.key==key);value=r?.value??0;return r!=null&&Finite(value);}
        static bool TryY(AttemptRecord a,string key,out double value)
        {if(key=="periodSquared"){if(Reading(a,"period",out double period)&&period>0){value=period*period;return Finite(value);}value=0;return false;}return Reading(a,key,out value);}
        static bool TryX(LessonSpec spec,AttemptRecord a,string key,out double value)
        {
            if(spec.id=="P03"&&key=="normalForce")return Reading(a,key,out value)&&value>0;
            if(key=="initialSpeedSquared"){if(TryX(spec,a,"speed",out double speed)&&speed>=0){value=speed*speed;return Finite(value);}value=0;return false;}
            if(key=="externalForceFromMass"){if(TryX(spec,a,"hanger",out double mass)&&mass>0){value=mass*ScienceModels.G;return Finite(value);}value=0;return false;}
            if(key=="inverseTotalMass"){if(TryX(spec,a,"cart",out double cart)&&TryX(spec,a,"hanger",out double hanger)&&cart>0&&hanger>0){value=1/(cart+hanger);return Finite(value);}value=0;return false;}
            int i=Array.FindIndex(spec.parameters,p=>p.key==key);if(i>=0){value=a.parameterValues!=null&&a.parameterValues.Length>i?a.parameterValues[i]:double.NaN;return Finite(value);}return Reading(a,key,out value);
        }
        static bool HasGateObservation(AttemptRecord a)
        {
            var detail=ReadDetail(a);return detail?.gateA!=null&&detail.gateB!=null&&detail.gateA.complete&&detail.gateB.complete&&detail.gateA.blockingSeconds>0&&detail.gateB.blockingSeconds>0&&Finite(detail.gateA.blockingSeconds)&&Finite(detail.gateB.blockingSeconds)&&Finite(detail.gateA.meanSpeed)&&Finite(detail.gateB.meanSpeed)&&Finite(detail.gateA.midpointPosition)&&Finite(detail.gateB.midpointPosition)&&Math.Abs(detail.gateB.midpointPosition-detail.gateA.midpointPosition)>1e-8;
        }
        static string Controls(LessonSpec spec,AttemptRecord a,string independent,out string vi,out string en)
        {
            var key=new StringBuilder(a.model??"unspecified model");var v=new StringBuilder();var e=new StringBuilder();
            void Add(string name,string number,string labelVi,string labelEn,string unit="")
            {key.Append('|').Append(name).Append('=').Append(number);if(v.Length>0){v.Append("; ");e.Append("; ");}v.Append(labelVi).Append('=').Append(number).Append(' ').Append(unit);e.Append(labelEn).Append('=').Append(number).Append(' ').Append(unit);}
            void Missing(string name){Add(name,"unknown:"+(a.attemptId??Guid.NewGuid().ToString("N")),"Thiếu cấu hình "+name,"Missing configuration "+name);}
            bool angleGeometry=spec.id=="P02"&&independent=="angle";
            bool forceSeries=spec.id=="P07"&&independent=="externalForceFromMass",massSeries=spec.id=="P07"&&independent=="inverseTotalMass";
            for(int i=0;i<spec.parameters.Length;i++)
            {
                var p=spec.parameters[i];
                if(p.key==independent||spec.id=="P03"&&independent=="normalForce"&&p.key=="mass"||independent=="initialSpeedSquared"&&p.key=="speed"||independent=="dilutionFactor"&&p.key=="dilution"||independent=="solventFront"&&p.key=="front"||angleGeometry&&p.key=="height"||forceSeries&&(p.key=="cart"||p.key=="hanger")||massSeries&&p.key=="cart")continue;
                string number=a.parameterValues!=null&&a.parameterValues.Length>i&&Finite(a.parameterValues[i])?a.parameterValues[i].ToString("R",Inv):"?";
                Add(p.key,number,p.vi,p.en,p.unit);
            }
            if(forceSeries)
            {if(TryX(spec,a,"cart",out double cart)&&TryX(spec,a,"hanger",out double hanger)&&cart>0&&hanger>0)Add("totalMass",(cart+hanger).ToString("G12",Inv),"Tổng M","Total M","kg");else Missing("totalMass");}
            var detail=ReadDetail(a);
            // Group only stable physical settings, never the entire JSON trace or a timestamp.
            if(spec.id=="M01"&&detail!=null&&detail.gearDriverTeeth>0&&detail.gearDrivenTeeth>0)
            {if(independent!="driver")Add("actualGearDriver",detail.gearDriverTeeth.ToString(Inv),"Răng đầu vào thực","Actual input teeth");if(independent!="driven")Add("actualGearDriven",detail.gearDrivenTeeth.ToString(Inv),"Răng đầu ra thực","Actual output teeth");}
            if(spec.id=="C02")
            {if(detail!=null&&System.Text.RegularExpressions.Regex.IsMatch(a.detailJson,"\"reversedLeads\"\\s*:"))Add("polarity",detail.reversedLeads?"reversed":"normal","Đầu đo","Probe polarity");else Missing("polarity");}
            if(spec.id=="M02")
            {if(Reading(a,"pivotMark",out double pivot))Add("pivot",pivot.ToString("R",Inv),"Vạch điểm tựa","Pivot mark");else if(detail!=null&&System.Text.RegularExpressions.Regex.IsMatch(a.detailJson,"\"pivot\"\\s*:"))Add("pivot",detail.pivot.ToString(Inv),"Vạch điểm tựa","Pivot mark");else Missing("pivot");}
            if(spec.id=="M04")
            {if(detail!=null&&!string.IsNullOrEmpty(detail.beltConfiguration))Add("belt",detail.beltConfiguration,"Bộ đai","Belt configuration");else if(Reading(a,"centreDistance",out double span))Add("beltSpan",span.ToString("R",Inv),"Khoảng tâm đai","Belt centre distance","mm");else Missing("belt");
             // New schema records drive direction as a parameter; legacy details may record it explicitly.
             if(!spec.parameters.Any(p=>p.key=="driveLarge"||p.key=="largeDrive"||p.key=="reverseDrive"))
             {if(detail!=null&&System.Text.RegularExpressions.Regex.IsMatch(a.detailJson,"\"driveLarge\"\\s*:"))Add("driveLarge",detail.driveLarge?"large":"small","Pulley chủ động","Driven input pulley");else Missing("driveLarge");}}
            if(spec.id=="M06")
            {if(detail!=null&&!string.IsNullOrEmpty(detail.followerType))Add("follower",detail.followerType,"Đầu tiếp xúc","Follower contact");else if(Reading(a,"roller",out double roller))Add("follower",roller>.5?"roller":"knife","Đầu tiếp xúc","Follower contact");else Missing("follower");}
            if(spec.id=="M07")
            {if(detail!=null&&!string.IsNullOrEmpty(detail.sampleId))Add("sample",detail.sampleId,"Mã mẫu","Sample ID");else if(Reading(a,"sampleIndex",out double sample))Add("sample",sample.ToString("R",Inv),"Mẫu nguồn","Source sample");else Missing("sample");}
            vi=v.Length==0?"Không có tham số kiểm soát khác":v.ToString();en=e.Length==0?"No other controlled parameters":e.ToString();
            if(angleGeometry){vi+="; độ cao nòng thay theo góc (xem từng lượt)";en+="; muzzle height covaries with angle (inspect each record)";}
            if(a.parameterValues==null||a.parameterValues.Length!=spec.parameters.Length){key.Append("|incomplete-schema:").Append(a.attemptId);vi+="; thiếu sơ đồ tham số";en+="; incomplete parameter schema";}
            return key.ToString();
        }
        static NotebookCurve ActualCurve(string id,AttemptRecord a,string xKey,string yKey)
        {
            var curve=new NotebookCurve{attemptId=a.attemptId};var detail=ReadDetail(a);if(detail==null)return curve;
            void Add(double x,double y,string sample=""){if(Finite(x)&&Finite(y))curve.points.Add(new NotebookPoint{x=x,y=y,attemptId=a.attemptId,sampleId=sample});}
            if((id=="B04"||id=="B05")&&xKey=="@time"&&yKey=="gasVolume"&&detail.gas!=null)
                foreach(var p in detail.gas)if(p!=null&&p.seconds>=detail.reactionAt)Add(p.seconds-detail.reactionAt,p.volumeMl,p.sampleId);
            if((id=="C07"&&yKey=="gasVolume"||id=="C08"&&yKey=="temperature")&&xKey=="@time"&&detail.curve!=null)
                foreach(var p in detail.curve)if(p!=null)Add(p.seconds,p.value);
            if((id=="M05"&&yKey=="sliderPosition"||id=="M06"&&yKey=="lift")&&xKey=="@angle"&&detail.series!=null)
                foreach(var p in detail.series)if(p!=null)Add(p.a,p.b);
            return curve;
        }
        static NotebookFit PendulumFit(NotebookGroup group,LessonSpec spec)
        {
            var fit=new NotebookFit{gravity=double.NaN};var bins=group.summaries;
            fit.eligible=bins.Count>=2&&bins.All(b=>b.n>=3);
            fit.smallAngle=group.records.All(a=>TryX(spec,a,"angle",out double angle)&&Math.Abs(angle)<=10);
            fit.vi="Cần ít nhất 2 chiều dài và ≥3 lượt riêng mỗi chiều dài, cùng khối lượng/góc/số chu kỳ. T² lấy từ từng chu kỳ đo đã lưu.";
            fit.en="Collect ≥2 lengths with ≥3 separate attempts at each length, holding mass, angle and cycle count fixed. T² uses each saved measured period.";
            if(!fit.eligible)return fit;
            double mx=bins.Average(b=>b.x),my=bins.Average(b=>b.mean),ssx=bins.Sum(b=>(b.x-mx)*(b.x-mx));if(ssx<=0)return fit;
            fit.slope=bins.Sum(b=>(b.x-mx)*(b.mean-my))/ssx;fit.intercept=my-fit.slope*mx;
            double total=bins.Sum(b=>(b.mean-my)*(b.mean-my)),residual=bins.Sum(b=>Math.Pow(b.mean-fit.intercept-fit.slope*b.x,2));fit.rSquared=total>0?1-residual/total:0;
            if(fit.slope>0&&fit.smallAngle)fit.gravity=4*Math.PI*Math.PI/fit.slope;
            fit.en="Fit of observed mean T² to L: slope="+F(fit.slope)+" s²/m; intercept="+F(fit.intercept)+" s²; R²="+F(fit.rSquared)+". "+(Finite(fit.gravity)?"g="+F(fit.gravity)+" m/s². ":"No valid small-angle gravity estimate. ")+"Small-angle approximation requires release angle ≤10° here; nonlinear periods at larger angles must be interpreted separately. This is not a physical calibration.";
            fit.vi="Hồi quy trung bình T² đã đo theo L: hệ số góc="+F(fit.slope)+" s²/m; giao điểm="+F(fit.intercept)+" s²; R²="+F(fit.rSquared)+". "+(Finite(fit.gravity)?"g="+F(fit.gravity)+" m/s². ":"Chưa có ước lượng g hợp lệ cho góc nhỏ. ")+"Xấp xỉ góc nhỏ ở đây yêu cầu góc thả ≤10°; chu kỳ phi tuyến góc lớn cần diễn giải riêng. Đây không phải hiệu chuẩn thực tế.";return fit;
        }
        static string F(double value)=>value.ToString("G5",Inv);
        public static void Render(VlabUi ui,RectTransform host,NotebookData data,string selectedGroup=null,Action<string,string,string> selectionChanged=null)
        {
            bool vi=ui.Vietnamese;float width=host.rect.width>100?host.rect.width:1200,height=host.rect.height>100?host.rect.height:720;
            var group=data.groups.FirstOrDefault(g=>g.key==selectedGroup)??data.groups.FirstOrDefault();
            void Change(string x,string y,string g)=>selectionChanged?.Invoke(x,y,g);
            string Next(List<NotebookAxis> choices,string current){if(choices.Count==0)return current;int i=choices.FindIndex(a=>a.key==current);return choices[(i+1)%choices.Count].key;}
            float half=(width-24)/2;
            if(!data.tableOnly)
            {
                var xb=ui.Button(host,"X: "+data.x.Label(vi),0,0,half,56,()=>Change(Next(data.xChoices,data.independentKey),data.dependentKey,group?.key));xb.interactable=selectionChanged!=null&&data.xChoices.Count>1;
                var yb=ui.Button(host,"Y: "+data.y.Label(vi),half+24,0,half,56,()=>Change(data.independentKey,Next(data.yChoices,data.dependentKey),group?.key));yb.interactable=selectionChanged!=null&&data.yChoices.Count>1;
            }
            int groupIndex=group==null?-1:data.groups.IndexOf(group);
            var gb=ui.Button(host,(vi?"Nhóm điều kiện ":"Controlled group ")+(groupIndex+1)+" / "+data.groups.Count,0,68,Math.Min(310,width),56,
                ()=>Change(data.independentKey,data.dependentKey,data.groups[(groupIndex+1)%data.groups.Count].key));gb.interactable=selectionChanged!=null&&data.groups.Count>1;
            ui.Text(host,group==null?(vi?"Chưa có lượt đã lưu.":"No saved attempts."):(vi?group.vi:group.en),330,70,Math.Max(100,width-330),74,22,VlabUi.Secondary);
            string summary=data.experimentId=="P02"?(vi?"Độ cao nòng thay theo góc; không suy ra quy tắc hai góc bù. Chọn trục v₀² khi giữ góc và độ cao.":"Muzzle height covaries with angle; do not infer complementary-angle equality. Choose v₀² with fixed angle and height."):
                data.experimentId=="P07"?(vi?"Ngoại lực: giữ tổng M. Trục 1/M: giữ tải treo. Chỉ vẽ gia tốc có hai khoảng che cổng.":"Force: hold total M. Axis 1/M: hold hanger mass. Plot acceleration only with two saved gate intervals."):
                data.experimentId=="B02"?(vi?"Bốn ảnh sơ đồ không mô phỏng đáp ứng nồng độ. Giữ bảng số đếm cùng mã tế bào.":"Four schematic images do not model concentration response. Retain counts from the same cell IDs."):
                data.curveMode?(vi?"Chỉ nối các mốc đã lưu; mỗi lượt giữ đường riêng. Xem điều kiện và kết quả bên dưới.":"Only saved samples are connected; each attempt keeps its own trace. Conditions and results appear below."):
                (vi?"Mỗi nhóm giữ điều kiện khác cố định. Khoảng min–max không phải khoảng tin cậy. Xem giải thích và số đo bên dưới.":"Each group holds other conditions fixed. Min–max is not a confidence interval. Explanation and readings appear below.");
            ui.Text(host,summary,0,144,width,76,22,VlabUi.Secondary);
            float tableTop=228;
            var points=group==null?new List<NotebookPoint>():data.curveMode?group.curves.SelectMany(c=>c.points).ToList():group.points;
            bool enough=points.Select(p=>p.x).Distinct().Count()>=2;
            if(!data.tableOnly&&enough)
            {
                float chartHeight=Math.Min(230,Math.Max(120,height-470)),plotWidth=Math.Max(200,width-110);
                double xmin=points.Min(p=>p.x),xmax=points.Max(p=>p.x),ymin=points.Min(p=>p.y),ymax=points.Max(p=>p.y);
                double padding=Math.Abs(ymax-ymin)<1e-12?Math.Max(1,Math.Abs(ymax)*.1):(ymax-ymin)*.1;ymin-=padding;ymax+=padding;
                var texture=Scatter(group,data.curveMode,xmin,xmax,ymin,ymax);var raw=ui.Picture(host,texture,82,226,plotWidth,chartHeight);
                raw.gameObject.AddComponent<NotebookPlotLifetime>().owned=texture;
                ui.Text(host,data.y.Label(vi),0,218,width,30,22);
                ui.Text(host,F(ymax),0,250,78,28,22,VlabUi.Secondary);ui.Text(host,F(ymin),0,226+chartHeight-26,78,28,22,VlabUi.Secondary);
                ui.Text(host,F(xmin),82,234+chartHeight,120,40,22,VlabUi.Secondary);ui.Text(host,F(xmax),width-160,234+chartHeight,140,40,22,VlabUi.Secondary);
                ui.Text(host,data.x.Label(vi),82,280+chartHeight,plotWidth,40,22);
                tableTop=320+chartHeight;
            }
            else if(!data.tableOnly)
            {
                ui.Text(host,vi?"Cần ít nhất hai giá trị X khác nhau trong cùng nhóm điều kiện. Hãy thu thêm số đo; sổ không tạo dữ liệu thay bạn.":"Collect at least two distinct X values in the same controlled group. This notebook does not generate missing measurements.",0,226,width,66,22);tableTop=306;
            }
            var text=new StringBuilder();
            text.AppendLine(vi?data.noticeVi:data.noticeEn).AppendLine();
            if(group?.fit!=null)text.AppendLine(vi?group.fit.vi:group.fit.en).AppendLine();
            if(group!=null&&!data.curveMode&&!data.tableOnly)
            {
                text.AppendLine((vi?"X | trung bình Y | n riêng biệt | min–max":"X | mean Y | distinct n | min–max")+"\n");
                foreach(var b in group.summaries)text.AppendLine(F(b.x)+" | "+F(b.mean)+" | "+b.n+" | "+F(b.min)+"–"+F(b.max));text.AppendLine();
            }
            if(group!=null&&data.curveMode)
            {
                int legend=0;foreach(var curve in group.curves){string hex=ColorUtility.ToHtmlStringRGB(Ink[legend++%Ink.Length]);text.AppendLine("<color=#"+hex+">●</color> "+(vi?"Lượt ":"Attempt ")+Short(curve.attemptId)+": "+curve.points.Count+(vi?" mốc đã lưu":" saved points"));}
            }
            text.AppendLine(vi?"Kết quả gốc đã lưu:":"Original saved results:");
            foreach(var a in group?.records??data.rows)
            {
                text.AppendLine("\n"+Short(a.attemptId)+" · "+(a.savedAtUtc??"")+" · "+(a.model??""));
                foreach(var r in ObservationCards.PresentationReadings((a.readings??new List<Reading>()).Where(r=>r!=null),a.experimentId))text.AppendLine((vi?r.labelVi??r.key:r.labelEn??r.key)+": "+(r.unit=="presence"?ObservationCards.FormatValue(r,vi):F(r.value))+" "+ObservationCards.DisplayUnit(r));
            }
            if(data.rows.Count==0)text.AppendLine(vi?"Hoàn thành và lưu một lượt để bắt đầu so sánh.":"Complete and save an attempt to begin comparing.");
            ui.Scroll(host,text.ToString(),0,tableTop,width,Math.Max(150,height-tableTop),22);
        }
        static string Short(string id)=>string.IsNullOrEmpty(id)?"?":id.Substring(0,Math.Min(10,id.Length));
        static readonly Color32[] Ink={new Color32(99,215,250,255),new Color32(255,192,98,255),new Color32(168,234,169,255),new Color32(227,174,244,255)};
        // Standard Cartesian scatterplot: numeric axis text is uGUI/TMP, not rasterized tiny lettering.
        static Texture2D Scatter(NotebookGroup group,bool curves,double xmin,double xmax,double ymin,double ymax)
        {
            const int w=1000,h=320;var pixels=new Color32[w*h];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(16,29,50,255);
            void Pixel(int x,int y,Color32 color){if(x>=0&&x<w&&y>=0&&y<h)pixels[y*w+x]=color;}
            void Line(int x0,int y0,int x1,int y1,Color32 color)
            {int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;while(true){Pixel(x0,y0,color);if(x0==x1&&y0==y1)break;int twice=2*err;if(twice>=dy){err+=dy;x0+=sx;}if(twice<=dx){err+=dx;y0+=sy;}}}
            int X(double x)=>(int)Math.Round(8+(x-xmin)/(xmax-xmin)*(w-17));int Y(double y)=>(int)Math.Round(8+(y-ymin)/(ymax-ymin)*(h-17));
            for(int i=0;i<=4;i++){int x=8+(w-17)*i/4,y=8+(h-17)*i/4;Line(x,8,x,h-9,new Color32(44,65,88,255));Line(8,y,w-9,y,new Color32(44,65,88,255));}
            void Dot(NotebookPoint p,Color32 color){int x=X(p.x),y=Y(p.y);for(int j=-4;j<=4;j++)for(int i=-4;i<=4;i++)if(i*i+j*j<=16)Pixel(x+i,y+j,color);}
            if(curves)
            {
                int index=0;foreach(var curve in group.curves){var color=Ink[index++%Ink.Length];NotebookPoint last=null;foreach(var p in curve.points){if(last!=null&&p.x>=last.x)Line(X(last.x),Y(last.y),X(p.x),Y(p.y),color);Dot(p,color);last=p;}}
            }
            else
            {
                foreach(var point in group.points)Dot(point,Ink[0]);
                foreach(var summary in group.summaries){int x=X(summary.x);Line(x,Y(summary.min),x,Y(summary.max),Ink[1]);Dot(new NotebookPoint{x=summary.x,y=summary.mean},Ink[1]);}
            }
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){name="Saved experiment observations",filterMode=FilterMode.Bilinear};texture.SetPixels32(pixels);texture.Apply(false,false);return texture;
        }
    }
    public sealed class NotebookPlotLifetime:MonoBehaviour
    {public Texture2D owned;void OnDestroy(){if(owned)Destroy(owned);}}
}
