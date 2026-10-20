using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Presentation only. All created objects belong to the cue; target objects are never modified.
    public sealed class GuideCue : MonoBehaviour,IDisposable
    {
        static readonly Color Amber=new Color(1f,.69f,.15f,1f);
        static readonly Color Dark=new Color(.055f,.075f,.105f,1f);
        const float Period=1.1f;
        Transform target;
        EquipmentHandle equipment;
        bool reducedMotion,stopped,world;
        public bool IsStopped=>stopped;
        float started;
        readonly List<Image> borders=new List<Image>();
        RectTransform pointer;
        LineRenderer ring,rim,path,dot;
        Material ownedMaterial;

        public static GuideCue AttachTo(RectTransform target,bool reducedMotion)
        {
            if(!target)throw new ArgumentNullException(nameof(target));
            var go=new GameObject("Guide cue · UI",typeof(RectTransform),typeof(CanvasGroup),typeof(LayoutElement));
            var rect=(RectTransform)go.transform;rect.SetParent(target,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(-7,-7);rect.offsetMax=new Vector2(7,7);
            var group=go.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;go.GetComponent<LayoutElement>().ignoreLayout=true;
            var cue=go.AddComponent<GuideCue>();cue.target=target;cue.reducedMotion=reducedMotion;cue.started=Time.unscaledTime;
            cue.Border(rect,"Top",new Vector2(0,1),Vector2.one,new Vector2(0,-4),Vector2.zero);
            cue.Border(rect,"Bottom",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,4));
            cue.Border(rect,"Left",Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(4,0));
            cue.Border(rect,"Right",new Vector2(1,0),Vector2.one,new Vector2(-4,0),Vector2.zero);
            cue.pointer=cue.Border(rect,"Pointer",new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(-20,-6),new Vector2(-8,6));
            cue.pointer.localRotation=Quaternion.Euler(0,0,45);cue.Render(0);return cue;
        }

        public static GuideCue AttachTo(Transform target,bool reducedMotion)
        {
            if(!(target is RectTransform rect))throw new ArgumentException("Guide button target must have a RectTransform.",nameof(target));
            return AttachTo(rect,reducedMotion);
        }

        public static GuideCue ShowEquipment(Transform parent,EquipmentHandle item,bool reducedMotion,Material cordMaterial=null)
        {
            if(!item)throw new ArgumentNullException(nameof(item));
            var go=new GameObject("Guide cue · "+item.assetId);go.transform.SetParent(parent,false);go.layer=2;
            var cue=go.AddComponent<GuideCue>();cue.target=item.transform;cue.equipment=item;cue.world=true;cue.reducedMotion=reducedMotion;cue.started=Time.unscaledTime;
            if(cordMaterial)cue.ownedMaterial=new Material(cordMaterial);
            else
            {
                var shader=Shader.Find("Universal Render Pipeline/Unlit");
                if(!shader)shader=Shader.Find("Sprites/Default");
                if(!shader){Destroy(go);throw new InvalidOperationException("An included unlit guide material is required.");}
                cue.ownedMaterial=new Material(shader);
            }
            cue.ownedMaterial.name="Guide cue · owned unlit material";
            cue.rim=cue.Line("Contrast rim",.008f,Dark);
            cue.ring=cue.Line("Amber target",.004f,Amber);
            cue.path=cue.Line("Installation route",.002f,new Color(1f,.69f,.15f,.65f));
            cue.dot=cue.Line("Route dot",.006f,Amber);
            cue.Render(0);return cue;
        }

        RectTransform Border(RectTransform parent,string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax;
            var image=go.GetComponent<Image>();image.color=Amber;image.raycastTarget=false;borders.Add(image);
            // A dark edge keeps the amber legible against a pale panel without intercepting selection.
            var outline=go.AddComponent<Outline>();outline.effectColor=Dark;outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=false;
            return rect;
        }

        LineRenderer Line(string name,float width,Color colour)
        {
            var go=new GameObject(name);go.layer=2;go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.sharedMaterial=ownedMaterial;
            line.startWidth=line.endWidth=width;line.numCornerVertices=3;line.numCapVertices=3;
            line.startColor=line.endColor=colour;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",colour);properties.SetColor("_Color",colour);line.SetPropertyBlock(properties);
            return line;
        }

        void Update()
        {
            if(stopped)return;
            if(!target||!target.gameObject.activeInHierarchy){Stop();return;}
            Render(reducedMotion?0:Mathf.Repeat(Time.unscaledTime-started,Period)/Period);
        }

        void Render(float phase)
        {
            if(!world)
            {
                float pulse=reducedMotion?1:.8f+.2f*Mathf.Cos(phase*Mathf.PI*2);
                foreach(var border in borders)if(border)border.color=new Color(Amber.r,Amber.g,Amber.b,.7f+.25f*pulse);
                if(pointer)pointer.localScale=Vector3.one*(reducedMotion?1:.95f+.05f*pulse);
                return;
            }
            if(!equipment||!TryVisibleBounds(equipment,out var bounds)){SetLines(false);return;}
            SetLines(true);
            if(equipment.installed)
            {
                // Keep the highlight below the item so it cannot cover a small bob or instrument scale.
                var installedCentre=new Vector3(bounds.center.x,bounds.min.y-.008f,bounds.center.z);
                Ellipse(ring,installedCentre,bounds.extents.x+.012f,bounds.extents.z+.012f);Ellipse(rim,installedCentre,bounds.extents.x+.012f,bounds.extents.z+.012f);path.enabled=dot.enabled=false;
                return;
            }
            Vector3 source=bounds.center;
            var delta=equipment.rotation*Quaternion.Inverse(equipment.transform.rotation);
            var destination=TransformBounds(bounds,Matrix4x4.TRS(equipment.slot,delta,Vector3.one)*Matrix4x4.Translate(-equipment.transform.position));
            Vector3 anchor=destination.center;float x=Mathf.Max(.025f,destination.extents.x+.012f),z=Mathf.Max(.025f,destination.extents.z+.012f);
            var centre=new Vector3(anchor.x,destination.min.y+.006f,anchor.z);
            Ellipse(ring,centre,x,z);Ellipse(rim,centre,x,z);
            path.loop=false;path.positionCount=2;path.SetPosition(0,source);path.SetPosition(1,anchor);
            if(reducedMotion){dot.enabled=false;return;}
            // This small guide mark moves; the supplied item, its sockets and the camera do not.
            Ellipse(dot,Vector3.Lerp(source,anchor,phase),.009f,.009f);
        }

        void SetLines(bool visible)
        {if(ring)ring.enabled=visible;if(rim)rim.enabled=visible;if(path)path.enabled=visible;if(dot)dot.enabled=visible;}
        static void Ellipse(LineRenderer line,Vector3 centre,float x,float z)
        {
            line.loop=true;line.positionCount=40;
            for(int i=0;i<40;i++){float a=2*Mathf.PI*i/40;line.SetPosition(i,centre+new Vector3(Mathf.Cos(a)*x,0,Mathf.Sin(a)*z));}
        }
        static Bounds TransformBounds(Bounds source,Matrix4x4 matrix)
        {
            var e=source.extents;var x=matrix.MultiplyVector(new Vector3(e.x,0,0));var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
            return new Bounds(matrix.MultiplyPoint3x4(source.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
        }
        static bool TryVisibleBounds(EquipmentHandle item,out Bounds bounds)
        {
            bounds=default;bool found=false;
            foreach(var renderer in item.GetComponentsInChildren<Renderer>(false))
            {
                if(!renderer.enabled||!(renderer is MeshRenderer||renderer is SkinnedMeshRenderer))continue;
                if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
            }
            // Source chips are Graphics.RenderMeshInstanced objects and need their actual mesh matrices.
            foreach(var chips in item.GetComponentsInChildren<VlabChips>(false))
            {
                if(!chips.isActiveAndEnabled||!chips.SharedMesh)continue;
                for(int i=0;i<chips.Count;i++)
                {var b=TransformBounds(chips.SharedMesh.bounds,chips.MatrixAt(i));if(!found){bounds=b;found=true;}else bounds.Encapsulate(b);}
            }
            return found;
        }

        // Call from an editor smoke check: rendering one cue frame must not change its target or lesson.
        public bool AssertPresentationOnly(ExperimentSession session,out string failure)
        {
            failure="";if(!target){failure="The cue target has been lost.";return false;}
            var position=target.position;var rotation=target.rotation;var scale=target.localScale;
            var phase=session?.Phase;var operation=session?.Operation;var elapsed=session?.Elapsed;var dose=session?.Dose;
            var parameters=session==null?null:new Dictionary<string,double>(session.parameters);
            Render(0);
            if(reducedMotion)
            {
                var first=PresentationSnapshot();Render(.5f);var second=PresentationSnapshot();
                if(first.Count!=second.Count){failure="Reduced-motion cue geometry changed.";return false;}
                for(int i=0;i<first.Count;i++)if(first[i]!=second[i]){failure="Reduced-motion cue is animated.";return false;}
            }
            if(target.position!=position||target.rotation!=rotation||target.localScale!=scale){failure="Cue changed the target transform.";return false;}
            if(session!=null&&(session.Phase!=phase||session.Operation!=operation||session.Elapsed!=elapsed||session.Dose!=dose||session.parameters.Count!=parameters.Count))
            {failure="Cue changed lesson state.";return false;}
            if(parameters!=null)foreach(var p in parameters)if(!session.parameters.TryGetValue(p.Key,out var value)||value!=p.Value){failure="Cue changed a lesson parameter.";return false;}
            return true;
        }

        List<Vector4> PresentationSnapshot()
        {
            var values=new List<Vector4>();
            foreach(var border in borders)if(border)values.Add(border.color);
            if(pointer){var s=pointer.localScale;values.Add(new Vector4(s.x,s.y,s.z,0));}
            foreach(var line in new[]{ring,rim,path,dot})
            {
                if(!line)continue;values.Add(new Vector4(line.enabled?1:0,line.positionCount,line.startWidth,line.endWidth));
                for(int i=0;i<line.positionCount;i++){var p=line.GetPosition(i);values.Add(new Vector4(p.x,p.y,p.z,0));}
            }
            return values;
        }

        public void Stop()
        {if(stopped)return;stopped=true;gameObject.SetActive(false);Destroy(gameObject);}
        public void Dispose()=>Stop();
        void OnDestroy(){if(ownedMaterial)Destroy(ownedMaterial);}
    }
}
