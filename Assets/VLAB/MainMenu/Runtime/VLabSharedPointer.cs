using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VLAB.Core.Input;
using LabInput = VLAB.Core.Input.InputManager;

namespace VLAB.MainMenu
{
    /// <summary>One visible pointing convention for desktop, viewer and decoded controller input.</summary>
    public sealed class VLabSharedPointer : MonoBehaviour
    {
        private Camera view;
        private LabInput input;
        private GameObject model, dot;
        private LineRenderer line;
        private Material material;
        private PointerEventData uiPointer;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
        private readonly Dictionary<BaseInputModule,bool> modules = new Dictionary<BaseInputModule,bool>();
        private VLabGazeInputModule controllerModule;
        private VLAB.ChemistryLab.DesktopTitrationInterface chemistryInterface;
        private IVLABInputProvider previousProvider;
        private VLabControllerReplayProvider replay;
        private Vector2 aim;
        private uint sequence;
        private bool routing;
        private bool previousActivityPress;
        private VLAB.PhysicsLab.Common.LabInteractable hoveredActivity;
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] nativeRays;
        public Ray CurrentRay { get; private set; }
        public bool HasTarget { get; private set; }
        public LabInput Input => input;

        private IEnumerator Start()
        {
            yield return null; yield return null;
            view=Camera.main;
            if(view==null || EventSystem.current==null)yield break;
            nativeRays=view.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()?.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true);
            input=FindAnyObjectByType<LabInput>();
            if(input==null)
            {
                input=gameObject.AddComponent<LabInput>();
                input.SetProvider(Application.platform==RuntimePlatform.Android ? (IVLABInputProvider)gameObject.AddComponent<PhoneInputProvider>() : gameObject.AddComponent<SimulatorInputProvider>());
            }
            uiPointer=new PointerEventData(EventSystem.current);
            GetComponent<VLABApplicationUI>()?.BindInput(input);
            chemistryInterface=FindAnyObjectByType<VLAB.ChemistryLab.DesktopTitrationInterface>();
            var chemistry=view.GetComponent<VLAB.ChemistryLab.DesktopLabNavigator>();
            if(chemistry!=null)chemistry.SharedInput=input;
            var grabber=view.GetComponent<VLAB.ChemistryLab.Interaction.DesktopLabGrabber>();
            if(grabber!=null)grabber.SharedInput=input;
            material=new Material(Shader.Find("Unlit/Color")){color=VLABUI.Cyan};
            var rayObject=new GameObject("VLAB Controller Ray");rayObject.transform.SetParent(transform,false);
            line=rayObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=2;
            line.startWidth=.0024f;line.endWidth=.0012f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="VLAB Pointer Target";dot.transform.SetParent(transform,false);
            Destroy(dot.GetComponent<Collider>());dot.GetComponent<Renderer>().sharedMaterial=material;
            dot.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            dot.transform.localScale=Vector3.one*.012f;
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            if(assets?.controllerVisual!=null)model=Instantiate(assets.controllerVisual,transform);
        }
        private void Update()
        {
            if(input==null)return;
#if UNITY_EDITOR
            if(Keyboard.current?.f9Key.wasPressedThisFrame==true)ToggleReplay();
            if(replay!=null && ReferenceEquals(input.Provider,replay))
            {
                var k=Keyboard.current;
                aim.x+=((k.lKey.isPressed?1:0)-(k.jKey.isPressed?1:0))*50*Time.unscaledDeltaTime;
                aim.y=Mathf.Clamp(aim.y+((k.kKey.isPressed?1:0)-(k.iKey.isPressed?1:0))*50*Time.unscaledDeltaTime,-75,75);
                var replayState=previousProvider?.ReadState()??default;
                replayState.Look=Gamepad.current?.rightStick.ReadValue()??Vector2.zero;
                replay.Submit(replayState,Quaternion.Euler(aim.y,aim.x,0),++sequence,k.rKey.wasPressedThisFrame);
            }
#endif
            if(input.HasRayProvider!=routing)SetRouting(input.HasRayProvider);
            if(input.Provider is IVLabRayProvider rayProvider && !rayProvider.TryGetRay(view,out _))
            {hoveredActivity?.SetHighlighted(false);hoveredActivity=null;previousActivityPress=false;return;}
            ProcessActivity();
        }
        private void ProcessActivity()
        {
            bool pressed=input.CurrentState.PrimaryPressed;
            var activity=GetComponent<VLabActivityWorkbench>()?.Active;
            if(activity==null || Time.timeScale<=0 || view==null || uiPointer==null || (!input.HasRayProvider && !VLabHeadPose.PhoneViewer && UnityEngine.XR.XRSettings.isDeviceActive && NativePointerAvailable()))
            {previousActivityPress=pressed;return;}
            var point=VLabHeadPose.PhoneViewer || input.HasRayProvider ? new Vector2(Screen.width*.5f,Screen.height*.5f) : Mouse.current?.position.ReadValue()??new Vector2(Screen.width*.5f,Screen.height*.5f);
            var ray=input.PointerRay(view,point);
            VLAB.PhysicsLab.Common.LabInteractable next=null;
            if(VLabPointerUi.Raycast(ray,uiPointer,uiHits).gameObject==null && Physics.Raycast(ray,out var hit,5,~0,QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<VLabActivity>()==activity)
                next=hit.collider.GetComponentInParent<VLAB.PhysicsLab.Common.LabInteractable>();
            if(next!=hoveredActivity){hoveredActivity?.SetHighlighted(false);hoveredActivity=next;hoveredActivity?.SetHighlighted(true);}
            if(pressed && !previousActivityPress && hoveredActivity!=null)
            {hoveredActivity.BeginInteraction();hoveredActivity.Activate();hoveredActivity.EndInteraction();}
            previousActivityPress=pressed;
        }
        public void ToggleReplay()
        {
            if(input==null)return;
            if(replay!=null && ReferenceEquals(input.Provider,replay)){input.SetProvider(previousProvider);replay.Disconnect();return;}
            previousProvider=input.Provider;
            replay=replay??gameObject.AddComponent<VLabControllerReplayProvider>();
            aim=Vector2.zero;replay.Disconnect();input.SetProvider(replay);
        }
        private void SetRouting(bool enabled)
        {
            routing=enabled;
            if(chemistryInterface!=null)chemistryInterface.SpatialOnly=enabled;
            if(EventSystem.current==null)return;
            if(enabled)
            {
                modules.Clear();
                foreach(var module in EventSystem.current.GetComponents<BaseInputModule>())
                {modules[module]=module.enabled;module.enabled=false;}
                controllerModule=EventSystem.current.gameObject.AddComponent<VLabGazeInputModule>();
                controllerModule.Input=input;
            }
            else
            {
                if(controllerModule!=null){controllerModule.enabled=false;Destroy(controllerModule);}
                foreach(var pair in modules)if(pair.Key!=null)pair.Key.enabled=pair.Value;
                modules.Clear();
            }
        }
        private void LateUpdate()
        {
            if(view==null || line==null || input==null)return;
            if(input.Provider is IVLabRayProvider provider && !provider.TryGetRay(view,out _))
            {line.enabled=false;dot.SetActive(false);if(model!=null)model.SetActive(false);HasTarget=false;return;}
            var position=VLabHeadPose.PhoneViewer || input.HasRayProvider || Cursor.lockState==CursorLockMode.Locked ? new Vector2(Screen.width*.5f,Screen.height*.5f) : Mouse.current?.position.ReadValue()??new Vector2(Screen.width*.5f,Screen.height*.5f);
            CurrentRay=input.PointerRay(view,position);
            var length=VLabComfortSettings.Current.pointerLength;
            var endpoint=CurrentRay.GetPoint(length);
            HasTarget=Physics.Raycast(CurrentRay,out var hit,length,~(1<<LayerMask.NameToLayer("Ignore Raycast")),QueryTriggerInteraction.Ignore);
            if(HasTarget)endpoint=hit.point;
            var uiHit=VLabPointerUi.Raycast(CurrentRay,uiPointer,uiHits);
            if(uiHit.gameObject!=null)
            {
                endpoint=uiHit.worldPosition;HasTarget=true;
            }
            var native=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            bool nativeTracked=!routing && !VLabHeadPose.PhoneViewer && NativePointerAvailable() && native.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool tracked) && tracked;
            line.enabled=!nativeTracked;dot.SetActive(!nativeTracked);
            if(model!=null)model.SetActive(!nativeTracked);
            var heading=Quaternion.Euler(0,view.transform.eulerAngles.y,0);
            var origin=input.HasRayProvider?CurrentRay.origin:view.transform.TransformPoint(new Vector3(VLabComfortSettings.Current.leftHanded?-.20f:.20f,-.17f,.60f));
            if(model!=null)model.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(endpoint-origin));
            line.SetPosition(0,origin);line.SetPosition(1,endpoint);dot.transform.position=endpoint;
            material.color=VLabComfortSettings.Current.highContrast?Color.white:HasTarget?new Color(.3f,1,.8f):VLABUI.Cyan;
            line.startWidth=VLabComfortSettings.Current.highContrast?.004f:.0024f;
        }
        private bool NativePointerAvailable()
        {
            if(nativeRays==null)return false;
            foreach(var interactor in nativeRays)
                if(interactor!=null && interactor.isActiveAndEnabled)return true;
            return false;
        }
        private void OnDestroy()
        {
            if(routing)SetRouting(false);
            if(material!=null)Destroy(material);
        }
    }
}
