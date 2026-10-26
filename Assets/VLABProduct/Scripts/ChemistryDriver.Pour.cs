using System;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ChemistryDriver
    {
        string pouringSource="";bool calProbeTracking;double calObservedSeconds,calObservedPeak=double.NaN;
        public bool GasReceiverOpen=>id=="C07"&&AllInstalled&&!gasSealed&&Vector3.Distance(Named("stopper","_Seat_SOCKET").position,Named("flask","_NeckSocket_SOCKET").position)>.04f;
        public bool GasAcidComplete=>id=="C07"&&AllInstalled&&Math.Abs(I<VlabFlask>("flask").VolumeMl-P("volume"))<1e-7;
        public bool CanManualPour(string sourceKey)=>AllInstalled&&configured&&!failed&&(id=="C08"&&(sourceKey=="acidCup"||sourceKey=="baseCup")&&prepared==3&&session.Phase==ExperimentPhase.Running&&!paused&&I<VlabBeaker>(sourceKey).VolumeMl>1e-8||id=="C07"&&sourceKey=="acid"&&prepared==0&&session.Phase==ExperimentPhase.Preparing&&!contact&&!gasSealed&&!GasAcidComplete&&I<VlabReagentBottle>("acid").Liquid.VolumeMl>1e-8);
        void OpenGasFlask()
        {
            Must(id=="C07"&&prepared==0&&!contact&&!gasSealed,"Open the reaction flask before adding acid.");
            if(GasReceiverOpen)return;
            var item=equipment["stopper"];var previous=item.transform.position;var rig=item.GetComponentInParent<ApparatusRig>();
            foreach(var offset in new[]{new Vector3(-.12f,0,-.12f),new Vector3(.12f,0,.12f),new Vector3(-.12f,0,.12f),new Vector3(.12f,0,-.12f)})
            {
                item.transform.position=equipment["flask"].transform.position+offset;
                if(rig&&rig.TryRestInstalledTool(item,out _)){session.Assistance="Stopper placement assistance";return;}
            }
            item.transform.position=previous;throw new InvalidOperationException("Clear a supported place beside the flask for its stopper.");
        }
        public bool BeginManualPour(string sourceKey,out string message)
        {
            message="";if(!CanManualPour(sourceKey)){message="Start the prepared trial before pouring a remaining measured solution.";return false;}
            pouringSource=sourceKey;return true;
        }
        public void StopManualPour(){pouringSource="";}
        public Transform PourOutlet(string sourceKey)=>Named(sourceKey,"_PourSpout_SOCKET");
        LiquidPourTransfer.Vessel PourSource(string key)
        {
            var source=I<VlabBeaker>(key);var spout=PourOutlet(key);var local=source.transform.InverseTransformPoint(spout.position);
            return new LiquidPourTransfer.Vessel{liquid=source,localUp=Vector3.back,localMouth=new Vector3(0,0,local.z),outlet=spout,mouthRadiusM=(float)Math.Sqrt(source.CrossSectionArea/Math.PI),open=true};
        }
        LiquidPourTransfer.Vessel PourReceiver()
        {
            var cal=I<VlabCalorimeter>("cal");var vessel=cal.Vessel;
            return new LiquidPourTransfer.Vessel{liquid=vessel,localUp=Vector3.back,localMouth=vessel.transform.InverseTransformPoint(Named("cal","_LidClosed_SOCKET").position),mouthRadiusM=(float)Math.Sqrt(vessel.CrossSectionArea/Math.PI),open=cal.LidOpen};
        }
        public double TransferManualPour(double requestedMl,out string message)
        {
            message="Pouring stopped.";if(pouringSource==""||!CanManualPour(pouringSource))return 0;
            if(id=="C07")
            {
                var bottle=I<VlabReagentBottle>("acid");var flask=I<VlabFlask>("flask");
                if(!Finite(requestedMl)||requestedMl<=0)return 0;
                double dose=Math.Min(requestedMl,Math.Max(0,P("volume")-flask.VolumeMl));
                if(bottle.Liquid.TryPourInto(flask,dose,bottle.CapOpen,GasReceiverOpen,out var accepted)){message="Acid received: "+flask.VolumeMl.ToString("0.0")+" mL";return accepted;}
                message="Open the flask and source; tilt the actual mouth above an unobstructed receiver.";return 0;
            }
            var cal=I<VlabCalorimeter>("cal");double allowed=LiquidPourTransfer.AllowedVolume(PourSource(pouringSource),PourReceiver(),requestedMl,out message);if(allowed<=0)return 0;
            bool acid=pouringSource=="acidCup";double moved=cal.TryPourFrom(I<VlabBeaker>(pouringSource),acid,V(acid?"acidM":"baseM",.5),V(acid?"acidC":"baseC",25),allowed,out message);
            if(moved>0){calObservedSeconds=0;calObservedPeak=double.NaN;lastSample=clock-10;}
            return moved;
        }
        bool CalDoseComplete()
        {
            var c=I<VlabCalorimeter>("cal");return Math.Abs(c.AcidReceivedMl-V("acidMl",25))<1e-7&&Math.Abs(c.BaseReceivedMl-V("baseMl",25))<1e-7;
        }
        bool CalSensorImmersed()=>I<VlabCalorimeter>("cal").Vessel.VolumeMl>0&&I<VlabThermometer>("thermometer").Immersed(CalRegion());
        void ObserveCalorimeter()
        {
            Must(id=="C08"&&session.Phase==ExperimentPhase.Running&&I<VlabCalorimeter>("cal").Vessel.VolumeMl>0,"Pour some measured solution before immersing the thermometer.");
            var c=I<VlabCalorimeter>("cal");var point=c.Vessel.transform.position;point.y=c.Vessel.transform.position.y+(float)((c.Vessel.InteriorBottom+c.Vessel.SurfaceHeightMeters)*.5);PlaceThermometer(point);
            Must(I<VlabThermometer>("thermometer").TryObserve(CalRegion(),(float)c.TemperatureC),"The entire thermometer sensor must be in the actual liquid.");calProbeTracking=true;
            session.Assistance+=(session.Assistance==""?"":"; ")+"Thermometer positioning assistance";
        }
        void AdvanceCalorimeter(double seconds)
        {
            var c=I<VlabCalorimeter>("cal");c.Advance(seconds);
            bool observed=calProbeTracking&&c.Vessel.VolumeMl>0&&I<VlabThermometer>("thermometer").TryObserve(CalRegion(),(float)c.TemperatureC);
            if(!observed){calObservedSeconds=0;return;}
            if(CalDoseComplete()){calObservedSeconds+=seconds;calObservedPeak=double.IsNaN(calObservedPeak)?c.TemperatureC:Math.Max(calObservedPeak,c.TemperatureC);}
            if(clock+seconds-lastSample>=10-1e-8){curve.Add(new Point{seconds=clock+seconds,value=I<VlabThermometer>("thermometer").ReadingC});lastSample=clock+seconds;}
        }
        ModelResult ObserveCalorimetry()
        {
            var c=I<VlabCalorimeter>("cal");var r=R("Finite actual pours; SDK strong-acid/base model: 57 kJ/mol, density 1 g/mL, c=4.18 J/(g K), illustrative 2 s mixing. Thermal state restarts safely on reopening; saved readings and history remain in the notebook.");
            r.Add("acidReceived",c.AcidReceivedMl,"mL","Acid đã nhận","Accepted acid").Add("baseReceived",c.BaseReceivedMl,"mL","Base đã nhận","Accepted base").Add("acidRemaining",I<VlabBeaker>("acidCup").VolumeMl,"mL","Acid còn lại","Acid remaining").Add("baseRemaining",I<VlabBeaker>("baseCup").VolumeMl,"mL","Base còn lại","Base remaining").Add("receivedVolume",c.Vessel.VolumeMl,"mL","Thể tích trong cốc","Receiver volume").Add("acidMoles",c.AcidMoles,"mol","Mol acid đã nhận","Accepted acid moles").Add("baseMoles",c.BaseMoles,"mol","Mol base đã nhận","Accepted base moles").Add("releasedHeat",c.ReleasedHeatJ,"J","Nhiệt mô hình đã tỏa","Model heat released").Add("lostHeat",c.LostHeatJ,"J","Nhiệt mô hình thất thoát","Model heat lost").Add("reactedAmount",c.ReactedMoles,"mol","Mol phản ứng trong mô hình","Model reacted amount").Add("heatCapacity",c.TotalHeatCapacity,"J/K","Tổng nhiệt dung","Total heat capacity").Add("initialTemperature",c.InitialTemperatureC,"°C","Nhiệt độ nền theo nhiệt dung","Sensible-heat baseline").Add("observationTime",calObservedSeconds,"s","Thời gian quan sát sau lần rót cuối","Observation after last pour");
            if(calProbeTracking&&CalSensorImmersed()&&!float.IsNaN(I<VlabThermometer>("thermometer").ReadingC))r.Add("temperature",I<VlabThermometer>("thermometer").ReadingC,"°C","Nhiệt kế đã đọc","Observed thermometer");
            if(!double.IsNaN(calObservedPeak)){r.Add("peakTemperature",calObservedPeak,"°C","Nhiệt độ quan sát cao nhất sau lần rót cuối","Observed peak after last pour");if(c.LimitingMoles>0)r.Add("apparentEnthalpy",-c.TotalHeatCapacity*(calObservedPeak-c.InitialTemperatureC)/c.LimitingMoles/1000,"kJ/mol","Enthalpy biểu kiến từ số đọc","Apparent enthalpy from readings");}
            return r;
        }
        void AssistPourBoth()
        {
            Must(id=="C08"&&session.Phase==ExperimentPhase.Running,"Start the empty prepared calorimeter before using pour assistance.");session.Assistance+=(session.Assistance==""?"":"; ")+"Assisted calorimeter source positioning";
            foreach(string key in new[]{"acidCup","baseCup"})
            {
                if(I<VlabBeaker>(key).VolumeMl<=1e-8)continue;var item=equipment[key];var position=item.transform.position;var rotation=item.transform.rotation;
                try{var source=I<VlabBeaker>(key);var spout=PourOutlet(key);item.transform.rotation=Quaternion.Euler(180,0,0);var mouth=PourReceiver();item.transform.position+=mouth.liquid.transform.TransformPoint(mouth.localMouth)+Vector3.up*.12f-spout.position;Must(BeginManualPour(key,out var why),why);double expected=source.VolumeMl;Must(Math.Abs(TransferManualPour(expected,out why)-expected)<1e-8,why);}
                finally{StopManualPour();item.transform.SetPositionAndRotation(position,rotation);}
            }
        }
    }
}
