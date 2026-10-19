using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace Vlab.Product
{
    [Serializable] public sealed class ExperimentBrief
    {public string id,titleVi,goalVi,theoryVi,variablesVi,examplesVi,mistakesVi,modelVi,testsVi,sources;public string[] stepsVi,assets;}
    [Serializable] public sealed class BriefCatalog {public ExperimentBrief[] items;}
    public sealed class VlabApp:MonoBehaviour
    {
        public static VlabApp Instance{get;private set;}
        public LocalProfile Profile{get;private set;}public ExperimentSession Session{get;private set;}
        public string Route{get;private set;}="boot";public Camera View{get;private set;}
        public VlabInput Input{get;private set;}public ApparatusRig Apparatus{get;private set;}
        public LabPanoramaPreview Preview{get;private set;}
        // The root release gate enables the integrated catalog only after its journey checks pass.
        public static bool IntegratedLessonsEnabled=true;
        public static bool IsLessonAvailable(string id)=>id=="P01"||(IntegratedLessonsEnabled&&LessonSpec.All.ContainsKey(id));
        ProfileStore store;VlabUi ui;RectTransform page,modal;Transform origin;
        BriefCatalog catalog;ExperimentBrief selected;string subject="P",settingsReturn="home",supportReturn="home",notebookReturn="home",status="",legalViewing="";
        string privacyHash,termsHash;GameSettings draft;string draftLocale;int settingsTab;
        bool changing,dirty,explicitAcceptance,writeProtected,roomEntryCancelled,notebookKeyboardRequested;float inputAfter,nextDisplay;int attemptSeed=1;
        TextMeshProUGUI roomLoadStatus;UnityEngine.UI.Image roomLoadFill;UnityEngine.UI.Button roomLoadBack;AsyncOperation activeRoomLoad;
        TextMeshProUGUI liveReading,liveHint,liveTime,liveStage,liveActionLabel;
        UnityEngine.UI.Button liveAction;RectTransform pointerRoot;
        bool modalAllowsSimulation,modalResumeActivity,activityReturnRunning;ExperimentSession modalSession;
        int equipmentPage,variablePage,instrumentPage,readingPage;string inspectionTab="";EquipmentHandle inspectedItem;RectTransform inspectionOutput;string inspectionReadingKeys="",liveReadingError="";AttemptRecord shownResult;
        UnityEngine.UI.RawImage inspectionImage;TextMeshProUGUI inspectionReadings,inspectionStatus;
        readonly Dictionary<string,UnityEngine.UI.Button> inspectionControls=new Dictionary<string,UnityEngine.UI.Button>();
        IExperimentAuthority observedAuthority;bool authorityFrozen;
        LessonGuide guide;ExperimentSession guideSession;int guidePage=-1;bool readinessResume;UnityEngine.UI.Button guideLocate;
        Vector2? selectedInspectionUv;ExperimentSession selectedInspectionSession;TextMeshProUGUI selectedCellSummary;
        RectTransform flatPointerRoot;UnityEngine.UI.Image flatGazeProgress;
        readonly JournalState journal=new JournalState();
        GuideCue worldGuideCue,buttonGuideCue;string cueStep;
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();GameObject gazeTarget;float gazeTime;bool gazeConsumed;Vector2 gazePosition;
        UnityEngine.UI.Image gazeProgress;UniversalRenderPipelineAsset runtimePipeline;
        static readonly Dictionary<string,string> readingVi=new Dictionary<string,string>{{"period","Chu kỳ"},{"timedCycles","Số chu kỳ"},{"measuredTime","Thời gian đo"},{"gravityEstimate","g suy ra"}};
        bool Vi=>(Route=="settings"?draftLocale:Profile.locale)!="en";
        string L(string vi,string en)=>Vi?vi:en;
        Color Accent=>VlabUi.Hex(subject=="P"?"93BAC8":subject=="C"?"BAABC4":subject=="B"?"A8BBA6":"CBB99C");
        public string Status=>status;
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
            catalog=JsonUtility.FromJson<BriefCatalog>(Resources.Load<TextAsset>("Catalog/experiments").text);
            string profilePath=Path.Combine(Application.persistentDataPath,"Profile");
#if UNITY_EDITOR
            string testProfile=UnityEditor.SessionState.GetString("VLAB.TestProfile","");if(!string.IsNullOrEmpty(testProfile))profilePath=testProfile;
#endif
            store=new ProfileStore(profilePath);var loaded=store.Load();Profile=loaded.profile;status=loaded.message;writeProtected=loaded.writeProtected;
            privacyHash=DocumentHash("privacy");termsHash=DocumentHash("terms");
            origin=new GameObject("Learner Origin").transform;origin.SetParent(transform,false);
            var cameraGo=new GameObject("VLAB Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));cameraGo.tag="MainCamera";cameraGo.transform.SetParent(origin,false);View=cameraGo.GetComponent<Camera>();View.nearClipPlane=.04f;View.farClipPlane=40;View.fieldOfView=65;
            Input=gameObject.AddComponent<VlabInput>();Input.Adopt(View);Input.BlocksTouchLook=ray=>
            {
                if(modal||PointerHitsUi(ray))return true;
                return Physics.Raycast(ray,out var hit,3.5f)&&(hit.collider.GetComponentInParent<EquipmentHandle>()||hit.collider.GetComponentInParent<LabFixture>());
            };Input.ModeChanged+=OnModeChanged;
            Preview=gameObject.AddComponent<LabPanoramaPreview>();Preview.Initialize(View,Profile.settings.reducedMotion);
            ui=new VlabUi(transform);var eventGo=new GameObject("VLAB Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));eventGo.transform.SetParent(transform,false);eventGo.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var grading=new GameObject("Daylight grading",typeof(UnityEngine.Rendering.Volume));grading.transform.SetParent(transform,false);var volume=grading.GetComponent<UnityEngine.Rendering.Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=Resources.Load<UnityEngine.Rendering.VolumeProfile>("UI/VlabColor");
            SceneManager.sceneLoaded+=OnSceneLoaded;
        }
        void Start(){AdoptScene();ApplySettings();ShowSetupOrHome();}
        void OnDestroy(){if(activeRoomLoad!=null&&!activeRoomLoad.isDone)activeRoomLoad.allowSceneActivation=true;if(Instance==this){SceneManager.sceneLoaded-=OnSceneLoaded;Instance=null;}}
        string DocumentHash(string id)
        {var vi=Resources.Load<TextAsset>("Legal/"+id+"_vi");var en=Resources.Load<TextAsset>("Legal/"+id+"_en");return vi&&en?ProfileStore.Hash(vi.text+"\n"+en.text):"";}
        bool Save()
        {if(Session!=null&&!Session.Complete)Profile.checkpoint=Session.Checkpoint();if(store.Save(Profile,out var error)){writeProtected=false;return true;}status=error;return false;}
        void ShowSetupOrHome(){switch(Profile.NextSetup(privacyHash,termsHash)){case "language":ShowLanguage();break;case "privacy":ShowLegal("privacy",true);break;case "terms":ShowLegal("terms",true);break;case "safety":ShowSafety();break;default:ShowHome();break;}}
        RectTransform NewPage(string route,string backgroundExperiment=null)
        {
            ClearGuideCue();
            if(Route=="activity"&&route!="activity"){activityReturnRunning=ActivityRunIntent();Session?.Pause();}
            Route=route;ui.Vietnamese=Vi;inputAfter=Time.unscaledTime+.22f;gazeTarget=null;gazeTime=0;gazeConsumed=false;
            RefreshBackground(backgroundExperiment);
            if(route!="activity")Apparatus?.CancelGrab();
            if(page){page.gameObject.SetActive(false);Destroy(page.gameObject);}if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}
            modalAllowsSimulation=modalResumeActivity=false;modalSession=null;ClearInspection();
            page=ui.Page(route);PlaceReadableSurface(page,route!="explore");Input.Gameplay=route=="activity"||route=="explore";RoomBranding.SetVisible(SceneManager.GetActiveScene(),route!="home");
            SyncAuthority();
            liveReading=liveHint=liveTime=liveStage=liveActionLabel=null;liveAction=null;return page;
        }
        void RefreshBackground(string experimentId=null)
        {
            if(Route=="activity"||Route=="explore"||Route=="loading"){Preview.Clear();return;}
            if(Route=="home"){Preview.Show("Home");return;}
            string context=Route=="help"?supportReturn:Route=="settings"?settingsReturn:Route;
            if(experimentId==null&&context=="notebook")
            {
                string linked=journal.notesMode?Profile.notebookPages.ElementAtOrDefault(journal.notePage)?.experimentId:journal.experiment;
                if(!string.IsNullOrEmpty(linked))experimentId=linked;
            }
            string id=experimentId??(context=="browser"||context=="readiness"?selected?.id:context=="results"?shownResult?.experimentId:Session?.spec.id??Profile.checkpoint?.experimentId??selected?.id);
            string code=string.IsNullOrEmpty(id)?subject:id.Substring(0,1);
            Preview.Show(code=="P"?"Physics":code=="C"?"Chemistry":code=="B"?"Biology":"Engineering");
        }
        void PlaceReadableSurface(RectTransform content,bool foreground)
        {
            if(!content)return;bool close=foreground&&ui.canvas.renderMode==RenderMode.WorldSpace;
            // Reading surfaces sit in front of bench cabinets; the head and authored apparatus stay still.
            bool home=close&&content==page&&Route=="home";
            content.localPosition=home?ui.canvas.transform.InverseTransformPoint(View.transform.position+View.transform.forward*1.25f):new Vector3(0,0,close?-625:0);content.localScale=Vector3.one*(close?(home?.96f:.75f):1);
            // Face the opening Home pose once; subsequent head movement leaves it anchored.
            content.localRotation=home?Quaternion.Inverse(ui.canvas.transform.rotation)*View.transform.rotation:Quaternion.identity;
        }
        TextMeshProUGUI modeLabel;
        string ModeLabel=>Input.VrMode?L("Thoát VR","Exit VR"):Application.isEditor?(Input.SimulatorMode?L("Thoát mô phỏng","Exit simulation"):L("Mô phỏng VR","Simulate VR")):L("Vào VR","Enter VR");
        void ModeButton(Transform parent,float x,float y,float width,float height)
        {
            var button=Button(parent,ModeLabel,x,y,width,height,()=>{Session?.Pause();if(Application.isEditor)Input.SetSimulator(!Input.SimulatorMode);else Input.SetVr(!Input.VrMode);});
            button.name="VR mode";modeLabel=button.GetComponentInChildren<TextMeshProUGUI>();
        }
        void Header(string title,Action back=null)
        {
            var bar=ui.Panel(page,"Header",40,30,1520,85);ui.Text(bar,title,back==null?24:180,20,1050,60,40).fontWeight=FontWeight.SemiBold;
            if(back!=null)Button(bar,L("‹ Quay lại","‹ Back"),12,12,155,60,back);
            ModeButton(bar,1290,12,210,60);
        }
        UnityEngine.UI.Button Button(Transform parent,string label,float x,float y,float w,float h,Action action,Color? accent=null)
        {return ui.Button(parent,label,x,y,w,h,()=>{if(Time.unscaledTime>=inputAfter&&!changing)action();},accent);}
        void FooterStatus(){if(!string.IsNullOrEmpty(status))ui.Text(page,status,100,920,1400,64,18,VlabUi.Secondary);}
        void Logo(Transform parent,float x,float y,float w,float h,bool roomBackdrop=false)
        {
            var tex=Resources.Load<Texture2D>(roomBackdrop?"Branding/RoomBanner":"UI/vlab-banner-original");if(!tex)return;
            var raw=ui.Picture(parent,tex,x,y,w,h);raw.name="Original VLAB logo";
            // Bounds are measured from the supplied transparent image by the build tool; source pixels remain unchanged.
            var crop=Resources.Load<TextAsset>("UI/logo-crop");if(crop){var v=crop.text.Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();raw.uvRect=new Rect(v[0],v[1],v[2],v[3]);}
        }
        void ShowLanguage()
        {
            if(Route!="language")draftLocale=Profile.locale;
            NewPage("language");var box=ui.Panel(page,"Language",370,190,860,620);Logo(box,235,35,390,124);ui.Text(box,"Chào mừng / Welcome",40,185,780,65,36,null,TextAlignmentOptions.Center);
            Button(box,(draftLocale=="vi"?"✓ ":"")+"Tiếng Việt",100,290,310,80,()=>{draftLocale="vi";ShowLanguage();},draftLocale=="vi"?Accent:(Color?)null);
            Button(box,(draftLocale=="en"?"✓ ":"")+"English",450,290,310,80,()=>{draftLocale="en";ShowLanguage();},draftLocale=="en"?Accent:(Color?)null);
            ui.Text(box,"Language → Privacy → Terms → Safety",50,430,760,50,20,VlabUi.Secondary,TextAlignmentOptions.Center);FooterStatus();
            var next=Button(box,draftLocale=="en"?"Continue":"Tiếp tục",310,510,240,64,()=>ChooseLanguage(draftLocale),Accent);next.interactable=!writeProtected&&(draftLocale=="vi"||draftLocale=="en");next.gameObject.SetActive(!writeProtected);
            if(next.interactable)EventSystem.current.SetSelectedGameObject(next.gameObject);
            if(writeProtected)Button(box,L("Khôi phục hồ sơ","Profile recovery"),180,520,500,64,()=>Confirm(status+"\n"+L("Đặt lại hồ sơ này?","Reset this local profile?"),()=>ResetProfile(true)));
        }
        public void ChooseLanguage(string locale){Profile.locale=locale;if(Save())ShowSetupOrHome();else ShowLanguage();}
        void ShowLegal(string id,bool setup)
        {
            legalViewing=id;explicitAcceptance=false;NewPage(setup?id:"legal");var box=ui.Panel(page,"Legal",370,150,860,700);
            ui.Text(box,id=="privacy"?L("Chính sách bảo mật","Privacy policy"):L("Điều khoản sử dụng","Terms of service"),40,30,780,65,36);
            var doc=Resources.Load<TextAsset>("Legal/"+id+(Vi?"_vi":"_en"));
            if(!doc){ui.Text(box,L("Không tìm thấy tài liệu. Không thể ghi nhận chấp thuận.","Document unavailable. Acceptance cannot be recorded."),40,130,780,180);Button(box,L("Thử lại","Retry"),40,540,240,64,()=>ShowLegal(id,setup));return;}
            ui.Scroll(box,doc.text,40,110,780,390);
            if(setup)
            {
                var consent=Button(box,L("[ ] Tôi đã đọc và đồng ý","[ ] I have read and accept"),40,520,780,64,()=>{});
                consent.onClick.RemoveAllListeners();consent.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter)return;explicitAcceptance=!explicitAcceptance;consent.GetComponentInChildren<TextMeshProUGUI>().text=(explicitAcceptance?"[x] ":"[ ] ")+L("Tôi đã đọc và đồng ý","I have read and accept");});
                Button(box,L("Tiếp tục","Continue"),580,610,240,64,()=>{if(!explicitAcceptance){Toast(L("Cần xác nhận rõ ràng để tiếp tục.","Explicit acceptance is required."));return;}Profile.Accept(id,id=="privacy"?privacyHash:termsHash,true);if(Save())ShowSetupOrHome();},Accent);
            }
            else{Button(box,L("Quay lại","Back"),40,610,240,64,ShowAbout);Button(box,"PDF",580,610,240,64,()=>StartCoroutine(LocalDocuments.OpenPdf(id+(Vi?"_vi":"_en")+".pdf",Toast)));}
            FooterStatus();
        }
        void ShowSafety()
        {
            NewPage("safety");var box=ui.Panel(page,"Safety",370,190,860,620);ui.Text(box,L("Trước khi vào phòng","Before entering the laboratory"),40,40,780,65,36);
            SafetyPresentation.Instructions(ui,box,Vi);
            Button(box,L("Tôi đã hiểu · Vào VLAB","I understand · Enter VLAB"),160,500,540,70,()=>{Profile.safetyAcknowledged=true;if(Save())ShowSetupOrHome();},Accent);FooterStatus();
        }
        public void ShowHome()
        {
            if(Profile.NextSetup(privacyHash,termsHash)!="home"){ShowSetupOrHome();return;}
            Session?.Pause();NewPage("home");HomePresentation.Backdrop(ui,page);Logo(page,605,65,390,124);ModeButton(page,1290,64,230,72);
            string[] subjects={"P","C","B","M"},namesVi={"Vật lí","Hóa học","Sinh học","Kỹ thuật"},namesEn={"Physics","Chemistry","Biology","Engineering"},desVi={"Chuyển động và năng lượng","Chất và phản ứng","Sự sống và tế bào","Cơ cấu và đo lường"},desEn={"Motion and energy","Matter and reactions","Life and cells","Mechanisms & measurement"},images={"physics","chemistry","biology","engineering"};
            var cards=new RectTransform[4];for(int i=0;i<4;i++)
            {
                int n=i;Color color=VlabUi.Hex(new[]{"93BAC8","BAABC4","A8BBA6","CBB99C"}[i]);var card=HomePresentation.Card(ui,page,"Subject "+subjects[i],images[i],Vi?namesVi[i]:namesEn[i],Vi?desVi[i]:desEn[i],color,i);cards[i]=card;
                var button=card.gameObject.AddComponent<UnityEngine.UI.Button>();button.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter||changing)return;subject=subjects[n];selected=catalog.items.First(x=>x.id.StartsWith(subject));StartCoroutine(Portal(card));});
                var focus=card.gameObject.AddComponent<PortalFocus>();focus.FocusColor=color;focus.ReducedMotion=Profile.settings.reducedMotion;focus.Focus=on=>{if(on)Preview.Show(namesEn[n]);else RefreshBackground();};
            }
            page.gameObject.AddComponent<HomePortalArc>().Cards=cards;
            string[] utility={L("Cài đặt","Settings"),L("Trợ giúp","Help"),L("Giới thiệu","About"),L("Sổ tay","Notebook")};Action[] actions={()=>OpenSettings("home"),()=>ShowHelp("home"),ShowAbout,()=>ShowNotebook("home")};
            for(int i=0;i<4;i++){int n=i;var button=HomePresentation.Utility(ui,page,utility[i],i,()=>{if(Time.unscaledTime>=inputAfter&&!changing)actions[n]();});if(i==3)button.name="Open journal";}
            HomePresentation.Helper(ui,page,L("Nhìn hoặc trỏ để chọn","Look or point to select"),918);FooterStatus();
        }
        IEnumerator Portal(RectTransform selectedCard)
        {
            if(Profile.settings.reducedMotion){ShowBrowser();yield break;}
            changing=true;float elapsed=0,duration=.32f;
            selectedCard.GetComponent<PortalFocus>().enabled=false;
            var group=page.gameObject.AddComponent<CanvasGroup>();
            while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,elapsed/duration);if(Profile.settings.reducedMotion)group.alpha=1-t;else{selectedCard.localScale=Vector3.one*(1+.5f*t);group.alpha=1-.35f*t;}yield return null;}
            changing=false;ShowBrowser();
        }
        string SubjectName=>subject=="P"?L("Vật lí","Physics"):subject=="C"?L("Hóa học","Chemistry"):subject=="B"?L("Sinh học","Biology"):L("Kỹ thuật","Engineering");
        string SceneName=>subject=="P"?"Physics":subject=="C"?"Chemistry":subject=="B"?"Biology":"Engineering";
        public void ShowBrowser()
        {
            NewPage("browser");var box=ui.Panel(page,"Experiment browser",140,130,1320,750);
            Button(box,L("‹ Trang chủ","‹ Home"),32,28,220,64,ShowHome).name="Browser home";
            var heading=ui.Text(box,SubjectName,290,28,820,65,40);heading.fontWeight=FontWeight.SemiBold;
            var lessons=catalog.items.Where(x=>x.id.StartsWith(subject)).OrderBy(x=>x.id,StringComparer.Ordinal).ToArray();if(selected==null||!selected.id.StartsWith(subject))selected=lessons[0];
            ui.Text(box,lessons.Length+L(" thí nghiệm"," experiments"),1140,28,148,60,20,VlabUi.Secondary,TextAlignmentOptions.Center).name="Browser lesson count";
            for(int i=0;i<lessons.Length;i++)
            {
                var brief=lessons[i];int saved=Profile.attempts.Count(record=>record.experimentId==brief.id);
                string savedLabel=saved==0?L("Chưa có số đo","No saved readings"):saved+L(" lượt đã lưu",saved==1?" saved attempt":" saved attempts");
                var button=Button(box,LessonGuide.ShortTitle(brief.id,Vi)+"\n<size=20>"+savedLabel+"</size>",32+(i%2)*420,145+(i/2)*124,400,112,()=>{selected=brief;ShowBrowser();},selected==brief?Accent:(Color?)null);
                button.name="Browser lesson "+brief.id;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=ui.LargeText?29.12f:26;
                if(selected==brief)ui.Rule(button.transform,0,12,5,88,UiTheme.On(Accent));
            }
            ui.Rule(box,870,145,1,569,VlabUi.Stroke);
            var title=ui.Text(box,LessonGuide.ShortTitle(selected.id,Vi),890,142,398,88,32);title.fontWeight=FontWeight.SemiBold;title.name="Browser selected title";
            var preview=Button(box,L("Khám phá phòng","Explore laboratory"),890,242,398,184,()=>StartCoroutine(LoadRoom(false,false)));preview.name="Browser explore";preview.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
            var photo=Resources.Load<Texture2D>("UI/Preview_"+SceneName);
            if(photo)
            {
                var image=ui.Picture(preview.transform,photo,0,0,398,184);image.transform.SetAsFirstSibling();
                float crop=Mathf.Min(1,(float)photo.width/photo.height/(398f/184));image.uvRect=new Rect(0,(1-crop)/2,1,crop);
                var shade=ui.Rule(preview.transform,0,128,398,56,VlabUi.Hex("202B36"));shade.SetSiblingIndex(1);
                var caption=preview.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(12,-134);caption.rectTransform.sizeDelta=new Vector2(374,44);caption.color=Color.white;
            }
            ui.Text(box,ReadinessView.Goal(selected.id,Vi),890,442,398,112,24,VlabUi.Secondary).name="Browser selected goal";
            Button(box,L("Ôn bài & công thức","Study & formulas"),890,650,398,64,()=>ShowNotebook("browser")).name="Browser study";
            var start=Button(box,IsLessonAvailable(selected.id)?L("Bắt đầu thí nghiệm","Start experiment"):L("Đang chuẩn bị","In development"),890,564,398,80,()=>ShowReadiness(false),Accent);
            start.name="Browser start";start.interactable=IsLessonAvailable(selected.id);start.GetComponentInChildren<TextMeshProUGUI>().fontSize=ui.LargeText?31.36f:28;
            FooterStatus();
        }
        void ShowReadiness(bool resume)
        {
            readinessResume=resume;NewPage("readiness");
            ReadinessView.Render(ui,page,selected,Vi,Accent,resume,ShowBrowser,()=>ShowHelp("readiness"),()=>StartCoroutine(LoadRoom(true,resume)));
        }
        public void CancelRoomEntry()
        {
            if(!changing||Route!="loading"||roomEntryCancelled)return;
            roomEntryCancelled=true;
            if(roomLoadStatus)roomLoadStatus.text=L("Đang hủy vào trạm…\nChờ Unity hoàn tất nạp phòng rồi quay lại danh sách. Lượt mới sẽ không bắt đầu.","Cancelling station entry…\nWaiting for Unity to finish loading the room before returning to the browser. A new trial will not start.");
            if(roomLoadBack)roomLoadBack.interactable=false;
        }
        void ShowRoomLoadError(bool experiment,bool resume,string scene,string diagnostic)
        {
            changing=false;roomLoadStatus=null;roomLoadFill=null;roomLoadBack=null;
            if(!string.IsNullOrEmpty(diagnostic))Debug.LogWarning("VLAB room load: "+diagnostic);
            NewPage("load-error");var box=ui.Panel(page,"Room load problem",350,220,900,560);
            ui.Text(box,L("Chưa mở được phòng","The room could not be opened"),40,40,820,80,34);
            ui.Text(box,L("Phòng chưa sẵn sàng hoặc có lỗi khi mở. Số đo đã lưu vẫn còn. Thử lại hoặc quay về danh sách để chọn bài khác.","The room is unavailable or could not be opened. Saved observations are retained. Retry, or return to the browser to choose another lesson."),40,160,820,180,26);
            Button(box,L("Quay lại","Back"),40,440,360,70,ShowBrowser);
            Button(box,L("Thử lại","Retry"),450,440,410,70,()=>StartCoroutine(LoadRoom(experiment,resume,scene)),Accent);
        }
        IEnumerator LoadRoom(bool experiment,bool resume,string sceneOverride=null)
        {
            if(changing)yield break;changing=true;roomEntryCancelled=false;Session?.Pause();NewPage("loading");
            var requested=selected;string scene=sceneOverride??SceneName;
            var box=ui.Panel(page,"Room loading",350,250,900,500);
            ui.Text(box,L("Đang mở phòng thí nghiệm","Opening laboratory"),40,35,820,75,34);
            roomLoadStatus=ui.Text(box,L("Đang nạp dữ liệu phòng…","Loading room data…"),40,135,820,140,26);
            var track=ui.Panel(box,"Native load progress",40,295,820,16,VlabUi.Raised);
            roomLoadFill=ui.Rule(track,0,0,0,16,Accent).GetComponent<UnityEngine.UI.Image>();
            // Native Unity scene IO cannot abort. Cancel only revokes the intent to enter a lesson.
            roomLoadBack=ui.Button(box,L("Hủy và quay lại","Cancel and return"),40,375,820,70,()=>{if(Time.unscaledTime>=inputAfter)CancelRoomEntry();});
            if(!Application.CanStreamedLevelBeLoaded(scene)){ShowRoomLoadError(experiment,resume,scene,"Scene unavailable: "+scene);yield break;}
            AsyncOperation load=null;string failure=null;
            try{load=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);}catch(Exception error){failure=error.Message;}
            if(load==null){ShowRoomLoadError(experiment,resume,scene,failure??"SceneManager returned no operation.");yield break;}
            // Keep the progress UI reachable while native IO completes; activation is never represented as cancellable IO.
            activeRoomLoad=load;load.allowSceneActivation=false;
            while(!load.isDone)
            {
                if(roomLoadFill)roomLoadFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,820*Mathf.Clamp01(load.progress/.9f));
                if(!roomEntryCancelled&&roomLoadStatus)roomLoadStatus.text=load.progress<.9f?
                    L("Đang nạp dữ liệu phòng: ","Loading room data: ")+Mathf.FloorToInt(Mathf.Clamp01(load.progress/.9f)*100)+"%":
                    L("Dữ liệu đã nạp. Đang mở phòng…","Room data loaded. Opening the room…");
                // Scene activation may need several frames even after loading reaches0.9. Never claim it was aborted.
                if(load.progress>=.9f)load.allowSceneActivation=true;
                yield return null;
            }
            activeRoomLoad=null;roomLoadStatus=null;roomLoadFill=null;roomLoadBack=null;changing=false;
            if(roomEntryCancelled)
            {Session=null;Apparatus=null;selected=requested;ShowBrowser();yield break;}
            if(experiment)
            {
                GameObject rigGo=null;failure=null;
                try
                {
                    Session=resume&&Profile.checkpoint!=null?ExperimentSession.Restore(Profile.checkpoint):new ExperimentSession(requested.id,attemptSeed++);
                    rigGo=new GameObject("Experiment apparatus");Apparatus=rigGo.AddComponent<ApparatusRig>();
                    Vector3 worktop=FindWorktop();Apparatus.Build(Session,worktop);Apparatus.Changed+=RefreshActivity;
                    var eye=worktop+new Vector3(-.4f,.75f,-2.5f);var look=worktop+new Vector3(-.55f,.75f,0);
                if(Session.spec.id=="P02")look.x=Apparatus.items.Single(item=>item.assetId=="launcher").slot.x+.25f;
                else if(Session.spec.id=="P08")look.x-=.2f;
                    origin.position=eye;origin.rotation=Quaternion.LookRotation(look-eye,Vector3.up);View.transform.localPosition=Vector3.zero;View.transform.localRotation=Quaternion.identity;
                }
                catch(Exception error){failure=error.Message;}
                if(failure!=null)
                {if(rigGo)Destroy(rigGo);Apparatus=null;Session=null;ShowRoomLoadError(experiment,resume,scene,failure);yield break;}
                ShowActivity();Save();
            }
            else{Session=null;ShowExplore();}
        }
