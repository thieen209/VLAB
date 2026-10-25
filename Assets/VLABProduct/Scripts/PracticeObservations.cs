using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ChemistryDriver
    {
        public override ModelResult RecordableObservation()
        {
            var result=Observe();
            if(id=="C01"&&CanRecord(out _))
            {
                var valid=titres.Where(t=>t.valid).ToArray();double mean=valid.Skip(valid.Length-3).Average(t=>t.delivered);
                result.Add("meanTitre",mean,"mL","Trung bình ba titre","Mean of three titres").Add("acidConcentration",P("base")*mean/10,"mol/L","Nồng độ giấm pha loãng","Diluted vinegar concentration");
            }
            return result;
        }
        public override ModelResult ProgressObservation()
        {
            if(!AllInstalled)return null;var r=new ModelResult();
            if(id=="C01")
            {
                if(initialRead)r.Add("initialMeniscus",initialMeniscus,"mL","Mặt khum đầu đã đọc","Read initial meniscus");
                if(finalRead)r.Add("finalMeniscus",finalMeniscusRead,"mL","Mặt khum cuối đã đọc","Read final meniscus");
                for(int n=0;n<titres.Count;n++)r.Add("titre"+n,titres[n].delivered,"mL","Lượt "+(n+1)+(titres[n].valid?" · hợp lệ":" · bị loại"),"Titre "+(n+1)+(titres[n].valid?" · valid":" · rejected"));
                r.detailJson=JsonUtility.ToJson(new Draft{lesson=id,titres=titres});
            }
            else if(id=="C03")
            {
                if(initialWeighed)r.Add("initialMass",initialMass,"g","Khối lượng cân đầu","Initial measured mass");
                if(finalWeighed)r.Add("finalMass",finalMass,"g","Khối lượng cân cuối","Final measured mass");
            }
            else if(id=="C04")
            {
                if(naclUnits>0)r.Add("NaClSolid",naclSolid?1:0,"presence","NaCl: cặn trắng","NaCl: white solid");
                if(nitrateUnits>0)r.Add("NaNO3Solid",0,"presence","NaNO3: cặn trắng","NaNO3: white solid");
                if(waterUnits>0)r.Add("waterSolid",0,"presence","Nước: cặn trắng","Water: white solid");
            }
            else if(id=="C06")
            {
                if(waterObserved)r.Add("waterControlPh",waterPh,"","pH nước đã đo","Measured water pH");
                var observed=Observe();foreach(var value in observed.readings.Where(x=>x.key=="pH"||x.key=="BTBstate"))r.readings.Add(value);
            }
            else if(id=="C07")
            {
                for(int n=0;n<curve.Count;n++)r.Add("time"+n,curve[n].seconds,"s","Thời gian mốc "+(n+1),"Point "+(n+1)+" time").Add("volume"+n,curve[n].value,"mL","Thể tích mốc "+(n+1),"Point "+(n+1)+" volume");
                r.detailJson=JsonUtility.ToJson(new Details{lesson=id,curve=curve});
            }
            else if(id=="C08")
            {
                if(acidTempSeen)r.Add("acidTemperature",V("acidC",25),"°C","Nhiệt độ acid đã đọc","Measured acid temperature");
                if(baseTempSeen)r.Add("baseTemperature",V("baseC",25),"°C","Nhiệt độ base đã đọc","Measured base temperature");
            }
            return r.readings.Count>0?r:null;
        }
    }
    public sealed partial class BiologyDriver
    {
        public override ModelResult ProgressObservation()
        {
            var r=new ModelResult();
            if(Id=="B03")
            {
                for(int n=0;n<s.samples.Count;n++)
                {
                    var sample=s.samples[n];string vi=" mẫu "+(n+1),en=" sample "+(n+1);
                    if(sample.initialRead)r.Add("initialMass"+(n+1),sample.initial,"g","Khối lượng đầu"+vi,"Initial mass"+en);
                    if(sample.finalRead)r.Add("finalMass"+(n+1),sample.final,"g","Khối lượng cuối"+vi,"Final mass"+en);
                }
            }
            else if(Id=="B04"||Id=="B05")
            {
                for(int n=0;n<s.gas.Count;n++)
                {
                    var p=s.gas[n];r.Add("time"+n,p.seconds,"s","Thời gian mốc "+(n+1),"Point "+(n+1)+" time").Add("volume"+n,p.volumeMl,"mL","Thể tích mốc "+(n+1),"Point "+(n+1)+" volume").Add("temperature"+n,p.sampleC,"°C","Nhiệt độ mẫu mốc "+(n+1),"Point "+(n+1)+" sample temperature");
                }
            }
            else if(Id=="B01"&&s.structures.Count>0)r.Add("classifiedStructures",s.structures.Count,"","Vùng đã tự phân loại","Regions classified by learner");
            else if(Id=="B08"&&s.cells.Count>0)r=Observe();
            else if(Id=="B02"&&s.cells.Any(x=>x.baseline>=0))
            {
                r.Add("baselineClassified",s.cells.Count(x=>x.baseline>=0),"","Tế bào đã phân loại ban đầu","Baseline cells classified");
                if(s.saltAdded)r.Add("saltClassified",s.cells.Count(x=>x.salt>=0),"","Tế bào đã phân loại trong muối","Salt cells classified");
                if(s.waterAdded)r.Add("recoveryClassified",s.cells.Count(x=>x.recovery>=0),"","Tế bào đã phân loại sau thay nước","Recovery cells classified");
            }
            // Only sampled observations enter the draft; a changing simulation clock is not a new reading.
            r.detailJson=JsonUtility.ToJson(new BiologyLessonState{lessonId=Id,structures=s.structures,cells=s.cells,samples=s.samples,gas=s.gas});
            return r.readings.Count>0?r:null;
        }
    }
}
