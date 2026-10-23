using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

namespace Vlab.Product
{
    // Presentation only. All created objects belong to the cue; target objects are never modified.
    public sealed class GuideCue : MonoBehaviour,IDisposable
    {
        static readonly Color Amber=new Color(1f,.69f,.15f,1f);
        static readonly Color Dark=new Color(.055f,.075f,.105f,1f);
        const float Period=1.1f;
        static readonly int[] OutlineWalk={0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4};
        static readonly Vector3[] CornerSigns={new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,-1,1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(1,1,-1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        Transform target;
        EquipmentHandle equipment;
        bool reducedMotion,stopped,world;
        public bool IsStopped=>stopped;
        float started,topViewport=.97f;
        Rect clearArea=new Rect(.03f,.03f,.94f,.94f);
        readonly List<Image> borders=new List<Image>();
        RectTransform pointer;
        LineRenderer ring,rim,path,dot,objectOutline,leader;
        RectTransform callout;
        TextMeshProUGUI caption;
        Camera view;
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
            cue.rim=cue.Line("Contrast rim",.003f,Dark);
            cue.ring=cue.Line("Amber target",.0015f,Amber);
            cue.path=cue.Line("Installation route",.002f,new Color(1f,.69f,.15f,.65f));
            cue.dot=cue.Line("Route dot",.006f,Amber);
            cue.objectOutline=cue.Line("Object highlight",.003f,Amber);
            cue.leader=cue.Line("Callout leader",.002f,Amber);
            cue.CreateCallout();
            cue.Render(0);return cue;
        }

        // The app supplies localized content from its current, validated guide step.
        public void SetContext(string label,string instruction,Camera camera,float clearTop=.97f,Rect? observationArea=null)
        {
            view=camera;topViewport=Mathf.Clamp(clearTop,.3f,.97f);
            clearArea=observationArea??new Rect(.03f,.03f,.94f,.94f);
            if(!caption)return;
            callout.sizeDelta=new Vector2(360,150);
            caption.text=label+(string.IsNullOrWhiteSpace(instruction)?"":"\n"+instruction);
            callout.sizeDelta=new Vector2(360,Mathf.Clamp(Mathf.Ceil(caption.GetPreferredValues(caption.text,316,float.PositiveInfinity).y)+36,80,280));
            caption.ForceMeshUpdate(true,true);
        }

        void CreateCallout()
        {
            var go=new GameObject("Object instruction",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(Image));
            go.layer=2;callout=(RectTransform)go.transform;callout.SetParent(transform,false);
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var group=go.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            var face=go.GetComponent<Image>();face.color=new Color(Dark.r,Dark.g,Dark.b,.96f);face.raycastTarget=false;
            var textObject=new GameObject("Localized instruction",typeof(RectTransform),typeof(TextMeshProUGUI));
            textObject.layer=2;var rect=(RectTransform)textObject.transform;rect.SetParent(callout,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(22,18);rect.offsetMax=new Vector2(-22,-18);
            caption=textObject.GetComponent<TextMeshProUGUI>();caption.font=Resources.Load<TMP_FontAsset>("UI/VlabBody");
            caption.fontSize=26;caption.color=Color.white;caption.alignment=TextAlignmentOptions.MidlineLeft;
            caption.textWrappingMode=TextWrappingModes.Normal;caption.overflowMode=TextOverflowModes.Ellipsis;caption.raycastTarget=false;
            // No raw asset ID or unlocalized fallback appears before app context is supplied.
            go.SetActive(false);
        }

        void RenderObject(Bounds bounds,float phase)
        {
            var e=bounds.extents+Vector3.one*.006f;var c=bounds.center;
            // One continuous edge walk outlines the actual item without copying meshes or altering source shaders.
            bool hasLine=false,hasMesh=false;
            foreach(var renderer in equipment.GetComponentsInChildren<Renderer>(false))if(renderer.enabled)
            {hasLine|=renderer is LineRenderer;hasMesh|=renderer is MeshRenderer||renderer is SkinnedMeshRenderer;}
            objectOutline.enabled=hasLine&&!hasMesh;objectOutline.loop=false;objectOutline.positionCount=OutlineWalk.Length;
            for(int i=0;i<OutlineWalk.Length;i++)objectOutline.SetPosition(i,c+Vector3.Scale(e,CornerSigns[OutlineWalk[i]]));
            float pulse=reducedMotion?1:.85f+.15f*Mathf.Cos(phase*Mathf.PI*2);
            objectOutline.startWidth=objectOutline.endWidth=.003f*pulse;
            if(!view)view=Camera.main;
            bool show=view&&caption&&!string.IsNullOrWhiteSpace(caption.text)&&Vector3.Dot(view.transform.forward,c-view.transform.position)>0;
            callout.gameObject.SetActive(show);leader.enabled=show;
            if(!show)return;
            var anchor=new Vector3(c.x,bounds.max.y+.035f,c.z);
            float distance=Vector3.Distance(view.transform.position,anchor);
            // Keep angular text size readable as the learner moves, with a bounded physical footprint in VR.
            float scale=Mathf.Clamp(distance*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*.0012f,.00015f,.0024f);
            callout.localScale=Vector3.one*scale;
            callout.position=anchor+Vector3.up*(callout.sizeDelta.y*scale*.5f+.035f);
            var screen=view.WorldToViewportPoint(callout.position);
            float halfHeight=callout.sizeDelta.y*scale/(4*Mathf.Max(.01f,screen.z)*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad));
            float halfWidth=halfHeight*callout.sizeDelta.x/callout.sizeDelta.y/view.aspect;
            float left=1,right=0;
            for(int corner=0;corner<8;corner++)
            {
                var point=view.WorldToViewportPoint(c+Vector3.Scale(bounds.extents,CornerSigns[corner]));
                if(point.z>0){left=Mathf.Min(left,point.x);right=Mathf.Max(right,point.x);}
            }
            // Put the note beside the apparatus, rather than clamping it behind a tall stand.
            float beside=left-halfWidth-.015f;
            if(beside>=clearArea.xMin+halfWidth)screen.x=beside;
            else if(right+halfWidth+.015f<=clearArea.xMax-halfWidth)screen.x=right+halfWidth+.015f;
            screen.x=Mathf.Clamp(screen.x,clearArea.xMin+halfWidth,clearArea.xMax-halfWidth);screen.y=Mathf.Clamp(screen.y,.03f+halfHeight,topViewport-halfHeight);
            callout.position=view.ViewportToWorldPoint(screen);
            callout.rotation=view.transform.rotation;
            var localAnchor=callout.InverseTransformPoint(anchor);float edgeRatio=Mathf.Max(Mathf.Abs(localAnchor.x)/(callout.sizeDelta.x*.5f),Mathf.Abs(localAnchor.y)/(callout.sizeDelta.y*.5f));
            var edge=edgeRatio>0?new Vector3(localAnchor.x/edgeRatio,localAnchor.y/edgeRatio,0):Vector3.zero;
            leader.loop=false;leader.positionCount=2;leader.SetPosition(0,anchor);leader.SetPosition(1,callout.TransformPoint(edge));
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
            if(world&&Time.unscaledTime-started>12){Stop();return;}
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
            RenderObject(bounds,phase);
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
        {if(ring)ring.enabled=visible;if(rim)rim.enabled=visible;if(path)path.enabled=visible;if(dot)dot.enabled=visible;if(objectOutline)objectOutline.enabled=visible;if(leader)leader.enabled=visible;if(!visible&&callout)callout.gameObject.SetActive(false);}
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
                if(!renderer.enabled)continue;
                Bounds visible;
                if(renderer is LineRenderer line)
                {
                    if(line.positionCount==0)continue;
                    Vector3 point=line.GetPosition(0);if(!line.useWorldSpace)point=line.transform.TransformPoint(point);visible=new Bounds(point,Vector3.zero);
                    for(int i=1;i<line.positionCount;i++){point=line.GetPosition(i);if(!line.useWorldSpace)point=line.transform.TransformPoint(point);visible.Encapsulate(point);}
                    visible.Expand(Mathf.Max(line.startWidth,line.endWidth)*line.widthMultiplier);
                }
                else if(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)visible=renderer.bounds;
                else continue;
                if(!found){bounds=visible;found=true;}else bounds.Encapsulate(visible);
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
            if(world)
            {
                if(objectOutline.enabled&&objectOutline.positionCount!=OutlineWalk.Length){failure="Object highlight is incomplete.";return false;}
                foreach(var graphic in callout.GetComponentsInChildren<Graphic>(true))
                    if(graphic.raycastTarget){failure="Object instruction intercepts input.";return false;}
                if(GetComponentInChildren<Collider>()){failure="Guide adds a physical obstacle.";return false;}
            }
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
            foreach(var line in new[]{ring,rim,path,dot,objectOutline,leader})
            {
                if(!line)continue;values.Add(new Vector4(line.enabled?1:0,line.positionCount,line.startWidth,line.endWidth));
                for(int i=0;i<line.positionCount;i++){var p=line.GetPosition(i);values.Add(new Vector4(p.x,p.y,p.z,0));}
            }
            if(callout){var p=callout.position;var s=callout.localScale;values.Add(new Vector4(p.x,p.y,p.z,callout.gameObject.activeSelf?1:0));values.Add(new Vector4(s.x,s.y,s.z,0));}
            return values;
        }

        public void Stop()
        {if(stopped)return;stopped=true;gameObject.SetActive(false);Destroy(gameObject);}
        public void Dispose()=>Stop();
        void OnDestroy(){if(ownedMaterial)Destroy(ownedMaterial);}
    }
}

