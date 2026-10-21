using System;
using System.Collections.Generic;

namespace Vlab.Product
{
    public enum ExperimentPhase { Preparing, Ready, Running, Paused, Measured, Invalid }
    public sealed class PendulumDynamics
    {
        public double Angle{get;private set;}public double AngularVelocity{get;private set;}
        public double Time{get;private set;}public int Cycles{get;private set;}
        public double FirstCrossing{get;private set;}=-1;
        public double LastCrossing{get;private set;}=-1;
        public double TargetCrossing{get;private set;}=-1;
        readonly double length;readonly int targetCycles;double remainder;
        public PendulumDynamics(double length,double degrees,int targetCycles=0){this.length=length;this.targetCycles=targetCycles;Angle=degrees*Math.PI/180;}
        public void Advance(double seconds)
        {
            if(seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentOutOfRangeException();
            remainder+=seconds;const double h=.002;
            while(remainder>=h)
            {
                double previous=Angle,previousTime=Time,q=Angle,w=AngularVelocity;
                double A(double x)=>-ScienceModels.G/length*Math.Sin(x);
                double k1q=w,k1w=A(q),k2q=w+h*k1w/2,k2w=A(q+h*k1q/2),k3q=w+h*k2w/2,k3w=A(q+h*k2q/2),k4q=w+h*k3w,k4w=A(q+h*k3q);
                Angle+=h*(k1q+2*k2q+2*k3q+k4q)/6;AngularVelocity+=h*(k1w+2*k2w+2*k3w+k4w)/6;Time+=h;remainder-=h;
                if(previous>0&&Angle<=0&&AngularVelocity<0)
                {
                    double crossing=previousTime+h*previous/(previous-Angle);
                    if(FirstCrossing<0)FirstCrossing=crossing;else Cycles++;
                    LastCrossing=crossing;
                    if(targetCycles>0&&Cycles==targetCycles)TargetCrossing=crossing;
                }
            }
        }
        public int MeasuredCycles=>TargetCrossing>=0?targetCycles:Cycles;
        public double MeasuredStop=>TargetCrossing>=0?TargetCrossing:LastCrossing;
        public double MeasuredPeriod=>MeasuredCycles>0?(MeasuredStop-FirstCrossing)/MeasuredCycles:0;
    }
    public sealed class ExperimentSession
    {
        public string Assistance="";public int ReleaseDirection=1;public readonly LessonSpec spec;
        public readonly Dictionary<string,double> parameters;
        public ExperimentPhase Phase{get;private set;}=ExperimentPhase.Preparing;
        public int Operation{get;private set;}public int Seed{get;private set;}
        public double Elapsed{get;private set;}public double Dose{get;private set;}
        public bool ApparatusReady{get;private set;}
        public PendulumDynamics Pendulum{get;private set;}
        public ModelResult Recorded{get;private set;}
        public string Failure{get;private set;}="";
        public bool Complete=>Recorded!=null;
        public IExperimentAuthority Authority{get;set;}
        public string RestoredDetailJson{get;private set;}="";
        public ExperimentSession(string id,int seed=1){spec=LessonSpec.All[id];parameters=ScienceModels.Defaults(id);Seed=seed;}
        public void SetApparatusReady(bool ready)
        {
            if(!ready&&(Phase==ExperimentPhase.Running||Phase==ExperimentPhase.Paused)){Invalidate("Apparatus disconnected; stop and prepare a new attempt.");}
            ApparatusReady=ready;UpdatePreparation();
        }
        public bool Prepare(int operation,out string message)
        {
            message="";
            if(!ApparatusReady){message="Install the highlighted equipment first.";return false;}
            if(Phase!=ExperimentPhase.Preparing||operation!=Operation){message="Complete the current preparation action first.";return false;}
            Operation++;UpdatePreparation();return true;
        }
        void UpdatePreparation(){if(Phase==ExperimentPhase.Preparing&&ApparatusReady&&Operation>=spec.operationsEn.Length)Phase=ExperimentPhase.Ready;}
        public bool SetParameter(string key,double value,out string message)
        {
            message="";var p=Array.Find(spec.parameters,x=>x.key==key);
            if(p==null||double.IsNaN(value)||double.IsInfinity(value)||value<p.min-1e-9||value>p.max+1e-9){message="Value is outside the configured experiment range.";return false;}
            bool live=Authority!=null?Authority.CanEditParameter(key):spec.id=="C01"&&key=="flow"||spec.id=="B02"&&key=="water";
            if((Phase==ExperimentPhase.Running||Phase==ExperimentPhase.Paused||Phase==ExperimentPhase.Measured)&&!live){message="Start a new attempt before changing a controlled variable.";return false;}
            if(Authority==null&&spec.id=="B02"&&key=="water"&&value>.5&&Elapsed<parameters["duration"]){message="Observe the salt-exposure interval before replacing it with water.";return false;}
            if(p.choices!=null&&!Array.Exists(p.choices,x=>Math.Abs(x-value)<1e-8)){message="Choose one of the delivered instrument configurations.";return false;}
            parameters[key]=value;
            if(spec.id=="M04"&&key=="d1")parameters["d2"]=120-value;
            if(spec.id=="M01"&&key=="driver"&&parameters["driven"]==value)parameters["driven"]=value==12?24:12;
            return true;
        }
        public bool Start(out string message)
        {
            message="";
            if(Phase!=ExperimentPhase.Ready||!ApparatusReady){message="Preparation is incomplete.";return false;}
            if(Authority!=null&&!Authority.CanStart(out message))return false;
            try{if(Authority==null)ScienceModels.Evaluate(spec.id,parameters,0);}
            catch(Exception ex){message=ex.Message;return false;}
            if(spec.id=="P01")Pendulum=new PendulumDynamics(parameters["length"],parameters["angle"]*ReleaseDirection,(int)parameters["cycles"]);
            Phase=ExperimentPhase.Running;Authority?.SetPaused(false);return true;
        }
        public void Advance(double seconds)
        {
            if(Phase!=ExperimentPhase.Running)return;
            if(seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentOutOfRangeException();
            Elapsed+=seconds;
            if(Authority!=null)Authority.Advance(seconds);
            else if(spec.id=="C01")Dose=Math.Min(25,Dose+.5*parameters["flow"]/100*seconds);
            Pendulum?.Advance(seconds);
        }
        public void Pause(){if(Phase==ExperimentPhase.Running){Phase=ExperimentPhase.Paused;Authority?.SetPaused(true);}}
        public void Resume(){if(Phase==ExperimentPhase.Paused&&ApparatusReady){Phase=ExperimentPhase.Running;Authority?.SetPaused(false);}}
        public void InputLost(){Pause();Authority?.InputLost();if(spec.id=="C01")parameters["flow"]=0;}
        public void Invalidate(string reason){Failure=reason;Recorded=null;Phase=ExperimentPhase.Invalid;Authority?.SetPaused(true);if(spec.id=="C01")parameters["flow"]=0;}
        public ModelResult Observe()=>Authority!=null?Authority.Observe():ScienceModels.Evaluate(spec.id,parameters,Elapsed,Dose,Seed);
        public bool Measure(out string message)
        {
            message="";
            if(Phase!=ExperimentPhase.Running&&Phase!=ExperimentPhase.Paused){message="Run the prepared experiment before recording.";return false;}
            if(Authority!=null)
            {
                if(!Authority.TryMeasure(out var result,out message))return false;
                if(result==null||result.readings.Count==0){message="No instrument observations are available.";return false;}
                Recorded=result;Phase=ExperimentPhase.Measured;Authority.SetPaused(true);return true;
            }
            double needed=spec.id=="C03"?parameters["time"]:spec.id=="B03"?parameters["duration"]:spec.duration;
            if(spec.id=="P01")
            {
                if(Pendulum==null||Pendulum.Cycles<(int)parameters["cycles"]){message="Count the selected number of full cycles between crossings in the same direction.";return false;}
                double period=Pendulum.MeasuredPeriod;Recorded=new ModelResult().Add("period",period,"s").Add("timedCycles",Pendulum.MeasuredCycles,"").Add("measuredTime",Pendulum.MeasuredStop-Pendulum.FirstCrossing,"s").Add("gravityEstimate",4*Math.PI*Math.PI*parameters["length"]/(period*period),"m/s²");
            }
            else
            {
                if(Elapsed+1e-8<needed){message="Continue observing before recording this attempt.";return false;}
                if(spec.id=="C01")
                {
                    var result=Observe();double endpoint=result.Get("endpointTarget");
                    if(Dose<endpoint-.05){message="The endpoint has not been reached. Continue dropwise.";return false;}
                    if(Dose>endpoint+.10){Invalidate("Endpoint overshot. Retain the reason and prepare a fresh aliquot.");message=Failure;return false;}
                    if(parameters["flow"]>0){message="Close the burette valve before reading the meniscus.";return false;}
                }
                if(spec.id=="B01"&&Observe().Get("focusQuality")<.7){message="The specimen is out of focus. Adjust the focus before recording.";return false;}
                Recorded=Observe();
            }
            Phase=ExperimentPhase.Measured;return true;
        }
        public SessionCheckpoint Checkpoint()
        {
            var values=new double[spec.parameters.Length];for(int i=0;i<values.Length;i++)values[i]=parameters[spec.parameters[i].key];
            // Running chemistry/physics safely restarts at preparation; recorded notebook entries remain separate.
            return new SessionCheckpoint{experimentId=spec.id,operation=Authority==null&&Phase==ExperimentPhase.Preparing?Operation:0,values=values,seed=Seed,apparatusReady=false,detailJson=Authority?.SaveState()??"",assistance=Assistance};
        }
        public static ExperimentSession Restore(SessionCheckpoint checkpoint)
        {
            var s=new ExperimentSession(checkpoint.experimentId,checkpoint.seed);for(int i=0;i<s.spec.parameters.Length;i++)s.parameters[s.spec.parameters[i].key]=checkpoint.values[i];
            s.Operation=checkpoint.operation;s.ApparatusReady=false;s.RestoredDetailJson=checkpoint.detailJson??"";s.Assistance=checkpoint.assistance??"";return s;
        }
        public AttemptRecord Record(string observation="")
        {
            if(!Complete||Phase!=ExperimentPhase.Measured)throw new InvalidOperationException("No completed observation to record.");
            var values=new double[spec.parameters.Length];for(int i=0;i<values.Length;i++)values[i]=parameters[spec.parameters[i].key];
            // A recorded attempt owns its observations. Copying the list alone would leave
            // mutable Reading objects shared with the live instrument result and other records.
            var readings=new List<Reading>();foreach(var reading in Recorded.readings)readings.Add(new Reading(reading.key,reading.value,reading.unit,reading.labelVi,reading.labelEn));
            return new AttemptRecord{experimentId=spec.id,attemptId=Guid.NewGuid().ToString("N"),savedAtUtc=DateTime.UtcNow.ToString("O"),model=spec.modelLabel,modelNotesVi=LessonContext.Explanation(spec.id,true,Recorded),modelNotesEn=LessonContext.Explanation(spec.id,false,Recorded),seed=Seed,readings=readings,parameterValues=values,observation=observation,detailJson=Recorded.detailJson,assistance=string.IsNullOrEmpty(Assistance)?"no-step-assist-recorded":Assistance};
        }
    }
}
