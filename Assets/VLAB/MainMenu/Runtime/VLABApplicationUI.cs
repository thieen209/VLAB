using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.SceneFlow;
using LabInput = VLAB.Core.Input.InputManager;

namespace VLAB.MainMenu
{
    public sealed class VLABApplicationUI : MonoBehaviour
    {
        private VLABMenuAssets assets;
        private VLABUI ui;
        private Canvas canvas;
        private RectTransform safe, screen;
        private CanvasGroup group;
        private bool menu, busy, paused, wasLocked;
        private float previousTimeScale;
        private string state, settingsTab="learning", languageDraft;
        private LabInput input;
        private DesktopPlayerRig rig;
        private bool rigEnabled;
        private bool lastRigCursorLocked,inputWasBlocked;
        private CursorLockMode lastCursorLock,savedCursorLock;
        private bool lastCursorVisible,savedCursorVisible;
        private readonly Dictionary<Behaviour,bool> suspendedLabBehaviours = new Dictionary<Behaviour,bool>();
        private readonly Dictionary<BaseRaycaster,bool> suspendedRaycasters=new Dictionary<BaseRaycaster,bool>();
        private readonly List<Canvas> suspendedCanvases = new List<Canvas>();
        private readonly List<bool> canvasStates = new List<bool>();
        private readonly Dictionary<XRBaseInteractable,int> suspendedInteractions = new Dictionary<XRBaseInteractable,int>();
        private VLABLegalPage legalPage;
        private RawImage documentImage;
        private ScrollRect documentScroll;
        private string[] pages;
        private int pageIndex;
        private bool legalPrivacy, legalReadOnly;
        private float zoom=1;
        private float documentWidth;
        private Rect lastSafe;
        private Vector2 lastSize;
        private Coroutine animation;
        private Text pageLabel;
        private Button previousPage,nextPage;
        private Button zoomIn,zoomOut,acceptDocument;
        private string failedScene,errorReturn;
        private int lastBackFrame=-1;
        private readonly Collider[] menuObstacles = new Collider[64];
        private readonly RaycastHit[] menuSightHits = new RaycastHit[32];
        private Vector3 menuTarget;
        private Quaternion menuRotation;
        private float menuScale;
        public bool IsPaused => paused;
        public string CurrentScreen => state;
        public bool IsTransitioning => busy;
        public void BindInput(LabInput source)
        {
            if(input==source)return;
            if(input!=null)input.PausePressed-=NavigateBack;
            input=source;
            if(input!=null)input.PausePressed+=NavigateBack;
        }
        private string L(string vi,string en) => VLABOnboarding.Language=="en" ? en : vi;

