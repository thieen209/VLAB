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
        private readonly List<Material> materials=new List<Material>();
        private GameObject station;
        private Canvas board;
        private Text instruction, result;
        private VLABApplicationUI app;
        private InputManager input;
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
            foreach(var behaviour in FindObjectsByType<MonoBehaviour>())
                if(behaviour is VLAB.DemoLabs.VLabExperimentController || behaviour is VLAB.DemoLabs.VLabInteractionDriver || behaviour is VLAB.DemoLabs.MicroscopeView || behaviour is VLAB.ChemistryLab.DesktopLabNavigator || behaviour is VLAB.ChemistryLab.DesktopTitrationInterface || behaviour is VLAB.ChemistryLab.ChemistryControllerBridge || behaviour is VLAB.ChemistryLab.Interaction.DesktopLabGrabber || behaviour is VLAB.ChemistryLab.Interaction.LabGrabRecovery || behaviour is VLAB.PhysicsLab.Interaction.DesktopPlayerRig || behaviour is UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider)
                {behaviours[behaviour]=behaviour.enabled;behaviour.enabled=false;}
            foreach(var body in FindObjectsByType<Rigidbody>())
            {
                if(IsPreservedRigPart(body.transform,view.transform))continue;
                bodies[body]=new BodyState { Position=body.position,Rotation=body.rotation,Kinematic=body.isKinematic,Sleeping=body.IsSleeping(),Velocity=body.linearVelocity,AngularVelocity=body.angularVelocity };
                body.isKinematic=true;
            }
            foreach(var renderer in FindObjectsByType<Renderer>())
                if(!IsPreservedRigPart(renderer.transform,view.transform)){renderers[renderer]=renderer.enabled;renderer.enabled=false;}
            foreach(var collider in FindObjectsByType<Collider>())
                if(!IsPreservedRigPart(collider.transform,view.transform)){colliders[collider]=collider.enabled;collider.enabled=false;}
            foreach(var canvas in FindObjectsByType<Canvas>())
                if(!canvas.transform.IsChildOf(transform)){behaviours[canvas]=canvas.enabled;canvas.enabled=false;}
            station=new GameObject("VLAB Learning Workbench");station.transform.SetParent(transform,false);
            var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            // Center the combined board and apparatus on the current gaze, without moving the head.
            // The station stays upright and remains fixed after opening.
            const float stageScale=.85f;
            station.transform.localScale=Vector3.one*stageScale;
            station.transform.SetPositionAndRotation(view.transform.position+view.transform.forward*3.1f-Vector3.up*(.47f*stageScale),Quaternion.LookRotation(forward));
            Surface("Work surface",new Vector3(.65f,-.06f,0),new Vector3(2.65f,.12f,1.5f),VLABUI.Surface);
            Surface("Cyan front edge",new Vector3(.65f,-.07f,-.755f),new Vector3(2.6f,.015f,.015f),VLABUI.Cyan);
            Surface("Workbench base",new Vector3(.65f,-.6f,.2f),new Vector3(1.9f,1.05f,1),new Color(.035f,.065f,.10f));
            Surface("Floor",new Vector3(0,-1.18f,-.5f),new Vector3(8,.1f,8),new Color(.025f,.04f,.065f));
            Surface("Back wall",new Vector3(0,.7f,2.0f),new Vector3(8,4,.15f),new Color(.035f,.07f,.10f));
            if(FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>()==null)
                station.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            var content=new GameObject(typeName);content.transform.SetParent(station.transform,false);
            content.transform.localPosition=new Vector3(.65f,0,0);
            Active=(VLabActivity)content.AddComponent(type);Active.Build();Active.Changed+=Refresh;
            BuildBoard();Refresh();
            input=FindAnyObjectByType<InputManager>();
            if(input!=null)input.ResetPressed+=HandleResetInput;
        }
        private bool IsPreservedRigPart(Transform candidate,Transform view)
        {
            return candidate.IsChildOf(transform) || candidate.IsChildOf(view)
                || candidate.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()!=null
                || candidate.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>()!=null;
        }
        private void HandleResetInput(){if(isActiveAndEnabled && Active!=null && Time.timeScale>0)ResetActivity();}
        private void Surface(string name,Vector3 position,Vector3 scale,Color color)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(station.transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
            var material=new Material(Shader.Find("Standard")){color=color};material.SetFloat("_Glossiness",.2f);materials.Add(material);part.GetComponent<Renderer>().sharedMaterial=material;
        }
        private void BuildBoard()
        {
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");var ui=new VLABUI(assets!=null && assets.font!=null?assets.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            board=new GameObject("VLAB Activity Instructions",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            board.transform.SetParent(station.transform,false);board.renderMode=RenderMode.WorldSpace;board.worldCamera=Camera.main;
            board.transform.localPosition=new Vector3(-1.4f,.76f,.1f);board.transform.localRotation=Quaternion.Euler(0,-15,0);board.transform.localScale=Vector3.one*.0018f;
            ((RectTransform)board.transform).sizeDelta=new Vector2(850,800);board.gameObject.layer=LayerMask.NameToLayer("UI");
            board.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            ui.Panel(board.transform,"Panel",0,0,1,1,VLABUI.Surface);
            ui.Ribbon(board.transform,Active.Title,.05f,.88f,.9f,.09f);
            ui.Text(board.transform,"Objective",Active.Objective,.07f,.70f,.86f,.16f,28);
            ui.Text(board.transform,"Theory",TheorySynopsis(Active),.07f,.535f,.86f,.145f,25,VLABUI.Muted);
            ui.Button(board.transform,"ActivityFullTheory","Lý thuyết đầy đủ",.43f,.475f,.50f,.052f,()=>{if(app==null)return;if(!app.IsPaused)app.TogglePause();app.Show("help");});
            instruction=ui.Text(board.transform,"Instruction","",.07f,.29f,.86f,.17f,26);
            result=ui.Text(board.transform,"Result","",.07f,.12f,.86f,.16f,24,VLABUI.White);
            ui.Button(board.transform,"ResetActivity","Đặt lại",.05f,.025f,.28f,.075f,ResetActivity);
            ui.Button(board.transform,"ChooseActivity","Chọn bài",.36f,.025f,.28f,.075f,()=>app?.SelectExperiment());
            ui.Button(board.transform,"ActivityMenu","Menu",.67f,.025f,.28f,.075f,()=>app?.TogglePause());
        }
        private static string TheorySynopsis(VLabActivity activity)
        {
            if(activity is VLabCellActivity)return "Tế bào lá có thành, màng, tế bào chất, không bào, lục lạp, nhân và ti thể. Đây là sơ đồ cắt mở; màu và kích thước là quy ước.";
            if(activity is VLabMoleculeActivity)return "H₂O có hai liên kết O–H, dạng gấp khúc 104,5°. Hai cặp electron không liên kết trên O ảnh hưởng góc liên kết.";
            if(activity is VLabQualitativeActivity)return "Ag⁺ + Cl⁻ → AgCl↓ trắng. Đối chứng NaNO₃ không tạo kết tủa khi thêm AgNO₃. Bài học mô phỏng hiện tượng định tính.";
            if(activity is VLabGearActivity)return "Hai bánh răng ngoài quay ngược chiều. nB/nA = −ZA/ZB. Bánh 12 răng dẫn bánh 24 răng làm tốc độ giảm một nửa.";
            if(activity is VLabLeverActivity)return "Cân bằng khi hai mômen bằng nhau: F tải × d tải = F tác dụng × d tác dụng. Bỏ qua khối lượng thanh và ma sát.";
            var theory=activity.Theory??"";
            return theory.Length<=150?theory:theory.Substring(0,147)+"…";
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
            foreach(var material in materials)if(material!=null)Destroy(material);materials.Clear();
        }
        private void OnDestroy()=>Close();
    }
}
