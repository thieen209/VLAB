using System;
using UnityEngine;

public sealed class VlabBelt : MonoBehaviour
{
    [Serializable] public sealed class Configuration
    {
        public string name;
        public GameObject root;
        public Renderer surface;
        public double length,centerDistance;
        public bool crossed;
    }
    [SerializeField] private Configuration[] configurations;
    private MaterialPropertyBlock properties;
    public int Selected {get;private set;}
    public Configuration Current=>configurations[Selected];
    public Configuration[] Configurations=>configurations;
    public double InputRpm {get;private set;}
    public double Slip {get;private set;}
    public bool DriveLarge {get;private set;}
    public bool Running {get;set;}
    public double Phase {get;private set;}
    public double SmallTurns {get;private set;}
    public double LargeTurns {get;private set;}
    public double OutputRpm=>InputRpm*(DriveLarge?2:.5)*(Current.crossed?-1:1)*(1-Slip);
    // shortcut: all prescribed slip is at the output; add tension/elasticity only with a corresponding solver.
    public double LinearSpeed=>InputRpm*(Math.PI*(DriveLarge?.08:.04)/60)*(DriveLarge&&Current.crossed?-1:1);
    private void Awake()=>SelectConfiguration(0);
    private void Update(){if(Running)Advance(Time.deltaTime);}
    public void SelectConfiguration(int index)
    {
        if(index<0||index>=configurations.Length)throw new ArgumentOutOfRangeException(nameof(index));
        var config=configurations[index];
        if(config.root==null||config.surface==null||!double.IsFinite(config.length)||config.length<=0)throw new InvalidOperationException("Invalid authored belt configuration.");
        Selected=index;
        for(int i=0;i<configurations.Length;i++)configurations[i].root.SetActive(i==index);
        ResetDrive();
    }
    public bool Fits(double centerDistance,double diameterA,double diameterB,double beltLength)
    {
        return double.IsFinite(centerDistance)&&double.IsFinite(diameterA)&&double.IsFinite(diameterB)&&double.IsFinite(beltLength)
            &&Math.Abs(centerDistance-Current.centerDistance)<=1e-5&&Math.Abs(diameterA-.04)<=1e-6
            &&Math.Abs(diameterB-.08)<=1e-6&&Math.Abs(beltLength-Current.length)<=1e-5;
    }
    public void ConfigureDrive(double rpm,double slip,bool driveLarge)
    {
        if(!double.IsFinite(rpm)||!double.IsFinite(rpm*2)||!double.IsFinite(slip)||slip<0||slip>1)throw new ArgumentOutOfRangeException("Drive parameters");
        InputRpm=rpm;Slip=slip;DriveLarge=driveLarge;
    }
    public void Advance(double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
        double cycles=LinearSpeed*seconds/Current.length;
        double small=(DriveLarge?OutputRpm:InputRpm)*(seconds/60);
        double large=(DriveLarge?InputRpm:OutputRpm)*(seconds/60);
        if(!double.IsFinite(cycles)||!double.IsFinite(small)||!double.IsFinite(large))throw new ArgumentOutOfRangeException(nameof(seconds));
        Phase=Wrap(Phase+cycles%1);SmallTurns=Wrap(SmallTurns+small%1);LargeTurns=Wrap(LargeTurns+large%1);ApplyPhase();
    }
    private static double Wrap(double turns)=>(turns+1)%1;
    public void ResetDrive(){Running=false;InputRpm=0;Slip=0;DriveLarge=false;Phase=0;SmallTurns=0;LargeTurns=0;ApplyPhase();}
    private void ApplyPhase()
    {
        if(properties==null)properties=new MaterialPropertyBlock();
        Current.surface.GetPropertyBlock(properties);
        properties.SetVector("_BaseMap_ST",new Vector4(1,1,-(float)Phase,0));Current.surface.SetPropertyBlock(properties);
    }
}
