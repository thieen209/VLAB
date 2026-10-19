using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed class KitItemSpec
    {
        public readonly string key,prefab,vi,en,dependsOn;
        public readonly Vector3 position,euler;public readonly bool onSurface;
        public KitItemSpec(string key,string prefab,string vi,string en,Vector3 position,Vector3 euler=default,bool onSurface=true,string dependsOn="")
        {this.key=key;this.prefab=prefab;this.vi=vi;this.en=en;this.position=position;this.euler=euler;this.onSurface=onSurface;this.dependsOn=dependsOn;}
    }
    public sealed class LessonControl
    {
        public readonly string id,vi,en;public readonly bool enabled;
        public LessonControl(string id,string vi,string en,bool enabled=true){this.id=id;this.vi=vi;this.en=en;this.enabled=enabled;}
    }
    public interface IExperimentAuthority
    {
        bool CanStart(out string message);
        void Advance(double seconds);
        ModelResult Observe();
        bool CanRecord(out string message);
        bool TryMeasure(out ModelResult result,out string message);
        void SetPaused(bool paused);
        void InputLost();
        string SaveState();
        void RestoreState(string json);
        bool CanEditParameter(string key);
    }
    // Subject drivers bind delivered instrument state to the lesson's explicit clock.
    // Kit slots are authored metres relative to the worktop; prefab world scale stays one.
    public abstract class LessonDriver:IExperimentAuthority
    {
        protected readonly ExperimentSession session;
        protected IReadOnlyDictionary<string,EquipmentHandle> equipment;
        protected Vector3 worktop;
        protected LessonDriver(ExperimentSession session){this.session=session;}
        public abstract KitItemSpec[] Kit{get;}
        public virtual void Bind(IReadOnlyDictionary<string,EquipmentHandle> items,Vector3 at){equipment=items;worktop=at;}
        public virtual void Installed(string key){}
        public abstract bool Prepare(int completedOperation,out string message);
        public abstract void Configure();
        public abstract void Advance(double seconds);
        public abstract ModelResult Observe();
        public abstract bool CanStart(out string message);
        public abstract bool CanRecord(out string message);
        public abstract bool TryMeasure(out ModelResult result,out string message);
        public virtual void SetPaused(bool paused){}
        public virtual void InputLost(){SetPaused(true);}
        public virtual string SaveState()=>"";
        public virtual void RestoreState(string json){}
        public virtual bool CanEditParameter(string key)=>false;
        public virtual Texture Inspection(string key)=>null;
        public virtual Rect InspectionUv(string key)=>new Rect(0,0,1,1);
        public virtual bool PointOnInspection(string key,Vector2 uv,out string message){message="This instrument has no selectable image.";return false;}
        public virtual LessonControl[] Controls(string key)=>Array.Empty<LessonControl>();
        public virtual bool Operate(string id,out string message){message="This instrument has no action in the current lesson state.";return false;}
        public virtual bool Validate(out string message){message="";return equipment.Values.All(x=>x.installed);}
        protected T Instrument<T>(string key) where T:Component
        {if(!equipment.TryGetValue(key,out var item))throw new InvalidOperationException("Required apparatus key: "+key);return item.GetComponentInChildren<T>(true)??throw new InvalidOperationException(key+" lacks "+typeof(T).Name);}
        protected Transform Socket(string key,string suffix)
        {return equipment[key].GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith(suffix,StringComparison.Ordinal));}
        protected void Fit(string key,string socketSuffix,Transform target)
        {var item=equipment[key];item.transform.position+=target.position-Socket(key,socketSuffix).position;}
        protected double P(string key)=>session.parameters[key];
        protected bool Set(string key,double value,out string message)=>session.SetParameter(key,value,out message);
        protected ModelResult Kernel(double dose=0)=>ScienceModels.Evaluate(session.spec.id,session.parameters,session.Elapsed,dose,session.Seed);
        protected static bool Require(bool condition,string failure,out string message){message=condition?"":failure;return condition;}
    }
}
