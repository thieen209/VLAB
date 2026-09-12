using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLAB.Core.Input;

namespace VLAB.MainMenu
{
    /// <summary>Scene-owned supplementary workbench; restores the existing lesson exactly on exit.</summary>
    public sealed class VLabActivityWorkbench : MonoBehaviour
    {
        private readonly Dictionary<Renderer,bool> renderers=new Dictionary<Renderer,bool>();
        private readonly Dictionary<Collider,bool> colliders=new Dictionary<Collider,bool>();
        private readonly Dictionary<Behaviour,bool> behaviours=new Dictionary<Behaviour,bool>();
        private readonly Dictionary<Rigidbody,BodyState> bodies=new Dictionary<Rigidbody,BodyState>();
        private GameObject station;
        private Canvas board;
        private Text instruction, result;
        private VLABApplicationUI app;
        private InputManager input;
        private VLAB.DemoLabs.VLabInteractionDriver demo;
        private VLAB.ChemistryLab.DesktopLabNavigator navigator;
        private bool demoSuspended, navigatorSuspended;
        private struct BodyState
        {
            public Vector3 Position, Velocity, AngularVelocity;
            public Quaternion Rotation;
            public bool Kinematic, Sleeping;
        }
        public VLabActivity Active { get; private set; }
        public void Open(string typeName)
        {
            Close();
            var type=typeof(VLabActivity).Assembly.GetType("VLAB.MainMenu."+typeName);
            if(type==null || !typeof(VLabActivity).IsAssignableFrom(type))throw new ArgumentException("Unknown VLAB activity: "+typeName);
            app=GetComponent<VLABApplicationUI>();
            var view=Camera.main;
            if(view==null)throw new InvalidOperationException("A main camera is required to open a learning workbench.");
            var anchor=FindAnyObjectByType<VLabExperimentStation>();
            if(anchor==null)throw new InvalidOperationException("The lab requires a serialized ExperimentContentAnchor.");
            demo=FindAnyObjectByType<VLAB.DemoLabs.VLabInteractionDriver>();
            if(demo!=null){demo.ReturnHeld();demoSuspended=demo.ExperimentInputSuspended;demo.ExperimentInputSuspended=true;}
            navigator=FindAnyObjectByType<VLAB.ChemistryLab.DesktopLabNavigator>();
            if(navigator!=null){navigatorSuspended=navigator.ExperimentInputSuspended;navigator.ExperimentInputSuspended=true;}
            FindAnyObjectByType<VLAB.ChemistryLab.Interaction.DesktopLabGrabber>()?.Release();
            foreach(var behaviour in FindObjectsByType<MonoBehaviour>())
                if(behaviour is VLAB.DemoLabs.VLabExperimentController || behaviour is VLAB.DemoLabs.MicroscopeView || behaviour is VLAB.ChemistryLab.DesktopTitrationInterface || behaviour is VLAB.ChemistryLab.ChemistryControllerBridge || behaviour is VLAB.ChemistryLab.Interaction.DesktopLabGrabber)
                {behaviours[behaviour]=behaviour.enabled;behaviour.enabled=false;}
            foreach(var root in anchor.GuidedContent)
            {
                if(root==null)continue;
                foreach(var body in root.GetComponentsInChildren<Rigidbody>(true))
                {
                    bodies[body]=new BodyState { Position=body.position,Rotation=body.rotation,Kinematic=body.isKinematic,Sleeping=body.IsSleeping(),Velocity=body.linearVelocity,AngularVelocity=body.angularVelocity };
                    body.isKinematic=true;
                }
                foreach(var behaviour in root.GetComponentsInChildren<Behaviour>(true))
                    if(behaviour is UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable || behaviour is VLAB.ChemistryLab.Interaction.LabGrabRecovery)
                    {behaviours[behaviour]=behaviour.enabled;behaviour.enabled=false;}
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){renderers[renderer]=renderer.enabled;renderer.enabled=false;}
                foreach(var collider in root.GetComponentsInChildren<Collider>(true)){colliders[collider]=collider.enabled;collider.enabled=false;}
            }
            foreach(var canvas in FindObjectsByType<Canvas>())
                if(!canvas.transform.IsChildOf(transform)){behaviours[canvas]=canvas.enabled;canvas.enabled=false;}
            station=new GameObject("VLAB Learning Workbench");station.transform.SetParent(transform,false);
            station.transform.localScale=Vector3.one*.85f;
            station.transform.SetPositionAndRotation(anchor.transform.position,anchor.transform.rotation);
            if(FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)
                station.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            var content=new GameObject(typeName);content.transform.SetParent(station.transform,false);
            content.transform.localPosition=new Vector3(.65f,0,0);
            Active=(VLabActivity)content.AddComponent(type);Active.Build();Active.Changed+=Refresh;
            BuildBoard();Refresh();
            input=FindAnyObjectByType<InputManager>();
            if(input!=null)input.ResetPressed+=HandleResetInput;
        }
        private void HandleResetInput(){if(isActiveAndEnabled && Active!=null && Time.timeScale>0)ResetActivity();}
        private void BuildBoard()
        {
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");var ui=new VLABUI(assets!=null && assets.font!=null?assets.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            board=new GameObject("VLAB Activity Instructions",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            board.transform.SetParent(station.transform,false);board.renderMode=RenderMode.WorldSpace;board.worldCamera=Camera.main;
            board.transform.localPosition=new Vector3(-1.5f,.9f,.1f);board.transform.localRotation=Quaternion.Euler(0,-12,0);board.transform.localScale=Vector3.one*.0018f;
            ((RectTransform)board.transform).sizeDelta=new Vector2(750,780);board.gameObject.layer=LayerMask.NameToLayer("UI");
            board.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            ui.Panel(board.transform,"Panel",0,0,1,1,VLABUI.Surface);
            ui.Ribbon(board.transform,Active.Title,.05f,.88f,.9f,.09f);
            ui.Text(board.transform,"Objective",Active.Objective,.07f,.70f,.86f,.16f,28);
            ui.Button(board.transform,"ActivityFullTheory","Xem lý thuyết →",.07f,.60f,.86f,.065f,()=>{if(app==null)return;if(!app.IsPaused)app.TogglePause();app.Show("help");});
            instruction=ui.Text(board.transform,"Instruction","",.07f,.36f,.86f,.20f,29);
            result=ui.Text(board.transform,"Result","",.07f,.13f,.86f,.20f,27,VLABUI.White);
            ui.Button(board.transform,"ResetActivity","Đặt lại",.05f,.025f,.28f,.075f,ResetActivity);
            ui.Button(board.transform,"ChooseActivity","Chọn bài",.36f,.025f,.28f,.075f,()=>app?.SelectExperiment());
            ui.Button(board.transform,"ActivityMenu","Menu",.67f,.025f,.28f,.075f,()=>app?.TogglePause());
            VLabCollapsiblePanel.Configure(board);
        }
        private void Refresh()
        {
            if(Active==null || instruction==null)return;
            instruction.text=Active.Instruction;
            result.text=Active.Result;
        }
        public void ResetActivity(){if(Active!=null){Active.ResetActivity();Refresh();}}
        public void Close()
        {
            if(demo!=null)demo.ExperimentInputSuspended=demoSuspended;
            if(navigator!=null)navigator.ExperimentInputSuspended=navigatorSuspended;
            demo=null;navigator=null;
            if(input!=null)input.ResetPressed-=HandleResetInput;
            input=null;
            if(Active!=null)Active.Changed-=Refresh;
            Active=null;
            if(station!=null){station.SetActive(false);Destroy(station);station=null;}
            foreach(var pair in renderers)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            foreach(var pair in colliders)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            foreach(var pair in bodies)
            {
                if(pair.Key==null)continue;
                var body=pair.Key;var saved=pair.Value;
                body.position=saved.Position;body.rotation=saved.Rotation;body.isKinematic=saved.Kinematic;
                if(!saved.Kinematic){body.linearVelocity=saved.Velocity;body.angularVelocity=saved.AngularVelocity;}
                if(saved.Sleeping)body.Sleep();else body.WakeUp();
            }
            foreach(var pair in behaviours)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            renderers.Clear();colliders.Clear();behaviours.Clear();bodies.Clear();
        }
        private void OnDestroy()
        {
            // Scene teardown must not re-enable interactables whose manager is being destroyed.
            if(input!=null)input.ResetPressed-=HandleResetInput;
            if(Active!=null)Active.Changed-=Refresh;
        }
    }
}
