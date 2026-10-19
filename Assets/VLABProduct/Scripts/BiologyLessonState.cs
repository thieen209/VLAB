using System;
using System.Collections.Generic;
namespace Vlab.Product
{
    // Only explicitly sampled observations are saved. Process state is never resumed as a live chemical setup.
    [Serializable] public sealed class BiologyLessonState
    {
        public int version=1; public string lessonId="",groupId="";
        public int prep,objective,selectedCell,selectedStructure,selectedSample;
        public float focus=6.5f; public bool paused=true,wet,mounted,lowered,baselineLocked;
        public bool saltAdded,waterAdded,bufferAdded,filtered,alcoholAdded,noAlcohol,spooled;
        public bool inactive,reacting,frontMarked,extracted,ground,seated;
        public double clock,saltAt,waterAt,reactionAt,gasVolume,thermalDose;
        public double sampleC=25,bathC=25,frontMm;
        public List<BiologyCellAnswer> cells=new List<BiologyCellAnswer>();
        public List<BiologySample> samples=new List<BiologySample>();
        public List<BiologyGasPoint> gas=new List<BiologyGasPoint>();
        public List<BiologyStructureAnswer> structures=new List<BiologyStructureAnswer>();
    }
    [Serializable] public sealed class BiologyCellAnswer
    {public int id;public string answer="";public int baseline=-1,salt=-1,recovery=-1;}
    [Serializable] public sealed class BiologyStructureAnswer
    {public int cell,region;public string answer="";public bool correct;}
    [Serializable] public sealed class BiologySample
    {public string id="";public double initial,final,concentration,soakStart=-1;public bool initialRead,blotted,finalRead;}
    [Serializable] public sealed class BiologyGasPoint
    {public double seconds,volumeMl,sampleC,bathC;public string sampleId="";}
}
