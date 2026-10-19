#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Vlab.Product;
using UnityEngine;

// Synthetic unit fixtures only. These values never enter a user's profile or the product notebook.
public static class NotebookDataChecks
{
    static int count;
    static void C(bool b,string m){if(!b)throw new Exception(m);count++;}
    static void Near(double a,double b,string m)=>C(Math.Abs(a-b)<1e-9,m+" "+a+" != "+b);
    static AttemptRecord Record(string id,string name,string key,double x,string y,double value,double mass=.1,double angle=8)
    {
        var spec=LessonSpec.All[id];var defaults=ScienceModels.Defaults(id);if(defaults.ContainsKey(key))defaults[key]=x;
        if(id=="P01"){defaults["mass"]=mass;defaults["angle"]=angle;}
        return new AttemptRecord{experimentId=id,attemptId=name,model="unit fixture",observation="Synthetic test only",parameterValues=spec.parameters.Select(p=>defaults[p.key]).ToArray(),readings=new List<Reading>{new Reading(y,value,"s","Số đo kiểm thử","Test observation")}};
    }
    static List<AttemptRecord> Pendulum(int repeats=3,double angle=8)
    {
        var records=new List<AttemptRecord>();foreach(double length in new[]{.4,.8})for(int i=0;i<repeats;i++)
            records.Add(Record("P01",length+"-"+i,"length",length,"period",2*Math.PI*Math.Sqrt(length/9.81),.1,angle));return records;
    }
    public static string Run()
    {
        count=0;var records=Pendulum();var d=ExperimentNotebook.BuildData("P01",records);
        C(d.rows.Count==6&&d.groups.Count==1,"Saved attempts retained in one controlled group");var g=d.groups[0];
        C(g.points.Count==6&&g.summaries.Count==2&&g.summaries.All(x=>x.n==3),"Independent repeats grouped by actual saved L");
        C(g.fit.eligible&&g.fit.smallAngle,"Three-repeat/two-length small-angle fit eligibility");Near(g.fit.gravity,9.81,"g from actual stored T² fit");Near(g.fit.slope,4*Math.PI*Math.PI/9.81,"T² slope units");Near(g.fit.intercept,0,"Fit intercept");Near(g.fit.rSquared,1,"Fit R²");
        C(!ExperimentNotebook.BuildData("P01",Pendulum(2)).groups[0].fit.eligible,"Two repeats per length cannot claim required fit");
        var duplicate=Pendulum(2);duplicate.Add(duplicate[0]);duplicate.Add(duplicate[2]);var dd=ExperimentNotebook.BuildData("P01",duplicate);
        C(dd.rows.Count==6&&!dd.groups[0].fit.eligible&&dd.groups[0].summaries.All(x=>x.n==2),"Duplicate saved IDs do not inflate replication; all rows preserved");
        var controlled=Pendulum();controlled.Add(Record("P01","mass-changed","length",.4,"period",1.3,.2));controlled.Add(Record("P01","angle-changed","length",.4,"period",1.3,.1,20));
        C(ExperimentNotebook.BuildData("P01",controlled).groups.Count==3,"Different masses and release angles remain separate groups");
        var large=ExperimentNotebook.BuildData("P01",Pendulum(3,20)).groups[0].fit;C(large.eligible&&!large.smallAngle&&double.IsNaN(large.gravity),"Large-angle data cannot silently become small-angle g");
        var invalid=Pendulum();invalid.Add(Record("P01","invalid-period","length",.6,"period",double.NaN));var bad=ExperimentNotebook.BuildData("P01",invalid);
        C(bad.rows.Count==7&&bad.excluded==1&&bad.groups[0].points.Count==6,"Invalid observation retained in rows, excluded from plot and fit");
        var square=Pendulum();square[0].readings[0].value=1;square[1].readings[0].value=2;square[2].readings[0].value=3;
        Near(ExperimentNotebook.BuildData("P01",square).groups[0].summaries[0].mean,14d/3,"Mean of individual T², not square of mean T");
        var qualitative=Record("C04","presence","chloride",1,"NaClSolid",1);var q=ExperimentNotebook.BuildData("C04",new List<AttemptRecord>{qualitative});
        C(q.tableOnly&&q.rows.Count==1&&q.groups[0].points.Count==0,"Qualitative presence remains a table, not a fabricated numeric trend");
        foreach(var spec in LessonSpec.All.Values){var empty=ExperimentNotebook.BuildData(spec.id,new List<AttemptRecord>());C(empty.experimentId==spec.id&&empty.rows.Count==0,"Empty mapping is explicit for "+spec.id);}
        var before=records[0].parameterValues.ToArray();ExperimentNotebook.BuildData("P01",records,"mass","period");C(before.SequenceEqual(records[0].parameterValues)&&records[0].observation=="Synthetic test only","Notebook cannot mutate raw records/observations");
#if UNITY_5_3_OR_NEWER
        JsonFixtures();
#endif
        return "PASS "+count+" notebook data checks; synthetic fixtures, no measured science or visual acceptance claimed.";
    }
#if UNITY_5_3_OR_NEWER
    static void JsonFixtures()
    {
        var bio=Record("B04","bio-time","temperature",35,"gasVolume",4);var state=new BiologyLessonState{reactionAt=20};state.gas.Add(new BiologyGasPoint{seconds=20,volumeMl=0,sampleId="sample-A"});state.gas.Add(new BiologyGasPoint{seconds=30,volumeMl=4,sampleId="sample-A"});bio.detailJson=JsonUtility.ToJson(state);
        var bd=ExperimentNotebook.BuildData("B04",new List<AttemptRecord>{bio});C(bd.curveMode&&bd.groups[0].curves.Count==1,"Actual biology gas DTO parsed");
        Near(bd.groups[0].curves[0].points[0].x,0,"Biology time starts at saved actual mixing");Near(bd.groups[0].curves[0].points[1].x,10,"Actual saved biology time interval");Near(bd.groups[0].curves[0].points[1].y,4,"Actual saved biology volume retained");
        var chem=Record("C07","chem-time","volume",20,"gasVolume",8);chem.detailJson="{\"curve\":[{\"seconds\":0,\"value\":0},{\"seconds\":10,\"value\":8}]}";
        var cd=ExperimentNotebook.BuildData("C07",new List<AttemptRecord>{chem});Near(cd.groups[0].curves[0].points[1].y,8,"Actual chemistry curve DTO parsed without model evaluation");
        chem.detailJson="";C(ExperimentNotebook.BuildData("C07",new List<AttemptRecord>{chem}).groups[0].curves.Count==0,"Missing historical curve never reconstructed as theory");
        var potato=Record("B03","potato","concentration",.2,"meanMassChange",5);potato.detailJson="{\"samples\":[{\"id\":\"A\",\"initial\":2,\"final\":2.1,\"initialRead\":true,\"finalRead\":true,\"blotted\":true},{\"id\":\"B\",\"initial\":2,\"final\":2.2,\"initialRead\":true,\"finalRead\":true,\"blotted\":true},{\"id\":\"A\",\"initial\":2,\"final\":2.1,\"initialRead\":true,\"finalRead\":true,\"blotted\":true}]}";
        var pd=ExperimentNotebook.BuildData("B03",new List<AttemptRecord>{potato});C(pd.groups[0].summaries[0].n==2,"Biological replicate IDs deduplicate repeated weighing");Near(pd.groups[0].summaries[0].mean,7.5,"Independent sample changes averaged");
    }
#endif
}

#endif