#if UNITY_EDITOR
        // Exercise the real loading/recovery code, including an intentionally unavailable scene.
        public IEnumerator CheckRoomLoad(string scene,bool experiment=true,bool resume=false)=>LoadRoom(experiment,resume,scene);
#endif
        Vector3 FindWorktop()
        {
            var tops=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(x=>x.bounds.max.y>.72f&&x.bounds.max.y<1.12f&&x.bounds.size.x>.65f&&x.bounds.size.z>.3f&&(x.name.IndexOf("top",StringComparison.OrdinalIgnoreCase)>=0||x.name.IndexOf("bench",StringComparison.OrdinalIgnoreCase)>=0)).OrderByDescending(x=>x.bounds.size.x*x.bounds.size.z).ToArray();
            if(tops.Length>0){var b=tops[0].bounds;return new Vector3(b.center.x,b.max.y+.005f,b.center.z);}
            return new Vector3(2.4f,.93f,2.9f);
        }
        void OnSceneLoaded(Scene scene,LoadSceneMode mode){AdoptScene();}
        void AdoptScene()
        {
            var cameras=FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach(var cam in cameras)
            {
                if(cam==View)continue;if(cam.name.Contains("Learner")){origin.SetPositionAndRotation(cam.transform.position,Quaternion.Euler(0,cam.transform.eulerAngles.y,0));View.transform.localPosition=Vector3.zero;View.transform.localRotation=Quaternion.Euler(cam.transform.eulerAngles.x,0,0);}
                cam.enabled=false;cam.tag="Untagged";var listener=cam.GetComponent<AudioListener>();if(listener)listener.enabled=false;
            }
            foreach(var review in FindObjectsByType<LabReview>(FindObjectsSortMode.None))review.enabled=false;
            foreach(var interactor in FindObjectsByType<LabInteractor>(FindObjectsSortMode.None))interactor.enabled=false;
            foreach(var events in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))if(events.transform.parent!=transform)events.enabled=false;
            RoomLighting.Apply();RoomBranding.Apply(SceneManager.GetActiveScene(),Vi);View.enabled=true;View.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;ui?.SetWorld(Input.VrMode||Input.SimulatorMode,View);
        }
        void ShowExplore()
        {
            NewPage("explore");Header(SubjectName,ShowBrowser);var nodes=FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("Teleport_")).OrderBy(x=>x.name).ToArray();
            for(int i=0;i<nodes.Length;i++){int n=i;Button(page,L("Trạm ","Station ")+(i+1),70+i*225,840,210,64,()=>{origin.position=nodes[n].position;RecenterInterface();});}
            ui.Text(page,L("Điện thoại: vuốt nền để nhìn · PC: giữ chuột phải · Căn lại trong cài đặt","Phone: drag the background to look · PC: right-mouse look · Recenter in Settings"),100,135,1300,50,22,VlabUi.Secondary);
        }
        public void ShowActivity()
        {
            NewPage("activity");
            // A small station placard, not a screen-sized HUD. The working volume stays clear.
            var box=ui.Panel(page,"Current laboratory action",80,230,440,500);
            ui.Text(box,SubjectName,24,20,392,32,22,VlabUi.Secondary);
            ui.Text(box,LessonGuide.ShortTitle(selected.id,Vi),24,60,392,48,30);
            ui.Rule(box,24,112,392,3,Accent);
            liveStage=ui.Text(box,"",24,128,392,36,24,Accent);
            liveHint=ui.Text(box,"",24,174,392,136,28);
            liveAction=Button(box,"",24,326,392,70,PerformContextAction,Accent);
            liveActionLabel=liveAction.GetComponentInChildren<TextMeshProUGUI>();
            liveActionLabel.fontSize=ui.LargeText?29:26;
            liveTime=ui.Text(box,"",24,414,392,72,24,VlabUi.Secondary);
            Button(page,L("Dụng cụ & hướng dẫn","Station tools"),80,748,280,70,ShowStationTools);
            Button(page,L("‹ Quay lại","‹ Back"),376,748,144,70,ShowPause);
            guideLocate=Button(page,L("Chỉ tôi chỗ này","Show me where"),80,836,440,70,()=>{if(Profile.settings.hints)ShowGuideTarget(CurrentGuide());else{guidePage=-1;ShowGuidance();}});
            RefreshActivity();
        }
        LessonGuide Guide(){if(!ReferenceEquals(guideSession,Session)){guideSession=Session;guide=Session==null?null:new LessonGuide(Session);guidePage=-1;}return guide;}
        GuideTask CurrentGuide()=>Guide()?.Current(Apparatus,Vi,ActivityRunIntent());
        string GuideCard(GuideTask task)
        {
            if(task.kind=="prepare")return (Vi?Session.spec.operationsVi:Session.spec.operationsEn)[Session.Operation];
            if(task.kind=="image")return L("Chọn ô trên hình, rồi tự chọn nhãn. Mã ô không bị đếm trùng.","Select an image cell, then choose its label. IDs are not counted twice.");
            if(task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell")return L("Chọn kính hiển vi rồi tự phân loại ô đang chọn.","Open the microscope and classify the selected cell.");
            return task.action;
        }
        void PerformContextAction()
        {
            if(!Apparatus.Ready){ShowEquipment();return;}
            if(Profile.settings.hints&&Session.Phase==ExperimentPhase.Preparing){var task=CurrentGuide();if(task.kind=="instrument"||task.kind=="image"){ShowGuideTarget(task);return;}}
            switch(Session.Phase)
            {
                case ExperimentPhase.Preparing:PrepareAction();break;
                case ExperimentPhase.Ready:
                    if(!Session.Start(out status))Toast(LessonGuide.ExplainFailure(status,Vi));else{Apparatus.ApplyPreparation(Session.Operation);RefreshActivity();}break;
                case ExperimentPhase.Paused:Session.Resume();RefreshActivity();break;
                case ExperimentPhase.Running:RecordMeasurement();break;
                case ExperimentPhase.Invalid:ShowPause();break;
            }
        }
        void ShowStationTools()
        {
            var box=Modal(L("Dụng cụ & hướng dẫn","Station tools"),720,590,Apparatus.Driver!=null);
            Button(box,L("Dụng cụ","Equipment"),40,120,304,88,ShowEquipment);
            Button(box,L("Thông số","Variables"),376,120,304,88,ShowVariables);
            Button(box,L("Hướng dẫn","Guidance"),40,240,304,88,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Sổ tay","Notebook"),376,240,304,88,()=>ShowNotebook("activity"));
            Button(box,Input.VrMode?L("Thoát VR","Exit VR"):L("Chế độ VR","Enter VR"),40,360,640,70,()=>Input.SetVr(!Input.VrMode));
            Button(box,L("Trở lại trạm","Return to station"),280,470,400,72,()=>{CloseModal();RefreshActivity();},Accent);
        }
        void RefreshActivity()
        {
            if(Route!="activity"||Session==null||!liveHint)return;
            if(cueStep!=null&&cueStep!=GuideStep(CurrentGuide()))ClearGuideCue();
            string action;bool enabled=true;
            if(!Apparatus.Ready)
            {
                liveStage.text=L("LẮP DỤNG CỤ","ASSEMBLE STATION");
                liveHint.text=Apparatus.Driver==null?L("Chọn giá đỡ trước, rồi lắp từng dụng cụ vào trạm.","Begin with the stand, then fit each instrument to the station."):L("Lắp dụng cụ tại trạm. Chọn món đã lắp để xem và thao tác.","Fit the station equipment. Select a fitted item to inspect it.");
                action=L("Mở dụng cụ","Open equipment");
            }
            else if(Session.Phase==ExperimentPhase.Preparing)
            {
                liveStage.text=L("CHUẨN BỊ · ","PREPARATION · ")+(Session.Operation+1)+" / "+Session.spec.operationsEn.Length;
                liveHint.text=Profile.settings.hints?(Vi?Session.spec.operationsVi:Session.spec.operationsEn)[Session.Operation]:L("Chuẩn bị theo quy trình. Mở Hướng dẫn khi cần.","Follow the preparation procedure. Open Guidance when needed.");
                action=L("Thực hiện bước này","Perform current action");
            }
            else if(Session.Phase==ExperimentPhase.Ready)
            {
                liveStage.text=L("SẴN SÀNG ĐO","READY TO MEASURE");
                liveHint.text=Apparatus.Driver==null?L("Thả nhẹ, không đẩy. Quan sát chuyển động của con lắc.","Release gently without pushing. Observe the pendulum's motion."):L("Dụng cụ đã sẵn sàng. Bắt đầu lượt và thao tác trên dụng cụ.","Start the prepared trial. Select instruments to operate them.");
                action=Apparatus.Driver==null?L("Thả / Bắt đầu","Release / Start"):L("Bắt đầu lượt đo","Start trial");
            }
            else if(Session.Phase==ExperimentPhase.Running)
            {
                bool enough=Session.Authority!=null?Session.Authority.CanRecord(out _):Session.Pendulum!=null?Session.Pendulum.Cycles>=(int)Session.parameters["cycles"]:Session.Elapsed>=Session.spec.duration;
                liveStage.text=enough?L("CÓ THỂ GHI SỐ ĐO","MEASUREMENT READY"):L("ĐANG QUAN SÁT","OBSERVING");
                liveHint.text=Apparatus.Driver!=null?(enough?L("Đã đủ quan sát hợp lệ. Ghi lượt thực hành vào sổ tay.","The required observations are complete. Record this trial."):L("Quan sát và chọn dụng cụ để hoàn thành thao tác còn lại.","Observe and select an instrument to complete the remaining actions.")):(enough?L("Đã đủ chu kỳ toàn phần. Ghi lượt đo vào sổ tay.","The selected full cycles are complete. Record this observation."):L("Đếm từ lần qua vị trí cân bằng đến lần qua cùng chiều.","Count between crossings of the equilibrium point in the same direction."));
                action=L("Ghi số đo","Record measurement");enabled=enough;
            }
            else
            {
                liveStage.text=Session.Phase==ExperimentPhase.Invalid?L("CẦN CHUẨN BỊ LẠI","PREPARE AGAIN"):L("ĐÃ TẠM DỪNG","PAUSED");
                liveHint.text=Session.Phase==ExperimentPhase.Invalid?Session.Failure:L("Mọi số đo đang được giữ. Tiếp tục khi bạn sẵn sàng.","Your observations are retained. Continue when you are ready.");
                action=Session.Phase==ExperimentPhase.Invalid?L("Mở tạm dừng","Open pause menu"):L("Tiếp tục","Resume");
            }
            if(Profile.settings.hints)
            {
                var task=CurrentGuide();liveHint.text=GuideCard(task);liveHint.enableAutoSizing=true;liveHint.fontSizeMin=24;liveHint.fontSizeMax=ui.LargeText?29:28;
                if(Session.Phase==ExperimentPhase.Preparing&&(task.kind=="instrument"||task.kind=="image"))action=L("Mở dụng cụ cần dùng","Open required instrument");
            }
            liveActionLabel.text=action;liveAction.name=action;liveAction.interactable=enabled;
            if(Session.Pendulum!=null)
            {
                var motion=Session.Pendulum;int requested=(int)Session.parameters["cycles"];
                double clock=motion.FirstCrossing<0?0:(motion.TargetCrossing>=0?motion.TargetCrossing:motion.Time)-motion.FirstCrossing;
                liveTime.text=L("Chu kỳ toàn phần  ","Full cycles  ")+Math.Min(motion.Cycles,requested)+" / "+requested+"\n"+L("Đồng hồ  ","Clock  ")+clock.ToString("0.00")+" s";
            }
            else liveTime.text=L("Đồng hồ  ","Clock  ")+Session.Elapsed.ToString("0.00")+" s";
        }
        void PrepareAction(){if(Apparatus.PrepareNext(out status))Save();else Toast(LessonGuide.ExplainFailure(status,Vi));RefreshActivity();}
        void ShowEquipment()
        {
            var box=Modal(L("Dụng cụ · Chọn và lắp","Equipment · Select and install"),1000,760,Apparatus.Driver!=null);
            int pages=Math.Max(1,(Apparatus.items.Count+5)/6);equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            var selection=ui.Text(box,Apparatus.StoredItemCount>0?L("Có ","There are ")+Apparatus.StoredItemCount+L(" món trong khay dự trữ. Chọn món rồi Đặt vào vị trí lắp."," parts in the storage tray. Select one, then Place at installation anchor."):L("Chọn dụng cụ cần lắp.","Choose an instrument to fit."),620,300,340,200,24,VlabUi.Secondary);
            for(int row=0;row<6;row++)
            {
                int index=equipmentPage*6+row;if(index>=Apparatus.items.Count)break;var item=Apparatus.items[index];
                Button(box,(item.installed?L("Đã lắp · ","Fitted · "):item.stowed?L("Khay · ","Tray · "):"")+EquipmentName(item),40,110+row*75,550,65,()=>
                {if(item.installed&&Apparatus.Driver!=null){instrumentPage=0;ShowInstrument(item);return;}Apparatus.Select(item);status=L("Đã chọn ","Selected ")+EquipmentName(item);selection.text=status;}).name="Equipment item "+item.assetId;
            }
            Button(box,L("Đặt vào vị trí lắp","Place at installation anchor"),620,180,340,90,()=>{if(!Apparatus.InstallSelected())Toast(L("Chọn món chưa lắp và lắp dụng cụ hỗ trợ trước.","Select an uninstalled item and fit its supporting equipment first."));else{Save();ShowEquipment();}},Accent);
            if(pages>1){Button(box,L("Trang trước","Previous"),40,650,175,64,()=>{equipmentPage=Math.Max(0,equipmentPage-1);ShowEquipment();});ui.Text(box,(equipmentPage+1)+" / "+pages,235,660,140,45,22,VlabUi.Secondary,TextAlignmentOptions.Center);Button(box,L("Trang sau","Next"),395,650,195,64,()=>{equipmentPage=Math.Min(pages-1,equipmentPage+1);ShowEquipment();});}
            Button(box,L("Xong","Done"),620,650,340,65,()=>{CloseModal();RefreshActivity();});
        }
        string EquipmentName(EquipmentHandle item)=>Apparatus.EquipmentLabel(item,Vi);
        int InstrumentPageSize()=>Apparatus?.Driver is BiologyDriver?5:6;
        Reading[] LiveReadings()
        {
            liveReadingError="";if(Apparatus?.Driver==null||!Apparatus.Ready)return Array.Empty<Reading>();
            try{return Session.Observe().readings.ToArray();}catch(Exception error){liveReadingError=LessonGuide.ExplainFailure(error.Message,Vi);return Array.Empty<Reading>();}
        }
        public void ShowInstrument(EquipmentHandle item)
        {
            if(!item||!item.installed||Apparatus?.Driver==null)return;
            var driver=Apparatus.Driver;var texture=driver.Inspection(item.assetId);
            bool same=inspectedItem==item;if(!same){inspectionTab=texture?"view":"readings";readingPage=0;}
            var box=Modal(EquipmentName(item),1440,900,true);inspectedItem=item;
            var title=box.GetComponentInChildren<TextMeshProUGUI>();title.fontSize=40;title.fontWeight=FontWeight.SemiBold;
            selectedCellSummary=ui.Text(box,"",1000,24,400,80,26,Accent,TextAlignmentOptions.TopRight);
            string[] tabs=texture?new[]{"view","readings","explain"}:new[]{"readings","explain"};
            for(int i=0;i<tabs.Length;i++)
            {
                string tab=tabs[i];string label=tab=="view"?L("Hình quan sát","Observation"):tab=="readings"?L("Số đo trực tiếp","Live readings"):L("Hiểu kết quả","Interpretation");
                var button=Button(box,label,40+i*304,116,288,64,()=>{inspectionTab=tab;ShowInstrument(item);},inspectionTab==tab?Accent:(Color?)null);button.name="Instrument tab "+tab;
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize=26;
            }
            ui.Text(box,L("THAO TÁC","CONTROLS"),1000,132,400,45,26,VlabUi.Secondary);
            inspectionOutput=ui.Rect(box,"Instrument observation output",40,205,920,515);
            if(inspectionTab=="view"&&texture)
            {
                var frame=ui.Panel(inspectionOutput,"Schematic view",0,0,920,450,VlabUi.Raised);Rect uv=driver.InspectionUv(item.assetId);
                float aspect=Mathf.Abs(texture.width*uv.width/(texture.height*uv.height)),width=Mathf.Min(880,420*aspect),height=width/aspect;
                inspectionImage=ui.Picture(frame,texture,20+(880-width)/2,15+(420-height)/2,width,height);inspectionImage.uvRect=uv;inspectionImage.raycastTarget=true;
                if(driver is BiologyDriver biology&&biology.HasB01Presentation)inspectionImage.gameObject.AddComponent<MicroscopePresentation>().Bind(inspectionImage,biology);
                var selection=inspectionImage.gameObject.AddComponent<InstrumentInspection>();selection.MarkerColor=VlabUi.Hex("F0A34D");if(ReferenceEquals(selectedInspectionSession,Session))selection.SetSelection(selectedInspectionUv);
                selection.Selected=sourceUv=>
                {
                    if(!driver.PointOnInspection(item.assetId,sourceUv,out var message)){inspectionStatus.text=LessonGuide.ExplainFailure(message,Vi);return;}
                    selectedInspectionUv=sourceUv;selectedInspectionSession=Session;Save();ShowInstrument(item);if(inspectionStatus)inspectionStatus.text=L("Đã đánh dấu. Chọn tên cấu trúc bạn quan sát được.","Marked. Choose the structure you observed.");
                };
                ui.Text(inspectionOutput,Session.spec.id=="B01"?MicroscopePresentation.Notice(Vi):L("Chọn một vị trí trên hình để quan sát và tự phân loại.","Select a location to observe and classify it yourself."),0,468,920,50,24,VlabUi.Secondary);
            }
            else if(inspectionTab=="readings")
            {
                RenderInstrumentReadings(item,LiveReadings());
            }
            else
            {
                var reader=ui.Scroll(inspectionOutput,InstrumentReadings(),0,0,920,515,28);inspectionReadings=reader.GetComponentInChildren<TextMeshProUGUI>();
            }
            var controls=driver.Controls(item.assetId);int pageSize=InstrumentPageSize(),pages=Math.Max(1,(controls.Length+pageSize-1)/pageSize);instrumentPage=Mathf.Clamp(instrumentPage,0,pages-1);
            for(int row=0;row<pageSize;row++)
            {
                int index=instrumentPage*pageSize+row;if(index>=controls.Length)break;var control=controls[index];
                var button=Button(box,Vi?control.vi:control.en,1000,205+row*76,400,66,()=>
                {
                    if(!driver.Controls(item.assetId).Any(c=>c.id==control.id&&control.enabled))return;
                    bool okay=driver.Operate(control.id,out var message);if(okay){Guide().Applied(control.id);Save();}
                    ShowInstrument(item);if(inspectionStatus)inspectionStatus.text=okay?L("Đã thực hiện.","Action applied."):LessonGuide.ExplainFailure(message,Vi);
                });button.interactable=control.enabled;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=26;inspectionControls[control.id]=button;
            }
            if(controls.Length==0)ui.Text(box,L("Dụng cụ này không cần nút thao tác lúc này.","No instrument controls are needed right now."),1000,210,400,180,28,VlabUi.Secondary);
            if(pages>1)
            {
                Button(box,L("Trang trước","Previous"),1000,675,190,64,()=>{instrumentPage=Math.Max(0,instrumentPage-1);ShowInstrument(item);}).interactable=instrumentPage>0;
                Button(box,L("Trang sau","Next"),1210,675,190,64,()=>{instrumentPage=Math.Min(pages-1,instrumentPage+1);ShowInstrument(item);}).interactable=instrumentPage+1<pages;
            }
            inspectionStatus=ui.Text(box,L("Chọn thao tác ở bên phải; xem các giá trị ở thẻ Số đo trực tiếp.","Choose a control on the right; open Live readings for the values."),40,746,1360,54,24,VlabUi.Secondary);
            Button(box,L("Dụng cụ khác","Other equipment"),40,820,400,64,ShowEquipment);
            Button(box,L("Hướng dẫn","Guidance"),480,820,400,64,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Trở lại trạm","Return to station"),920,820,480,64,()=>{CloseModal();RefreshActivity();},Accent);
            RefreshInstrument();
        }
        static string ReadingKeys(IEnumerable<Reading> readings)=>string.Join("|",readings.Select(r=>r.key+":"+r.unit));
        void RenderInstrumentReadings(EquipmentHandle item,Reading[] readings)
        {
            foreach(Transform child in inspectionOutput){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            inspectionReadingKeys=ReadingKeys(readings);readingPage=Mathf.Clamp(readingPage,0,ObservationCards.PageCount(readings)-1);
            ObservationCards.Render(ui,inspectionOutput,readings,Vi,readingPage,0,0,920,420);
            if(readings.Length==0&&!string.IsNullOrEmpty(liveReadingError))inspectionOutput.GetComponentInChildren<TextMeshProUGUI>().text=liveReadingError;
            int count=ObservationCards.PageCount(readings);
            if(count>1)
            {
                Button(inspectionOutput,L("Số đo trước","Previous readings"),0,444,330,64,()=>{readingPage=Math.Max(0,readingPage-1);ShowInstrument(item);}).interactable=readingPage>0;
                ui.Text(inspectionOutput,(readingPage+1)+" / "+count,350,460,220,40,26,VlabUi.Secondary,TextAlignmentOptions.Center);
                Button(inspectionOutput,L("Số đo tiếp","More readings"),590,444,330,64,()=>{readingPage=Math.Min(count-1,readingPage+1);ShowInstrument(item);}).interactable=readingPage+1<count;
            }
        }
        string InstrumentReadings()
        {
            if(Apparatus?.Driver==null||!Apparatus.Ready)return L("Lắp đủ dụng cụ để đọc quan sát.","Fit all required equipment before reading observations.");
            try{var result=Session.Observe();return FormatReadings(result.readings)+"\n\n"+L("CÁCH HIỂU KẾT QUẢ","HOW TO INTERPRET")+"\n"+LessonContext.Explanation(Session.spec.id,Vi,result);}catch(Exception error){return error.Message;}
        }
        void RefreshInstrument()
        {
            if(!modal||!inspectedItem||Apparatus?.Driver==null)return;
            var driver=Apparatus.Driver;
            if(inspectionReadings)inspectionReadings.text=InstrumentReadings();
            if(inspectionOutput&&inspectionTab=="readings")
            {
                var readings=LiveReadings();if(ReadingKeys(readings)!=inspectionReadingKeys)RenderInstrumentReadings(inspectedItem,readings);else ObservationCards.Refresh(inspectionOutput,readings,Vi);
                if(!string.IsNullOrEmpty(liveReadingError)&&inspectionStatus)inspectionStatus.text=liveReadingError;
            }
            if(inspectionImage){inspectionImage.texture=driver.Inspection(inspectedItem.assetId);inspectionImage.uvRect=driver.InspectionUv(inspectedItem.assetId);}
            var controls=driver.Controls(inspectedItem.assetId);
            foreach(var pair in inspectionControls)if(pair.Value)pair.Value.interactable=controls.Any(control=>control.id==pair.Key&&control.enabled);
            if(selectedCellSummary&&driver is BiologyDriver&&(Session.spec.id=="B01"||Session.spec.id=="B02"||Session.spec.id=="B08"))
            {
                var state=JsonUtility.FromJson<BiologyLessonState>(driver.SaveState());
                int classified=Session.spec.id=="B01"?state.structures.Count:state.cells.Count(cell=>Session.spec.id=="B08"?!string.IsNullOrEmpty(cell.answer):!state.saltAdded?cell.baseline>=0:!state.waterAdded?cell.salt>=0:cell.recovery>=0);
                selectedCellSummary.text=(state.selectedCell>0?L("Ô đang chọn ","Selected ID ")+state.selectedCell:L("Chưa chọn ô","Select an image cell"))+"\n"+L("Đã phân loại: ","Classified: ")+classified+(Session.spec.id=="B08"?" / 100":"");
            }
        }
        void ClearInspection(){inspectedItem=null;inspectionImage=null;inspectionReadings=inspectionStatus=selectedCellSummary=null;inspectionOutput=null;inspectionControls.Clear();}
        void ShowVariables()
        {
            var box=Modal(L("Thông số thí nghiệm","Experiment variables"),1050,740,Apparatus.Driver!=null);
            int pages=Math.Max(1,(Session.spec.parameters.Length+3)/4);variablePage=Mathf.Clamp(variablePage,0,pages-1);
            for(int row=0;row<4;row++)
            {
                int index=variablePage*4+row;if(index>=Session.spec.parameters.Length)break;var p=Session.spec.parameters[index];int y=110+row*115;
                var text=ui.Text(box,(Vi?p.vi:p.en)+"   "+Session.parameters[p.key].ToString("0.###")+" "+p.unit,40,y,650,90,26);
                Action<double> set=direction=>
                {
                    double value=Math.Max(p.min,Math.Min(p.max,Session.parameters[p.key]+p.step*direction));
                    if(p.choices!=null&&p.choices.Length>0){int current=Array.FindIndex(p.choices,choice=>Math.Abs(choice-Session.parameters[p.key])<1e-9);int next=Mathf.Clamp(Math.Max(0,current)+(direction>0?1:-1),0,p.choices.Length-1);value=p.choices[next];}
                    if(Session.SetParameter(p.key,value,out status)){Guide().Variable(p.key,value);text.text=(Vi?p.vi:p.en)+"   "+value.ToString("0.###")+" "+p.unit;Apparatus.RefreshConfiguration();Save();}else Toast(LessonGuide.ExplainFailure(status,Vi));
                };
                bool fixedValue=p.readOnly||Math.Abs(p.max-p.min)<1e-10;
                bool editable=!fixedValue&&((Session.Phase==ExperimentPhase.Preparing||Session.Phase==ExperimentPhase.Ready)||(Session.Authority!=null&&Session.Authority.CanEditParameter(p.key)));
                if(fixedValue)ui.Text(box,L("Cố định","Fixed value"),730,y+10,220,55,22,VlabUi.Secondary);
                else{Button(box,"−",730,y,100,64,()=>set(-1)).interactable=editable;Button(box,"+",850,y,100,64,()=>set(1)).interactable=editable;}
            }
            if(Session.spec.parameters.Length==0)ui.Text(box,L("Bài này dùng thao tác trực tiếp trên dụng cụ.","This lesson uses the instrument controls directly."),40,160,950,180,28,VlabUi.Secondary);
            if(pages>1){Button(box,L("Trang trước","Previous"),330,640,180,64,()=>{variablePage=Math.Max(0,variablePage-1);ShowVariables();});ui.Text(box,(variablePage+1)+" / "+pages,530,650,100,45,22,VlabUi.Secondary,TextAlignmentOptions.Center);Button(box,L("Trang sau","Next"),650,640,180,64,()=>{variablePage=Math.Min(pages-1,variablePage+1);ShowVariables();});}
            Button(box,L("Quay lại","Back"),40,640,240,64,CloseModal);
        }
        void ShowGuidance()
        {
            var box=Modal(L("Hướng dẫn từng bước","One step at a time"),1100,800);
            GuideTask task=guidePage<0?CurrentGuide():Guide().Preparation(Apparatus,Vi,guidePage);
            ui.Text(box,guidePage<0?L("VIỆC CẦN LÀM BÂY GIỜ","YOUR CURRENT TASK"):L("THAM KHẢO BƯỚC CHUẨN BỊ ","PREPARATION REFERENCE ")+(guidePage+1),40,115,1020,50,24,Accent);
            ui.Text(box,task.action,40,185,1020,180,30).name="Guidance current action";
            ui.Text(box,L("Khi nào là xong?","What counts as success?"),40,390,1020,55,28);
            ui.Text(box,task.success,40,455,1020,110,26,VlabUi.Secondary).name="Guidance success criterion";
            Button(box,L("Bước trước","Previous step"),40,585,250,64,()=>{guidePage=Math.Max(-1,guidePage-1);ShowGuidance();});
            Button(box,L("Việc hiện tại","Current task"),330,585,250,64,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Bước sau","Next step"),620,585,250,64,()=>{guidePage=Math.Min(Session.spec.operationsEn.Length-1,guidePage+1);ShowGuidance();});
            Button(box,L("Chỉ tôi chỗ này","Show me where"),40,690,400,70,()=>ShowGuideTarget(task),Accent);
            Button(box,L("Gợi ý: ","Hints: ")+(Profile.settings.hints?L("Bật","On"):L("Tắt","Off")),470,690,270,70,()=>{Profile.settings.hints=!Profile.settings.hints;Save();ShowGuidance();});
            Button(box,L("Quay lại","Back"),770,690,290,70,CloseModal);
        }
        void ShowGuideTarget(GuideTask task)
        {
            ClearGuideCue();
            if(task==null)return;
            if(task.kind=="pause"){CloseModal();ShowPause();return;}
            if(task.kind=="variables"){CloseModal();ShowVariables();return;}
            var item=Apparatus.items.FirstOrDefault(x=>x.assetId==task.key);
            if(task.kind=="install"&&item){equipmentPage=Apparatus.items.IndexOf(item)/6;ShowEquipment();Apparatus.Select(item);if(!item.stowed)PointAtEquipment(item);return;}
            if(Session.spec.id=="P02"&&item&&item.installed&&task.key=="target"){CloseModal();Apparatus.Select(item);PointAtEquipment(item);RefreshActivity();return;}
            if(item&&item.installed&&Apparatus.Driver!=null)
            {
                var controls=Apparatus.Driver.Controls(item.assetId);int index=Array.FindIndex(controls,c=>!string.IsNullOrEmpty(task.control)&&(c.id==task.control||c.id.StartsWith(task.control,StringComparison.Ordinal)));
                if(Session.spec.id=="C01"&&index>=0){ShowTitrationGuide(item,task,controls[index].id);return;}
                instrumentPage=Math.Max(0,index)/InstrumentPageSize();inspectionTab=task.kind=="image"||task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell"?"view":inspectionTab;ShowInstrument(item);
                bool classification=task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell";
                if(index>=0&&!classification&&inspectionControls.TryGetValue(controls[index].id,out var button)){button.GetComponent<UnityEngine.UI.Image>().color=Accent;button.GetComponentInChildren<TextMeshProUGUI>().color=UiTheme.On(Accent);buttonGuideCue=GuideCue.AttachTo(button.transform,Profile.settings.reducedMotion);}
                else if(classification&&inspectionImage)buttonGuideCue=GuideCue.AttachTo(inspectionImage.transform,Profile.settings.reducedMotion);
                cueStep=GuideStep(CurrentGuide());
                if(inspectionStatus)inspectionStatus.text=classification?L("Tự chọn nhãn phù hợp với hình; không có đáp án chọn sẵn.","Choose the label matching the image; no answer is preselected."):index>=0?L("Chọn nút được tô sáng. Ứng dụng chưa làm hộ thao tác.","Select the highlighted button. No action has been performed for you."):L("Quan sát dụng cụ; Quay lại để dùng nút bước trên bảng.","Inspect the instrument; return to the station card for its task button.");return;
            }
            CloseModal();if(item){Apparatus.Select(item);PointAtEquipment(item);}if(liveAction)buttonGuideCue=GuideCue.AttachTo(liveAction.transform,Profile.settings.reducedMotion);cueStep=GuideStep(CurrentGuide());RefreshActivity();
        }
        void ShowTitrationGuide(EquipmentHandle item,GuideTask task,string controlId)
        {
            var driver=Apparatus.Driver;var control=driver.Controls(item.assetId).Single(value=>value.id==controlId);
            var box=Modal(EquipmentName(item),520,700,true);box.name="C01 guided sidepanel";box.anchoredPosition=new Vector2(60,-180);
            page.gameObject.SetActive(false);
            modal.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            ui.Text(box,L("QUAN SÁT DỤNG CỤ ĐƯỢC ĐÁNH DẤU","LOOK AT THE MARKED INSTRUMENT"),32,110,456,76,24,Accent);
            ui.Text(box,task.action,32,200,456,160,28);
            var feedback=ui.Text(box,task.success,32,386,456,128,24,VlabUi.Secondary);
            UnityEngine.UI.Button action=null;
            action=Button(box,Vi?control.vi:control.en,32,526,456,72,()=>
            {
                if(!driver.Controls(item.assetId).Any(value=>value.id==controlId&&value.enabled))return;
                bool okay=driver.Operate(controlId,out var message);
                if(okay){Guide().Applied(controlId);Save();action.interactable=false;feedback.text=L("Đã thực hiện. Trở lại trạm để tiếp tục từng bước.","Done. Return to the station to continue step by step.");}
                else feedback.text=LessonGuide.ExplainFailure(message,Vi);
            },Accent);action.interactable=control.enabled;
            Button(box,L("Trở lại trạm","Return to station"),32,616,456,64,()=>{CloseModal();RefreshActivity();});
            PointAtEquipment(item);buttonGuideCue=GuideCue.AttachTo(action.transform,Profile.settings.reducedMotion);
        }
        string GuideStep(GuideTask task)=>task==null?null:Session.Operation+":"+Session.Phase+":"+task.kind+":"+task.key+":"+task.control;
        void ClearGuideCue(){if(worldGuideCue)worldGuideCue.Stop();if(buttonGuideCue)buttonGuideCue.Stop();worldGuideCue=buttonGuideCue=null;cueStep=null;}
        void PointAtEquipment(EquipmentHandle item)
        {
            var cord=Resources.Load<GameObject>("Equipment/VLAB_string").GetComponentInChildren<LineRenderer>();
            worldGuideCue=GuideCue.ShowEquipment(Apparatus.transform,item,Profile.settings.reducedMotion,cord?cord.sharedMaterial:null);cueStep=GuideStep(CurrentGuide());
        }
        void RecordMeasurement()
        {
            if(!Session.Measure(out status)){Toast(LessonGuide.ExplainFailure(status,Vi));RefreshActivity();return;}
            var record=Session.Record();Profile.attempts.Add(record);Profile.checkpoint=null;
            if(!store.Save(Profile,out status)){Profile.attempts.Remove(record);Toast(status);return;}ShowResults(record);
        }
        string FormatReadings(IEnumerable<Reading> readings)=>string.Join("\n",readings.Select(r=>ReadingLabel(r)+"   "+ObservationCards.FormatValue(r.value,Vi)+" "+r.unit));
        public static string ReadingCaption(Reading reading,bool vi)
        {string supplied=vi?reading.labelVi:reading.labelEn;string caption=!string.IsNullOrEmpty(supplied)?supplied:vi&&readingVi.TryGetValue(reading.key,out var label)?label:reading.key;return caption.Replace("SDK ","");}
        string ReadingLabel(Reading reading)=>ReadingCaption(reading,Vi);
        void ShowResults(AttemptRecord record,int readingIndex=0)
        {
            shownResult=record;NewPage("results");var box=ui.Panel(page,"Results",80,60,1440,880);
            var title=ui.Text(box,L("Đã lưu vào sổ thực hành","Saved to your lab notebook"),40,30,1360,70,40);title.fontWeight=FontWeight.SemiBold;
            ui.Text(box,LessonGuide.ShortTitle(record.experimentId,Vi),40,112,1360,55,30,VlabUi.Secondary);
            int pages=ObservationCards.PageCount(record.readings);readingIndex=Mathf.Clamp(readingIndex,0,pages-1);
            ObservationCards.Render(ui,box,record.readings,Vi,readingIndex,40,190,1360,400);
            if(pages>1)
            {
                Button(box,L("Số đo trước","Previous readings"),40,614,380,64,()=>ShowResults(record,readingIndex-1)).interactable=readingIndex>0;
                ui.Text(box,(readingIndex+1)+" / "+pages,440,630,560,40,26,VlabUi.Secondary,TextAlignmentOptions.Center);
                Button(box,L("Số đo tiếp","More readings"),1020,614,380,64,()=>ShowResults(record,readingIndex+1)).interactable=readingIndex+1<pages;
            }
            ui.Text(box,L("Mở sổ để xem điều kiện, giải thích và so sánh các lượt đã ghi.","Open your notebook for conditions, interpretation and comparisons."),40,706,1360,55,26,VlabUi.Secondary);
            Button(box,L("Làm lại","Try again"),40,786,340,64,Restart);
            Button(box,L("Danh sách thí nghiệm","Back to experiments"),408,786,480,64,()=>{Session=null;ShowBrowser();});
            Button(box,L("Xem trong sổ","Open notebook"),916,786,484,64,()=>{journal.notesMode=false;journal.subject=record.experimentId.Substring(0,1);journal.experiment=record.experimentId;journal.attempt=0;ShowNotebook("results");},Accent).name="Open result journal";
        }
        void ShowNotebook(string back)
        {
            if(Route!="notebook"&&Route!="notebook-analysis"&&(back=="browser"||back=="readiness"||back=="activity"||back=="pause"||back=="results"))
            {
                string id=back=="results"?shownResult?.experimentId:back=="browser"||back=="readiness"?selected?.id:Session?.spec.id;
                if(!string.IsNullOrEmpty(id)&&LessonSpec.All.ContainsKey(id)){journal.notesMode=false;journal.subject=id.Substring(0,1);journal.experiment=id;journal.indexPage=(int.Parse(id.Substring(1))-1)/3;journal.contentTab=back=="results"?0:4;journal.studyChapter=journal.contentPage=0;}
            }
            notebookReturn=back;NewPage("notebook");Header(L("Sổ thực hành","Lab notebook"),()=>{if(SaveNotebookNotes())ReturnTo(back);});
            JournalView.Render(ui,page,Profile,Vi,journal,()=>ShowNotebook(back),()=>ShowNotebookAnalysis(back,journal.experiment,null,null,null),FormatReadings,SaveNotebookNotes,(message,accept)=>Confirm(message,accept),(Input.VrMode||Input.SimulatorMode)?OpenNotebookKeyboard:(Action<TMP_InputField>)null);
            string linked=journal.notesMode?Profile.notebookPages.ElementAtOrDefault(journal.notePage)?.experimentId:journal.experiment;
            if(!string.IsNullOrEmpty(linked))RefreshBackground(linked);
        }
        bool SaveNotebookNotes()
        {
            if(Save())
            {
                journal.dirtyNotes.Clear();journal.noteStatus=L("Đã lưu trên thiết bị","Saved on this device");
                var caption=page?page.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(text=>text.name=="Journal note status"):null;if(caption)caption.text=journal.noteStatus;return true;
            }
            Debug.LogWarning("VLAB notebook save failed: "+status);
            var box=Modal(L("Chưa lưu được ghi chép","Notebook could not be saved"),1000,460);
            ui.Text(box,L("Trang viết vẫn được giữ trong phiên này. Kiểm tra dung lượng thiết bị rồi thử lưu lại.","Your draft remains in this session. Check device storage, then retry saving."),40,125,920,140,28);
            Button(box,L("Quay lại trang viết","Back to writing"),40,340,440,70,CloseModal);
            Button(box,L("Thử lưu lại","Retry save"),520,340,440,70,()=>{CloseModal();SaveNotebookNotes();},Accent);return false;
        }
        void OpenNotebookKeyboard(TMP_InputField target)
        {
            if(!target||notebookKeyboardRequested||(modal&&modal.GetComponentInChildren<NotebookKeyboardState>()))return;
            notebookKeyboardRequested=true;StartCoroutine(OpenNotebookKeyboardAfterSelection(target));
        }
        IEnumerator OpenNotebookKeyboardAfterSelection(TMP_InputField target)
        {
            yield return null;notebookKeyboardRequested=false;if(!target||Route!="notebook")yield break;
            var box=Modal(L("Ghi vào sổ","Write in your notebook"),1440,880);
            NotebookKeyboard.Render(ui,box,target,Vi,CloseModal);
        }
        void ShowNotebookAnalysis(string back,string id,string x,string y,string group)
        {
            var ids=Profile.attempts.Select(a=>a.experimentId).Distinct().OrderBy(v=>v).ToArray();if(ids.Length==0){ShowNotebook(back);return;}
            id=id??(selected!=null&&ids.Contains(selected.id)?selected.id:ids[0]);
            NewPage("notebook-analysis",id);Header(L("Phân tích số đo đã lưu","Analyse saved observations"),()=>ShowNotebook(back));
            string current=id;Button(page,LessonGuide.ShortTitle(current,Vi),450,120,700,64,()=>{int index=Array.IndexOf(ids,current);ShowNotebookAnalysis(back,ids[(index+1)%ids.Length],null,null,null);});
            var pane=ui.Panel(page,"Saved observation analysis",80,200,1440,730);
            var data=ExperimentNotebook.BuildData(current,Profile.attempts,x,y);
            ExperimentNotebook.Render(ui,pane,data,group,(nextX,nextY,nextGroup)=>ShowNotebookAnalysis(back,current,nextX,nextY,nextGroup));
        }
        void ShowPause()
        {
            Session?.Pause();NewPage("pause");Header(L("Tạm dừng","Paused"));var box=ui.Panel(page,"Pause",520,200,560,640);
            Button(box,L("Tiếp tục","Resume"),50,70,460,75,()=>{Session?.Resume();if(Session!=null)ShowActivity();else ShowExplore();},Accent);
            Button(box,L("Cài đặt","Settings"),50,175,460,70,()=>OpenSettings("pause"));Button(box,L("Trợ giúp","Help"),50,270,460,70,()=>ShowHelp("pause"));
            Button(box,L("Làm lại lượt","Restart attempt"),50,365,460,70,()=>Confirm(L("Lượt chưa ghi sẽ bị xóa. Lượt trong sổ tay vẫn được giữ.","The unrecorded attempt will be cleared. Notebook entries are retained."),Restart));
            Button(box,L("Rời phòng","Leave laboratory"),50,460,460,70,()=>Confirm(L("Lưu điểm chuẩn bị và rời phòng?","Save preparation checkpoint and leave?"),()=>{Save();ShowBrowser();}));
        }
        void Restart(){Session=null;if(Apparatus)Destroy(Apparatus.gameObject);StartCoroutine(LoadRoom(true,false));}
        void OpenSettings(string back){settingsReturn=back;draft=Profile.settings.Copy();draftLocale=Profile.locale;dirty=false;settingsTab=0;ShowSettings();}
        void SettingsCategories(RectTransform box,string[] labels)
        {
            var rail=ui.Rect(box,"Settings category rail",32,104,1036,72);
            ui.Rule(rail,0,70,1036,2,VlabUi.Stroke).name="Settings category baseline";
            float width=1036f/labels.Length;
            for(int i=0;i<labels.Length;i++)
            {
                int tab=i;var hit=ui.Rect(rail,i==5?"Settings interaction tab":labels[i],i*width,0,width,72);
                var target=hit.gameObject.AddComponent<UnityEngine.UI.Image>();target.color=Color.clear;
                var button=hit.gameObject.AddComponent<UnityEngine.UI.Button>();
                if(i==settingsTab){var selected=ui.Panel(hit,"Settings active category surface",3,4,width-6,60,VlabUi.Raised);SettingsFlat(selected);selected.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
                var caption=ui.Text(hit,labels[i],8,6,width-16,60,24,i==settingsTab?VlabUi.TextColor:VlabUi.Secondary,TextAlignmentOptions.Center);
                caption.GetComponent<VlabTextBacking>().enabled=false;caption.fontWeight=i==settingsTab?FontWeight.Bold:FontWeight.Medium;
                button.targetGraphic=caption;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.Lerp(Color.white,Accent,.35f);colors.pressedColor=Accent;button.colors=colors;
                button.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter||changing)return;settingsTab=tab;ShowSettings();});
                if(i==settingsTab)ui.Rule(rail,i*width+18,68,width-36,4,Accent).name="Settings selected category";
            }
        }
        void SettingsFlat(RectTransform rect)
        {foreach(var effect in rect.GetComponents<UnityEngine.UI.Shadow>())effect.enabled=false;var opacity=rect.GetComponent<VlabPanelOpacity>();if(opacity){opacity.Solid=true;opacity.FlatAlpha=rect.GetComponent<UnityEngine.UI.Image>().color.a;}}
        void SettingsInk(TextMeshProUGUI text)
        {var backing=text.GetComponent<VlabTextBacking>();if(backing)backing.enabled=false;}
        UnityEngine.UI.Button SettingsQuiet(RectTransform box,string label,float x,float y,float width,float height,Action action,bool outline=false)
        {
            var button=Button(box,label,x,y,width,height,action);var face=button.GetComponent<UnityEngine.UI.Image>();face.color=Color.clear;
            var opacity=button.GetComponent<VlabPanelOpacity>();opacity.Solid=false;opacity.FlatAlpha=0;
            foreach(var effect in button.GetComponents<UnityEngine.UI.Shadow>())effect.enabled=outline&&effect is UnityEngine.UI.Outline;
            var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.color=VlabUi.TextColor;SettingsInk(caption);return button;
        }
        void SettingsSection(RectTransform box,string title,string explanation)
        {
            var heading=ui.Text(box,title,48,206,1004,48,32);heading.name="Settings section title";heading.fontStyle=FontStyles.Bold;SettingsInk(heading);
            var hint=ui.Text(box,explanation,48,258,1004,44,24,VlabUi.Secondary);hint.name="Settings section hint";SettingsInk(hint);
        }
        UnityEngine.UI.Button SettingsSwitch(RectTransform box,string label,bool state,float top,Action action)
        {
            var button=SettingsQuiet(box,label,48,top,1004,72,action);var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(0,-10);caption.rectTransform.sizeDelta=new Vector2(820,52);caption.alignment=TextAlignmentOptions.Left;caption.fontSize=ui.LargeText?31.36f:28;
            var track=ui.Panel(button.transform,"Settings switch track",868,15,124,42,state?Accent:VlabUi.Stroke);SettingsFlat(track);track.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var knob=ui.Panel(track,"Settings switch knob",state?84:6,5,32,32,UiTheme.On(state?Accent:VlabUi.Stroke));SettingsFlat(knob);knob.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            button.targetGraphic=track.GetComponent<UnityEngine.UI.Image>();ui.Rule(box,48,top+78,1004,2,new Color(VlabUi.Stroke.r,VlabUi.Stroke.g,VlabUi.Stroke.b,.4f));return button;
        }
        void SettingsValueBar(RectTransform box,string label,string valueName,float top,float normalised,string minus,string plus,string minusName,string plusName,Action decrease,Action increase)
        {
            var value=ui.Text(box,label,48,top+14,500,52,28);if(!string.IsNullOrEmpty(valueName))value.name=valueName;SettingsInk(value);
            var minusButton=Button(box,minus,572,top,128,80,decrease);if(!string.IsNullOrEmpty(minusName))minusButton.name=minusName;SettingsFlat((RectTransform)minusButton.transform);
            var plusButton=Button(box,plus,924,top,128,80,increase);if(!string.IsNullOrEmpty(plusName))plusButton.name=plusName;SettingsFlat((RectTransform)plusButton.transform);
            var track=ui.Rule(box,716,top+34,192,12,VlabUi.Stroke);track.name="Settings value track";
            ui.Rule(box,716,top+34,192*Mathf.Clamp01(normalised),12,Accent).name="Settings value fill";
        }
        void ShowSettings()
        {
            NewPage("settings");var box=ui.Panel(page,"Settings",250,110,1100,780);
            var heading=ui.Text(box,L("Cài đặt","Settings"),48,28,540,64,40);heading.name="Settings title";heading.fontStyle=FontStyles.Bold;SettingsInk(heading);
            var draftHint=ui.Text(box,dirty?L("Chưa áp dụng","Not applied"):L("Thiết lập cá nhân","Personal settings"),600,44,240,38,24,VlabUi.Secondary);SettingsInk(draftHint);
            SettingsQuiet(box,L("Quay lại","Back"),872,28,196,64,SettingsBack).name="Settings back";
            string[] tabs=Vi?new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"}:new[]{"General","Audio","Graphics","Controls","Appearance","Interaction"};
            SettingsCategories(box,tabs);
            Action<Action> edit=action=>{action();dirty=true;ShowSettings();};
            if(settingsTab==0)
            {
                SettingsSection(box,tabs[0],L("Chọn cách đọc và nhận hướng dẫn phù hợp với bạn.","Choose how you read and receive guidance."));
                var language=SettingsQuiet(box,L("Ngôn ngữ: ","Language: ")+(draftLocale=="vi"?"Tiếng Việt":"English"),48,310,1004,72,()=>edit(()=>draftLocale=draftLocale=="vi"?"en":"vi"));var languageText=language.GetComponentInChildren<TextMeshProUGUI>();languageText.rectTransform.anchoredPosition=new Vector2(0,-10);languageText.rectTransform.sizeDelta=new Vector2(820,52);languageText.fontSize=ui.LargeText?31.36f:28;languageText.alignment=TextAlignmentOptions.Left;
                var languageValue=ui.Panel(language.transform,"Settings language value",868,12,124,48,VlabUi.Raised);SettingsFlat(languageValue);languageValue.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;var languageCode=ui.Text(languageValue,draftLocale=="vi"?"VI":"EN",0,7,124,40,24,null,TextAlignmentOptions.Center);SettingsInk(languageCode);language.targetGraphic=languageValue.GetComponent<UnityEngine.UI.Image>();
                SettingsSwitch(box,L("Chữ lớn: ","Larger text: ")+(draft.largeText?L("Bật","On"):L("Tắt","Off")),draft.largeText,390,()=>edit(()=>draft.largeText=!draft.largeText));
                SettingsSwitch(box,L("Gợi ý từng bước: ","Step-by-step hints: ")+(draft.hints?L("Bật","On"):L("Tắt","Off")),draft.hints,470,()=>edit(()=>draft.hints=!draft.hints));
                SettingsSwitch(box,L("Hiệu ứng chuyển động: ","UI animation: ")+(!draft.reducedMotion?L("Bật","On"):L("Tắt","Off")),!draft.reducedMotion,550,()=>edit(()=>draft.reducedMotion=!draft.reducedMotion)).name="Settings UI animation";
            }
            if(settingsTab==1)
            {
                SettingsSection(box,tabs[1],L("Chỉnh âm lượng bằng nút − / +; tắt tiếng vẫn giữ mức đã chọn.","Use − / + to adjust; muting keeps your chosen level."));
                SettingsValueBar(box,L("Âm lượng: ","Master volume: ")+Mathf.RoundToInt(draft.volume*100)+"%","Settings volume value",338,draft.volume,"−","+",null,null,()=>edit(()=>draft.volume=Mathf.Clamp01(draft.volume-.05f)),()=>edit(()=>draft.volume=Mathf.Clamp01(draft.volume+.05f)));
                SettingsSwitch(box,L("Tắt tiếng: ","Mute: ")+(draft.muted?L("Bật","On"):L("Tắt","Off")),draft.muted,474,()=>edit(()=>draft.muted=!draft.muted));
            }
            if(settingsTab==2)
            {
                SettingsSection(box,tabs[2],L("Chọn một mức hiển thị cho điện thoại đang dùng.","Choose a rendering preset for this device."));
                string[] presets=Vi?new[]{"Hiệu năng","Cân bằng","Chất lượng"}:new[]{"Performance","Balanced","Quality"};
                for(int i=0;i<3;i++){int n=i;var preset=SettingsQuiet(box,presets[i],48,318+i*100,1004,80,()=>edit(()=>draft.graphics=n));var caption=preset.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(72,-12);caption.rectTransform.sizeDelta=new Vector2(920,56);caption.alignment=TextAlignmentOptions.Left;caption.fontSize=ui.LargeText?31.36f:28;var radio=ui.Panel(preset.transform,"Settings preset radio",12,22,36,36,VlabUi.Stroke);SettingsFlat(radio);radio.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;var centre=ui.Panel(radio,"Settings preset centre",6,6,24,24,draft.graphics==i?Accent:VlabUi.PanelColor);SettingsFlat(centre);centre.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;preset.targetGraphic=radio.GetComponent<UnityEngine.UI.Image>();if(draft.graphics==i)ui.Rule(preset.transform,72,72,920,3,Accent);}
            }
            if(settingsTab==3)
            {
                string controllerName=Input.AvailableControllerName;if(controllerName!=null&&controllerName.Length>20)controllerName=controllerName.Substring(0,20)+"…";
                SettingsSection(box,tabs[3],L("Đổi chế độ ngay · ","Mode changes now · ")+(controllerName==null?L("Chưa nhận HID","No HID detected"):"HID: "+controllerName));
                foreach(var text in box.GetComponentsInChildren<TextMeshProUGUI>().Where(text=>text.name=="Settings section title"||text.name=="Settings section hint"))text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,680);
                SettingsQuiet(box,L("Gán nút tay cầm","Button mapping"),752,206,300,72,ShowControllerBindings).name="Controls button mapping";
                bool headset=Input.VrMode||Input.SimulatorMode;
                var phone=SettingsQuiet(box,L("Điện thoại thường","Phone screen"),48,310,502,72,()=>{Session?.Pause();if(Input.SimulatorMode)Input.SetSimulator(false);if(Input.VrMode)Input.SetVr(false);ShowSettings();});phone.name="Controls phone";
                var vr=SettingsQuiet(box,Application.isEditor?L("Mô phỏng kính VR · PC","VR viewer simulation · PC"):L("Kính VR điện thoại","Phone VR viewer"),550,310,502,72,()=>{Session?.Pause();if(Application.isEditor)Input.SetSimulator(!Input.SimulatorMode);else Input.SetVr(!Input.VrMode);ShowSettings();});vr.name=Application.isEditor?"Controls PC simulator":"Controls VR";vr.interactable=Application.isEditor||Input.CanScanViewer;
                ui.Rule(box,48+(headset?502:0),380,502,4,Accent);
                var help=ui.Text(box,L("Điện thoại: chạm chọn; vuốt nền lab để nhìn.\nKính: nhìn để trỏ; nút kính hoặc giữ nhìn (khi bật tự chọn).\nChọn: ","Phone: tap to select; drag empty lab space to look.\nViewer: gaze; trigger or dwell (when enabled) selects.\nSelect: ")+ControllerBindings.Label(Profile.settings.controllerSelect,Vi)+L(" · Quay lại: "," · Back: ")+ControllerBindings.Label(Profile.settings.controllerBack,Vi)+L("\nMở Gán nút tay cầm để xem các nút và hướng dẫn cần điều khiển.","\nOpen Button mapping for other actions and stick instructions."),48,402,1004,136,24);SettingsInk(help);
                Button(box,L("Căn lại","Recenter"),48,554,312,72,()=>{Input.Recenter();RecenterInterface();});
                var qr=Button(box,Input.CanScanViewer?L("Quét QR kính","Scan viewer QR"):L("QR · Android","QR · Android"),740,554,312,72,Input.ScanViewer);qr.name="Controls viewer QR";qr.interactable=Input.CanScanViewer;
                var pair=Button(box,Input.CanPairController?L("Kết nối tay cầm","Pair controller"):"Bluetooth · Android",394,554,312,72,()=>{Session?.Pause();if(!Input.OpenControllerPairing())Toast(L("Không mở được cài đặt Bluetooth. Hãy mở từ cài đặt điện thoại.","Bluetooth settings could not open. Use your phone settings."));});pair.name="Controls pair controller";pair.interactable=Input.CanPairController;
                foreach(var button in new[]{pair,qr})button.GetComponentInChildren<TextMeshProUGUI>().fontSize=ui.LargeText?26.88f:24;
            }
            if(settingsTab==4)
            {
                SettingsSection(box,tabs[4],L("Chọn bảng màu; dùng − / + để chỉnh độ đục nền.","Choose a palette; use − / + to adjust panel opacity."));
                string[] themes=Vi?new[]{"Xanh đêm","Giấy sáng","Xám than"}:new[]{"Navy","Paper","Graphite"};for(int i=0;i<themes.Length;i++){int theme=i;var button=Button(box,themes[i],48+i*344,318,316,100,()=>edit(()=>draft.uiTheme=theme));button.name="Appearance theme "+theme;SettingsFlat((RectTransform)button.transform);var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(88,-24);caption.rectTransform.sizeDelta=new Vector2(212,52);caption.alignment=TextAlignmentOptions.Left;caption.fontSize=ui.LargeText?26.88f:24;var palette=UiTheme.Palette(i);var swatch=ui.Panel(button.transform,"Settings palette swatch",16,16,56,68,palette.panel);SettingsFlat(swatch);swatch.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;ui.Rule(swatch,10,16,36,5,palette.text);ui.Rule(swatch,10,30,24,3,palette.secondary);if(draft.uiTheme==i)ui.Rule(button.transform,16,94,284,4,Accent);}
                SettingsValueBar(box,L("Độ đục nền: ","Panel opacity: ")+Mathf.RoundToInt(draft.uiOpacity*100)+"%","Settings opacity value",486,(draft.uiOpacity-.55f)/.45f,"− 5%","+ 5%","Appearance opacity minus","Appearance opacity plus",()=>edit(()=>draft.uiOpacity=Mathf.Max(.55f,draft.uiOpacity-.05f)),()=>edit(()=>draft.uiOpacity=Mathf.Min(1,draft.uiOpacity+.05f)));
                var note=ui.Text(box,L("55–100%. Chọn Áp dụng để lưu trên thiết bị.","55–100%. Apply saves your choice on this device."),48,600,1004,42,24,VlabUi.Secondary);SettingsInk(note);
            }
            if(settingsTab==5)
            {
                SettingsSection(box,tabs[5],L("Giữ nhìn dùng cho kính VR; vuốt / chuột dùng ở màn hình thường.","Dwell applies to VR; swipe / mouse look applies to the phone screen."));
                SettingsSwitch(box,L("Tự chọn khi giữ nhìn: ","Gaze auto-select: ")+(draft.gazeAutoSelect?L("Bật","On"):L("Tắt","Off")),draft.gazeAutoSelect,310,()=>edit(()=>draft.gazeAutoSelect=!draft.gazeAutoSelect)).name="Interaction gaze toggle";
                SettingsValueBar(box,L("Thời gian giữ nhìn: ","Gaze dwell duration: ")+draft.gazeDwellSeconds.ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo(Vi?"vi-VN":"en-US"))+L(" giây"," seconds"),"Interaction dwell value",416,(draft.gazeDwellSeconds-.6f)/1.4f,L("− 0,1 s","− 0.1 s"),L("+ 0,1 s","+ 0.1 s"),"Interaction dwell minus","Interaction dwell plus",()=>edit(()=>draft.gazeDwellSeconds=Mathf.Max(.6f,Mathf.Round((draft.gazeDwellSeconds-.1f)*10)/10)),()=>edit(()=>draft.gazeDwellSeconds=Mathf.Min(2,Mathf.Round((draft.gazeDwellSeconds+.1f)*10)/10)));
                SettingsValueBar(box,L("Độ nhạy vuốt / chuột: ","Swipe / mouse sensitivity: ")+draft.mouseLookSensitivity.ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo(Vi?"vi-VN":"en-US"))+"×","Interaction mouse value",518,(draft.mouseLookSensitivity-.5f)/1.5f,L("− 0,1×","− 0.1×"),L("+ 0,1×","+ 0.1×"),"Interaction mouse minus","Interaction mouse plus",()=>edit(()=>draft.mouseLookSensitivity=Mathf.Max(.5f,Mathf.Round((draft.mouseLookSensitivity-.1f)*10)/10)),()=>edit(()=>draft.mouseLookSensitivity=Mathf.Min(2,Mathf.Round((draft.mouseLookSensitivity+.1f)*10)/10)));
                var note=ui.Text(box,L("Giữ nhìn: 0,6–2 giây. Vuốt / chuột: 0,5–2×. Nút bấm luôn chọn được.","Dwell: 0.6–2 s. Swipe / mouse: 0.5–2×. Button selection always works."),48,612,1004,36,22,VlabUi.Secondary);SettingsInk(note);
            }
            ui.Rule(box,32,656,1036,2,VlabUi.Stroke);
            SettingsQuiet(box,L("Hủy","Cancel"),32,680,224,72,()=>ReturnTo(settingsReturn),true);Button(box,L("Áp dụng","Apply"),796,680,272,72,ApplyDraft,Accent).name="Settings apply";
            if(settingsTab<3||settingsTab==4||settingsTab==5)SettingsQuiet(box,L("Mặc định mục này","Category defaults"),288,680,344,72,()=>{var defaults=new GameSettings();if(settingsTab==0){draftLocale="vi";draft.largeText=defaults.largeText;draft.hints=defaults.hints;draft.reducedMotion=defaults.reducedMotion;}if(settingsTab==1){draft.volume=defaults.volume;draft.muted=defaults.muted;}if(settingsTab==2)draft.graphics=defaults.graphics;if(settingsTab==4){draft.uiTheme=defaults.uiTheme;draft.uiOpacity=defaults.uiOpacity;}if(settingsTab==5){draft.gazeAutoSelect=defaults.gazeAutoSelect;draft.gazeDwellSeconds=defaults.gazeDwellSeconds;draft.mouseLookSensitivity=defaults.mouseLookSensitivity;}dirty=true;ShowSettings();});
        }
        void SettingsBack()
        {
            if(!dirty){ReturnTo(settingsReturn);return;}var box=Modal(L("Thay đổi chưa áp dụng","Unapplied changes"),1000,430);ui.Text(box,L("Giữ lại, bỏ thay đổi, hoặc áp dụng trước khi quay lại.","Stay here, discard changes, or apply them before returning."),40,120,920,120,26);
            Button(box,L("Ở lại","Stay"),40,320,280,64,CloseModal);Button(box,L("Bỏ thay đổi","Discard"),360,320,280,64,()=>ReturnTo(settingsReturn));Button(box,L("Áp dụng","Apply"),680,320,280,64,ApplyDraft,Accent);
        }
        void ApplyDraft(){var previous=Profile.settings;string oldLocale=Profile.locale;Profile.settings=draft.Copy();Profile.locale=draftLocale;if(Save()){ApplySettings();dirty=false;ReturnTo(settingsReturn);}else{Profile.settings=previous;Profile.locale=oldLocale;Toast(status);}}
        void ApplySettings()
        {
            RoomBranding.SetLanguage(SceneManager.GetActiveScene(),Profile.locale!="en");
            Input.ConfigureControllerBindings(Profile.settings);
            ui.SetReducedMotion(Profile.settings.reducedMotion);
            foreach(var focus in ui.canvas.GetComponentsInChildren<PortalFocus>(true))focus.ReducedMotion=Profile.settings.reducedMotion;
            AudioListener.volume=Profile.settings.muted?0:Profile.settings.volume;ui.LargeText=Profile.settings.largeText;UiTheme.Select(Profile.settings.uiTheme,Profile.settings.uiOpacity);Preview.ReducedMotion=Profile.settings.reducedMotion;Input.MouseLookSensitivity=Profile.settings.mouseLookSensitivity;
            var source=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(source)
            {
                var previous=runtimePipeline;runtimePipeline=Instantiate(source);runtimePipeline.name="VLAB runtime quality";
                runtimePipeline.renderScale=Profile.settings.graphics==0?.65f:Profile.settings.graphics==1?.85f:1;
                runtimePipeline.shadowDistance=Profile.settings.graphics==0?8:Profile.settings.graphics==1?15:25;
                runtimePipeline.msaaSampleCount=Profile.settings.graphics==2?4:2;QualitySettings.renderPipeline=runtimePipeline;if(previous)Destroy(previous);
            }
            Application.targetFrameRate=60;
        }
        void ShowControllerBindings()
        {
            var state=modal?modal.GetComponentInChildren<ControllerDiagramState>():null;
            bool right=state&&state.RightHand,side=state&&state.SideView;
            ShowControllerDiagram(right,side);
        }
        void ShowControllerDiagram(bool right,bool side)
        {
            var box=Modal(L("Gán nút tay cầm","Controller buttons"),1100,860);page.gameObject.SetActive(false);modal.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            ui.Text(box,L("Chạm vào nút trên hình hoặc tên nút để đổi chức năng. Áp dụng trong Cài đặt để lưu.","Choose a button on the image or its callout, then assign an action. Apply in Settings to save."),40,102,1020,88,24,VlabUi.Secondary);
            var diagram=ui.Rect(box,"Controller diagram area",32,202,1036,450);
            ControllerDiagramView.Render(ui,diagram,draft,Vi,Profile.settings.reducedMotion,option=>{var view=modal.GetComponentInChildren<ControllerDiagramState>();ShowControllerActionPicker(option,view.RightHand,view.SideView);},right,side);
            Button(box,L("Mặc định nút","Default buttons"),40,674,320,64,()=>{var defaults=new GameSettings();draft.controllerSelect=defaults.controllerSelect;draft.controllerGrip=defaults.controllerGrip;draft.controllerMenu=defaults.controllerMenu;draft.controllerRecenter=defaults.controllerRecenter;draft.controllerBack=defaults.controllerBack;dirty=true;ShowControllerBindings();});
            Button(box,L("Hướng dẫn tay cầm","Controller help"),390,674,670,64,()=>ShowControllerHelp(0));
            Button(box,L("Quay lại cài đặt","Back to Settings"),40,756,1020,64,()=>{CloseModal();ShowSettings();});
        }
        void ShowControllerActionPicker(int physical,bool right,bool side)
        {
            var box=Modal(ControllerBindings.Label(physical,Vi),1000,800);page.gameObject.SetActive(false);modal.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            ui.Text(box,L("Chọn chức năng cho nút này. Nếu đã dùng, hai nút sẽ đổi chức năng cho nhau.","Choose this button's action. An existing assignment is swapped when possible."),40,105,920,90,26,VlabUi.Secondary);
            int row=0;foreach(ControllerAction action in Enum.GetValues(typeof(ControllerAction)))
            {
                var current=action;Button(box,ControllerBindings.ActionLabel(current,Vi),40,212+row++*86,920,72,()=>
                {
                    if(ControllerBindings.TryAssignPhysical(draft,physical,current)){dirty=true;ShowControllerDiagram(right,side);}
                    else {var message=box.Find("Controller assignment message");if(message)message.GetComponent<TextMeshProUGUI>().text=L("Nút này đang gộp nhiều thao tác. Chọn nút khác hoặc khôi phục mặc định.","This button overlaps several actions. Choose another button or restore defaults.");}
                }).name="Controller assign "+current;
            }
            ui.Text(box,"",40,646,920,64,24,VlabUi.Secondary).name="Controller assignment message";
            Button(box,L("Quay lại hình tay cầm","Back to controller"),40,714,920,64,()=>ShowControllerDiagram(right,side)).name="Controller diagram back";
        }
        void ShowControllerHelp(int chapter)
        {
            var box=Modal(L("Dùng tay cầm","Using the controller"),1100,800);
            page.gameObject.SetActive(false);
            string[] titles=Vi?new[]{"Thao tác","Kết nối","Hiệu chỉnh"}:new[]{"Controls","Connection","Calibration"};
            for(int i=0;i<3;i++){int next=i;Button(box,titles[i],40+i*344,110,332,64,()=>ShowControllerHelp(next));}
            string copy;
            if(chapter==0)copy=L("Trên màn hình: cần trái di chuyển con trỏ; cần phải xoay góc nhìn trong lab. Khi đeo kính: hướng nhìn điều khiển con trỏ.\n\nTrong Gán nút, chọn chức năng Chọn, Giữ, Menu, Căn lại hoặc Quay lại rồi Áp dụng. Thả nút trước khi dùng cấu hình mới. Ngắt kết nối sẽ dừng thao tác đang giữ.\n\nTrigger được dùng như nút theo ngưỡng trên tay cầm HID. Dữ liệu analog và hướng của tay cầm BLE riêng cần đúng giao thức firmware.","On screen: left stick moves the pointer; right stick looks around the lab. In a viewer, head direction points.\n\nChoose Select, Grip, Menu, Recenter or Back in Button mapping, then Apply. Release controls before using a new mapping. Disconnect cancels an active hold.\n\nHID triggers use a button threshold. Custom BLE analog and orientation data require the matching firmware protocol.");
            else if(chapter==1)copy=L("Tay cầm trong tài liệu dự án: thả các nút, đặt hai tay phẳng; bật tay trái trước rồi tay phải. Tay phải là cổng BLE tên VLAB CONTROLLER; điện thoại không cần tìm hai thiết bị.\n\nMở Bluetooth để ghép đôi. Saved / Đã lưu chưa có nghĩa đang kết nối và có dữ liệu. Với tay cầm HID, trở lại app và bấm nút để dùng; tên thiết bị nhận được hiện trong Cài đặt.\n\nFirmware BLE riêng chưa được xác minh khi chưa có UUID và định dạng gói. Điện thoại và Cardboard vẫn dùng được khi chưa có tay cầm.","For the project's controller: release controls, rest both hands flat, power the left hand before the right. The right hand is the single BLE gateway named VLAB CONTROLLER.\n\nPair in Bluetooth Settings. Saved does not prove a live connection or data. For HID, return and press a controller button; Settings shows the detected name.\n\nCustom BLE firmware requires verified UUIDs and packet format. Phone and Cardboard remain available without a controller.");
            else copy=L("Chỉ dùng các tổ hợp sau nếu tay cầm chạy đúng firmware trong hướng dẫn dự án:\n\nJoystick: giữ A+B 3 giây rồi thả; để trung tâm 2,5 giây, sau đó đi hết bốn hướng và vòng biên trong 8 giây. Thả về giữa.\n\nTrigger: giữ GRIP + nhấn cần 3 giây rồi thả; không bóp trong 2,5 giây đầu, sau đó bóp hết hành trình tới khoảng giây thứ 6. Thả và thử mức trung gian.\n\nHướng tay cầm: giữ MENU+GRIP 2 giây. Đây khác với Căn lại kính VR. App không tự gửi lệnh hiệu chỉnh khi chưa biết giao thức.","Use these combinations only with the firmware described in the project manual:\n\nJoystick: hold A+B for 3 s, release; stay neutral for 2.5 s, then cover all four directions and the rim for 8 s. Return to centre.\n\nTrigger: hold GRIP + stick-click for 3 s, release; no squeeze for 2.5 s, then squeeze fully until about 6 s. Release and test intermediate values.\n\nController heading: hold MENU+GRIP for 2 s. This differs from recentering the VR viewer. No calibration command is sent without a defined protocol.");
            ui.Scroll(box,copy,40,206,1020,456,26).name="Controller usage guide";
            Button(box,L("Quay lại gán nút","Back to button mapping"),40,704,1020,64,ShowControllerBindings);
        }
        void ShowHelp(string back)
        {
            supportReturn=back;NewPage("help");Header(L("Trợ giúp","Help"),()=>ReturnTo(back));
            ui.Scroll(page,L("TRỎ VÀ CHỌN\nChọn dụng cụ rồi đặt vào vị trí lắp. Thông số chỉ thay đổi khi đang chuẩn bị; bắt đầu lượt mới để so sánh biến.\n\nTẠM DỪNG VÀ QUAY LẠI\nEsc trên PC hoặc nút Quay lại mở tạm dừng. Hướng dẫn và cài đặt không làm mất số đo đã ghi.\n\nCĂN LẠI\nNhìn về phía trước rồi nhấn R hoặc chọn Căn lại. Giao diện VR đứng trong thế giới, không dán vào mặt.\n\nNGOẠI TUYẾN\nDữ liệu lưu trên thiết bị. Tháo cài đặt hoặc xóa dữ liệu ứng dụng có thể xóa hồ sơ.\n\nLiên hệ: nguamanvlab@gmail.com","POINT AND SELECT\nSelect equipment and place it at its installation anchor. Change controlled variables during preparation; start a new attempt for a comparison.\n\nPAUSE AND RETURN\nEsc on PC or the Back control pauses. Guidance and settings preserve recorded notebook measurements.\n\nRECENTER\nLook forward and press R or choose Recenter. VR panels stay in the world rather than following your face.\n\nOFFLINE\nData is stored on this device. Uninstalling or clearing app data may remove the profile.\n\nSupport: nguamanvlab@gmail.com"),250,170,1100,650,26);
        }
        void ShowAbout()
        {
            NewPage("about");Header(L("Giới thiệu VLAB","About VLAB"),ShowHome);var box=ui.Panel(page,"About",370,170,860,650);Logo(box,235,25,390,124);
            ui.Text(box,L("Phòng thí nghiệm STEM ngoại tuyến\nĐội Ngựa Mán · Trường THPT Chuyên Cao Bằng","Offline STEM laboratory\nĐội Ngựa Mán · Cao Bang High School for the Gifted")+"\n"+Application.version,50,175,760,120,26,null,TextAlignmentOptions.Center);
            Button(box,L("Chính sách bảo mật","Privacy policy"),40,330,370,65,()=>ShowLegal("privacy",false));Button(box,L("Điều khoản","Terms"),450,330,370,65,()=>ShowLegal("terms",false));
            Button(box,L("Sổ tay","Notebook"),40,425,370,65,()=>ShowNotebook("about"));Button(box,L("Giấy phép và nguồn","Credits and licenses"),450,425,370,65,()=>{var b=Modal(L("Nguồn và giấy phép","Credits and licenses"),1100,800);var credits=Resources.Load<TextAsset>("Legal/credits");ui.Scroll(b,credits?credits.text:L("Đang tổng hợp attribution.","Attribution is being assembled."),40,110,1020,560);Button(b,L("Quay lại","Back"),40,700,240,64,CloseModal);});
            Button(box,L("Xóa tiến độ","Reset progress"),40,520,370,65,()=>Confirm(L("Xóa các lượt đo đã ghi? Trang ghi chép riêng vẫn được giữ.","Clear recorded attempts? Written notebook pages are kept."),()=>ResetProfile(false)));Button(box,L("Đặt lại ứng dụng","Reset application"),450,520,370,65,()=>Confirm(L("Xóa toàn bộ hồ sơ, ghi chép, cài đặt và chấp thuận?","Clear profile, written notes, settings and acknowledgements?"),()=>ResetProfile(true)));
        }
        void ResetProfile(bool full){if(store.Reset(Profile,full,out var next,out status)){Profile=next;Session=null;ApplySettings();ShowSetupOrHome();}else Toast(status);}
        RectTransform Modal(string title,float width,float height,bool live=false)
        {
            ClearGuideCue();
            Apparatus?.CancelGrab();
            bool resume=ActivityRunIntent();ClearInspection();
            if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=ui.Page("Modal input blocker");PlaceReadableSurface(modal,true);var blocker=modal.gameObject.AddComponent<UnityEngine.UI.Image>();blocker.color=new Color(0,0,0,.32f);blocker.raycastTarget=true;
            modalSession=Session;modalResumeActivity=resume;modalAllowsSimulation=live;
            if(Route=="activity"){if(live&&resume)Session?.Resume();else if(!live)Session?.Pause();}SyncAuthority();
            var box=ui.Panel(modal,"Dialog",(1600-width)/2,(1000-height)/2,width,height);ui.Text(box,title,40,25,width-80,70,32);inputAfter=Time.unscaledTime+.2f;return box;
        }
        bool ActivityRunIntent()=>modal&&ReferenceEquals(modalSession,Session)?modalResumeActivity:Session!=null&&Session.Phase==ExperimentPhase.Running;
        void CloseModal()
        {
            ClearGuideCue();
            if(page&&!page.gameObject.activeSelf)page.gameObject.SetActive(true);
            bool resume=modalResumeActivity&&ReferenceEquals(modalSession,Session);if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=null;modalAllowsSimulation=modalResumeActivity=false;modalSession=null;ClearInspection();
            if(Route=="activity"&&resume)Session?.Resume();SyncAuthority();inputAfter=Time.unscaledTime+.2f;gazeTarget=null;gazeConsumed=false;
        }
        void SyncAuthority()
        {
            var authority=Session?.Authority;bool frozen=Session==null||Session.Phase!=ExperimentPhase.Running||Route!="activity"||(modal&&!modalAllowsSimulation);
            if(authority!=null&&(!ReferenceEquals(authority,observedAuthority)||frozen!=authorityFrozen))authority.SetPaused(frozen);
            observedAuthority=authority;authorityFrozen=frozen;
        }
        void Toast(string message){status=message;var b=Modal(L("Thông báo","Notice"),900,430);ui.Text(b,message,40,120,820,170,26);Button(b,L("Quay lại","Back"),580,320,280,64,CloseModal);}
        void Confirm(string message,Action accept){var b=Modal(L("Xác nhận","Confirm"),900,430);ui.Text(b,message,40,120,820,140,26);Button(b,L("Hủy","Cancel"),40,320,280,64,CloseModal);Button(b,L("Xác nhận","Confirm"),580,320,280,64,()=>{CloseModal();accept();},Accent);}
        void ReturnTo(string back){if(back=="results"&&shownResult!=null)ShowResults(shownResult);else if(back=="pause")ShowPause();else if(back=="activity"){if(activityReturnRunning)Session?.Resume();ShowActivity();}else if(back=="readiness")ShowReadiness(readinessResume);else if(back=="browser")ShowBrowser();else if(back=="about")ShowAbout();else ShowHome();}
        void ControllerBack()
        {
            if(modal){CloseModal();if(Route=="settings")ShowSettings();return;}
            switch(Route)
            {
                case "loading":CancelRoomEntry();break;
                case "load-error":ShowBrowser();break;
                case "settings":SettingsBack();break;
                case "activity":case "explore":ShowPause();break;
                case "pause":Session?.Resume();if(Session!=null)ShowActivity();else ShowExplore();break;
                case "readiness":ShowBrowser();break;
                case "browser":case "about":ShowHome();break;
                case "notebook":ReturnTo(notebookReturn);break;
                case "help":ReturnTo(supportReturn);break;
                case "results":ShowBrowser();break;
            }
        }
        void OnModeChanged(bool vr,string message)
        {
            if(modeLabel)modeLabel.text=ModeLabel;
            bool enteringWorld=ui.canvas.renderMode!=RenderMode.WorldSpace&&(vr||Input.SimulatorMode);
            Session?.Pause();modalResumeActivity=activityReturnRunning=false;Apparatus?.CancelGrab();ui.SetWorld(vr||Input.SimulatorMode,View);PlaceReadableSurface(page,Route!="explore");PlaceReadableSurface(modal,true);if(enteringWorld)StartCoroutine(AlignAfterTracking());SyncAuthority();
            var desktopModule=GetComponentInChildren<InputSystemUIInputModule>();if(desktopModule)desktopModule.enabled=!Input.ControllerPointerActive;
            if(!gazeProgress)
            {
                var pointer=new GameObject("VLAB world pointer",typeof(RectTransform),typeof(Canvas));pointer.transform.SetParent(transform,false);pointerRoot=pointer.GetComponent<RectTransform>();pointerRoot.sizeDelta=new Vector2(48,48);pointerRoot.localScale=Vector3.one*.0006f;
                var pointerCanvas=pointer.GetComponent<Canvas>();pointerCanvas.renderMode=RenderMode.WorldSpace;pointerCanvas.worldCamera=View;pointerCanvas.sortingOrder=100;
                var rim=ui.Rect(pointerRoot,"Pointer contrast rim",0,0,48,48);var dark= rim.gameObject.AddComponent<UnityEngine.UI.Image>();dark.sprite=VlabUi.PointerRing();dark.color=new Color(.02f,.035f,.07f,.9f);dark.raycastTarget=false;rim.localScale=Vector3.one*1.14f;
                var ring=ui.Rect(pointerRoot,"Pointer outline",0,0,48,48);var outline=ring.gameObject.AddComponent<UnityEngine.UI.Image>();outline.sprite=VlabUi.PointerRing();outline.color=new Color(1,1,1,.9f);outline.raycastTarget=false;
                var rect=ui.Rect(pointerRoot,"Gaze dwell progress",0,0,48,48);gazeProgress=rect.gameObject.AddComponent<UnityEngine.UI.Image>();gazeProgress.sprite=VlabUi.PointerRing();gazeProgress.color=Accent;gazeProgress.raycastTarget=false;gazeProgress.type=UnityEngine.UI.Image.Type.Filled;gazeProgress.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;gazeProgress.fillOrigin=2;
            }
            if(!flatPointerRoot)
            {
                flatPointerRoot=ui.Rect(ui.surface,"Flat controller pointer",0,0,48,48);flatPointerRoot.anchorMin=flatPointerRoot.anchorMax=Vector2.one*.5f;flatPointerRoot.pivot=Vector2.one*.5f;
                var rim=flatPointerRoot.gameObject.AddComponent<UnityEngine.UI.Image>();rim.sprite=VlabUi.PointerRing();rim.color=new Color(.02f,.035f,.07f,.95f);rim.raycastTarget=false;
                var ring=ui.Rect(flatPointerRoot,"Flat pointer outline",4,4,40,40);var outline=ring.gameObject.AddComponent<UnityEngine.UI.Image>();outline.sprite=VlabUi.PointerRing();outline.color=Color.white;outline.raycastTarget=false;
                var progress=ui.Rect(flatPointerRoot,"Flat dwell progress",4,4,40,40);flatGazeProgress=progress.gameObject.AddComponent<UnityEngine.UI.Image>();flatGazeProgress.sprite=VlabUi.PointerRing();flatGazeProgress.color=Accent;flatGazeProgress.raycastTarget=false;flatGazeProgress.type=UnityEngine.UI.Image.Type.Filled;flatGazeProgress.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;
            }
            pointerRoot.gameObject.SetActive(Input.ControllerPointerActive&&(vr||Input.SimulatorMode));
            flatPointerRoot.gameObject.SetActive(Input.ControllerPointerActive&&!vr&&!Input.SimulatorMode);
            if(Route=="notebook"&&page)ShowNotebook(notebookReturn);
            if(Route=="settings"&&page&&settingsTab==3)ShowSettings();
            if(!string.IsNullOrEmpty(message))Toast(message);
        }
        void Update()
        {
            if(!Input||!View||Profile==null)return;var frame=Input.Read();
            if(frame.back&&Time.unscaledTime>=inputAfter)ControllerBack();
            else if(frame.menu&&Route=="loading"&&Time.unscaledTime>=inputAfter)CancelRoomEntry();
            else if(frame.menu&&Route=="load-error")ShowBrowser();
            else if(frame.menu&&Route!="language"&&Route!="privacy"&&Route!="terms"&&Route!="safety"&&Route!="loading"){if(modal)CloseModal();else if(Route=="activity"||Route=="explore")ShowPause();else ShowHome();}
            if(frame.recenter){Input.Recenter();RecenterInterface();}if(frame.lost){Session?.InputLost();modalResumeActivity=activityReturnRunning=false;Apparatus?.CancelGrab();}
            SyncAuthority();
            if(Session!=null&&Route=="activity"&&(!modal||modalAllowsSimulation))Session.Advance(Time.unscaledDeltaTime);
            if(Session!=null&&Route=="activity"&&Time.unscaledTime>=nextDisplay){nextDisplay=Time.unscaledTime+.1f;RefreshActivity();RefreshInstrument();}
            if(Input.ControllerPointerActive&&!frame.lost)ProcessGaze(frame);
            else if(frame.lost){gazeTime=0;gazeConsumed=true;}
            if((Route=="activity"||Route=="explore")&&!modal&&Time.unscaledTime>=inputAfter)
            {
                if((frame.pressed||frame.grabPressed)&&!PointerHitsUi(frame.pointer)&&Physics.Raycast(frame.pointer,out var hit,3.5f))
                {var equipment=hit.collider.GetComponentInParent<EquipmentHandle>();if(equipment&&Apparatus){if(equipment.installed&&Apparatus.Driver!=null){instrumentPage=0;ShowInstrument(equipment);}else Apparatus.BeginGrab(equipment,frame.pointer);}else{var fixture=hit.collider.GetComponentInParent<LabFixture>();if(fixture)fixture.Activate();}}
                if(Apparatus)Apparatus.UpdateGrab(frame);
            }
        }
        bool PointerHitsUi(Ray ray)
        {RaycastInterface(ray,out _);return uiHits.Count>0;}
        void RaycastInterface(Ray ray,out PointerEventData pointer)
        {
            uiHits.Clear();Vector3 point=ray.GetPoint(2.25f);bool blocked=false;
            if(ui.canvas.renderMode==RenderMode.WorldSpace)
            {
                var surface=modal?modal:page?page:ui.canvas.GetComponent<RectTransform>();var plane=new Plane(surface.forward,surface.position);
                if(!plane.Raycast(ray,out float distance)||distance<0){pointer=new PointerEventData(EventSystem.current);return;}
                point=ray.GetPoint(distance);
                // A trigger cannot activate a menu through the apparatus or a wall.
                blocked=Physics.Raycast(ray,Mathf.Max(0,distance-.015f),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            }
            Vector3 screen=View.WorldToScreenPoint(point);pointer=new PointerEventData(EventSystem.current){position=new Vector2(screen.x,screen.y)};
            if(screen.z>0&&!blocked)EventSystem.current.RaycastAll(pointer,uiHits);
        }
        void ProcessGaze(ActionFrame frame)
        {
            RaycastInterface(frame.pointer,out var pointer);
            if(flatPointerRoot&&flatPointerRoot.gameObject.activeSelf)
            {
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.surface,pointer.position,null,out var local))flatPointerRoot.anchoredPosition=local;
                flatPointerRoot.SetAsLastSibling();
            }
            if(pointerRoot)
            {
                float distance=2.25f;var surface=modal?modal:page?page:ui.canvas.GetComponent<RectTransform>();var plane=new Plane(surface.forward,surface.position);
                if(plane.Raycast(frame.pointer,out float planeDistance)&&planeDistance>.1f)distance=Mathf.Min(planeDistance,3.5f);
                if(Physics.Raycast(frame.pointer,out var hit,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))distance=Mathf.Max(.08f,hit.distance-.012f);
                pointerRoot.position=frame.pointer.GetPoint(distance);pointerRoot.rotation=Quaternion.LookRotation(View.transform.forward,View.transform.up);
                // Keep the pointer about 0.7 degrees wide instead of shrinking with distance.
                pointerRoot.localScale=Vector3.one*(distance*.000267f);
            }
            var target=uiHits.Count>0?ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiHits[0].gameObject):null;
            if(target){var button=target.GetComponent<UnityEngine.UI.Button>();if(button&&!button.IsInteractable())target=null;}
            if(target!=gazeTarget){if(gazeTarget)ExecuteEvents.Execute(gazeTarget,pointer,ExecuteEvents.pointerExitHandler);gazeTarget=target;gazeTime=0;gazeConsumed=false;gazePosition=pointer.position;if(target)ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerEnterHandler);}
            else if(target&&target.GetComponent<InstrumentInspection>()&&Vector2.Distance(gazePosition,pointer.position)>12){gazePosition=pointer.position;gazeTime=0;gazeConsumed=false;}
            bool dwell=(Input.VrMode||Input.SimulatorMode)&&Profile.settings.gazeAutoSelect;
            bool ready=target&&Time.unscaledTime>=inputAfter;
            bool click=AdvanceGazeTimer(ref gazeTime,ref gazeConsumed,dwell&&ready,ready&&frame.pressed,Profile.settings.gazeDwellSeconds,Time.unscaledDeltaTime);
            float progress=dwell&&ready?Mathf.Clamp01(gazeTime/Profile.settings.gazeDwellSeconds):0;
            if(gazeProgress)gazeProgress.fillAmount=progress;
            if(flatGazeProgress)flatGazeProgress.fillAmount=progress;
            if(click)ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
        }
        internal static bool AdvanceGazeTimer(ref float elapsed,ref bool consumed,bool dwell,bool pressed,float duration,float deltaSeconds)
        {
            elapsed=dwell?elapsed+Mathf.Max(0,deltaSeconds):0;
            if(!pressed&&(!dwell||consumed||elapsed<duration))return false;
            consumed=true;return true;
        }
        void RecenterInterface(){ui.Recenter(View);PlaceReadableSurface(page,Route!="explore");PlaceReadableSurface(modal,true);}
        IEnumerator AlignAfterTracking(){yield return null;yield return null;if(ui.canvas.renderMode==RenderMode.WorldSpace)RecenterInterface();}
        void OnApplicationPause(bool paused){if(paused){Session?.Pause();modalResumeActivity=activityReturnRunning=false;SyncAuthority();Save();}}
        void OnApplicationFocus(bool focused){if(!focused){Session?.InputLost();modalResumeActivity=activityReturnRunning=false;SyncAuthority();Save();}}
        void OnApplicationQuit(){Session?.Pause();Save();}
    }
}
