using System;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ChemistryDriver
    {
        [Serializable] public sealed class QualitativeDraft
        {
            public int source=12,loaded,nacl,nitrate,water;
            public string target="";public bool labelled,selected;public double appearanceSeconds;
        }
        QualitativeDraft restoredQualitative;
        int dropperUnits;string dropperTarget="",lastDropperCup="";bool manualDropperActive;
        public bool CanManualDropper=>id=="C04"&&AllInstalled&&configured&&labels&&!failed&&session.Phase==ExperimentPhase.Preparing&&qualitativeSelection==1;
        public int CompletedDropperOperations=>!labels?0:nitrateUnits<2||waterUnits<2?1:naclUnits<2?2:3;
        public int LoadedDropperUnits=>dropperUnits;
        public string LastDropperCup=>lastDropperCup;
        public string ActiveDropperCup=>dropperTarget;
        int CupUnits(string cup)=>cup=="nacl"?naclUnits:cup=="nitrate"?nitrateUnits:cup=="water"?waterUnits:-1;
        public int QualitativeDoseUnits(string cup)=>CupUnits(cup);
        public bool BeginManualDropper(out string message)
        {
            message="";if(!CanManualDropper){message="Select AgNO3 before loading the dropper from the open source.";return false;}
            manualDropperActive=true;return true;
        }
        public void StopManualDropper()
        {manualDropperActive=false;if(Has("dropper")){var drop=I<VlabDropper>("dropper");if(drop)drop.SetSqueeze(0);}}
        bool DropperInSource()
        {
            var source=I<VlabReagentBottle>("reagent");var drop=I<VlabDropper>("dropper");
            if(!source.CapOpen)return false;
            var axis=drop.EmissionDirection;var side=Vector3.Cross(axis,drop.transform.right).normalized;
            for(int n=0;n<8;n++){float angle=n*Mathf.PI/4;var point=drop.Tip.position+.00045f*(drop.transform.right*Mathf.Cos(angle)+side*Mathf.Sin(angle));if(!source.Liquid.ContainsPoint(point,true))return false;}
            return true;
        }
        bool DropperOverCup(string cup)
        {
            var drop=I<VlabDropper>("dropper");var receiver=I<VlabBeaker>(cup);var origin=drop.Tip.position;var direction=drop.EmissionDirection;
            if(Vector3.Dot(direction,Vector3.down)<.98f)return false;
            // The beaker's delivered area/capacity defines its true rim height.
            double rim=receiver.InteriorBottom+receiver.CapacityMl*1e-6/receiver.CrossSectionArea;
            float fall=(receiver.transform.position.y+(float)rim-origin.y)/direction.y;
            if(fall<.005f||fall>.35f)return false;
            var landing=origin+direction*fall-Vector3.up*.001f;var cavity=BeakerRegion(cup,true);Physics.SyncTransforms();
            for(int n=0;n<8;n++){float angle=n*Mathf.PI/4;var edge=landing+.00045f*new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));if((cavity.ClosestPoint(edge)-edge).sqrMagnitude>1e-12f)return false;}
            foreach(var contact in Physics.OverlapSphere(origin,.00045f,~0,QueryTriggerInteraction.Ignore))if(!contact.transform.IsChildOf(drop.transform))return false;
            float travel=(receiver.transform.position.y+(float)receiver.SurfaceHeightMeters-origin.y)/direction.y;
            foreach(var hit in Physics.SphereCastAll(origin,.00045f,direction,Mathf.Max(0,travel-.002f),~0,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(drop.transform))return false;
            return true;
        }
        public Vector3 AssistedDropperTipTarget(Vector3 above)
        {
            var source=I<VlabReagentBottle>("reagent");var mouth=Named("reagent","_Mouth_SOCKET");
            var point=new Vector3(above.x,mouth.position.y-.002f,above.z);
            if(source.CapOpen&&source.Liquid.ContainsPoint(point,false,.0005f))return new Vector3(above.x,source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55).y,above.z);
            return above;
        }
        public int TransferManualDropper(out string message)
        {
            message="The simulated drops are uncalibrated; no mL or mol is inferred.";lastDropperCup="";
            if(!manualDropperActive||!CanManualDropper)return 0;
            var drop=I<VlabDropper>("dropper");drop.SetSqueeze(0);
            if(DropperInSource())
            {
                int deliveredToTarget=dropperTarget==""?0:CupUnits(dropperTarget);
                if(unitsLeft<=0||dropperUnits+deliveredToTarget>=2){message="The dropper already contains the remaining prescribed simulated dose.";return 0;}
                unitsLeft--;dropperUnits++;message="One uncalibrated simulated drop loaded from the actual source.";return 1;
            }
            if(dropperUnits==0){message="Immerse the tip in the open AgNO3 source to load simulated drops.";return 0;}
            foreach(string cup in new[]{"nacl","nitrate","water"})
            {
                if(!DropperOverCup(cup))continue;
                int existing=CupUnits(cup);if(existing>=2||dropperTarget!=""&&dropperTarget!=cup){message="Finish the current sample's two simulated drops before selecting another sample.";return 0;}
                var receiver=I<VlabBeaker>(cup);if(receiver.VolumeMl>=receiver.CapacityMl){message="The receiving cup is full.";return 0;}
                var solid=I<VlabPrecipitate>(cup=="nacl"?"solidNaCl":cup=="nitrate"?"solidNitrate":"solidWater");
                if(existing==0&&!solid.TrySetQualitative(false)){message="The sample's precipitate display is unavailable.";return 0;}
                dropperUnits--;dropperTarget=cup;lastDropperCup=cup;drop.SetSqueeze(1);
                if(cup=="nacl"){naclUnits++;if(existing==0){naclSolid=false;precipitateSeconds=0;}}
                else if(cup=="nitrate")nitrateUnits++;else waterUnits++;
                if(existing+1==2){qualitativeSelection=0;dropperTarget="";manualDropperActive=false;}
                message="One simulated drop accepted by the actual "+cup+" cup. Volume is uncalibrated.";return 1;
            }
            message="Point the actual dropper tip down through an unobstructed sample mouth.";return 0;
        }
        void DoseAtActualTip(string cup)
        {
            Must(CupUnits(cup)>=0&&DropperOverCup(cup),"Position the actual tip over this cup before dispensing.");
            Must(BeginManualDropper(out var why),why);
            try{Must(TransferManualDropper(out why)==1,why);}finally{StopManualDropper();}
        }
        void AssistQualitativeDose(string cup)
        {
            Must(id=="C04"&&labels&&CupUnits(cup)>=0&&CupUnits(cup)<2&&(dropperTarget==""||dropperTarget==cup),"Complete the current sample before selecting a new qualitative dose.");
            int needed=2-CupUnits(cup);Must(unitsLeft+dropperUnits>=needed,"Not enough simulated reagent remains.");
            var source=I<VlabReagentBottle>("reagent");Must(source.CapOpen,"Open the reagent cap first.");
            var drop=I<VlabDropper>("dropper");var position=drop.transform.position;var rotation=drop.transform.rotation;
            qualitativeSelection=1;Must(BeginManualDropper(out var why),why);session.Assistance="Assisted qualitative dropper positioning; simulated drops with uncalibrated volume";
            try
            {
                drop.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*drop.transform.rotation;
                drop.transform.position+=source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55)-drop.Tip.position;
                while(dropperUnits<needed)Must(TransferManualDropper(out why)==1,why);
                var receiver=I<VlabBeaker>(cup);float rim=receiver.transform.position.y+(float)(receiver.InteriorBottom+receiver.CapacityMl*1e-6/receiver.CrossSectionArea);
                drop.transform.position+=new Vector3(receiver.transform.position.x,rim+.05f,receiver.transform.position.z)-drop.Tip.position;
                while(CupUnits(cup)<2)Must(TransferManualDropper(out why)==1,why);
            }
            finally{StopManualDropper();drop.transform.SetPositionAndRotation(position,rotation);}
        }
        QualitativeDraft CaptureQualitative()=>new QualitativeDraft{source=unitsLeft,loaded=dropperUnits,nacl=naclUnits,nitrate=nitrateUnits,water=waterUnits,target=dropperTarget,labelled=labels,selected=qualitativeSelection==1,appearanceSeconds=precipitateSeconds};
        bool ValidQualitative(QualitativeDraft d)
        {
            if(d==null||d.source<0||d.source>12||d.loaded<0||d.loaded>2||d.nacl<0||d.nacl>2||d.nitrate<0||d.nitrate>2||d.water<0||d.water>2||d.source+d.loaded+d.nacl+d.nitrate+d.water!=12||!Finite(d.appearanceSeconds)||d.appearanceSeconds<0||d.appearanceSeconds>3)return false;
            if(!d.labelled&&(d.source!=12||d.selected)||d.nacl==0&&d.appearanceSeconds!=0||!d.selected&&(d.loaded>0||d.target!=""))return false;
            int partial=(d.nacl==1?1:0)+(d.nitrate==1?1:0)+(d.water==1?1:0);
            if(d.target=="")return partial==0;
            int dose=d.target=="nacl"?d.nacl:d.target=="nitrate"?d.nitrate:d.target=="water"?d.water:-1;
            return d.selected&&partial==1&&dose==1&&d.loaded<=1;
        }
        void RestoreQualitative()
        {
            var d=restoredQualitative;restoredQualitative=null;if(id!="C04"||!ValidQualitative(d))return;
            unitsLeft=d.source;dropperUnits=d.loaded;naclUnits=d.nacl;nitrateUnits=d.nitrate;waterUnits=d.water;dropperTarget=d.target;labels=d.labelled;qualitativeSelection=d.selected?1:0;precipitateSeconds=d.appearanceSeconds;naclSolid=naclUnits>0&&precipitateSeconds>0;
            I<VlabPrecipitate>("solidNaCl").TrySetQualitative(naclSolid,(float)(.8*precipitateSeconds/3));StopManualDropper();
        }
    }
}
