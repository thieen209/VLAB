#if UNITY_EDITOR
using System;
using UnityEngine;

public static class CalorimeterPourChecks
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Calorimeter checks require Play mode and native prefab initialization.");
        GameObject cup=null,acidObject=null,baseObject=null;int checks=0;
        void Check(bool okay,string why){checks++;if(!okay)throw new InvalidOperationException(why);}
        void Near(double a,double b,string why,double tolerance=1e-7)=>Check(double.IsFinite(a)&&double.IsFinite(b)&&Math.Abs(a-b)<=tolerance,why+" ["+a+", "+b+"]");
        try
        {
            cup=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Equipment/VLAB_calorimeter"));
            acidObject=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Equipment/VLAB_beaker_100ml"));
            baseObject=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Equipment/VLAB_beaker_100ml"));
            cup.transform.position=new Vector3(500,10,500);acidObject.transform.position=cup.transform.position+Vector3.left;baseObject.transform.position=cup.transform.position+Vector3.right;
            var cal=cup.GetComponent<VlabCalorimeter>();var acid=acidObject.GetComponent<VlabBeaker>();var alkali=baseObject.GetComponent<VlabBeaker>();
            void Fill(VlabBeaker source,double amount){source.Remove(source.VolumeMl);source.Add(amount);}
            void Begin(double capacity=20,double temperature=25)
            {cal.BeginPourTrial(capacity,temperature);cal.LossEnabled=false;cal.SetLid(true);Check(cal.StartMix(),"Incremental trial starts once");}
            double Pour(VlabBeaker source,bool isAcid,double amount,double temperature=25,double molarity=.5)=>cal.TryPourFrom(source,isAcid,molarity,temperature,amount,out _);
            void Energy()=>Near(cal.StoredHeatJ,cal.ReleasedHeatJ-cal.LostHeatJ,"Sensible plus reaction heat conserves energy across actual feeds",2e-6);

            Fill(acid,25);Fill(alkali,25);cal.BeginPourTrial(0,25);cal.SetLid(true);
            Near(Pour(acid,true,5),0,"No transfer before explicit timer start");Near(acid.VolumeMl,25,"Rejected start preserves source");
            Check(cal.StartMix(),"Massless empty cup can start timing");cal.Advance(2);Near(cal.ElapsedSeconds,2,"Empty trial preserves time without NaN");Near(cal.TemperatureC,25,"Empty massless temperature stays finite");
            cal.SetLid(false);Near(Pour(acid,true,5),0,"Closed lid blocks thermal transfer");cal.SetLid(true);
            Near(Pour(acid,true,25,20),25,"Accept exact first acid volume");Near(cal.TemperatureC,20,"First feed sets massless cup temperature");Energy();
            Near(Pour(alkali,false,12.5,30),12.5,"Partial base transfer is finite");cal.Advance(4);Energy();
            double released=cal.ReleasedHeatJ,lost=cal.LostHeatJ,elapsed=cal.ElapsedSeconds;
            Near(Pour(alkali,false,12.5,30),12.5,"Second base transfer accepts only remaining actual liquid");
            Near(cal.ReleasedHeatJ,released,"Later pour keeps previously released heat");Near(cal.LostHeatJ,lost,"Later pour keeps thermal exchange history");Near(cal.ElapsedSeconds,elapsed,"Later pour keeps elapsed time");
            Near(cal.AcidReceivedMl,25,"Acid volume ledger");Near(cal.BaseReceivedMl,25,"Base volume ledger");Near(cal.AcidMoles,.0125,"Acid mol ledger");Near(cal.BaseMoles,.0125,"Base mol ledger");
            Near(cal.InitialTemperatureC,25,"Unequal source temperatures use cumulative sensible baseline");Near(cal.AdiabaticTemperatureC,25+712.5/209,"Final ideal temperature follows actual 50 mL feed");Energy();
            cal.Advance(30);Energy();Near(cal.Vessel.VolumeMl+acid.VolumeMl+alkali.VolumeMl,50,"Volumes conserved after partial timed pours");Near(cal.ReactedMoles,cal.ReleasedHeatJ/57000,"Reacted amount derives from heat released, not one elapsed-time formula");
            Check(cal.PeakTemperatureC>=cal.PeakAfterLastPourC&&cal.PeakAfterLastPourSeconds>=cal.LastPourSeconds,"Analysis peak is scoped after latest addition while historical peak survives");
            cal.Paused=true;double pausedT=cal.TemperatureC;cal.Advance(10);Near(cal.TemperatureC,pausedT,"Pause freezes thermal integration");Near(cal.ElapsedSeconds,elapsed+30,"Pause freezes thermal time");
            Fill(acid,1);Near(Pour(acid,true,1),0,"Pause blocks additional solution");Near(acid.VolumeMl,1,"Paused dose is not lost");

            double[] Schedule(bool fine)
            {
                Fill(acid,25);Fill(alkali,25);Begin();cal.LossEnabled=true;
                void Advance(double seconds){if(fine){int n=(int)(seconds*100);for(int i=0;i<n;i++)cal.Advance(.01);}else cal.Advance(seconds);}
                Pour(acid,true,25,20);Advance(1);Pour(alkali,false,10,30);Advance(3);Pour(alkali,false,15,30);Advance(12);Energy();
                return new[]{cal.TemperatureC,cal.ReleasedHeatJ,cal.LostHeatJ,cal.ElapsedSeconds,cal.PeakTemperatureC,cal.PeakAfterLastPourC,cal.PeakAfterLastPourSeconds};
            }
            var coarse=Schedule(false);var fine=Schedule(true);for(int i=0;i<coarse.Length;i++)Near(fine[i],coarse[i],"Identical feed schedule is independent of thermal step partition "+i,3e-6);

            Fill(acid,80);Fill(alkali,80);Begin();Near(Pour(acid,true,100),80,"Source exhaustion limits accepted amount");Near(Pour(alkali,false,100),20,"Receiver capacity limits accepted amount");
            Near(alkali.VolumeMl,60,"Unaccepted solution stays in source");Near(cal.Vessel.VolumeMl+acid.VolumeMl+alkali.VolumeMl,160,"Capacity clipping conserves finite volumes");Near(cal.LimitingMoles,.01,"Actual accepted smaller reagent is limiting");
            Near(Pour(alkali,false,1),0,"Full cup rejects additional liquid");Energy();

            Fill(acid,20);Fill(alkali,20);Begin();Pour(acid,true,20,20,0);Pour(alkali,false,20,30,.5);cal.Advance(30);
            Near(cal.ReleasedHeatJ,0,"No acid mol means no neutralization heat");Energy();
            double beforeVolume=cal.Vessel.VolumeMl,beforeMoles=cal.AcidMoles,beforeHeat=cal.ReleasedHeatJ;
            Fill(acid,5);Near(Pour(acid,true,double.NaN),0,"NaN dose rejected");Near(Pour(acid,true,1,double.NaN),0,"NaN source temperature rejected");Near(Pour(acid,true,1,25,-1),0,"Negative molarity rejected");Near(Pour(cal.Vessel,true,1),0,"Self transfer rejected");
            Near(cal.Vessel.VolumeMl,beforeVolume,"Invalid requests preserve receiver");Near(cal.AcidMoles,beforeMoles,"Invalid requests preserve mol ledger");Near(cal.ReleasedHeatJ,beforeHeat,"Invalid requests preserve reaction history");Near(acid.VolumeMl,5,"Invalid requests preserve source");

            Fill(acid,80);Fill(alkali,20);Begin(0,100);Pour(acid,true,80,100,1);
            Near(Pour(alkali,false,20,100,1),0,"Unsupported boiling rejected before mutating either vessel");Near(alkali.VolumeMl,20,"Boiling rejection retains base");Near(cal.BaseMoles,0,"Boiling rejection records no fictitious mol");
            cal.Vessel.Add(1);Near(Pour(alkali,false,1),0,"Out-of-band receiver mutation blocks thermal transaction");
            bool rejected=false;try{cal.Advance(1);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Existing thermal-volume consistency guard remains active");

            cal.LoadTrial(25,.5,25,25,.5,25,20,25);cal.LossEnabled=false;Check(cal.StartMix(),"Legacy LoadTrial still starts");cal.Advance(20);
            Near(cal.ReleasedHeatJ,712.5*(1-Math.Exp(-10)),"Legacy 25+25 mL SDK result remains unchanged");Energy();Check(!cal.StartMix(),"Repeated start cannot double reaction heat");
            Near(Pour(alkali,false,1),0,"Legacy batch trial cannot be mutated through incremental API");
            return "CALORIMETER_POUR_CORE_PASS: "+checks+" native SDK assertions; incremental finite volume/mol/heat, timed feed history, step-independent loss and LoadTrial compatibility. No lesson input, rendered flow or physical calibration acceptance.";
        }
        finally{if(cup)UnityEngine.Object.DestroyImmediate(cup);if(acidObject)UnityEngine.Object.DestroyImmediate(acidObject);if(baseObject)UnityEngine.Object.DestroyImmediate(baseObject);}
    }
}
#endif