        public void Initialize(bool isMenu)
        {
            menu=isMenu;
            assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            ui=new VLABUI(assets != null && assets.font != null ? assets.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            canvas=new GameObject("VLAB Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform,false);
            canvas.sortingOrder=200;
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1440,900);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            // Spatial in the editor as well as on device; the canvas is scene-owned,
            // never head-locked. Both desktop and tracked pointers use the same UI.
            canvas.renderMode=RenderMode.WorldSpace;
            canvas.worldCamera=Camera.main;
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(1440,900);
            canvas.gameObject.layer=LayerMask.NameToLayer("UI");
            var trackedRaycaster=canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            trackedRaycaster.checkFor2DOcclusion=false;
            trackedRaycaster.checkFor3DOcclusion=false;
            PlaceInWorld();
            if(EventSystem.current==null)
            {
                var eventHost=new GameObject("VLAB EventSystem",typeof(EventSystem),typeof(XRUIInputModule));
                eventHost.transform.SetParent(transform,false);
            }
            safe=ui.Rect(canvas.transform,"Safe area",0,0,1,1);
            input=FindAnyObjectByType<LabInput>();
            if(input!=null)input.PausePressed+=NavigateBack;
            Cursor.lockState=CursorLockMode.Locked;
            Cursor.visible=false;
            lastCursorLock=Cursor.lockState;lastCursorVisible=Cursor.visible;
            if(menu)
            {
                AudioListener.volume=PhysicsLabPreferences.SoundEnabled?VLAB.ChemistryLab.LabPreferences.Current.volume:0;
                Show("boot"); StartCoroutine(Boot());
            }
            else
            {
                rig=FindAnyObjectByType<DesktopPlayerRig>();
                lastRigCursorLocked=rig!=null && rig.IsCursorLocked;
                Show("hud");
            }
        }
        private IEnumerator Boot() { yield return null; PlaceInWorld(); Show(VLABOnboarding.Next(assets)); }
        private void Update()
        {
            if(canvas==null)return;
            var size=new Vector2(Screen.width,Screen.height);
            if(canvas.renderMode!=RenderMode.WorldSpace && (lastSafe!=Screen.safeArea || lastSize!=size))
            {
                lastSafe=Screen.safeArea; lastSize=size;
                safe.anchorMin=lastSafe.min/size; safe.anchorMax=lastSafe.max/size;
                if(documentImage!=null) ResizeDocument();
            }
            if(input==null && Keyboard.current?.escapeKey.wasPressedThisFrame==true)NavigateBack();
        }
        private void PlaceInWorld()
        {
            if(canvas.renderMode!=RenderMode.WorldSpace || Camera.main==null)return;
            var view=Camera.main;
            canvas.worldCamera=view;
            var camera=view.transform;
            var forward=camera.forward;
            FindMenuPlacement(view);
            canvas.transform.localScale=Vector3.one*menuScale;
            canvas.transform.SetPositionAndRotation(menuTarget,menuRotation);
        }
        private void FindMenuPlacement(Camera view)
        {
            menuRotation=view.transform.rotation;
            float minimum=Mathf.Max(.18f,view.nearClipPlane+.08f);
            float distance=2.4f;
            for(;distance>minimum;distance-=.05f)
            {
                menuScale=2*distance*Mathf.Tan(Mathf.Min(view.fieldOfView*.38f,18f)*Mathf.Deg2Rad)/900f;
                menuTarget=view.transform.position+view.transform.forward*distance;
                if(MenuVolumeClear(menuTarget,menuRotation,menuScale,view))return;
            }
            menuScale=2*minimum*Mathf.Tan(Mathf.Min(view.fieldOfView*.38f,18f)*Mathf.Deg2Rad)/900f;
            menuTarget=view.transform.position+view.transform.forward*minimum;
        }
        private bool MenuVolumeClear(Vector3 position,Quaternion rotation,float scale,Camera view)
        {
            int count=Physics.OverlapBoxNonAlloc(position,new Vector3(720*scale+.025f,450*scale+.025f,.035f),menuObstacles,rotation,~(1<<LayerMask.NameToLayer("UI")),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var obstacle=menuObstacles[i];
                if(IgnoreMenuObstacle(obstacle,view))continue;
                return false;
            }
            if(count==menuObstacles.Length)return false;
            // Check the viewing cone as well as the panel volume: a free position behind
            // a wall is still unusable. Center plus four corners protect the whole UI.
            for(int corner=0;corner<5;corner++)
            {
                var offset=corner==0?Vector3.zero:new Vector3((corner%2==0?1:-1)*720*scale,(corner<3?1:-1)*450*scale,0);
                var delta=position+rotation*offset-view.transform.position;
                int hits=Physics.RaycastNonAlloc(view.transform.position,delta.normalized,menuSightHits,delta.magnitude,~(1<<LayerMask.NameToLayer("UI")),QueryTriggerInteraction.Ignore);
                if(hits==menuSightHits.Length)return false;
                for(int i=0;i<hits;i++)if(!IgnoreMenuObstacle(menuSightHits[i].collider,view))return false;
            }
            return true;
        }
        private bool IgnoreMenuObstacle(Collider obstacle,Camera view)=>obstacle is CharacterController || obstacle.transform.IsChildOf(transform) || obstacle.transform.IsChildOf(view.transform) || obstacle.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()!=null;
        private void LateUpdate()
        {
#if UNITY_EDITOR
            // Escape releases once for debugging; never fight the editor with a per-frame lock.
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {
                Cursor.lockState=CursorLockMode.None;
                Cursor.visible=true;
            }
#endif
            if(paused && Camera.main!=null)
            {
                var view=Camera.main;
                bool obstructed=!MenuVolumeClear(canvas.transform.position,canvas.transform.rotation,canvas.transform.localScale.x,view);
                if(Vector3.Angle(view.transform.forward,menuTarget-view.transform.position)>22 || Vector3.Distance(view.transform.position,menuTarget)>3 || obstructed)
                    FindMenuPlacement(view);
                float t=1-Mathf.Exp(-10*Time.unscaledDeltaTime);
                // Move immediately out of geometry; otherwise soften substantial head turns.
                canvas.transform.position=obstructed?menuTarget:Vector3.Lerp(canvas.transform.position,menuTarget,t);
                canvas.transform.rotation=Quaternion.Slerp(canvas.transform.rotation,menuRotation,t);
                canvas.transform.localScale=Vector3.one*Mathf.Lerp(canvas.transform.localScale.x,menuScale,t);
            }
            if(!paused)
            {
                if(rig!=null)lastRigCursorLocked=rig.IsCursorLocked;
                lastCursorLock=Cursor.lockState;lastCursorVisible=Cursor.visible;
            }
            if(documentScroll!=null && !Mathf.Approximately(documentWidth,documentScroll.viewport.rect.width))ResizeDocument();
        }
        public void Show(string destination)
        {
            if(busy)return;
            ReleasePage();
            if(animation!=null)StopCoroutine(animation);
            if(screen!=null){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}
            state=destination;
            screen=ui.Rect(safe,"Screen_"+state,0,0,1,1);
            group=screen.gameObject.AddComponent<CanvasGroup>();
            if(state=="hud")
            {
                ui.Button(screen,"OpenLabMenu",L("Menu","Menu"),.86f,.90f,.11f,.065f,TogglePause);
                if(gameObject.scene.name=="ChemistryLab" && GetComponent<VLabActivityWorkbench>()?.Active==null)
                    ui.Button(screen,"OpenChemistryBoard",L("Bảng hóa học","Chemistry controls"),.61f,.90f,.23f,.065f,()=>{PlaceInWorld();Show("chemistry");});
                return;
            }
            var bg=ui.Panel(screen,"Backdrop",0,0,1,1,new Color(.018f,.035f,.065f,menu?(state=="home"?.08f:.68f):.98f));
            bg.raycastTarget=true;
            ui.Panel(screen,"TopAccent",.04f,.972f,.92f,.004f,VLABUI.Cyan);
            ui.Text(screen,"Brand","VLAB",.055f,.865f,.18f,.10f,44).fontStyle=FontStyle.Bold;
            ui.Text(screen,"Descriptor",L("PHÒNG THÍ NGHIỆM ẢO","VIRTUAL LABORATORY"),.057f,.842f,.4f,.028f,15,VLABUI.Muted);
            if(menu && state=="home")
                foreach(var titleElement in new[]{"TopAccent","Brand","Descriptor"})screen.Find(titleElement).gameObject.SetActive(false);
            ui.Text(screen,"Footer",L("Tạo công nghệ cho mọi học sinh","Technology for every student"),.055f,.03f,.6f,.045f,18,VLABUI.Muted);
            ui.Text(screen,"Team","NGỰA MÁN  /  THPT CHUYÊN CAO BẰNG",.57f,.03f,.375f,.045f,14,VLABUI.Muted,TextAnchor.MiddleRight);
            if(state!="boot" && state!="language" && state!="terms" && state!="privacy" && state!="pause" && state!="home")
                ui.Button(screen,"Back",L("Quay lại","Back"),.80f,.895f,.145f,.065f,Back);
            switch(state)
            {
                case "boot": Title(L("Đang khởi động…","Starting…"),L("Sẵn sàng khám phá khoa học.","Get ready to explore science.")); break;
                case "language": Language();break;
                case "terms": Legal(false,false);break;
                case "privacy": Legal(true,false);break;
                case "terms-read": Legal(false,true);break;
                case "privacy-read": Legal(true,true);break;
                case "home": Home();break;
                case "labs": Labs();break;
                case "settings": Settings();break;
                case "chemistry": ChemistryBoard();break;
                case "activities": Activities();break;
                case "help": Help();break;
                case "about": About();break;
                case "pause": PauseView();break;
                case "exit": ExitView();break;
                case "quit": QuitView();break;
                case "error": ErrorView();break;
            }
            animation=StartCoroutine(Fade());
            StartCoroutine(FocusFirst());
        }
        private IEnumerator Fade()
        {
            if(VLAB.Core.Input.VLabComfortSettings.Current.reducedMotion){group.alpha=1;yield break;}
            group.alpha=0;
            for(float t=0;t<.18f;t+=Mathf.Max(Time.unscaledDeltaTime,.004f)){group.alpha=Mathf.Clamp01(t/.18f);yield return null;}
            group.alpha=1; animation=null;
        }
        private IEnumerator FocusFirst()
        {
            yield return null;
            if(screen==null || EventSystem.current==null)yield break;
            foreach(var b in screen.GetComponentsInChildren<Button>())
                if(b.interactable){EventSystem.current.SetSelectedGameObject(b.gameObject);break;}
        }
        private void Title(string title,string subtitle)
        {
            ui.Text(screen,"Title",title,.09f,.72f,.82f,.09f,38).fontStyle=FontStyle.Bold;
            ui.Text(screen,"Subtitle",subtitle,.09f,.65f,.82f,.06f,22,VLABUI.Muted);
        }
        private void Language()
        {
            languageDraft=VLABOnboarding.Language;
            Title("Chọn ngôn ngữ / Choose language","Bạn có thể thay đổi lựa chọn này trong Cài đặt.");
            Button vi=null,en=null;
            Action refresh=()=>
            {
                vi.GetComponent<Image>().color=languageDraft=="vi"?VLABUI.Cyan:VLABUI.Surface;
                en.GetComponent<Image>().color=languageDraft=="en"?VLABUI.Cyan:VLABUI.Surface;
                vi.GetComponentInChildren<Text>().text=languageDraft=="vi"?"Tiếng Việt · Đã chọn":"Tiếng Việt";
                en.GetComponentInChildren<Text>().text=languageDraft=="en"?"English · Selected":"English";
            };
            vi=ui.Button(screen,"Vietnamese","Tiếng Việt",.16f,.40f,.32f,.15f,()=>{languageDraft="vi";refresh();});
            en=ui.Button(screen,"English","English",.52f,.40f,.32f,.15f,()=>{languageDraft="en";refresh();});
            refresh();
            ui.Button(screen,"Continue","Tiếp tục / Continue",.34f,.21f,.32f,.08f,()=>{VLABOnboarding.SetLanguage(languageDraft);Show(VLABOnboarding.Next(assets));},true);
        }
        private void Home()
        {
            ui.Ribbon(screen,L("KHÁM PHÁ • THỰC HÀNH • HIỂU BIẾT","EXPLORE • EXPERIMENT • UNDERSTAND"),.09f,.73f,.82f,.075f);
            Card(.09f,.46f,"01",L("Vật lí","Physics"),L("Chuyển động · lực · năng lượng","Motion · forces · energy"),true);
            Card(.515f,.46f,"02",L("Hóa học","Chemistry"),L("Phân tử · dung dịch · phản ứng","Molecules · solutions · reactions"),true);
            Card(.09f,.20f,"03",L("Sinh học","Biology"),L("Tế bào · kính hiển vi · sự sống","Cells · microscopy · life"),true);
            Card(.515f,.20f,"04",L("Kỹ thuật / Cơ khí","Engineering"),L("Mạch điện · bánh răng · đòn bẩy","Circuits · gears · levers"),true);
            ui.Button(screen,"Settings",L("Cài đặt","Settings"),.09f,.10f,.15f,.065f,()=>Show("settings"));
            ui.Button(screen,"Help",L("Hướng dẫn","Help"),.255f,.10f,.15f,.065f,()=>Show("help"));
            ui.Button(screen,"About",L("Về VLAB","About VLAB"),.42f,.10f,.15f,.065f,()=>Show("about"));
            ui.Button(screen,"EnterLabs",L("Các phòng","All labs"),.585f,.10f,.15f,.065f,()=>Show("labs"));
            ui.Button(screen,"Quit",L("Thoát","Quit"),.75f,.10f,.15f,.065f,()=>Show("quit"));
        }
        private void Labs()
        {
            Title(L("Chọn không gian khám phá","Choose your laboratory"),L("Bốn lĩnh vực. Cùng một hành trình khám phá.","Four disciplines. One journey of discovery."));
            Card(.09f,.36f,"01",L("Vật lý","Physics"),L("Con lắc, lò xo, chuyển động, ma sát và động lượng.","Pendulums, springs, motion, friction and momentum."),true);
            Card(.515f,.36f,"02",L("Hóa học","Chemistry"),L("Khám phá chất và phản ứng hóa học.","Explore substances and chemical reactions."),true);
            Card(.09f,.105f,"03",L("Sinh học","Biology"),L("Khám phá thế giới sự sống.","Discover the living world."),true);
            Card(.515f,.105f,"04",L("Kỹ thuật","Engineering"),L("Tìm hiểu thiết kế và các hệ thống kỹ thuật.","Learn about design and engineering systems."),true);
        }
        private void Card(float x,float y,string index,string title,string description,bool available)
        {
            var sceneName = index=="01" ? PhysicsLabSceneNames.Base : index=="02" ? "ChemistryLab" : index=="03" ? "BiologyLab" : "EngineeringLab";
            available = Application.CanStreamedLevelBeLoaded(sceneName);
            var card=ui.Panel(screen,"Lab_"+index,x,y,.395f,.23f,VLABUI.Surface).transform;
            var accent=index=="03"?new Color32(6,214,160,255):index=="04"?new Color32(255,107,53,255):VLABUI.Cyan;
            card.localPosition+=Vector3.back*(index=="01" || index=="02"?22:12);
            ui.Panel(card,"Edge",0,0,.007f,1,available?accent:new Color(.25f,.35f,.4f));
            ui.Text(card,"Index",index,.06f,.73f,.12f,.18f,18,VLABUI.Cyan);
            ui.Text(card,"Name",title,.20f,.70f,.73f,.23f,29).fontStyle=FontStyle.Bold;
            ui.Text(card,"Description",description,.06f,.35f,.88f,.32f,20,VLABUI.Muted);
            var b=ui.Button(card,"Enter",available?L("Sẵn sàng · Vào phòng  →","Ready · Enter  →"):L("Đang phát triển","Coming soon"),.06f,.07f,.88f,.25f,()=>EnterLab(sceneName),available);
            b.interactable=available;
        }
        private void Legal(bool privacy,bool readOnly)
        {
            legalPrivacy=privacy;legalReadOnly=readOnly;pageIndex=0;zoom=1;
            ui.Text(screen,"LegalTitle",privacy?L("Chính sách bảo mật","Privacy Policy"):L("Điều khoản sử dụng","Terms of Service"),.09f,.735f,.82f,.08f,36).fontStyle=FontStyle.Bold;
            pages=assets==null?null:privacy?(VLABOnboarding.Language=="en"?assets.privacyEn:assets.privacyVi):(VLABOnboarding.Language=="en"?assets.termsEn:assets.termsVi);
            if(pages==null || pages.Length==0)
            {
                ui.Text(screen,"MissingDocument",L("Không thể mở tài liệu. Vui lòng kiểm tra bản cài đặt.","The document could not be opened. Please check the installation."),.12f,.35f,.76f,.25f,28);
                ui.Button(screen,"Retry",L("Thử lại","Retry"),.52f,.17f,.3f,.08f,()=>Show(state));
                ui.Button(screen,"LegalBack",L("Quay lại","Back"),.18f,.17f,.3f,.08f,Back);return;
            }
            var view=ui.Panel(screen,"DocumentViewer",.09f,.265f,.82f,.445f,Color.white);
            view.raycastTarget=true;
            documentScroll=view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            documentScroll.viewport=(RectTransform)view.transform;
            documentScroll.movementType=ScrollRect.MovementType.Clamped;
            documentScroll.scrollSensitivity=55;
            var content=ui.Rect(view.transform,"DocumentPage",0,1,1,0);
            content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
            documentImage=content.gameObject.AddComponent<RawImage>();documentImage.raycastTarget=true;
            documentScroll.content=content;
            previousPage=ui.Button(screen,"PreviousPage","‹",.09f,.185f,.075f,.065f,()=>ChangePage(-1));
            pageLabel=ui.Text(screen,"PageCount","",.18f,.185f,.28f,.065f,21,VLABUI.Muted,TextAnchor.MiddleCenter);
            nextPage=ui.Button(screen,"NextPage","›",.47f,.185f,.075f,.065f,()=>ChangePage(1));
            zoomOut=ui.Button(screen,"ZoomOut","−",.65f,.185f,.075f,.065f,()=>{zoom=Mathf.Max(1,zoom-.25f);ResizeDocument();});
            zoomIn=ui.Button(screen,"ZoomIn","+",.735f,.185f,.075f,.065f,()=>{zoom=Mathf.Min(2.5f,zoom+.25f);ResizeDocument();});
            ui.Text(screen,"ZoomHint",L("Kéo để đọc","Drag to read"),.82f,.185f,.09f,.065f,15,VLABUI.Muted);
            if(!readOnly)
            {
                ui.Button(screen,"LegalBack",L("Quay lại","Back"),.09f,.095f,.24f,.075f,()=>Show(privacy?"terms":"language"));
                acceptDocument=ui.Button(screen,"AcceptDocument",L("Tôi đồng ý · Tiếp tục","I agree · Continue"),.48f,.095f,.43f,.075f,()=>
                {
                    if(legalPage==null || legalPage.texture==null)return;
                    VLABOnboarding.Accept(privacy,assets); Show(VLABOnboarding.Next(assets));
                },true);
            }
            LoadPage();
        }
        private void ChangePage(int delta){pageIndex=Mathf.Clamp(pageIndex+delta,0,pages.Length-1);LoadPage();}
        private void LoadPage()
        {
            UnloadPage();
            legalPage=Resources.Load<VLABLegalPage>(pages[pageIndex]);
            documentImage.texture=legalPage!=null?legalPage.texture:null;
            if(acceptDocument!=null)acceptDocument.interactable=documentImage.texture!=null;
            pageLabel.text=documentImage.texture!=null?L("Trang ","Page ")+(pageIndex+1)+" / "+pages.Length:L("Không thể mở trang","Unable to open page");
            previousPage.interactable=pageIndex>0;nextPage.interactable=pageIndex<pages.Length-1;
            Canvas.ForceUpdateCanvases();ResizeDocument();
            documentScroll.verticalNormalizedPosition=1;documentScroll.horizontalNormalizedPosition=.5f;
        }
        private void ResizeDocument()
        {
            bool hasPage=documentImage!=null && documentImage.texture!=null;
            if(zoomOut!=null)zoomOut.interactable=hasPage && zoom>1;
            if(zoomIn!=null)zoomIn.interactable=hasPage && zoom<2.5f;
            if(documentImage==null || documentImage.texture==null)return;
            documentWidth=documentScroll.viewport.rect.width;
            float width=documentWidth*zoom;
            documentImage.rectTransform.sizeDelta=new Vector2(width,width*documentImage.texture.height/documentImage.texture.width);
            documentScroll.horizontal=zoom>1;
        }
        private void UnloadPage()
        {
            if(documentImage!=null)documentImage.texture=null;
            if(legalPage!=null)
            {
                if(legalPage.texture!=null)Resources.UnloadAsset(legalPage.texture);
                Resources.UnloadAsset(legalPage); legalPage=null;
            }
        }
        private void ReleasePage(){UnloadPage();documentImage=null;documentScroll=null;acceptDocument=null;zoomIn=null;zoomOut=null;}
        private void Settings()
        {
            ui.Ribbon(screen,L("CÀI ĐẶT","SETTINGS"),.16f,.72f,.68f,.078f);
            var tabNames=new[]{"LearningTab","SoundTab","ViewerTab","MotionTab","ControllerTab","AccessTab","GraphicsTab","LanguageTab"};
            var tabIds=new[]{"learning","audio","viewer","motion","controller","access","graphics","language"};
            var tabLabels=new[]{"THỰC HÀNH","ÂM THANH","KÍNH VR","DI CHUYỂN","TAY CẦM","HIỂN THỊ","ĐỒ HỌA","NGÔN NGỮ"};
            for(int i=0;i<tabIds.Length;i++)
            {var id=tabIds[i];ui.Button(screen,tabNames[i],VLABOnboarding.Language=="vi"?tabLabels[i]:id.ToUpperInvariant(),.035f+i*.117f,.615f,.112f,.067f,()=>{settingsTab=id;Show("settings");},settingsTab==id);}
            var comfort=VLAB.Core.Input.VLabComfortSettings.Current;
            if(settingsTab=="learning")
            {
                Row(.46f,L("Hướng dẫn thao tác","Action guidance"),OnOff(PhysicsLabPreferences.GuidanceEnabled),()=>PhysicsLabPreferences.GuidanceEnabled=!PhysicsLabPreferences.GuidanceEnabled);
                Row(.365f,L("Gợi ý khi cần","Contextual hints"),OnOff(PhysicsLabPreferences.GuidanceLevel>0),()=>PhysicsLabPreferences.GuidanceLevel=PhysicsLabPreferences.GuidanceLevel>0?0:1);
                Row(.27f,L("Độ nhạy chuột","Mouse sensitivity"),PhysicsLabPreferences.MouseSensitivity.ToString("0.00"),()=>PhysicsLabPreferences.MouseSensitivity=PhysicsLabPreferences.MouseSensitivity>=.199f?.02f:PhysicsLabPreferences.MouseSensitivity+.02f);
            }
            else if(settingsTab=="motion")
            {
                Row(.46f,"Tốc độ di chuyển",comfort.movementSpeed.ToString("F1")+" m/s",()=>ChangeComfort(()=>comfort.movementSpeed=comfort.movementSpeed>=2.5f?1:comfort.movementSpeed+.5f));
                Row(.365f,"Cách xoay hướng",comfort.smoothTurn?"Xoay liên tục":"Xoay từng nấc",()=>ChangeComfort(()=>comfort.smoothTurn=!comfort.smoothTurn));
                Row(.27f,"Góc xoay mỗi nấc",comfort.snapAngle.ToString("F0")+"°",()=>ChangeComfort(()=>comfort.snapAngle=comfort.snapAngle>=45?15:comfort.snapAngle+15));
            }
            else if(settingsTab=="controller")
            {
                ui.Text(screen,"ControllerStatus",input?.ActiveProviderName??"Chuột / bàn phím",.12f,.51f,.76f,.055f,23,VLABUI.Muted);
                Row(.42f,"Tay trỏ chính",comfort.leftHanded?"Trái":"Phải",()=>ChangeComfort(()=>comfort.leftHanded=!comfort.leftHanded));
                Row(.335f,"Độ dài tia",comfort.pointerLength.ToString("F0")+" m",()=>ChangeComfort(()=>comfort.pointerLength=comfort.pointerLength>=7?3:comfort.pointerLength+1));
                Row(.25f,"Hiệu chỉnh hướng", "Đặt lại",()=>{if(input?.Provider is VLAB.Core.Input.VLabControllerReplayProvider replay)replay.Calibrate();else GetComponent<VLabViewerRuntime>()?.Head?.Recenter();});
            }
            else if(settingsTab=="access")
            {
                Row(.46f,"Chuyển động giao diện",comfort.reducedMotion?"Giảm chuyển động":"Bình thường",()=>ChangeComfort(()=>comfort.reducedMotion=!comfort.reducedMotion));
                Row(.365f,"Cỡ chữ",comfort.textScale.ToString("F2")+"×",()=>ChangeComfort(()=>comfort.textScale=comfort.textScale>=1.14f?1:comfort.textScale+.05f));
                Row(.27f,"Tia tương phản cao",OnOff(comfort.highContrast),()=>ChangeComfort(()=>comfort.highContrast=!comfort.highContrast));
            }
            else if(settingsTab=="audio")
            {
                Row(.46f,L("Âm thanh ứng dụng","Application sound"),OnOff(PhysicsLabPreferences.SoundEnabled),()=>{PhysicsLabPreferences.SoundEnabled=!PhysicsLabPreferences.SoundEnabled;AudioListener.volume=PhysicsLabPreferences.SoundEnabled?VLAB.ChemistryLab.LabPreferences.Current.volume:0;});
                Row(.365f,L("Âm lượng","Volume"),VLAB.ChemistryLab.LabPreferences.Current.volume.ToString("0.0"),()=>ChangeGlobalSettings(s=>s.volume=s.volume>=.99f?0:Mathf.Min(1,s.volume+.1f)));
            }
            else if(settingsTab=="graphics")
            {
                var q=VLAB.ChemistryLab.LabPreferences.Current.quality;
                Row(.46f,L("Chất lượng hiển thị","Display quality"),q==0?L("Tiết kiệm","Low"):q==1?L("Cân bằng","Balanced"):L("Chi tiết","Detailed"),()=>ChangeGlobalSettings(s=>s.quality=(s.quality+1)%3));
                ui.Text(screen,"GraphicsHelp",L("Cân bằng dùng bóng cứng và khử răng cưa 4×.\nChọn Tiết kiệm để giảm tải khi điện thoại nóng.","Balanced uses hard shadows and 4× antialiasing.\nChoose Low to reduce load when the phone warms up."),.12f,.28f,.76f,.13f,22,VLABUI.Muted);
            }
            else if(settingsTab=="viewer")
            {
                var vr=VLabMobileVrMode.Instance;
                Row(.46f,L("Hiển thị kính VR","VR viewer display"),Application.platform==RuntimePlatform.Android?OnOff(VLabMobileVrMode.Enabled):L("Cần điện thoại Android","Requires Android"),()=>vr?.Request(!VLabMobileVrMode.Enabled));
                Row(.365f,L("Hướng nhìn phía trước","Forward direction"),L("Đặt lại","Recenter"),()=>GetComponent<VLabViewerRuntime>()?.Head?.Recenter());
                ui.Text(screen,"ViewerHelp",vr?.Status??L("Đổi chế độ kính tại trang chính. Giữ nút kính để đặt lại hướng.","Switch viewer mode from Home. Hold the viewer button to recenter."),.12f,.245f,.76f,.105f,20,VLABUI.Muted);
            }
            else
            {
                Row(.43f,L("Ngôn ngữ giao diện","Interface language"),VLABOnboarding.Language=="vi"?"Tiếng Việt":"English",()=>VLABOnboarding.SetLanguage(VLABOnboarding.Language=="vi"?"en":"vi"));
                ui.Text(screen,"TranslationScope",L("Ngôn ngữ áp dụng cho menu và tài liệu.\nNội dung thí nghiệm hiện có bằng tiếng Việt.","Language applies to menus and documents.\nExisting experiment content is in Vietnamese."),.12f,.24f,.76f,.14f,22,VLABUI.Muted);
            }
            ui.Text(screen,"Saved",L("Thay đổi được lưu tự động.","Changes are saved automatically."),.12f,.10f,.76f,.05f,19,VLABUI.Muted,TextAnchor.MiddleCenter);
            ui.Button(screen,"SettingsDone",L("Xong","Done"),.36f,.17f,.28f,.065f,Back,true);
        }
        private string OnOff(bool value)=>value?L("Bật","On"):L("Tắt","Off");
        private static void ChangeComfort(Action change){change();VLAB.Core.Input.VLabComfortSettings.Save();}
        private void ChemistryBoard()
        {
            var hub=FindAnyObjectByType<VLAB.ChemistryLab.ChemistryLabLessonHub>();
            if(hub==null){Title(L("Không có bài hóa học","No Chemistry lesson"),"");return;}
            ui.Ribbon(screen,L("PHÒNG HÓA HỌC","CHEMISTRY"),.12f,.73f,.76f,.068f);
            var names=new[]{L("CHUẨN ĐỘ","TITRATION"),L("PIN DANIELL","DANIELL CELL"),L("ĐIỆN PHÂN","ELECTROLYSIS")};
            for(int i=0;i<3;i++)
            {
                int lesson=i;
                ui.Button(screen,"ChemLesson_"+i,names[i],.12f+i*.258f,.625f,.245f,.065f,()=>{hub.Select(lesson);Show("chemistry");},(int)hub.ActiveLesson==i);
            }
            if(hub.ActiveLesson==VLAB.ChemistryLab.ChemistryLabLessonHub.Lesson.Titration)
            {
                var t=hub.Titration;
                ui.Text(screen,"ChemStatus",t.StatusMessage+"\nNaOH: "+t.Experiment.DeliveredVolumeMl.ToString("F2")+" mL · "+t.Experiment.RubricMessage,.12f,.50f,.76f,.11f,22,VLABUI.Muted);
                var labels=new[]{L("1 · Bảo hộ","1 · Safety"),L("2 · Nạp burette","2 · Fill burette"),L("3 · Lấy mẫu","3 · Sample"),L("4 · Chỉ thị","4 · Indicator"),"+1.00 mL","+0.10 mL","+0.01 mL",L("Ghi kết quả","Record result"),L("Làm lại lượt","Reset trial"),L("Reset lượng chất","Reset amounts"),L("Xóa kết quả","Clear results"),L("Cất bảng","Close controls")};
                Action[] actions={t.PerformSafety,t.PrepareBurette,t.AddSample,t.AddIndicator,t.DoseCoarse,t.DoseFast,t.DoseDrop,t.RecordResult,t.ResetTrial,t.ResetChemicalAmounts,t.ClearResults,()=>Show("hud")};
                for(int i=0;i<labels.Length;i++)
                {
                    int action=i;
                    ui.Button(screen,"ChemAction_"+i,labels[i],.12f+(i%2)*.395f,.43f-(i/2)*.067f,.365f,.054f,()=>{if(paused)return;actions[action]();if(action!=11)Show("chemistry");},i==7);
                }
            }
            else
            {
                var experiment=hub.ActiveConfigurable;
                if(experiment==null||experiment.Definition==null)return;
                var definition=experiment.Definition;
                ui.Text(screen,"ChemObjective",definition.learningObjective+"\n"+L("An toàn: ","Safety: ")+definition.safetyNote,.12f,.43f,.76f,.17f,23);
                ui.Text(screen,"ChemInstruction",experiment.CurrentInstruction()+"\n"+experiment.StatusMessage,.12f,.285f,.76f,.13f,22,VLABUI.Muted);
                ui.Button(screen,"ChemAdvance",L("Bước tiếp","Next step"),.12f,.18f,.235f,.07f,()=>{if(paused)return;experiment.Advance();Show("chemistry");},true);
                ui.Button(screen,"ChemRecord",L("Ghi kết quả","Record result"),.382f,.18f,.235f,.07f,()=>{if(paused)return;experiment.RecordResult();Show("chemistry");});
                ui.Button(screen,"ChemReset",L("Làm lại","Reset"),.645f,.18f,.235f,.07f,()=>{if(paused)return;experiment.ResetExperiment();Show("chemistry");});
            }
        }
        private static void ChangeGlobalSettings(Action<VLAB.ChemistryLab.LabUserSettings> change)
        {
            var settings=VLAB.ChemistryLab.LabPreferences.Decode(JsonUtility.ToJson(VLAB.ChemistryLab.LabPreferences.Current));
            change(settings);VLAB.ChemistryLab.LabPreferences.Apply(settings);
            AudioListener.volume=PhysicsLabPreferences.SoundEnabled?settings.volume:0;
        }
        private void Row(float y,string title,string value,Action change)
        {
            ui.Text(screen,"SettingLabel",title,.12f,y,.47f,.075f,25);
            ui.Button(screen,"Setting_"+title,value+"  ›",.62f,y,.26f,.075f,()=>{change();Show("settings");});
        }
        private void Help()
        {
            if(!menu)
            {
                LabHelp();
                return;
            }
            Title(L("Bắt đầu một thí nghiệm","Start an experiment"),L("Quan sát. Thử nghiệm. Rút ra kết luận.","Observe. Experiment. Draw conclusions."));
            ui.Text(screen,"Instructions",L("01   Chọn phòng thí nghiệm và bài thực hành.\n\n02   Đọc mục tiêu và làm theo gợi ý bên cạnh dụng cụ.\n\n03   Thực hiện phép đo, xem kết quả và giải thích.","01   Choose a laboratory and an experiment.\n\n02   Read the objective and follow the apparatus guidance.\n\n03   Take measurements, review results and explanations."),.12f,.30f,.76f,.31f,25);
            ui.Text(screen,"DesktopHelp",L("Trên máy tính: WASD để di chuyển · Chuột phải để nhìn\nE / chuột trái để tương tác · Q để thả · Esc để mở menu", "On desktop: WASD to move · Right mouse to look\nE / left mouse to interact · Q to release · Esc for menu"),.12f,.14f,.76f,.13f,21,VLABUI.Muted);
        }
        private void LabHelp()
        {
            var activity=GetComponent<VLabActivityWorkbench>()?.Active;
            if(activity!=null)
            {
                Title(activity.Title,activity.Objective);
                ui.Text(screen,"LabInstructions",activity.Theory+"\n\n"+activity.Instruction+"\n\n"+activity.Result,.12f,.20f,.76f,.43f,24);
                return;
            }
            var physics=FindAnyObjectByType<ExperimentPhysicalController>();
            var chemistry=FindAnyObjectByType<VLAB.ChemistryLab.ChemistryLabLessonHub>();
            string objective, instructions;
            if(physics?.Content!=null)
            {
                var content=physics.Content;
                objective=content.Objective;
                instructions=content.SetupInstruction+"\n\n"+content.ActionInstruction+"\n\n"+content.Takeaway;
            }
            else if(chemistry!=null)
            {
                var lesson=chemistry.ActiveConfigurable;
                objective=lesson?.Definition!=null ? lesson.Definition.learningObjective : "Xác định nồng độ axit bằng phép chuẩn độ và so sánh các lần đo.";
                instructions=lesson?.Definition!=null ? lesson.CurrentInstruction()+"\n\n"+lesson.Definition.safetyNote : "1. Chọn bảo hộ, nạp burette và lấy mẫu.\n2. Thêm chỉ thị, rót NaOH; gần điểm cuối dùng từng giọt.\n3. Ghi thể tích tại màu hồng nhạt bền và xem kết quả.\n\nBảng hóa học cung cấp cùng thao tác khi dùng kính điện thoại.";
            }
            else if(gameObject.scene.name=="BiologyLab")
            {
                objective="Chuẩn bị tiêu bản biểu bì hành và nhận biết cấu trúc tế bào.";
                instructions="1. Đặt lam kính, nhỏ nước, thêm mẫu hành rồi đậy lamen.\n2. Gắn lam vào bàn kính và khóa hai kẹp.\n3. Chọn vật kính 10×, chỉnh nét; sau đó chuyển 40×.\n4. Chọn nhân, thành tế bào và tế bào chất theo yêu cầu.\n\nLấy nét rõ trước khi tăng độ phóng đại.";
            }
            else if(gameObject.scene.name=="EngineeringLab")
            {
                objective="Lắp mạch LED và giải thích vai trò của điện trở hạn dòng.";
                instructions="1. Đặt điện trở 220 Ω và LED đúng cực.\n2. Nối nguồn +5 V → điện trở → cực A của LED.\n3. Nối cực K về GND; kiểm tra mạch rồi bật nguồn.\n4. So sánh điện trở 100 Ω, 220 Ω và 1 kΩ.\n\nMô hình dùng I ≈ (5 − 2)/R. Điện trở nhỏ làm dòng lớn hơn.";
            }
            else
            {
                objective="Chọn một bài thực hành trên bàn điều khiển.";
                instructions="Đọc mục tiêu, quan sát dụng cụ và làm theo hướng dẫn tại bàn.\nDùng menu để đặt lại bài, chọn bài khác hoặc về menu chính.";
            }
            Title("Hướng dẫn · "+LabName(),objective);
            ui.Text(screen,"LabInstructions",instructions,.12f,.25f,.76f,.37f,24);
            ui.Text(screen,"LabControlHelp","Trỏ và chọn dụng cụ · Q: trả dụng cụ · Esc: mở/đóng menu",.12f,.14f,.76f,.065f,21,VLABUI.Muted);
        }

        public void ResetCurrentExperiment()
        {
            if(menu || busy)return;
            RestoreLab();
            Show("hud");
            var workbench=GetComponent<VLabActivityWorkbench>();
            if(workbench?.Active!=null){workbench.ResetActivity();return;}
            FindAnyObjectByType<GrabController>()?.Release();
            FindAnyObjectByType<VLAB.ChemistryLab.Interaction.DesktopLabGrabber>()?.Release();
            var physics=FindAnyObjectByType<ExperimentPhysicalController>();
            if(physics!=null){physics.RestartExperiment();return;}
            var demo=FindAnyObjectByType<VLAB.DemoLabs.VLabExperimentController>();
            if(demo!=null){demo.ResetExperiment();return;}
            var chemistry=FindAnyObjectByType<VLAB.ChemistryLab.ChemistryLabLessonHub>();
            if(chemistry!=null)
            {
                if(chemistry.ActiveConfigurable!=null)chemistry.ActiveConfigurable.ResetExperiment();
                else chemistry.Titration.ResetTrial();
            }
        }

        public void SelectExperiment()
        {
            if(menu || busy)return;
            if(!paused)TogglePause();
            Show("activities");
        }
        private void Activities()
        {
            Title("Chọn bài · "+LabName(),"Mỗi bài có mục tiêu, hướng dẫn và kết quả riêng.");
            var subject=gameObject.scene.name;
            if(subject=="PhysicsLab_Base")
            {
                var titles=new[]{"Con lắc đơn","Ném xiên","Ma sát","Cổng quang","Dao động lò xo","Bảo toàn động lượng"};
                for(int i=0;i<titles.Length;i++)
                {
                    var sceneName=PhysicsLabSceneNames.ContentScenes[i+1];
                    ui.Button(screen,"Experiment_0"+(i+1),titles[i],.12f+(i%2)*.39f,.48f-(i/2)*.15f,.37f,.11f,()=>
                    {RestoreLab();Show("hud");PhysicsLabSceneFlow.Instance?.LoadContent(sceneName);});
                }
                return;
            }
            var names=subject=="BiologyLab" ? new[]{"Kính hiển vi · tiêu bản hành","Khám phá tế bào 3D"}
                : subject=="EngineeringLab" ? new[]{"Lắp mạch điện LED","Bộ truyền bánh răng","Cân bằng đòn bẩy"}
                : subject=="ChemistryLab" ? new[]{"Chuẩn độ · pin · điện phân","Lắp ráp phân tử nước","Nhận biết phản ứng kết tủa"}
                : new[]{"Bàn chọn sáu thí nghiệm vật lý"};
            var types=subject=="BiologyLab" ? new[]{"","VLabCellActivity"}
                : subject=="EngineeringLab" ? new[]{"","VLabGearActivity","VLabLeverActivity"}
                : subject=="ChemistryLab" ? new[]{"","VLabMoleculeActivity","VLabQualitativeActivity"} : new[]{""};
            for(int i=0;i<names.Length;i++)
            {
                var activityType=types[i];
                ui.Button(screen,"Activity_"+i,names[i],.20f,.49f-i*.13f,.60f,.095f,()=>StartActivity(activityType),i==0);
            }
        }
        public void StartActivity(string activityType)
        {
            if(menu || busy)return;
            RestoreLab();Show("hud");
            var workbench=GetComponent<VLabActivityWorkbench>();
            if(string.IsNullOrEmpty(activityType))
            {
                workbench?.Close();
                if(gameObject.scene.name=="PhysicsLab_Base")PhysicsLabSceneFlow.Instance?.LoadHub();
                else if(gameObject.scene.name=="ChemistryLab"){PlaceInWorld();Show("chemistry");}
                return;
            }
            if(workbench==null)workbench=gameObject.AddComponent<VLabActivityWorkbench>();
            workbench.Open(activityType);
            Show("hud");
        }
        private void About()
        {
            Title(L("Thực hành STEM cho mọi học sinh","STEM practice for every student"),"VLAB · Ngựa mán · THPT Chuyên Cao Bằng");
            ui.Text(screen,"Mission",L("Khám phá khoa học qua phòng thí nghiệm ảo trên điện thoại.\n\nVLAB bổ trợ thực hành trong phòng thí nghiệm thật, giúp học sinh có thêm cơ hội tự tay thử nghiệm và học từ kết quả.","Explore science through a virtual laboratory on your phone.\n\nVLAB supplements real laboratory practice, giving students more opportunities to experiment and learn from results."),.12f,.31f,.76f,.29f,26,VLABUI.Muted);
            ui.Button(screen,"ViewTerms",L("Điều khoản sử dụng","Terms of Service"),.12f,.16f,.36f,.085f,()=>Show("terms-read"));
            ui.Button(screen,"ViewPrivacy",L("Chính sách bảo mật","Privacy Policy"),.52f,.16f,.36f,.085f,()=>Show("privacy-read"));
        }
        public void TogglePause()
        {
            if(menu || busy || PhysicsLabSceneFlow.Instance?.IsTransitioning==true)return;
            if(paused){Resume();return;}
            paused=true;previousTimeScale=Time.timeScale;Time.timeScale=0;
            savedCursorLock=lastCursorLock;savedCursorVisible=lastCursorVisible;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(input!=null){inputWasBlocked=input.BlockExperimentInput;input.BlockExperimentInput=true;}
            // DesktopPlayerRig's earlier Pause listener releases the cursor first.
            // Use the last completed frame's pose to restore the pre-Pause state.
            if(rig!=null){wasLocked=lastRigCursorLocked;rigEnabled=rig.enabled;rig.ReleaseCursor();rig.enabled=false;}
            suspendedLabBehaviours.Clear();
            foreach(var behaviour in FindObjectsByType<MonoBehaviour>())
                if(behaviour is VLAB.ChemistryLab.DesktopLabNavigator || behaviour is VLAB.ChemistryLab.DesktopTitrationInterface || behaviour is VLAB.ChemistryLab.Interaction.DesktopLabGrabber || behaviour is VLAB.ChemistryLab.ChemistryControllerBridge || behaviour is VLAB.DemoLabs.VLabInteractionDriver || behaviour is VLAB.DemoLabs.MicroscopeView || behaviour is UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider)
                { suspendedLabBehaviours[behaviour]=behaviour.enabled; behaviour.enabled=false; }
            suspendedInteractions.Clear();
            foreach(var interactable in FindObjectsByType<XRBaseInteractable>())
            {
                if(interactable.isSelected && interactable.interactionManager!=null)
                    interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)interactable);
                suspendedInteractions[interactable]=interactable.interactionLayers;
                interactable.interactionLayers=0;
            }
            suspendedCanvases.Clear();canvasStates.Clear();
            foreach(var c in FindObjectsByType<Canvas>())
                if(c!=canvas){suspendedCanvases.Add(c);canvasStates.Add(c.enabled);c.enabled=false;}
            suspendedRaycasters.Clear();
            foreach(var raycaster in FindObjectsByType<BaseRaycaster>())
                if(!raycaster.transform.IsChildOf(canvas.transform))
                {suspendedRaycasters[raycaster]=raycaster.enabled;raycaster.enabled=false;}
            PlaceInWorld();Show("pause");
        }
        public void Resume()
        {
            if(!paused || busy)return;
            RestoreLab();Show("hud");
        }
        private void RestoreLab()
        {
            if(!paused)return;
            paused=false;Time.timeScale=previousTimeScale;
            foreach(var pair in suspendedLabBehaviours) if(pair.Key!=null) pair.Key.enabled=pair.Value;
            suspendedLabBehaviours.Clear();
            if(input!=null)input.BlockExperimentInput=inputWasBlocked;
            if(rig!=null){rig.enabled=rigEnabled;rig.SetCursorLocked(wasLocked);}
            else{Cursor.lockState=savedCursorLock;Cursor.visible=savedCursorVisible;}
            foreach(var pair in suspendedInteractions)if(pair.Key!=null)pair.Key.interactionLayers=pair.Value;
            suspendedInteractions.Clear();
            for(int i=0;i<suspendedCanvases.Count;i++)if(suspendedCanvases[i]!=null)suspendedCanvases[i].enabled=canvasStates[i];
            suspendedCanvases.Clear();canvasStates.Clear();
            foreach(var pair in suspendedRaycasters)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            suspendedRaycasters.Clear();
            foreach(var panel in FindObjectsByType<ExperimentContextPanel>())panel.Refresh();
        }
        private string LabName() => gameObject.scene.name == "ChemistryLab" ? L("Hóa học","Chemistry") : gameObject.scene.name == "BiologyLab" ? L("Sinh học","Biology") : gameObject.scene.name == "EngineeringLab" ? L("Kỹ thuật","Engineering") : L("Vật lý","Physics");
        private void PauseView()
        {
            Title(L("Phòng thí nghiệm · " + LabName(), LabName()+" Lab"),L("Đã tạm dừng. Tiếp tục khi bạn sẵn sàng.","Paused. Continue when you are ready."));
            ui.Button(screen,"Resume",L("Tiếp tục thực hành","Resume experiment"),.25f,.49f,.50f,.09f,Resume,true);
            ui.Button(screen,"LabHelp",L("Hướng dẫn","Help"),.25f,.39f,.24f,.075f,()=>Show("help"));
            ui.Button(screen,"LabSettings",L("Cài đặt","Settings"),.51f,.39f,.24f,.075f,()=>Show("settings"));
            ui.Button(screen,"ResetExperiment",L("Đặt lại bài","Reset experiment"),.25f,.29f,.24f,.075f,ResetCurrentExperiment);
            ui.Button(screen,"SelectExperiment",L("Chọn bài","Choose experiment"),.51f,.29f,.24f,.075f,SelectExperiment);
            ui.Button(screen,"ReturnHome",L("Về menu chính","Return to Main Menu"),.25f,.18f,.50f,.075f,()=>Show("exit"));
        }
        private void ExitView()
        {
            Title(L("Rời phòng thí nghiệm?","Leave the laboratory?"),L("Tiến trình của lần thực hành này sẽ không được lưu.","Progress from this experiment session will not be saved."));
            ui.Button(screen,"KeepLearning",L("Tiếp tục thực hành","Keep experimenting"),.19f,.37f,.30f,.10f,Resume,true);
            ui.Button(screen,"ConfirmExit",L("Về menu chính","Return to Main Menu"),.52f,.37f,.30f,.10f,ReturnHome);
        }
        private void Back()
        {
            if(busy)return;
            if(state=="terms-read" || state=="privacy-read")Show("about");
            else if(state=="chemistry")Show("hud");
            else if(state=="error")Show(errorReturn);
            else if(state=="quit")Show("home");
            else if(state=="exit")Show("pause");
            else if(state=="home")Show("quit");
            else if(state=="language" || state=="boot")return;
            else if(state=="pause")Resume();
            else if(state=="privacy")Show("terms");
            else if(state=="terms")Show("language");
            else Show(menu?"home":"pause");
        }
        public void NavigateBack()
        {
            // InputManager and the UI module can report the same Cancel this frame.
            if(busy || lastBackFrame==Time.frameCount)return;
            lastBackFrame=Time.frameCount;
            if(!menu && !paused)TogglePause();else Back();
        }
        private void QuitView()
        {
            Title(L("Thoát VLAB?","Quit VLAB?"),L("Bạn có thể quay lại để tiếp tục khám phá.","You can return to explore again."));
            ui.Button(screen,"CancelQuit",L("Ở lại","Stay"),.19f,.37f,.30f,.10f,()=>Show("home"),true);
            ui.Button(screen,"ConfirmQuit",L("Thoát ứng dụng","Quit application"),.52f,.37f,.30f,.10f,()=>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            });
        }
        private void ErrorView()
        {
            Title(L("Không thể mở không gian","Unable to open this space"),L("Không tải được cảnh. Thử lại hoặc quay về màn hình trước.","The scene could not be loaded. Retry or return to the previous screen."));
            ui.Button(screen,"RetryLoad",L("Thử lại","Retry"),.35f,.35f,.3f,.1f,()=>StartCoroutine(LoadScene(failedScene)),true);
        }
        private void LoadFailed(string sceneName)
        {
            if(state!="error")errorReturn=state;
            failedScene=sceneName;busy=false;Show("error");
        }
        private void EnterLab(string sceneName)
        {
            if(busy)return;
            // Hide and lock the desktop cursor as soon as the player starts an experiment.
            // DesktopPlayerRig applies the same state after the destination scene loads;
            // doing it here prevents the cursor from briefly remaining visible during loading.
            if(!VLAB.Core.Input.VLabHeadPose.PhoneViewer && !UnityEngine.XR.XRSettings.isDeviceActive)
            {
                Cursor.lockState=CursorLockMode.Locked;
                Cursor.visible=false;
            }
            StartCoroutine(LoadScene(sceneName));
        }
        private void ReturnHome()
        {
            if(busy)return;
            StartCoroutine(LoadScene(VLABMenuBootstrap.MenuScene));
        }
        private IEnumerator LoadScene(string sceneName)
        {
            if(!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                LoadFailed(sceneName);yield break;
            }
            busy=true; group.interactable=false;group.blocksRaycasts=true;
            var overlay=ui.Panel(screen,"Loading",0,0,1,1,VLABUI.Background);overlay.raycastTarget=true;
            var label=ui.Text(overlay.transform,"LoadingLabel",L("Đang mở không gian…","Opening your space…"),.15f,.4f,.7f,.2f,36,VLABUI.White,TextAnchor.MiddleCenter);
            AsyncOperation load=null;
            try {load=SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Single);}
            catch(Exception e){Debug.LogException(e);}
            if(load==null){LoadFailed(sceneName);yield break;}
            if(paused)RestoreLab();
            Time.timeScale=1f;
            while(!load.isDone){label.text=L("Đang tải  ","Loading  ")+Mathf.RoundToInt(load.progress/.9f*100)+"%";yield return null;}
        }
        private void OnDestroy()
        {
            if(input!=null)input.PausePressed-=NavigateBack;
            // Explicit resume/exit restores live components. Scene teardown only
            // restores global state; never re-enable objects being destroyed.
            if(paused){Time.timeScale=previousTimeScale;Cursor.lockState=savedCursorLock;Cursor.visible=savedCursorVisible;}
            ReleasePage();
        }
    }
}
