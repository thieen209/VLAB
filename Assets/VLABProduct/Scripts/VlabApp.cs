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
    public sealed partial class VlabApp:MonoBehaviour
    {
        public static VlabApp Instance{get;private set;}
        public LocalProfile Profile{get;private set;}public ExperimentSession Session{get;private set;}
        public string Route{get;private set;}="boot";public Camera View{get;private set;}
        public VlabInput Input{get;private set;}public ApparatusRig Apparatus{get;private set;}
        public LabPanoramaPreview Preview{get;private set;}
        public PerformanceOverlay Performance{get;private set;}
        // The root release gate enables the integrated catalog only after its journey checks pass.
        public static bool IntegratedLessonsEnabled=true;
        public static bool IsLessonAvailable(string id)=>id=="P01"||(IntegratedLessonsEnabled&&LessonSpec.All.ContainsKey(id));
        ProfileStore store;VlabUi ui;RectTransform page,modal;Transform origin;
        BriefCatalog catalog;ExperimentBrief selected;string subject="P",settingsReturn="home",supportReturn="home",notebookReturn="home",status="",legalViewing="";
        string privacyHash,termsHash;GameSettings draft;string draftLocale;int settingsTab;
        bool changing,dirty,explicitAcceptance,writeProtected,roomEntryCancelled,notebookKeyboardRequested;float inputAfter,nextDisplay;int attemptSeed=1;
        TextMeshProUGUI roomLoadStatus;UnityEngine.UI.Image roomLoadFill;UnityEngine.UI.Button roomLoadBack;AsyncOperation activeRoomLoad;
        TextMeshProUGUI liveReading,liveHint,liveTime,liveStage,liveActionLabel;
        AttemptRecord pendingMeasurement;ExperimentSession pendingMeasurementSession;SessionCheckpoint pendingMeasurementCheckpoint;
        bool HasPendingMeasurement=>pendingMeasurement!=null&&ReferenceEquals(pendingMeasurementSession,Session);
        UnityEngine.UI.Button liveAction,compactAction,activityFold;TextMeshProUGUI compactActionLabel;RectTransform activityCard;bool compactActivity=true;bool hudHidden;RectTransform pointerRoot;
        bool modalAllowsSimulation,modalResumeActivity,activityReturnRunning;ExperimentSession modalSession;
        int equipmentPage,variablePage,instrumentPage,readingPage;string inspectionTab="";EquipmentHandle inspectedItem;RectTransform inspectionOutput;string inspectionReadingKeys="",liveReadingError="";AttemptRecord shownResult;
        UnityEngine.UI.RawImage inspectionImage;TextMeshProUGUI inspectionReadings,inspectionStatus;
        readonly Dictionary<string,UnityEngine.UI.Button> inspectionControls=new Dictionary<string,UnityEngine.UI.Button>();
        IExperimentAuthority observedAuthority;bool authorityFrozen;
        LessonGuide guide;ExperimentSession guideSession;int guidePage=-1;bool readinessResume;
        Vector2? selectedInspectionUv;ExperimentSession selectedInspectionSession;TextMeshProUGUI selectedCellSummary;
        RectTransform flatPointerRoot;UnityEngine.UI.Image flatGazeProgress;
        readonly JournalState journal=new JournalState();
        GuideCue worldGuideCue,buttonGuideCue;string cueStep,observedGuideStep;float nextWorldHint;int guidanceTier;
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();GameObject gazeTarget;float gazeTime;bool gazeConsumed,suppressOpeningGaze;Vector2 gazePosition;
        UnityEngine.UI.Image gazeProgress;UniversalRenderPipelineAsset runtimePipeline;UnityEngine.Rendering.RenderPipelineAsset originalPipeline;int appliedGraphics=-1;
        static readonly Dictionary<string,string> readingVi=new Dictionary<string,string>{{"period","Chu kỳ"},{"timedCycles","Số chu kỳ"},{"measuredTime","Thời gian đo"},{"gravityEstimate","g suy ra"}};
        bool Vi=>(Route=="settings"?draftLocale:Profile.locale)!="en";
        string L(string vi,string en)=>Vi?vi:en;
        Color Accent=>VlabUi.Hex("3B82F6");
        public string Status=>status;
        internal bool HasOpenModal=>modal;
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
            catalog=JsonUtility.FromJson<BriefCatalog>(Resources.Load<TextAsset>("Catalog/experiments").text);
            string profilePath=Path.Combine(Application.persistentDataPath,"Profile");
#if UNITY_EDITOR
            string testProfile=UnityEditor.SessionState.GetString("VLAB.TestProfile","");if(!string.IsNullOrEmpty(testProfile))profilePath=testProfile;
#endif
            store=new ProfileStore(profilePath);var loaded=store.Load();Profile=loaded.profile;ApplyStorageLoadMessage(loaded);
            privacyHash=DocumentHash("privacy");termsHash=DocumentHash("terms");
            origin=new GameObject("Learner Origin").transform;origin.SetParent(transform,false);
            var cameraGo=new GameObject("VLAB Camera",typeof(Camera),typeof(AudioListener),typeof(UniversalAdditionalCameraData));cameraGo.tag="MainCamera";cameraGo.transform.SetParent(origin,false);View=cameraGo.GetComponent<Camera>();View.nearClipPlane=.04f;View.farClipPlane=40;View.fieldOfView=65;
            gameObject.AddComponent<VlabAudio>();
            Input=gameObject.AddComponent<VlabInput>();Input.Adopt(View);Input.BlocksTouchLook=ray=>
            {
                if(modal||PointerHitsUi(ray))return true;
                return Physics.Raycast(ray,out var hit,3.5f)&&(hit.collider.GetComponentInParent<EquipmentHandle>()||(hit.collider.GetComponentInParent<LabFixture>() is LabFixture fixture&&fixture.CanInteract));
            };Input.BlocksPhoneGameplay=PhoneUiOwnsTouch;Input.ModeChanged+=OnModeChanged;
            Preview=gameObject.AddComponent<LabPanoramaPreview>();Preview.Initialize(View,Profile.settings.reducedMotion);
            ui=new VlabUi(transform);var eventGo=new GameObject("VLAB Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));eventGo.transform.SetParent(transform,false);var inputModule=eventGo.GetComponent<InputSystemUIInputModule>();inputModule.AssignDefaultActions();
            // Mouse movement must not cancel an in-flight touchscreen press.
            inputModule.pointerBehavior=UIPointerBehavior.AllPointersAsIs;
            Performance=gameObject.AddComponent<PerformanceOverlay>();Performance.Initialize(ui);
            var grading=new GameObject("Daylight grading",typeof(UnityEngine.Rendering.Volume));grading.transform.SetParent(transform,false);var volume=grading.GetComponent<UnityEngine.Rendering.Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=Resources.Load<UnityEngine.Rendering.VolumeProfile>("UI/VlabColor");
            SceneManager.sceneLoaded+=OnSceneLoaded;
        }
        void Start(){
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;Screen.orientation=ScreenOrientation.AutoRotation;
#endif
            AdoptScene();ApplySettings();ShowSetupOrHome();}
        void OnDestroy(){if(activeRoomLoad!=null&&!activeRoomLoad.isDone)activeRoomLoad.allowSceneActivation=true;if(Instance==this){SceneManager.sceneLoaded-=OnSceneLoaded;if(runtimePipeline){if(QualitySettings.renderPipeline==runtimePipeline)QualitySettings.renderPipeline=originalPipeline;Destroy(runtimePipeline);}Instance=null;}}
        string DocumentHash(string id)
        {var vi=LegalPdfView.Metadata(id+"_vi");var en=LegalPdfView.Metadata(id+"_en");return vi!=null&&en!=null?ProfileStore.Hash(vi.sourceSha256+"\n"+en.sourceSha256):"";}
        bool Save()
        {if(HasPendingMeasurement){status=StorageSaveMessage();return false;}if(Session!=null&&!Session.Complete)Profile.checkpoint=Session.Checkpoint();if(store.Save(Profile,out var error)){writeProtected=false;return true;}StorageFailure(error);return false;}
        string StorageSaveMessage()=>L("Chưa xác nhận lưu. Dữ liệu trong phiên này vẫn được giữ. Kiểm tra dung lượng thiết bị rồi thử lưu lại; nếu có số đo chờ lưu, trở lại trạm và chọn Thử lưu số đo.","Saving is unconfirmed. This session's data is retained. Check device storage and retry; for a pending reading, return to the station and choose Retry saving reading.");
        void StorageFailure(string diagnostic,bool reset=false)
        {Debug.LogWarning("VLAB local storage: "+diagnostic);status=reset?L("Chưa xác nhận đặt lại. Trạng thái trên thiết bị có thể đã thay đổi. Không thể bảo đảm dữ liệu cũ còn nguyên; kiểm tra dung lượng và mở lại ứng dụng trước khi quyết định thử lại.","Reset is unconfirmed. Device state may have changed, so old data cannot be guaranteed. Check storage and reopen the application before deciding whether to retry."):StorageSaveMessage();}
        void StorageToast(){Toast(status);if(page)page.gameObject.SetActive(false);}
        void ApplyStorageLoadMessage(ProfileLoad loaded)
        {
            writeProtected=loaded.writeProtected;if(!string.IsNullOrEmpty(loaded.message))Debug.LogWarning("VLAB local profile load: "+loaded.message);
            status=loaded.writeProtected?L("Không thể mở hồ sơ đã lưu. Tệp hiện có được bảo vệ; hãy kiểm tra dung lượng hoặc chọn khôi phục hồ sơ. Chưa ghi đè dữ liệu.","The saved profile could not be opened. Existing files are protected; check storage or choose profile recovery. Data has not been overwritten."):loaded.recovered?L("Đã mở bản sao dự phòng hợp lệ. Hãy kiểm tra ghi chép và số đo trước khi tiếp tục lưu.","A valid backup was opened. Check your notes and readings before saving again."):string.IsNullOrEmpty(loaded.message)?"":L("Có vấn đề khi mở dữ liệu trên thiết bị. Hãy kiểm tra hồ sơ trước khi tiếp tục.","A problem occurred while opening local data. Check your profile before continuing.");
        }
        void ClearPendingMeasurement(){pendingMeasurement=null;pendingMeasurementSession=null;pendingMeasurementCheckpoint=null;}
        void ShowSetupOrHome(){if(string.IsNullOrEmpty(Profile.locale)){Profile.locale="vi";if(!writeProtected)Save();}ShowHome();}
        RectTransform NewPage(string route,string backgroundExperiment=null)
        {
            Input?.Dual?.CancelInputContext();Apparatus?.CancelHandGrabs();
            CancelFlagshipDrag();ClearGuideCue();CloseWorkbench();
            if(Route=="activity"&&route!="activity"){activityReturnRunning=ActivityRunIntent();Session?.Pause();}
            observedGuideStep=null;guidanceTier=0;nextWorldHint=Time.unscaledTime+12;
            if(route!="activity")hudHidden=false;
            Route=route;ui.Vietnamese=Vi;inputAfter=Time.unscaledTime+.22f;gazeTarget=null;gazeTime=0;gazeConsumed=false;suppressOpeningGaze=Input&&(Input.VrMode||Input.SimulatorMode);
            RefreshBackground(backgroundExperiment);
            if(route!="activity")Apparatus?.CancelGrab();
            if(page){page.gameObject.SetActive(false);Destroy(page.gameObject);}if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}
            modalAllowsSimulation=modalResumeActivity=false;modalSession=null;ClearInspection();
            if(route!="settings")UiTheme.Select(Profile.settings.uiTheme,Profile.settings.uiOpacity);
            page=ui.Page(route);PlaceReadableSurface(page,route!="explore");Input.Gameplay=route=="activity"||route=="explore";RoomBranding.SetVisible(SceneManager.GetActiveScene(),route!="home");
            SyncAuthority();
            liveReading=liveHint=liveTime=liveStage=liveActionLabel=compactActionLabel=null;liveAction=compactAction=null;activityCard=null;return page;
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
            if(close&&content==modal){content.localPosition=new Vector3(0,0,-1125);content.localScale=Vector3.one*.27f;content.localRotation=Quaternion.identity;return;}
            if(close&&content==page&&Route=="activity"){content.localPosition=new Vector3(0,0,-843.75f);content.localScale=Vector3.one*.54f;content.localRotation=Quaternion.identity;return;}
            content.localPosition=home?ui.canvas.transform.InverseTransformPoint(View.transform.position+View.transform.forward*1.25f):new Vector3(0,0,close?-843.75f:0);content.localScale=Vector3.one*(close?(home?.96f:.54f):1);
            // Face the opening Home pose once; subsequent head movement leaves it anchored.
            content.localRotation=home?Quaternion.Inverse(ui.canvas.transform.rotation)*View.transform.rotation:Quaternion.identity;
        }
        TextMeshProUGUI modeLabel;bool roundModeLabel;
        string ModeLabel=>Input.VrMode?L("Thoát VR","Exit VR"):Application.isEditor?(Input.SimulatorMode?L("Thoát mô phỏng","Exit simulation"):L("Mô phỏng VR","Simulate VR")):L("Vào VR","Enter VR");
        void ModeButton(Transform parent,float x,float y,float width,float height)
        {
            var button=Button(parent,L("Thoát VR","Exit VR"),x,y,width,height,()=>{Session?.Pause();if(Application.isEditor)Input.SetSimulator(false);else Input.SetVr(false);});
            button.name="VR exit";button.gameObject.SetActive(Input.VrMode||Input.SimulatorMode);roundModeLabel=false;modeLabel=button.GetComponentInChildren<TextMeshProUGUI>();
        }
        void Header(string title,Action back=null)
        {
            var bar=ui.Panel(page,"Header",40,30,1520,85);ui.Text(bar,title,back==null?24:180,20,1050,60,40).fontWeight=FontWeight.SemiBold;
            if(back!=null)Button(bar,L("‹ Quay lại","‹ Back"),12,12,155,60,back);
            ModeButton(bar,1290,12,210,60);
        }
        UnityEngine.UI.Button Button(Transform parent,string label,float x,float y,float w,float h,Action action,Color? accent=null)
        {return ui.Button(parent,label,x,y,w,h,()=>{if(Time.unscaledTime>=inputAfter&&!changing)action?.Invoke();},accent);}
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
            FooterStatus();
            var next=Button(box,draftLocale=="en"?"Continue":"Tiếp tục",310,510,240,64,()=>ChooseLanguage(draftLocale),Accent);next.interactable=!writeProtected&&(draftLocale=="vi"||draftLocale=="en");next.gameObject.SetActive(!writeProtected);
            if(next.interactable)EventSystem.current.SetSelectedGameObject(next.gameObject);
            if(writeProtected)Button(box,L("Khôi phục hồ sơ","Profile recovery"),180,520,500,64,()=>Confirm(status+"\n"+L("Đặt lại hồ sơ này?","Reset this local profile?"),()=>ResetProfile(true)));
        }
        public void ChooseLanguage(string locale){Profile.locale=locale;if(Save())ShowSetupOrHome();else ShowLanguage();}
        void ShowLegal(string id,bool setup)
        {
            legalViewing=id;explicitAcceptance=false;NewPage(setup?id:"legal");var box=ui.Panel(page,"Legal",240,70,1120,860);
            ui.Text(box,id=="privacy"?L("Chính sách bảo mật","Privacy policy"):L("Điều khoản sử dụng","Terms of service"),48,28,1024,68,38);
            ui.Text(box,L("Tài liệu đầy đủ · Đọc ngoại tuyến trên thiết bị","Full document · Available offline"),48,98,1024,42,24,VlabUi.Secondary);
            var pdf=box.gameObject.AddComponent<LegalPdfView>();
            if(!pdf.Initialize(ui,box,id+(Vi?"_vi":"_en"),Vi)){ui.Text(box,L("Không tìm thấy PDF gốc. Không thể ghi nhận chấp thuận.","Original PDF unavailable. Acceptance cannot be recorded."),40,130,980,180);Button(box,L("Thử lại","Retry"),40,540,240,64,()=>ShowLegal(id,setup));return;}
            if(setup)
            {
                var consent=Button(box,L("[ ] Tôi đã đọc và đồng ý","[ ] I have read and accept"),48,754,680,64,()=>{});
                consent.onClick.RemoveAllListeners();consent.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter)return;explicitAcceptance=!explicitAcceptance;consent.GetComponentInChildren<TextMeshProUGUI>().text=(explicitAcceptance?"[x] ":"[ ] ")+L("Tôi đã đọc và đồng ý","I have read and accept");});
                Button(box,L("Tiếp tục","Continue"),780,754,292,64,()=>{if(!explicitAcceptance||!pdf.Ready){Toast(L("Cần xác nhận rõ ràng và tài liệu đầy đủ để tiếp tục.","Explicit acceptance and an available document are required."));return;}Profile.Accept(id,id=="privacy"?privacyHash:termsHash,true);if(Save())ShowSetupOrHome();},Accent);
            }
            else{Button(box,L("Quay lại","Back"),48,754,300,72,ShowAbout);Button(box,L("Mở tài liệu PDF","Open PDF"),708,754,364,72,()=>StartCoroutine(LocalDocuments.OpenPdf(id+(Vi?"_vi":"_en")+".pdf",Toast)));}
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
            Apparatus?.GetComponent<BenchDisplayVisibility>()?.Restore();
            Session?.Pause();NewPage("home");HomePresentation.Backdrop(ui,page);Logo(page,605,65,390,124);RoundModeButton(page);
            string[] subjects={"P","C","B","M"},namesVi={"Vật lí","Hóa học","Sinh học","Kỹ thuật"},namesEn={"Physics","Chemistry","Biology","Engineering"},desVi={"Chuyển động và năng lượng","Chất và phản ứng","Sự sống và tế bào","Cơ cấu và đo lường"},desEn={"Motion and energy","Matter and reactions","Life and cells","Mechanisms & measurement"},images={"physics","chemistry","biology","engineering"};
            var cards=new RectTransform[4];for(int i=0;i<4;i++)
            {
                int n=i;Color color=Accent;var card=HomePresentation.Card(ui,page,"Subject "+subjects[i],images[i],Vi?namesVi[i]:namesEn[i],Vi?desVi[i]:desEn[i],color,i);cards[i]=card;
                var button=card.gameObject.AddComponent<UnityEngine.UI.Button>();button.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter||changing)return;subject=subjects[n];selected=catalog.items.First(x=>x.id.StartsWith(subject));StartCoroutine(Portal(card));});
                var focus=card.gameObject.AddComponent<PortalFocus>();focus.FocusColor=color;focus.ReducedMotion=Profile.settings.reducedMotion;focus.Focus=on=>{if(on)Preview.Show(namesEn[n]);else RefreshBackground();};
            }
            page.gameObject.AddComponent<HomePortalArc>().Cards=cards;
            string[] utility={L("Cài đặt","Settings"),L("Trợ giúp","Help"),L("Giới thiệu","About"),L("Sổ tay","Notebook")};Action[] actions={()=>OpenSettings("home"),()=>ShowHelp("home"),ShowAbout,()=>ShowNotebook("home")};
            for(int i=0;i<4;i++){int n=i;var button=HomePresentation.Utility(ui,page,utility[i],i,()=>{if(Time.unscaledTime>=inputAfter&&!changing)actions[n]();});if(i==3)button.name="Open journal";}
            QuickStartChoice();if(Profile.quickStartSeen&&!quickStartActive&&string.IsNullOrEmpty(status))HomePresentation.Helper(ui,page,Input.VrMode||Input.SimulatorMode?L("Nhìn hoặc trỏ để chọn","Look or point to select"):L("Chạm vào phòng để chọn","Tap a laboratory to select"),918);else FooterStatus();
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
            Apparatus?.GetComponent<BenchDisplayVisibility>()?.Restore();
            NewPage("browser");var box=ui.Panel(page,"Experiment browser",140,130,1320,750);
            Button(box,L("‹ Trang chủ","‹ Home"),32,28,220,64,ShowHome).name="Browser home";
            var heading=ui.Text(box,SubjectName,290,28,820,65,40);heading.fontWeight=FontWeight.SemiBold;
            var lessons=catalog.items.Where(x=>x.id.StartsWith(subject)).OrderBy(x=>x.id,StringComparer.Ordinal).ToArray();if(selected==null||!selected.id.StartsWith(subject))selected=lessons[0];
            ui.Text(box,lessons.Length+L(" thí nghiệm"," experiments"),1140,28,148,60,20,VlabUi.Secondary,TextAlignmentOptions.Center).name="Browser lesson count";
            for(int i=0;i<lessons.Length;i++)
            {
                var brief=lessons[i];int saved=Profile.attempts.Count(record=>record.experimentId==brief.id);
                string savedLabel=saved==0?L("Chưa có số đo","No saved readings"):saved+L(" lượt đã lưu",saved==1?" saved attempt":" saved attempts");
                var button=Button(box,LessonGuide.ShortTitle(brief.id,Vi)+(saved>0?"\n<size=18>"+savedLabel+"</size>":""),32+(i%2)*420,145+(i/2)*124,400,112,()=>{selected=brief;ShowBrowser();},selected==brief?Color.Lerp(VlabUi.Raised,Accent,.22f):(Color?)null);
                button.name="Browser lesson "+brief.id;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=ui.LargeText?29.12f:26;button.GetComponentInChildren<TextMeshProUGUI>().color=VlabUi.TextColor;
                if(selected==brief)ui.Rule(button.transform,0,12,4,88,Accent);
            }
            ui.Rule(box,870,145,1,569,VlabUi.Stroke);
            var title=ui.Text(box,LessonGuide.ShortTitle(selected.id,Vi),890,142,398,88,32);title.fontWeight=FontWeight.SemiBold;title.name="Browser selected title";
            var preview=Button(box,L("Khám phá phòng","Explore laboratory"),890,242,398,184,()=>StartCoroutine(LoadRoom(false,false)));preview.name="Browser explore";preview.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
            var photo=Resources.Load<Texture2D>("UI/Preview_"+SceneName);
            if(photo)
            {
                var image=ui.Picture(preview.transform,photo,0,0,398,184);image.transform.SetAsFirstSibling();
                float crop=Mathf.Min(1,(float)photo.width/photo.height/(398f/184));image.uvRect=new Rect(0,(1-crop)/2,1,crop);
                var shade=ui.Rule(preview.transform,0,128,398,56,VlabUi.PanelColor);shade.SetSiblingIndex(1);
                var caption=preview.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(12,-134);caption.rectTransform.sizeDelta=new Vector2(374,44);caption.color=Color.white;
            }
            ui.Text(box,ReadinessView.Goal(selected.id,Vi),890,442,398,112,24,VlabUi.Secondary).name="Browser selected goal";
            var start=Button(box,IsLessonAvailable(selected.id)?L("Bắt đầu thí nghiệm","Start experiment"):L("Đang chuẩn bị","In development"),890,564,398,80,()=>ShowReadiness(false),Accent);
            start.name="Browser start";start.interactable=IsLessonAvailable(selected.id);start.GetComponentInChildren<TextMeshProUGUI>().fontSize=ui.LargeText?31.36f:28;
            FooterStatus();box.gameObject.AddComponent<PhoneBrowserLayout>().Initialize(ui,box,quickStartActive?120:0);
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
            if(HasPendingMeasurement){ShowActivity();Toast(StorageSaveMessage());yield break;}
            ClearPendingMeasurement();
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
                    rigGo=new GameObject("Experiment apparatus");Apparatus=rigGo.AddComponent<ApparatusRig>();framedInstalledCount=-1;lastFocusedTaskTarget=null;
                    Vector3 worktop=FindWorktop();var bench=rigGo.AddComponent<BenchDisplayVisibility>();bench.ClearBench(worktop);bench.SetSurface(Session.spec.id,worktop);Apparatus.Build(Session,worktop);if(Flagship(Session.spec.id)){for(int n=0;n<40&&!Apparatus.Ready;n++){Apparatus.Select(Apparatus.NextInstallableItem());if(!Apparatus.InstallSelected())throw new InvalidOperationException("Prepared kit could not be seated.");}Session.Assistance="prepared-kit";if(Session.spec.id=="C04")rigGo.AddComponent<FlagshipStageLabels>().Initialize(Apparatus,View);}ConfigureFlagshipTargets();Apparatus.Changed+=RefreshActivity;
                    var eye=worktop+new Vector3(-.4f,.75f,-2.5f);var look=worktop+new Vector3(-.55f,.75f,0);
                if(Session.spec.id=="P02")look.x=Apparatus.items.Single(item=>item.assetId=="launcher").slot.x+.25f;
                else if(Session.spec.id=="P08")look.x-=.2f;
                    if(Profile.settings.focusAssist&&!Input.VrMode&&!Input.SimulatorMode){eye=worktop+new Vector3(-.25f,.7f,-1.8f);look=worktop+new Vector3(-.15f,.3f,0);}
                    // Keep the tracked head's parent level; phone focus owns the bench pitch.
                    if(Input.VrMode||Input.SimulatorMode)MoveToWorkstation();
                    else{origin.position=eye;origin.rotation=Quaternion.Euler(0,Quaternion.LookRotation(look-eye,Vector3.up).eulerAngles.y,0);View.transform.localPosition=Vector3.zero;View.transform.localRotation=Quaternion.identity;Input.FocusAtWorldTarget(look);}
                    Apparatus.SetCoachingEnabled(Profile.settings.hints);
                    if(!Input.VrMode&&!Input.SimulatorMode)
                    {
                        View.fieldOfView=Profile.settings.cameraFov;
                        if(Profile.settings.focusAssist&&Session.spec.id!="P02")FocusStation();
                    }
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
                if(cam==View)continue;if(cam.name.Contains("Learner")){origin.SetPositionAndRotation(cam.transform.position,Quaternion.Euler(0,cam.transform.eulerAngles.y,0));if(!Input.VrMode&&!Input.SimulatorMode){View.transform.localPosition=Vector3.zero;View.transform.localRotation=Quaternion.Euler(cam.transform.eulerAngles.x,0,0);}}
                cam.enabled=false;cam.tag="Untagged";var listener=cam.GetComponent<AudioListener>();if(listener)listener.enabled=false;
            }
            foreach(var review in FindObjectsByType<LabReview>(FindObjectsSortMode.None))review.enabled=false;
            foreach(var interactor in FindObjectsByType<LabInteractor>(FindObjectsSortMode.None))interactor.enabled=false;
            foreach(var events in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))if(events.transform.parent!=transform)events.enabled=false;
            RoomLighting.Apply(Profile.settings.GraphicsTier);RoomBranding.Apply(SceneManager.GetActiveScene(),Vi);View.enabled=true;View.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;ui?.SetWorld(Input.VrMode||Input.SimulatorMode,View);
        }
        void ShowExplore()
        {
            NewPage("explore");Header(SubjectName,ShowBrowser);var nodes=FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("Teleport_")).OrderBy(x=>x.name).ToArray();
            for(int i=0;i<nodes.Length;i++){int n=i;Button(page,L("Trạm ","Station ")+(i+1),70+i*225,840,210,64,()=>{origin.position=nodes[n].position;RecenterInterface();});}
            ui.Text(page,L("Điện thoại: vuốt nền để nhìn · PC: giữ chuột phải · Căn lại trong cài đặt","Phone: drag the background to look · PC: right-mouse look · Recenter in Settings"),100,135,1300,50,22,VlabUi.Secondary);
        }
        void RoundModeButton(RectTransform parent)
        {
            bool world=Input.VrMode||Input.SimulatorMode;
            var button=HomePresentation.Utility(ui,parent,world?L("Thoát VR","Exit VR"):"VR",4,()=>
            {if(Time.unscaledTime<inputAfter||changing)return;Session?.Pause();if(Application.isEditor)Input.SetSimulator(!Input.SimulatorMode);else Input.SetVr(!Input.VrMode);});
            button.name="VR mode";
            roundModeLabel=true;modeLabel=button.GetComponentInChildren<TextMeshProUGUI>();
        }
        public void ShowActivity()
        {
            NewPage("activity");bool world=ui.canvas.renderMode==RenderMode.WorldSpace;
            var contents=ui.Rect(page,"Activity HUD contents",0,0,1600,1000);
            var box=ui.Panel(contents,"Current laboratory action",36,32,400,362);activityCard=box;
            var title=ui.Text(box,LessonGuide.ShortTitle(selected.id,Vi),20,16,360,80,30);title.fontWeight=FontWeight.SemiBold;
            float titleHeight=Mathf.Max(40,title.GetPreferredValues(title.text,360,Mathf.Infinity).y);
            title.rectTransform.sizeDelta=new Vector2(360,titleHeight);
            liveStage=ui.Text(box,"",20,titleHeight+28,360,36,22,VlabUi.Secondary);
            liveHint=ui.Text(box,"",20,titleHeight+80,360,174,26);
            liveAction=Button(box,"",20,210,360,70,PerformContextAction,Accent);
            liveActionLabel=liveAction.GetComponentInChildren<TextMeshProUGUI>();liveActionLabel.fontSize=ui.LargeText?28:26;
            liveTime=ui.Text(box,"",20,294,360,56,22,VlabUi.Secondary);
            compactAction=Button(contents,"",560,858,480,72,PerformContextAction,Accent);compactAction.name="Compact current action";compactActionLabel=compactAction.GetComponentInChildren<TextMeshProUGUI>();
            float toolsY=world?100:176;
            float railY=world?180:32;
            var tools=ui.Panel(contents,"Station control rail",1272,railY,292,toolsY+240);
            Button(tools,world?L("Tới bàn thao tác","Go to workbench"):L("Nhìn về bàn","Focus station"),16,16,260,64,FocusStation).name="Focus workstation";
            if(!world)
            {
                Button(tools,L("Gần +","Near +"),16,92,124,64,()=>ZoomPhone(-8)).name="Phone zoom in";
                Button(tools,L("Xa −","Far −"),152,92,124,64,()=>ZoomPhone(8)).name="Phone zoom out";
            }
            ui.Rule(tools,20,toolsY-12,252,1,VlabUi.Stroke);
            Button(tools,L("Dụng cụ & trợ giúp","Tools & help"),16,toolsY,260,64,ShowStationTools).name="Station tools";
            Button(tools,L("‹ Quay lại","‹ Back"),16,toolsY+152,260,64,ShowPause);
            var fold=Button(tools,"",16,toolsY+76,260,64,null);activityFold=fold;
            Action foldPanel=()=>{activityCard.gameObject.SetActive(!compactActivity);compactAction.gameObject.SetActive(compactActivity);string label=L(compactActivity?"Hiện bảng":"Thu gọn bảng",compactActivity?"Show panel":"Hide panel");fold.name=label;fold.GetComponentInChildren<TextMeshProUGUI>().text=label;};
            fold.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter||changing)return;compactActivity=!compactActivity;foldPanel();});foldPanel();
            var visibility=Button(page,"",world?1176:1272,world?44:railY+toolsY+252,world?172:292,72,null);visibility.name="HUD visibility";
            Action showHud=()=>{contents.gameObject.SetActive(!hudHidden);visibility.GetComponentInChildren<TextMeshProUGUI>().text=L(hudHidden?"Hiện UI":"Ẩn UI",hudHidden?"Show UI":"Hide UI");if(hudHidden){ClearGuideCue();CloseWorkbench();}};
            visibility.onClick.AddListener(()=>{if(Time.unscaledTime<inputAfter||changing)return;hudHidden=!hudHidden;showHud();});showHud();
            ModeButton(page,1364,44,200,72);
            RefreshActivity();if(world)RecenterInterface();
        }
        LessonGuide Guide(){if(!ReferenceEquals(guideSession,Session)){guideSession=Session;guide=Session==null?null:new LessonGuide(Session);guidePage=-1;}return guide;}
        GuideTask CurrentGuide(){var task=Guide()?.Current(Apparatus,Vi,ActivityRunIntent());if(task!=null&&Session.spec.id=="M01"&&Apparatus.Ready&&(Session.Phase==ExperimentPhase.Preparing||Session.Phase==ExperimentPhase.Ready))task.key="motor";return task;}
        string GuideCard(GuideTask task)
        {
            if(task.kind=="prepare")return (Vi?Session.spec.operationsVi:Session.spec.operationsEn)[Session.Operation];
            if(task.kind=="image")return L("Chọn ô trên hình, rồi tự chọn nhãn. Mã ô không bị đếm trùng.","Select an image cell, then choose its label. IDs are not counted twice.");
            if(task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell")return L("Chọn kính hiển vi rồi tự phân loại ô đang chọn.","Open the microscope and classify the selected cell.");
            return task.action;
        }
        void PerformContextAction()
        {
            if(Apparatus.IsPullingBob){if(!Apparatus.ReleasePendulum(out var why))ActionFailure(why);else Save();RefreshActivity();return;}
            if(flagshipDrag){UpdateFlagshipDrag(new ActionFrame{pointer=lastInteractionRay,released=true});RefreshActivity();return;}
            if(Apparatus.InspectingInstalled){var item=Apparatus.Selected;Apparatus.CancelGrab();SelectWorkbenchTool(item);RefreshActivity();return;}
            if(Apparatus.IsCarrying){if(Apparatus.InstallSelected()){Save();RefreshActivity();}else{Apparatus.CanInstallSelected(out var why);Toast(LessonGuide.ExplainFailure(why,Vi));}return;}
            if(!Apparatus.Ready){ShowEquipment();return;}
            if(Profile.settings.hints&&Session.Phase==ExperimentPhase.Preparing){var task=CurrentGuide();if(task.kind=="instrument"||task.kind=="image"){ShowGuideTarget(task);return;}}
            switch(Session.Phase)
            {
                case ExperimentPhase.Preparing:if(Flagship(Session.spec.id))ShowGuideTarget(CurrentGuide());else PrepareAction();break;
                case ExperimentPhase.Ready:
                    if(!Session.Start(out status))ActionFailure(status);else{VlabAudio.Play(VlabSound.Experiment);Apparatus.ApplyPreparation(Session.Operation);RefreshActivity();}break;
                case ExperimentPhase.Paused:Session.Resume();RefreshActivity();break;
                case ExperimentPhase.Running:RecordMeasurement();break;
                case ExperimentPhase.Measured:if(HasPendingMeasurement)RecordMeasurement();break;
                case ExperimentPhase.Invalid:ShowPause();break;
            }
        }
        bool toolSheetFocusSaved;
        Vector3 toolSheetReturnPosition;Quaternion toolSheetReturnRotation;
        float toolSheetReturnFov;
        ExperimentSession toolSheetFocusSession;
        void FocusPhoneToolSheet()
        {
            if(toolSheetFocusSaved||Input.VrMode||Input.SimulatorMode||!Profile.settings.focusAssist||!Apparatus||Route!="activity")return;
            var task=CurrentGuide();var item=Apparatus.items.FirstOrDefault(x=>x.assetId==task?.key&&x.installed&&x.gameObject.activeInHierarchy);
            if(!item||!ApparatusRig.TryVisibleBounds(item,out _))return;
            toolSheetReturnPosition=origin.position;toolSheetReturnRotation=View.transform.rotation;toolSheetReturnFov=View.fieldOfView;toolSheetFocusSession=Session;toolSheetFocusSaved=true;
            FocusInstrument(item,true);
        }
        void ShowStationTools()
        {
            var box=Modal(L("Bảng điều khiển","Control panel"),720,590,Apparatus.Driver!=null);box.GetComponent<PhoneDialogLayout>().CompactControls=true;
            Button(box,L("Dụng cụ","Equipment"),40,120,304,88,ShowEquipment);
            Button(box,L("Thông số","Variables"),376,120,304,88,ShowVariables);
            Button(box,L("Hướng dẫn","Guidance"),40,240,304,88,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Sổ tay","Notebook"),376,240,304,88,()=>ShowNotebook("activity"));
            Button(box,L("Ánh sáng","Lighting"),40,360,304,72,ShowRoomLighting).name="Room lighting";
            Button(box,L("Bảng bài học","Lesson board"),376,360,304,72,ShowWhiteboard).name="Sketch board";
            Button(box,L("Trở lại trạm","Return to station"),280,480,400,72,()=>{CloseModal();RefreshActivity();},Accent).name="Station return";
        }
        void ShowRoomLighting()
        {
            var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();
            var lamps=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Ceiling illumination").ToArray();
            var box=Modal(L("Ánh sáng","Lighting"),720,500,Apparatus.Driver!=null);
            box.GetComponent<PhoneDialogLayout>().CompactControls=true;
            var status=ui.Text(box,"",40,108,640,80,28);
            var blind=Button(box,"",40,206,304,88,()=>{if(shade)shade.Toggle();});blind.name="Room shade";blind.interactable=shade;
            var ceiling=Button(box,"",376,206,304,88,()=>{bool enabled=!lamps.Any(l=>l&&l.enabled);foreach(var light in lamps)if(light)light.enabled=enabled;});ceiling.name="Room lamp";ceiling.interactable=lamps.Length>0;
            ui.Text(box,L("Rèm điều chỉnh ánh sáng cửa sổ. Đèn trần bổ trợ độc lập; ánh sáng nền vẫn giữ để dễ thao tác.","The blind controls window light. Ceiling fill is independent; base lighting remains for visibility."),40,318,640,80,24,VlabUi.Secondary);
            Button(box,L("Quay lại","Back"),160,420,400,64,ShowStationTools).name="Lighting back";
            StartCoroutine(RoomLightStatus(status,blind,ceiling,shade,lamps));
        }
        IEnumerator RoomLightStatus(TextMeshProUGUI label,UnityEngine.UI.Button blind,UnityEngine.UI.Button ceiling,LabShade shade,Light[] lamps)
        {
            var interval=new WaitForSecondsRealtime(.1f);
            var blindLabel=blind.GetComponentInChildren<TextMeshProUGUI>();var ceilingLabel=ceiling.GetComponentInChildren<TextMeshProUGUI>();
            while(label&&label.isActiveAndEnabled)
            {
                string state=!shade?L("Không có rèm","No blind"):shade.lowered?
                    (shade.OpenFraction>.001f?L("Rèm đang đóng","Blind closing"):L("Rèm đã đóng","Blind closed")):
                    (shade.OpenFraction<.999f?L("Rèm đang mở","Blind opening"):L("Rèm đã mở","Blind open"));
                bool lightOn=lamps.Any(l=>l&&l.enabled);
                label.text=state+"\n"+(lamps.Length==0?L("Không có đèn bổ trợ","No ceiling fill"):lightOn?L("Đèn trần: bật","Ceiling fill: on"):L("Đèn trần: tắt","Ceiling fill: off"));
                blindLabel.text=shade&&shade.lowered?L("Mở rèm","Open blind"):L("Đóng rèm","Close blind");
                ceilingLabel.text=lightOn?L("Tắt đèn trần","Turn off ceiling"):L("Bật đèn trần","Turn on ceiling");
                yield return interval;
            }
        }
        void RefreshActivity()
        {
            if(Route!="activity"||Session==null||!liveHint)return;
            if(Profile.settings.focusAssist&&!Input.PhoneGestureActive&&!flagshipDrag&&!Apparatus.IsPullingBob&&!Input.VrMode&&!Input.SimulatorMode&&!Apparatus.IsCarrying&&!modal&&!changing)
            {
                int installed=Apparatus.items.Count(item=>item.installed&&item.gameObject.activeInHierarchy);
                if(installed!=framedInstalledCount){framedInstalledCount=installed;if(installed>0&&Session.spec.id!="P02")FocusStation();}
            }
            var currentTask=CurrentGuide();string step=GuideStep(currentTask);

            FocusStepTarget(currentTask);
            if(cueStep!=null&&cueStep!=step)ClearGuideCue();
            if(observedGuideStep!=step){observedGuideStep=step;guideMistakes=0;guidanceTier=0;actionFailureUntil=0;nextWorldHint=Time.unscaledTime+12;}
            if(!quickStartActive&&!hudHidden&&Profile.settings.hints&&!modal&&!changing&&Time.unscaledTime>=nextWorldHint&&(!worldGuideCue||worldGuideCue.IsStopped)){guidanceTier++;nextWorldHint=Time.unscaledTime+(guidanceTier==1?23:35);var hinted=Apparatus.IsCarrying&&Apparatus.CanInstallSelected(out _)?Apparatus.Selected:Apparatus.items.FirstOrDefault(x=>x.assetId==currentTask?.key);if(hinted&&hinted.gameObject.activeInHierarchy)ShowEquipmentCue(hinted,guidanceTier>1);}
            string action;bool enabled=true;
            if(Apparatus.IsPullingBob||flagshipDrag)
            {liveStage.text=L("ĐANG THAO TÁC","MANIPULATING");liveHint.text=L("Di chuyển con trỏ để điều chỉnh. Chọn Thả khi đã ở vị trí mong muốn; Quay lại để hủy.","Move the pointer to adjust. Select Release at the intended position; Back cancels.");action=L("Thả dụng cụ","Release");}
            else if(Apparatus.InspectingInstalled)
            {liveStage.text=L("QUAN SÁT DỤNG CỤ","INSPECTING");liveHint.text=L("Di chuyển để nhìn gần; xoay bằng hai nút mũi tên. Đặt lại sẽ trả đúng vị trí cũ.","Move to inspect; rotate with the arrow controls. Return restores the original position.");action=L("Đặt lại dụng cụ","Return equipment");}
            else if(Apparatus.IsCarrying)
            {liveStage.text=L("ĐANG CẦM DỤNG CỤ","HOLDING EQUIPMENT");liveHint.text=L("Đặt vào vị trí lắp khi giá đỡ sẵn sàng. Mở Dụng cụ để trả lại khay.","Place at its mounting point when supports are ready. Open Equipment to return it to the tray.");action=L("Đặt dụng cụ","Place equipment");}
            else if(!Apparatus.Ready)
            {
                liveStage.text=L("LẮP DỤNG CỤ","ASSEMBLE STATION");
                liveHint.text=Apparatus.Driver==null?L("Lắp các dụng cụ còn lại vào trạm. Chọn dụng cụ để xem vị trí lắp.","Fit the remaining equipment. Choose an item to see its mounting point."):L("Lắp dụng cụ tại trạm. Chọn món đã lắp để xem và thao tác.","Fit the station equipment. Select a fitted item to inspect it.");
                action=L("Mở dụng cụ","Open equipment");
            }
            else if(Session.Phase==ExperimentPhase.Preparing)
            {
                liveStage.text=L("CHUẨN BỊ · ","PREPARATION · ")+(Session.Operation+1)+" / "+Session.spec.operationsEn.Length;
                liveHint.text=Profile.settings.hints?(Vi?Session.spec.operationsVi:Session.spec.operationsEn)[Session.Operation]:L("Chuẩn bị theo quy trình. Mở Hướng dẫn khi cần.","Follow the preparation procedure. Open Guidance when needed.");
                action=Flagship(Session.spec.id)?L("Chỉ vị trí thao tác","Show me where"):L("Chuẩn bị có hỗ trợ","Assist this preparation");
            }
            else if(Session.Phase==ExperimentPhase.Ready)
            {
                liveStage.text=L("SẴN SÀNG ĐO","READY TO MEASURE");
                liveHint.text=Apparatus.Driver==null?L("Thả nhẹ, không đẩy. Quan sát chuyển động của con lắc.","Release gently without pushing. Observe the pendulum's motion."):L("Dụng cụ đã sẵn sàng. Bắt đầu lượt và thao tác trên dụng cụ.","Start the prepared trial. Select instruments to operate them.");
                action=Apparatus.Driver==null?L("Thả / Bắt đầu","Release / Start"):L("Bắt đầu lượt đo","Start trial");
            }
            else if(HasPendingMeasurement)
            {liveStage.text=L("SỐ ĐO CHỜ LƯU","READING AWAITS SAVE");liveHint.text=L("Số đo đang được giữ. Thử lưu lại cùng lượt; không cần đo lại.","Reading retained. Retry saving this trial; no need to measure again.");action=L("Thử lưu số đo","Retry saving reading");}
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
            if(Profile.settings.hints&&!HasPendingMeasurement&&!Apparatus.IsCarrying&&!Apparatus.IsPullingBob&&!flagshipDrag)
            {
                var task=CurrentGuide();liveHint.text=GuideCard(task);
                if(Session.Phase==ExperimentPhase.Preparing&&(task.kind=="instrument"||task.kind=="image")&&!Flagship(Session.spec.id))action=L("Mở dụng cụ cần dùng","Open required instrument");
            }
            if(!Profile.settings.hints)ClearGuideCue();
            if(Time.unscaledTime<actionFailureUntil)liveHint.text=actionFailureText;if(Profile.settings.hints&&Flagship(Session.spec.id)&&Time.unscaledTime>=actionFailureUntil&&!Apparatus.IsPullingBob&&!flagshipDrag&&!Apparatus.IsCarrying)liveHint.text=FlagshipInstruction();liveActionLabel.text=action;liveAction.name=action;liveAction.interactable=enabled;if(compactAction){compactAction.interactable=enabled;compactActionLabel.text=action;}
            float hintHeight=Mathf.Max(64,liveHint.GetPreferredValues(liveHint.text,360,Mathf.Infinity).y+8);
            float actionY=-liveHint.rectTransform.anchoredPosition.y+hintHeight+16;
            bool showReadings=Session.Phase==ExperimentPhase.Running||Session.Phase==ExperimentPhase.Paused||HasPendingMeasurement;
            liveHint.rectTransform.sizeDelta=new Vector2(360,hintHeight);
            ((RectTransform)liveAction.transform).anchoredPosition=new Vector2(20,-actionY);
            liveTime.gameObject.SetActive(showReadings);
            liveTime.rectTransform.anchoredPosition=new Vector2(20,-(actionY+82));
            activityCard.sizeDelta=new Vector2(400,actionY+(showReadings?154:90));
            if(Session.Pendulum!=null)
            {
                var motion=Session.Pendulum;int requested=(int)Session.parameters["cycles"];
                double clock=motion.FirstCrossing<0?0:(motion.TargetCrossing>=0?motion.TargetCrossing:motion.Time)-motion.FirstCrossing;
                liveTime.text=L("Chu kỳ toàn phần  ","Full cycles  ")+Math.Min(motion.Cycles,requested)+" / "+requested+"\n"+L("Đồng hồ  ","Clock  ")+clock.ToString("0.00")+" s";
            }
            else if(Session.spec.id=="M01"&&(Session.Phase==ExperimentPhase.Running||Session.Phase==ExperimentPhase.Paused)){var readings=Session.Observe().readings;liveTime.text=L("Vào  ","Input  ")+readings.Single(r=>r.key=="inputSpeed").value.ToString("0.#")+" rpm\n"+L("Ra  ","Output  ")+readings.Single(r=>r.key=="outputSpeed").value.ToString("0.#")+" rpm";}else liveTime.text=L("Đồng hồ  ","Clock  ")+Session.Elapsed.ToString("0.00")+" s";
        }
        int guideMistakes;
        string actionFailureText;float actionFailureUntil;
        void ActionFailure(string reason){VlabAudio.Play(VlabSound.Warning);actionFailureText=LessonGuide.ExplainFailure(reason,Vi);actionFailureUntil=Time.unscaledTime+8;if(Route!="activity"||modal){Toast(actionFailureText);return;}if(liveHint)liveHint.text=actionFailureText;if(Profile.settings.hints&&++guideMistakes>=3){var item=Apparatus?.items.FirstOrDefault(x=>x.assetId==CurrentGuide()?.key);if(item)PointAtEquipment(item);}}
        void PrepareAction(){if(Apparatus.PrepareNext(out status)){Session.Assistance="guided-preparation";Save();}else ActionFailure(status);RefreshActivity();}
        void ShowEquipment()=>ShowWorkbenchPicker();
        void ShowQuickEquipment()=>ShowWorkbenchPicker();
        string EquipmentName(EquipmentHandle item)=>Apparatus.EquipmentLabel(item,Vi);
        int InstrumentPageSize()=>Apparatus?.Driver is BiologyDriver?5:6;
        internal static bool IsClassificationControl(string id)=>id.StartsWith("structure:",StringComparison.Ordinal)||id.StartsWith("stage:",StringComparison.Ordinal)||id=="cellNormal"||id=="cellContracted";
        Reading[] LiveReadings()
        {
            liveReadingError="";if(Apparatus?.Driver==null||!Apparatus.Ready)return Array.Empty<Reading>();
            try{return Session.Observe().readings.ToArray();}catch(Exception error){liveReadingError=LessonGuide.ExplainFailure(error.Message,Vi);return Array.Empty<Reading>();}
        }
        public void ShowInstrument(EquipmentHandle item)
        {
            if(!item||!item.installed||Apparatus?.Driver==null)return;
            var driver=Apparatus.Driver;var texture=driver.Inspection(item.assetId);
            bool classifier=driver is BiologyDriver&&item.assetId=="microscope"&&(Session.spec.id=="B01"||Session.spec.id=="B02"||Session.spec.id=="B08");
            bool same=inspectedItem==item;if(!same){inspectionTab=classifier&&Session.Phase==ExperimentPhase.Preparing?"tools":texture?"view":"readings";readingPage=0;}
            bool classification=classifier&&inspectionTab=="view";
            if(!toolSheetFocusSaved&&!Input.VrMode&&!Input.SimulatorMode&&Profile.settings.focusAssist&&Route=="activity"){toolSheetReturnPosition=origin.position;toolSheetReturnRotation=View.transform.rotation;toolSheetReturnFov=View.fieldOfView;toolSheetFocusSession=Session;toolSheetFocusSaved=true;}
            FocusInstrument(item,inspectionTab=="readings"&&Screen.width>Screen.height);var box=Modal(EquipmentName(item),1440,900,true);inspectedItem=item;
            var title=box.GetComponentInChildren<TextMeshProUGUI>();title.fontSize=40;title.fontWeight=FontWeight.SemiBold;
            selectedCellSummary=ui.Text(box,"",1000,24,300,80,26,Accent,TextAlignmentOptions.TopRight);selectedCellSummary.lineSpacing=0;
            string[] tabs=classifier?new[]{"view","tools","readings","explain"}:texture?new[]{"view","readings","explain"}:new[]{"readings","explain"};
            for(int i=0;i<tabs.Length;i++)
            {
                string tab=tabs[i];string label=tab=="view"?(classifier?L("Phân loại mẫu","Classify sample"):L("Hình quan sát","Observation")):tab=="tools"?L("Chỉnh kính","Microscope tools"):tab=="readings"?L("Số đo trực tiếp","Live readings"):L("Hiểu kết quả","Interpretation");
                float tabWidth=classifier?326:288;var button=Button(box,label,40+i*(tabWidth+16),116,tabWidth,64,()=>{if(tab=="view"&&Session.spec.id=="B01"&&!BeginMicroscopeObservation())return;inspectionTab=tab;instrumentPage=0;ShowInstrument(item);},inspectionTab==tab?Accent:(Color?)null);button.name="Instrument tab "+tab;
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize=26;
            }

            inspectionOutput=ui.Rect(box,"Instrument observation output",40,205,920,classifier&&Session.spec.id!="B01"&&(inspectionTab=="view"||inspectionTab=="tools")?450:515);
            if((inspectionTab=="view"||inspectionTab=="tools")&&texture)
            {
                var frame=ui.Panel(inspectionOutput,"Schematic view",0,0,920,450,VlabUi.Raised);Rect uv=driver.InspectionUv(item.assetId);
                float aspect=Mathf.Abs(texture.width*uv.width/(texture.height*uv.height)),width=Mathf.Min(880,420*aspect),height=width/aspect;
                if(driver is BiologyDriver layers&&layers.PlasmolysisWallTexture)
                {var wall=ui.Picture(frame,layers.PlasmolysisWallTexture,20+(880-width)/2,15+(420-height)/2,width,height);wall.name="Fixed cell wall";wall.uvRect=uv;}
                inspectionImage=ui.Picture(frame,texture,20+(880-width)/2,15+(420-height)/2,width,height);inspectionImage.uvRect=uv;inspectionImage.raycastTarget=true;
                if(driver is BiologyDriver biology&&biology.HasB01Presentation)inspectionImage.gameObject.AddComponent<MicroscopePresentation>().Bind(inspectionImage,biology);
                var selection=inspectionImage.gameObject.AddComponent<InstrumentInspection>();selection.MarkerColor=VlabUi.Hex("F0A34D");if(ReferenceEquals(selectedInspectionSession,Session))selection.SetSelection(selectedInspectionUv);
                selection.Selected=sourceUv=>
                {
                    if(Session.spec.id=="B01"&&Session.Phase==ExperimentPhase.Preparing&&!BeginMicroscopeObservation())return;
                    if(!driver.PointOnInspection(item.assetId,sourceUv,out var message)){inspectionStatus.text=LessonGuide.ExplainFailure(message,Vi);return;}
                    selectedInspectionUv=sourceUv;selectedInspectionSession=Session;Save();selection.SetSelection(sourceUv);
                    if(classifier&&inspectionTab!="view"){inspectionTab="view";instrumentPage=0;ShowInstrument(item);}else RefreshInstrument();
                    if(inspectionStatus)inspectionStatus.text=Session.spec.id=="B08"?L("Chọn kỳ phân bào vừa quan sát.","Choose the stage you observed."):Session.spec.id=="B02"?L("Tế bào đã co hay chưa co?","Is this cell plasmolysed?"):L("Chọn tên cấu trúc vừa quan sát.","Choose the structure you observed.");
                };
                if(!classifier||Session.spec.id=="B01")ui.Text(inspectionOutput,Session.spec.id=="B01"?MicroscopePresentation.Notice(Vi):L("Chọn một vị trí trên hình để quan sát và tự phân loại.","Select a location to observe and classify it yourself."),0,468,920,50,24,VlabUi.Secondary);
            }
            else if(inspectionTab=="readings")
            {
                RenderInstrumentReadings(item,LiveReadings());
            }
            else
            {
                var reader=ui.Scroll(inspectionOutput,InstrumentReadings(),0,0,920,515,28);inspectionReadings=reader.GetComponentInChildren<TextMeshProUGUI>();
            }
            var controls=driver.Controls(item.assetId);if(classifier)controls=controls.Where(c=>IsClassificationControl(c.id)==classification).ToArray();int pageSize=classification?6:InstrumentPageSize(),pages=Math.Max(1,(controls.Length+pageSize-1)/pageSize);instrumentPage=Mathf.Clamp(instrumentPage,0,pages-1);
            for(int row=0;row<pageSize;row++)
            {
                int index=instrumentPage*pageSize+row;if(index>=controls.Length)break;var control=controls[index];
                var button=Button(box,Vi?control.vi:control.en,1000+(classification?row%2*208:0),205+(classification?row/2*88:row*76),classification?192:400,classification?80:66,()=>
                {
                    if(!driver.Controls(item.assetId).Any(c=>c.id==control.id&&c.enabled))return;
                    bool okay=driver.Operate(control.id,out var message);if(okay){Guide().Applied(control.id);VlabAudio.Action(control.id,item.transform.position);Save();}
                    RefreshInstrument();if(inspectionStatus)inspectionStatus.text=LessonGuide.ExplainAction(message,okay,Vi);
                });button.name="Instrument control "+control.id;button.interactable=control.enabled;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=26;inspectionControls[control.id]=button;if(control.id.EndsWith("+",StringComparison.Ordinal)||control.id.EndsWith("-",StringComparison.Ordinal))button.gameObject.AddComponent<HoldToAdjust>();
            }
            if(controls.Length==0)ui.Text(box,L("Dụng cụ này không cần nút thao tác lúc này.","No instrument controls are needed right now."),1000,210,400,180,28,VlabUi.Secondary);
            if(pages>1)
            {
                Button(box,L("Trang trước","Previous"),1000,675,190,64,()=>{instrumentPage=Math.Max(0,instrumentPage-1);ShowInstrument(item);}).interactable=instrumentPage>0;
                Button(box,L("Trang sau","Next"),1210,675,190,64,()=>{instrumentPage=Math.Min(pages-1,instrumentPage+1);ShowInstrument(item);}).interactable=instrumentPage+1<pages;
            }
            inspectionStatus=ui.Text(box,classification?L("Chạm vị trí trên hình, rồi chọn nhãn.","Select an image location, then its label."):L("Chọn thao tác ở bên phải; xem các giá trị ở thẻ Số đo trực tiếp.","Choose a control on the right; open Live readings for the values."),40,746,1360,54,24,VlabUi.Secondary);
            Button(box,L("Dụng cụ khác","Other equipment"),40,820,400,64,ShowEquipment);
            Button(box,L("Hướng dẫn","Guidance"),480,820,400,64,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Trở lại trạm","Return to station"),920,820,480,64,()=>{CloseModal();RefreshActivity();},Accent);
            RefreshInstrument();
        }
        static string ReadingKeys(IEnumerable<Reading> readings)=>string.Join("|",readings.Select(r=>r.key+":"+r.unit));
        void RenderInstrumentReadings(EquipmentHandle item,Reading[] readings)
        {
            foreach(Transform child in inspectionOutput){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            inspectionReadingKeys=ReadingKeys(readings);int pageSize=ui.canvas.renderMode==RenderMode.WorldSpace?4:2;readingPage=Mathf.Clamp(readingPage,0,ObservationCards.PageCount(readings,pageSize)-1);
            ObservationCards.Render(ui,inspectionOutput,readings,Vi,readingPage,0,0,920,420,pageSize);
            if(readings.Length==0&&!string.IsNullOrEmpty(liveReadingError))inspectionOutput.GetComponentInChildren<TextMeshProUGUI>().text=liveReadingError;
            int count=ObservationCards.PageCount(readings,pageSize);
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
            FocusPhoneToolSheet();
            var box=Modal(L("Thông số thí nghiệm","Experiment variables"),1050,740,Apparatus.Driver!=null);box.GetComponent<PhoneDialogLayout>().ParameterPanel=true;
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
                if(fixedValue)ui.Text(box,L("Cố định","Fixed value"),730,y+10,220,55,22,VlabUi.Secondary).name="Parameter fixed "+p.key;
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
            if(Flagship(Session.spec.id)&&guidePage<0&&Session.Phase==ExperimentPhase.Preparing)Button(box,L("Hỗ trợ bước này","Assist this step"),330,585,250,64,()=>Confirm(L("Ứng dụng sẽ thực hiện bước chuẩn bị hiện tại nếu đủ điều kiện. Lượt thực hành sẽ được ghi là có hỗ trợ.","The app will perform this preparation if its conditions are met. The trial will be marked assisted."),()=>{CloseModal();PrepareAction();}));else Button(box,L("Việc hiện tại","Current task"),330,585,250,64,()=>{guidePage=-1;ShowGuidance();});
            Button(box,L("Bước sau","Next step"),620,585,250,64,()=>{guidePage=Math.Min(Session.spec.operationsEn.Length-1,guidePage+1);ShowGuidance();});
            Button(box,L("Chỉ tôi chỗ này","Show me where"),40,690,400,70,()=>ShowGuideTarget(task),Accent);
            Button(box,L("Gợi ý: ","Hints: ")+(Profile.settings.hints?L("Bật","On"):L("Tắt","Off")),470,690,270,70,()=>{Profile.settings.hints=!Profile.settings.hints;ClearGuideCue();Apparatus?.SetCoachingEnabled(Profile.settings.hints);Save();ShowGuidance();});
            Button(box,L("Quay lại","Back"),770,690,290,70,CloseModal);
        }
        void ShowGuideTarget(GuideTask task)
        {
            ClearGuideCue();
            if(task==null)return;
            if(task.kind=="pause"){CloseModal();ShowPause();return;}
            if(task.kind=="variables"){CloseModal();ShowVariables();return;}
            var item=Apparatus.items.FirstOrDefault(x=>x.assetId==task.key);
            if(task.kind=="install"&&item){equipmentPage=Apparatus.items.IndexOf(item)/4;ShowEquipment();return;}
            if(Session.spec.id=="P02"&&item&&item.installed&&task.key=="target"){CloseModal();Apparatus.Select(item);PointAtEquipment(item);RefreshActivity();return;}
            if(item&&item.installed&&Flagship(Session.spec.id)&&task.kind!="image"&&!task.control.StartsWith("structure:",StringComparison.Ordinal)){CloseModal();Apparatus.Select(item);PointAtEquipment(item);return;}
            if(item&&item.installed&&Apparatus.Driver!=null&&task.kind!="image"&&!task.control.EndsWith(":",StringComparison.Ordinal)&&task.control!="cell"){CloseModal();SelectWorkbenchTool(item,task.control);ShowEquipmentCue(item,true);return;}
            if(item&&item.installed&&Apparatus.Driver!=null)
            {
                var controls=Apparatus.Driver.Controls(item.assetId);int index=Array.FindIndex(controls,c=>!string.IsNullOrEmpty(task.control)&&(c.id==task.control||c.id.StartsWith(task.control,StringComparison.Ordinal)));
                if(Session.spec.id=="C01"&&index>=0){ShowTitrationGuide(item,task,controls[index].id);return;}
                bool microscope=Apparatus.Driver is BiologyDriver&&item.assetId=="microscope"&&(Session.spec.id=="B01"||Session.spec.id=="B02"||Session.spec.id=="B08");
                if(microscope){bool classify=task.kind=="image"||task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell";inspectionTab=classify?"view":"tools";var visible=controls.Where(c=>IsClassificationControl(c.id)==classify).ToArray();int visibleIndex=Array.FindIndex(visible,c=>!string.IsNullOrEmpty(task.control)&&(c.id==task.control||c.id.StartsWith(task.control,StringComparison.Ordinal)));instrumentPage=Math.Max(0,visibleIndex)/(classify?6:InstrumentPageSize());}
                else{instrumentPage=Math.Max(0,index)/InstrumentPageSize();inspectionTab=task.kind=="image"||task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell"?"view":inspectionTab;}
                if(microscope)inspectedItem=item;ShowInstrument(item);
                bool classification=task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell";
                if(index>=0&&!classification&&inspectionControls.TryGetValue(controls[index].id,out var button)){button.GetComponent<UnityEngine.UI.Image>().color=Accent;button.GetComponentInChildren<TextMeshProUGUI>().color=UiTheme.On(Accent);buttonGuideCue=GuideCue.AttachTo(button.transform,Profile.settings.reducedMotion);}
                else if(classification&&inspectionImage)buttonGuideCue=GuideCue.AttachTo(inspectionImage.transform,Profile.settings.reducedMotion);
                cueStep=GuideStep(CurrentGuide());
                if(inspectionStatus)inspectionStatus.text=classification?L("Quan sát rồi tự chọn nhãn.","Observe, then choose a label."):index>=0?L("Chọn nút được tô sáng. Ứng dụng chưa làm hộ thao tác.","Select the highlighted button. No action has been performed for you."):L("Quan sát dụng cụ; Quay lại để dùng nút bước trên bảng.","Inspect the instrument; return to the station card for its task button.");return;
            }
            CloseModal();if(item){Apparatus.Select(item);PointAtEquipment(item);}var action=compactAction&&compactAction.isActiveAndEnabled?compactAction:liveAction;if(action)buttonGuideCue=GuideCue.AttachTo(action.transform,Profile.settings.reducedMotion);cueStep=GuideStep(CurrentGuide());RefreshActivity();
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
        string GuideStep(GuideTask task)=>task==null?null:Session.Operation+":"+Session.Phase+":"+task.kind+":"+task.key+":"+task.control+":"+(Apparatus.IsCarrying?"carrying":"free")+":"+Apparatus.Selected?.assetId;
        void ClearGuideCue(){if(worldGuideCue)worldGuideCue.Stop();if(buttonGuideCue)buttonGuideCue.Stop();worldGuideCue=buttonGuideCue=null;cueStep=null;}
        void PointAtEquipment(EquipmentHandle item){ShowEquipmentCue(item,true);}
        void ShowEquipmentCue(EquipmentHandle item,bool details)
        {
            if(!Profile.settings.hints||!item||!Apparatus){ClearGuideCue();return;}
            float clearTop=.97f;
            if(!Input.VrMode&&!Input.SimulatorMode&&page)
            {
                var zoom=page.GetComponentsInChildren<RectTransform>().FirstOrDefault(rect=>rect.name=="Phone zoom in");
                if(zoom){var corners=new Vector3[4];zoom.GetWorldCorners(corners);clearTop=corners.Min(corner=>RectTransformUtility.WorldToScreenPoint(null,corner).y)/Screen.height-.02f;}
            }
            var cord=Resources.Load<GameObject>("Equipment/VLAB_string").GetComponentInChildren<LineRenderer>();
            string instruction=Apparatus.IsCarrying?(Apparatus.CanInstallSelected(out _)?L("Đưa dụng cụ tới vùng đánh dấu rồi chọn Đặt dụng cụ.","Move the equipment to its marked position, then select Place equipment."):(Apparatus.InstallSupport(item) is EquipmentHandle support?L("Lắp trước: ","Install first: ")+EquipmentName(support):L("Đặt lại dụng cụ rồi kiểm tra vị trí lắp.","Return the tool and check its placement."))):CurrentGuide()?.key==item.assetId?ShortInstruction(CurrentGuide().action):EquipmentPurpose(item);
            if(Session.spec.id=="P01"&&!Apparatus.IsCarrying)
            {
                string purpose=item.assetId switch{"stand"=>L("Đỡ và cố định hệ con lắc.","Supports the pendulum assembly."),"clamp"=>L("Giữ dây tại điểm treo.","Secures the cord at its suspension point."),"pendulum"=>L("Vật dao động để đo chu kỳ.","Oscillating mass used to measure the period."),"string"=>L("Nối điểm treo với quả nặng.","Connects the suspension point to the bob."),"ruler"=>L("Đọc chiều dài bằng vạch chia milimét.","Reads length using millimetre graduations."),"timer"=>L("Đo thời gian của các chu kỳ.","Measures the elapsed time of oscillations."),_=>""};
                instruction=Apparatus.IsPullingBob?L("Kéo quả sang bên. Chọn Thả khi sẵn sàng.","Move the bob aside. Select Release when ready."):CurrentGuide()?.key==item.assetId?FlagshipInstruction():purpose;
            }
            if(Flagship(Session.spec.id)&&Session.spec.id!="P01"&&!Apparatus.IsCarrying)instruction=CurrentGuide()?.key==item.assetId?FlagshipInstruction():FlagshipPurpose(item);
            if(!Apparatus.IsCarrying&&!string.IsNullOrEmpty(DirectGestureHint(item)))instruction=DirectGestureHint(item);
            if(worldGuideCue)worldGuideCue.Stop();
            worldGuideCue=GuideCue.ShowEquipment(Apparatus.transform,item,Profile.settings.reducedMotion,cord?cord.sharedMaterial:null);worldGuideCue.SetContext(EquipmentName(item),details?instruction:"",View,clearTop,!Input.VrMode&&!Input.SimulatorMode&&Route=="activity"?ActivityObservationArea():(Rect?)null);cueStep=GuideStep(CurrentGuide());
        }
        void RecordMeasurement()
        {
            if(!HasPendingMeasurement)
            {
                if(!Session.Measure(out status)){Toast(LessonGuide.ExplainFailure(status,Vi));RefreshActivity();return;}
                pendingMeasurement=Session.Record();pendingMeasurementSession=Session;pendingMeasurementCheckpoint=Profile.checkpoint;
            }
            var record=pendingMeasurement;bool added=!Profile.attempts.Any(attempt=>attempt.attemptId==record.attemptId);if(added)Profile.attempts.Add(record);Profile.checkpoint=null;
            if(!store.Save(Profile,out var error)){if(added)Profile.attempts.Remove(record);Profile.checkpoint=pendingMeasurementCheckpoint;StorageFailure(error);StorageToast();RefreshActivity();return;}
            writeProtected=false;ClearPendingMeasurement();status="";VlabAudio.Play(VlabSound.Success);ShowResults(record);
        }
        string FormatReadings(IEnumerable<Reading> readings)=>string.Join("\n",readings.Select(r=>ReadingLabel(r)+"   "+ObservationCards.FormatValue(r,Vi)+" "+ObservationCards.DisplayUnit(r)));
        public static string ReadingCaption(Reading reading,bool vi)
        {string supplied=vi?reading.labelVi:reading.labelEn;string caption=!string.IsNullOrEmpty(supplied)?supplied:vi&&readingVi.TryGetValue(reading.key,out var label)?label:reading.key;return caption.Replace("SDK ","");}
        string ReadingLabel(Reading reading)=>ReadingCaption(reading,Vi);
        void ShowResults(AttemptRecord record,int readingIndex=0)
        {
            shownResult=record;NewPage("results");var box=ui.Panel(page,"Results",80,60,1440,880);
            var title=ui.Text(box,L("Đã lưu vào sổ thực hành","Saved to your lab notebook"),40,30,1360,70,40);title.fontWeight=FontWeight.SemiBold;
            ui.Text(box,LessonGuide.ShortTitle(record.experimentId,Vi),40,112,1360,55,30,VlabUi.Secondary);
            int pages=ObservationCards.PageCount(record.readings);readingIndex=Mathf.Clamp(readingIndex,0,pages-1);
            ObservationCards.Render(ui,box,ObservationCards.PresentationReadings(record.readings,record.experimentId),Vi,readingIndex,40,190,1360,400);
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
            var box=Modal(L("Chưa xác nhận lưu ghi chép","Notebook saving is unconfirmed"),1000,460);
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
            Button(box,L("Rời phòng","Leave laboratory"),50,460,460,70,()=>Confirm(L("Lưu điểm chuẩn bị và rời phòng?","Save preparation checkpoint and leave?"),()=>{if(Save())ShowBrowser();else Toast(status);}));
        }
        void Restart(){if(HasPendingMeasurement){Confirm(L("Số đo chưa xác nhận lưu. Bỏ số đo đang giữ và bắt đầu lại?","Saving is unconfirmed. Discard the retained reading and restart?"),()=>{ClearPendingMeasurement();Restart();});return;}ClearPendingMeasurement();Session=null;if(Apparatus)Destroy(Apparatus.gameObject);StartCoroutine(LoadRoom(true,false));}
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
            var button=Button(box,label,x,y,width,height,action);var face=button.GetComponent<UnityEngine.UI.Image>();face.color=VlabUi.Raised;
            var opacity=button.GetComponent<VlabPanelOpacity>();opacity.Solid=true;opacity.FlatAlpha=1;
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
            UiTheme.Select(draft.uiTheme,draft.uiOpacity);NewPage("settings");var box=ui.Panel(page,"Settings",250,110,1100,780);
            var heading=ui.Text(box,L("Cài đặt","Settings"),48,28,540,64,40);heading.name="Settings title";heading.fontStyle=FontStyles.Bold;SettingsInk(heading);
            var draftHint=ui.Text(box,dirty?L("Chưa áp dụng","Not applied"):L("Thiết lập cá nhân","Personal settings"),600,44,240,48,22,VlabUi.Secondary);SettingsInk(draftHint);
            SettingsQuiet(box,L("Quay lại","Back"),872,28,196,64,SettingsBack).name="Settings back";
            string[] tabs=Vi?new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"}:new[]{"General","Audio","Graphics","Controls","Style","Interaction"};
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
                SettingsSection(box,tabs[2],L("FPS/ms đo khung hình, không phải độ trễ chuyển động đầu.","FPS/ms measure frames, not head-tracking latency."));
                string[] details=Vi?new[]{"Ưu tiên tốc độ","Cân bằng hình ảnh","Bóng mềm, viền mượt","Bóng chi tiết hơn"}:new[]{"Prioritize speed","Balanced rendering","Soft shadows, smoother edges","More detailed shadows"};
                for(int i=0;i<4;i++)
                {
                    int n=i;var preset=SettingsQuiet(box,VlabGraphics.Label(i,Vi),48+i%2*514,310+i/2*98,490,84,()=>edit(()=>draft.SetGraphicsTier(n)));preset.name="Graphics tier "+i;
                    var caption=preset.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(64,-3);caption.rectTransform.sizeDelta=new Vector2(414,44);caption.alignment=TextAlignmentOptions.Left;caption.fontSize=ui.LargeText?29.12f:26;caption.lineSpacing=0;
                    var detail=ui.Text(preset.transform,details[i],64,46,414,34,18,VlabUi.Secondary);detail.lineSpacing=0;SettingsInk(detail);
                    var radio=ui.Panel(preset.transform,"Settings preset radio",12,24,36,36,VlabUi.Stroke);SettingsFlat(radio);radio.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                    var centre=ui.Panel(radio,"Settings preset centre",6,6,24,24,draft.GraphicsTier==i?Accent:VlabUi.PanelColor);SettingsFlat(centre);centre.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;preset.targetGraphic=radio.GetComponent<UnityEngine.UI.Image>();if(draft.GraphicsTier==i)ui.Rule(preset.transform,64,79,414,3,Accent);
                }
                SettingsSwitch(box,L("Hiện FPS / thời gian khung hình","Show FPS / frame times"),draft.showPerformance,516,()=>edit(()=>draft.showPerformance=!draft.showPerformance)).name="Graphics performance toggle";
                int recommendation=VlabGraphics.RecommendedTier;
                SettingsQuiet(box,L("Dùng mức gợi ý: ","Use suggested level: ")+VlabGraphics.Label(recommendation,Vi)+L(" · ước tính cấu hình máy"," · hardware estimate"),48,604,1004,48,()=>edit(()=>draft.SetGraphicsTier(recommendation))).name="Graphics recommendation";
            }
            if(settingsTab==3)
            {
                string controllerName=Input.AvailableControllerName;if(controllerName!=null&&controllerName.Length>20)controllerName=controllerName.Substring(0,20)+"…";
                SettingsSection(box,tabs[3],L("Đổi chế độ ngay · ","Mode changes now · ")+(controllerName==null?L("Chưa nhận HID","No HID detected"):"HID: "+controllerName));
                foreach(var text in box.GetComponentsInChildren<TextMeshProUGUI>().Where(text=>text.name=="Settings section title"||text.name=="Settings section hint"))text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,680);
                SettingsQuiet(box,L("Đổi nút tay cầm →","Remap controller →"),752,206,300,72,ShowControllerBindings).name="Controls button mapping";
                bool headset=Input.VrMode||Input.SimulatorMode;
                var phone=SettingsQuiet(box,L("Điện thoại thường","Phone screen"),48,310,502,72,()=>{Session?.Pause();if(Input.SimulatorMode)Input.SetSimulator(false);if(Input.VrMode)Input.SetVr(false);ShowSettings();});phone.name="Controls phone";
                var vr=SettingsQuiet(box,Application.isEditor?L("Mô phỏng kính VR · PC","VR viewer simulation · PC"):L("Kính VR điện thoại","Phone VR viewer"),550,310,502,72,()=>{Session?.Pause();if(Application.isEditor)Input.SetSimulator(!Input.SimulatorMode);else Input.SetVr(!Input.VrMode);ShowSettings();});vr.name=Application.isEditor?"Controls PC simulator":"Controls VR";vr.interactable=Application.isEditor||Input.CanScanViewer;
                ui.Rule(box,48+(headset?502:0),380,502,4,Accent);
                Button(box,L("Camera & di chuyển điện thoại","Phone camera & movement"),48,402,1004,82,()=>ShowPhoneSettings(false)).name="Controls phone settings";
                var help=ui.Text(box,L("Điện thoại: chạm chọn; vuốt nền lab để nhìn.\nKính: nhìn để trỏ; nút kính hoặc giữ nhìn (khi bật tự chọn).\nChọn: ","Phone: tap to select; drag empty lab space to look.\nViewer: gaze; trigger or dwell (when enabled) selects.\nSelect: ")+ControllerBindings.Label(Profile.settings.controllerSelect,Vi)+L(" · Quay lại: "," · Back: ")+ControllerBindings.Label(Profile.settings.controllerBack,Vi)+L("\nMở Gán nút tay cầm để xem các nút và hướng dẫn cần điều khiển.","\nOpen Button mapping for other actions and stick instructions."),48,496,1004,44,22);help.text=L("Gán nút: chạm Đổi nút tay cầm ở trên. Cài đặt được lưu sau khi Áp dụng.","Use Remap controller above. Apply saves your configuration.");SettingsInk(help);
                Button(box,L("Căn lại","Recenter"),48,554,312,72,RecenterHead);
                var qr=Button(box,Input.CanScanViewer?L("Quét QR kính","Scan viewer QR"):L("QR · Android","QR · Android"),740,554,312,72,Input.ScanViewer);qr.name="Controls viewer QR";qr.interactable=Input.CanScanViewer;
                var pair=Button(box,L("Kết nối tay cầm BLE","BLE controllers"),394,554,312,72,()=>ShowDualControllers());pair.name="Controls pair controller";
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
            if(settingsTab<3||settingsTab==4||settingsTab==5)SettingsQuiet(box,L("Mặc định mục này","Category defaults"),288,680,344,72,()=>{var defaults=new GameSettings();if(settingsTab==0){draftLocale="vi";draft.largeText=defaults.largeText;draft.hints=defaults.hints;draft.reducedMotion=defaults.reducedMotion;}if(settingsTab==1){draft.volume=defaults.volume;draft.muted=defaults.muted;}if(settingsTab==2){draft.SetGraphicsTier(defaults.GraphicsTier);draft.showPerformance=defaults.showPerformance;}if(settingsTab==4){draft.uiTheme=defaults.uiTheme;draft.uiOpacity=defaults.uiOpacity;}if(settingsTab==5){draft.gazeAutoSelect=defaults.gazeAutoSelect;draft.gazeDwellSeconds=defaults.gazeDwellSeconds;draft.mouseLookSensitivity=defaults.mouseLookSensitivity;}dirty=true;ShowSettings();});
            box.gameObject.AddComponent<PhoneSettingsLayout>().Initialize(ui,box);
        }
        void SettingsBack()
        {
            if(!dirty){ReturnTo(settingsReturn);return;}var box=Modal(L("Thay đổi chưa áp dụng","Unapplied changes"),1000,430);ui.Text(box,L("Giữ lại, bỏ thay đổi, hoặc áp dụng trước khi quay lại.","Stay here, discard changes, or apply them before returning."),40,120,920,120,26);
            Button(box,L("Ở lại","Stay"),40,320,280,64,CloseModal);Button(box,L("Bỏ thay đổi","Discard"),360,320,280,64,()=>ReturnTo(settingsReturn));Button(box,L("Áp dụng","Apply"),680,320,280,64,ApplyDraft,Accent);
        }
        void ApplyDraft(){var previous=Profile.settings;string oldLocale=Profile.locale;Profile.settings=draft.Copy();Profile.locale=draftLocale;if(Save()){ApplySettings();dirty=false;ReturnTo(settingsReturn);}else{Profile.settings=previous;Profile.locale=oldLocale;StorageToast();}}
        void ApplySettings()
        {
            Performance.Configure(Profile.settings.showPerformance,Profile.locale!="en");
            RoomBranding.SetLanguage(SceneManager.GetActiveScene(),Profile.locale!="en");
            Input.ConfigureControllerBindings(Profile.settings);Input.ConfigurePhoneInput(Profile.settings);Apparatus?.SetCoachingEnabled(Profile.settings.hints);if(!Profile.settings.hints)ClearGuideCue();if(!Input.VrMode&&!Input.SimulatorMode)View.fieldOfView=Profile.settings.cameraFov;
            ui.SetReducedMotion(Profile.settings.reducedMotion);
            foreach(var focus in ui.canvas.GetComponentsInChildren<PortalFocus>(true))focus.ReducedMotion=Profile.settings.reducedMotion;
            AudioListener.volume=Profile.settings.muted?0:Profile.settings.volume;ui.LargeText=Profile.settings.largeText;UiTheme.Select(Profile.settings.uiTheme,Profile.settings.uiOpacity);foreach(var panel in ui.canvas.GetComponentsInChildren<VlabPanelOpacity>(true))panel.Apply(ui.canvas.renderMode==RenderMode.WorldSpace);Preview.ReducedMotion=Profile.settings.reducedMotion;Input.MouseLookSensitivity=Profile.settings.mouseLookSensitivity;
            if(!runtimePipeline)
            {
                var source=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if(source){originalPipeline=QualitySettings.renderPipeline;runtimePipeline=Instantiate(source);runtimePipeline.name="VLAB runtime quality";QualitySettings.renderPipeline=runtimePipeline;}
            }
            if(runtimePipeline&&appliedGraphics!=Profile.settings.GraphicsTier){appliedGraphics=Profile.settings.GraphicsTier;VlabGraphics.Apply(runtimePipeline,appliedGraphics);}Application.targetFrameRate=60;
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
            Button(page,L("Xem lại hướng dẫn nhanh","Replay quick tour"),250,744,1100,72,StartQuickStart).name="Replay quick tour";
            Button(page,L("Gửi phản hồi / báo lỗi","Feedback / report a problem"),250,840,1100,80,ShowFeedback).name="Feedback entry";
            ui.Scroll(page,L("TRỎ VÀ CHỌN\nChọn dụng cụ rồi đặt vào vị trí lắp. Thông số chỉ thay đổi khi đang chuẩn bị; bắt đầu lượt mới để so sánh biến.\n\nTẠM DỪNG VÀ QUAY LẠI\nEsc trên PC hoặc nút Quay lại mở tạm dừng. Hướng dẫn và cài đặt không làm mất số đo đã ghi.\n\nCĂN LẠI\nNhìn về phía trước rồi nhấn R hoặc chọn Căn lại. Giao diện VR đứng trong thế giới, không dán vào mặt.\n\nNGOẠI TUYẾN\nDữ liệu lưu trên thiết bị. Tháo cài đặt hoặc xóa dữ liệu ứng dụng có thể xóa hồ sơ.\n\nLiên hệ: nguamanvlab@gmail.com","POINT AND SELECT\nSelect equipment and place it at its installation anchor. Change controlled variables during preparation; start a new attempt for a comparison.\n\nPAUSE AND RETURN\nEsc on PC or the Back control pauses. Guidance and settings preserve recorded notebook measurements.\n\nRECENTER\nLook forward and press R or choose Recenter. VR panels stay in the world rather than following your face.\n\nOFFLINE\nData is stored on this device. Uninstalling or clearing app data may remove the profile.\n\nSupport: nguamanvlab@gmail.com"),250,170,1100,550,26);
        }
        void ShowAbout()
        {
            NewPage("about");Header(L("Giới thiệu VLAB","About VLAB"),ShowHome);var box=ui.Panel(page,"About",370,140,860,720);Logo(box,235,25,390,124);
            ui.Text(box,L("Phòng thí nghiệm STEM ngoại tuyến\nĐội Ngựa Mán · Trường THPT Chuyên Cao Bằng","Offline STEM laboratory\nĐội Ngựa Mán · Cao Bang High School for the Gifted")+"\n"+Application.version,50,175,760,120,26,null,TextAlignmentOptions.Center);
            Button(box,L("Chính sách bảo mật","Privacy policy"),40,330,370,65,()=>ShowLegal("privacy",false));Button(box,L("Điều khoản","Terms"),450,330,370,65,()=>ShowLegal("terms",false));
            Button(box,L("Phản hồi","Feedback"),40,610,780,68,ShowFeedback);
            Button(box,L("Sổ tay","Notebook"),40,425,370,65,()=>ShowNotebook("about"));Button(box,L("Giấy phép và nguồn","Credits and licenses"),450,425,370,65,()=>{var b=Modal(L("Nguồn và giấy phép","Credits and licenses"),1100,800);var credits=Resources.Load<TextAsset>("Legal/credits");ui.Scroll(b,credits?credits.text:L("Đang tổng hợp attribution.","Attribution is being assembled."),40,110,1020,560);Button(b,L("Quay lại","Back"),40,700,240,64,CloseModal);});
            Button(box,L("Xóa tiến độ","Reset progress"),40,520,370,65,()=>Confirm(L("Xóa các lượt đo đã ghi? Trang ghi chép riêng vẫn được giữ.","Clear recorded attempts? Written notebook pages are kept."),()=>ResetProfile(false)));Button(box,L("Đặt lại ứng dụng","Reset application"),450,520,370,65,()=>Confirm(L("Xóa toàn bộ hồ sơ, ghi chép, cài đặt và chấp thuận?","Clear profile, written notes, settings and acknowledgements?"),()=>ResetProfile(true)));
        }
        void ResetProfile(bool full){if(store.Reset(Profile,full,out var next,out var error)){ClearPendingMeasurement();Profile=next;Session=null;ApplySettings();ShowSetupOrHome();}else{StorageFailure(error,true);StorageToast();}}
        RectTransform Modal(string title,float width,float height,bool live=false)
        {
            Input?.Dual?.CancelInputContext();Apparatus?.CancelHandGrabs();
            CancelFlagshipDrag();
            if(Apparatus&&(Apparatus.IsCarrying||Apparatus.IsPullingBob))Apparatus.CancelGrab();
            var previousLayout=modal?modal.GetComponentInChildren<PhoneDialogLayout>():null;float previousScroll=previousLayout?previousLayout.ScrollPosition:1;string previousTitle=previousLayout?previousLayout.GetComponentInChildren<TextMeshProUGUI>()?.text:null;
            ClearGuideCue();
            bool resume=ActivityRunIntent();ClearInspection();gazeTarget=null;gazeTime=0;gazeConsumed=false;suppressOpeningGaze=Input&&(Input.VrMode||Input.SimulatorMode);
            if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}modal=ui.Page("Modal input blocker");PlaceReadableSurface(modal,true);var blocker=modal.gameObject.AddComponent<UnityEngine.UI.Image>();blocker.color=new Color(0,0,0,.32f);blocker.raycastTarget=true;modal.gameObject.AddComponent<VlabModalBackdrop>().Initialize(ui,blocker);
            modalSession=Session;modalResumeActivity=resume;modalAllowsSimulation=live;if(page)page.gameObject.SetActive(false);
            if(Route=="activity"){if(live&&resume)Session?.Resume();else if(!live)Session?.Pause();}SyncAuthority();
            var box=ui.Panel(modal,"Dialog",(1600-width)/2,(1000-height)/2,width,height);ui.Text(box,title,40,25,width-180,70,32);Button(box,"×",width-108,20,80,72,CloseModal).name="Close dialog";ui.ConfigurePhoneDialog(box);if(previousTitle==title)box.GetComponent<PhoneDialogLayout>().RestoreScroll(previousScroll);inputAfter=Time.unscaledTime+.2f;return box;
        }
        bool ActivityRunIntent()=>modal&&ReferenceEquals(modalSession,Session)?modalResumeActivity:Session!=null&&Session.Phase==ExperimentPhase.Running;
        void CloseModal()
        {
            Input?.Dual?.CancelInputContext();Apparatus?.CancelHandGrabs();
            Input?.Ble?.CancelCalibration();
            if(toolSheetFocusSaved)
            {
                if(ReferenceEquals(toolSheetFocusSession,Session)&&!Input.VrMode&&!Input.SimulatorMode)
                {origin.position=toolSheetReturnPosition;Input.FocusAtWorldTarget(View.transform.position+toolSheetReturnRotation*Vector3.forward);View.fieldOfView=toolSheetReturnFov;}
                toolSheetFocusSaved=false;toolSheetFocusSession=null;
            }
            ClearGuideCue();if(Apparatus&&Apparatus.IsCarrying)Apparatus.CancelGrab();
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
                case "browser":case "about":case "feedback":ShowHome();break;
                case "legal":ShowAbout();break;
                case "notebook":if(SaveNotebookNotes())ReturnTo(notebookReturn);break;
                case "notebook-analysis":ShowNotebook(notebookReturn);break;
                case "help":ReturnTo(supportReturn);break;
                case "results":ShowBrowser();break;
            }
        }
        void OnModeChanged(bool vr,string message)
        {
            if(modeLabel&&!roundModeLabel)modeLabel.GetComponentInParent<UnityEngine.UI.Button>(true).gameObject.SetActive(vr||Input.SimulatorMode);
            if(modeLabel)modeLabel.text=roundModeLabel?(Input.VrMode||Input.SimulatorMode?L("Thoát VR","Exit VR"):"VR"):L("Thoát VR","Exit VR");var homeHint=page?page.Find("Home selection hint")?.GetComponent<TMP_Text>():null;if(homeHint)homeHint.text=vr||Input.SimulatorMode?L("Nhìn hoặc trỏ để chọn","Look or point to select"):L("Chạm vào phòng để chọn","Tap a laboratory to select");
            bool enteringWorld=ui.canvas.renderMode!=RenderMode.WorldSpace&&(vr||Input.SimulatorMode);
            bool leavingWorld=ui.canvas.renderMode==RenderMode.WorldSpace&&!vr&&!Input.SimulatorMode;
            Session?.Pause();modalResumeActivity=activityReturnRunning=false;CancelFlagshipDrag();Apparatus?.CancelGrab();if(enteringWorld&&Apparatus&&(Route=="activity"||Route=="pause"||Route=="settings"))MoveToWorkstation();ui.SetWorld(vr||Input.SimulatorMode,View);if(!vr&&!Input.SimulatorMode)View.fieldOfView=Profile.settings.cameraFov;PlaceReadableSurface(page,Route!="explore");PlaceReadableSurface(modal,true);if(enteringWorld)StartCoroutine(AlignAfterTracking());SyncAuthority();
            var desktopModule=GetComponentInChildren<InputSystemUIInputModule>();if(desktopModule){desktopModule.enabled=!vr&&!Input.SimulatorMode;if(desktopModule.enabled){desktopModule.move?.action?.Disable();desktopModule.submit?.action?.Disable();desktopModule.cancel?.action?.Disable();}}
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
            if(message==VlabInput.MissingVrGyroscope)message=L("Điện thoại không báo có cảm biến con quay. Chưa thể dùng VR theo hướng đầu; hãy dùng chế độ điện thoại và vuốt để nhìn.",message);
            else if(message=="Cardboard stereo is available on Android. Use PC simulation here.")message=L("Kính Cardboard dùng trên Android. Trên máy tính, hãy chọn mô phỏng VR.",message);
            if(!string.IsNullOrEmpty(message))Toast(message);
            if((enteringWorld||leavingWorld)&&Route=="activity"&&!modal)ShowActivity();
        }
        void Update()
        {
            if(!Input||!View||Profile==null)return;Input.SetUiBlocked(modal||changing||(Route!="activity"&&Route!="explore"));var frame=Input.Read();
            if(frame.pointerChanged){gazeTarget=null;gazeTime=0;gazeConsumed=true;CancelFlagshipDrag();Apparatus?.CancelGrab();}
            UpdateDualHands(ref frame);MovePhone(frame.move);UpdateQuickStart();
            if(frame.back&&Time.unscaledTime>=inputAfter)ControllerBack();
            else if(frame.menu&&Route=="loading"&&Time.unscaledTime>=inputAfter)CancelRoomEntry();
            else if(frame.menu&&Route=="load-error")ShowBrowser();
            else if(frame.menu&&Route!="language"&&Route!="privacy"&&Route!="terms"&&Route!="safety"&&Route!="loading"){if(modal)CloseModal();else if(Route=="activity"||Route=="explore")ShowPause();else if((Route!="notebook"&&Route!="notebook-analysis")||SaveNotebookNotes())ShowHome();}
            if(frame.recenter)RecenterHead();if(frame.lost){Session?.InputLost();modalResumeActivity=activityReturnRunning=false;CancelFlagshipDrag();Apparatus?.CancelGrab();}
            SyncAuthority();
            if(Session!=null&&Route=="activity"&&(!modal||modalAllowsSimulation))Session.Advance(Time.unscaledDeltaTime);
            if(Session!=null&&Route=="activity"&&Time.unscaledTime>=nextDisplay){nextDisplay=Time.unscaledTime+.1f;RefreshActivity();RefreshInstrument();RefreshWorkbench();}
            if(Input.ControllerPointerActive&&!frame.lost)frame=ProcessGaze(frame);
            else if(frame.lost){gazeTime=0;gazeConsumed=true;}
            if((Route=="activity"||Route=="explore")&&!modal&&Time.unscaledTime>=inputAfter)
            {
                if((frame.pressed||frame.grabPressed)&&!PointerHitsUi(frame.pointer)&&Physics.Raycast(frame.pointer,out var hit,3.5f))
                {
                    var equipment=hit.collider.GetComponentInParent<EquipmentHandle>();
                    if(equipment&&Apparatus)
                    {
                        bool free=!Apparatus.IsCarrying&&!Apparatus.IsPullingBob&&!flagshipDrag;
                        if(free)
                        {
                            if(equipment.installed&&BeginFlagship(equipment,hit,frame)){}else if(equipment.installed&&Apparatus.Driver!=null){SelectWorkbenchTool(equipment);}
                            else if((frame.phoneWorldPress||!frame.held)&&!equipment.installed){Apparatus.Select(equipment);if(!Apparatus.TakeSelected(frame.pointer,out var why))Toast(LessonGuide.ExplainFailure(why,Vi));}
                            else Apparatus.BeginGrab(equipment,frame.pointer,!frame.held);
                            if(Session.spec.id=="P01"&&Profile.settings.hints&&!modal&&guideMistakes<3)PointAtEquipment(equipment);
                        }
                    }
                    else{var fixture=hit.collider.GetComponentInParent<LabFixture>();if(fixture&&fixture.CanInteract)ActivateFixture(fixture);}
                }
                if(!PointerHitsUi(frame.pointer))
                {lastInteractionRay=frame.pointer;UpdateFlagshipDrag(frame);if(Apparatus)Apparatus.UpdateGrab(frame);}
                else if(frame.released){CancelFlagshipDrag();if(Apparatus&&!Apparatus.HasLatchedCarry)Apparatus.CancelGrab();}
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
                // Live apparatus blocks world UI; the room photograph replaces hidden room geometry.
                blocked=!Preview.ReplacesScene&&Physics.Raycast(ray,Mathf.Max(0,distance-.015f),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            }
            Vector3 screen=View.WorldToScreenPoint(point);pointer=new PointerEventData(EventSystem.current){position=new Vector2(screen.x,screen.y)};
            if(screen.z>0&&!blocked)EventSystem.current.RaycastAll(pointer,uiHits);
        }
        ActionFrame ProcessGaze(ActionFrame frame)
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
                if(!Preview.ReplacesScene&&Physics.Raycast(frame.pointer,out var hit,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))distance=Mathf.Max(.08f,hit.distance-.012f);
                pointerRoot.position=frame.pointer.GetPoint(distance);pointerRoot.rotation=Quaternion.LookRotation(View.transform.forward,View.transform.up);
                // Keep the pointer about 0.7 degrees wide instead of shrinking with distance.
                pointerRoot.localScale=Vector3.one*(distance*.000267f);
            }
            var target=uiHits.Count>0?ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiHits[0].gameObject):null;
            if(target){var button=target.GetComponent<UnityEngine.UI.Button>();if(button&&!button.IsInteractable())target=null;}
            bool worldTarget=false;
            if(uiHits.Count==0&&!modal&&!changing&&(Route=="activity"||Route=="explore")&&(!Apparatus||(!Apparatus.IsCarrying&&!Apparatus.IsPullingBob))&&!flagshipDrag&&Physics.Raycast(frame.pointer,out var worldHit,3.5f))
            {var item=worldHit.collider.GetComponentInParent<EquipmentHandle>();if(item||(worldHit.collider.GetComponentInParent<LabFixture>() is LabFixture fixture&&fixture.CanInteract)){target=worldHit.collider.gameObject;worldTarget=true;}}
            if(target!=gazeTarget){if(gazeTarget)ExecuteEvents.Execute(gazeTarget,pointer,ExecuteEvents.pointerExitHandler);gazeTarget=target;gazeTime=0;gazeConsumed=false;gazePosition=pointer.position;if(target)ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerEnterHandler);}
            else if(target&&(target.GetComponent<InstrumentInspection>()||target.GetComponent<VlabWhiteboard>())&&Vector2.Distance(gazePosition,pointer.position)>12){gazePosition=pointer.position;gazeTime=0;gazeConsumed=false;}
            // A replacement page must not auto-activate the button that appears under the same gaze.
            if(suppressOpeningGaze){gazeConsumed=target;suppressOpeningGaze=false;}
            bool dwell=(Input.VrMode||Input.SimulatorMode)&&Profile.settings.gazeAutoSelect;
            bool ready=target&&Time.unscaledTime>=inputAfter;
            bool click=AdvanceGazeTimer(ref gazeTime,ref gazeConsumed,dwell&&ready,ready&&frame.pressed,Profile.settings.gazeDwellSeconds,Time.unscaledDeltaTime);
            float progress=dwell&&ready?Mathf.Clamp01(gazeTime/Profile.settings.gazeDwellSeconds):0;
            if(gazeProgress)gazeProgress.fillAmount=progress;
            if(flatGazeProgress)flatGazeProgress.fillAmount=progress;
            if(click){if(worldTarget)frame.pressed=true;else ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);}
            return frame;
        }
        internal static bool AdvanceGazeTimer(ref float elapsed,ref bool consumed,bool dwell,bool pressed,float duration,float deltaSeconds)
        {
            elapsed=dwell?elapsed+Mathf.Max(0,deltaSeconds):0;
            if(!pressed&&(!dwell||consumed||elapsed<duration))return false;
            consumed=true;return true;
        }

        bool PhoneUiOwnsTouch(Vector2 position)
        {
            if(!ui?.canvas||!EventSystem.current)return false;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            return hits.Any(hit=>hit.module is UnityEngine.UI.GraphicRaycaster);
        }
        void MovePhone(Vector2 move)
        {
            if(move.sqrMagnitude<.0001f||modal||changing||(Route!="activity"&&Route!="explore")||((Input.VrMode||Input.SimulatorMode)&&!Input.UsingDual))return;
            var delta=PhoneLocomotion.Direction(View.transform.rotation,move)*Profile.settings.moveSpeed*Mathf.Min(Time.unscaledDeltaTime,.05f);
            origin.position=PhoneLocomotion.Move(origin,delta,out _);
        }
        string FlagshipInstruction()
        {
            string id=Session.spec.id;
            if(id=="P01")return Session.Phase==ExperimentPhase.Preparing?(Session.Operation==0?L("Chạm giá đỡ để kiểm tra giá và kẹp.","Touch the stand to check its support and clamp."):Session.Operation==1?L("Chạm quả nặng để kiểm tra dây nối tới tâm quả.","Touch the bob to check the pivot-to-centre suspension."):Session.Operation==2?L("Chạm quả ở vị trí cân bằng để đặt mốc đo.","Touch the bob at equilibrium to set the timing mark."):L("Giữ quả nặng, kéo sang bên rồi thả tay.","Hold the bob, pull it aside, then release.")):Session.Phase==ExperimentPhase.Running?L("Đếm đủ chu kỳ cùng chiều qua mốc cân bằng rồi ghi số đo.","Count full same-direction cycles, then record."):L("Giữ quả nặng, kéo sang bên rồi thả tay.","Hold the bob, pull it aside, then release.");
            if(!Apparatus.Ready)return L("Chọn và lắp dụng cụ. Có thể tự lấy trên bàn hoặc mở danh sách.","Select and install the apparatus, directly or from the equipment list.");
            if(id=="C04")return Session.Phase==ExperimentPhase.Preparing?L("Chạm chai AgNO₃ để chọn thuốc thử. Giữ ống nhỏ giọt, kéo và thả trên cốc. So sánh với hai mẫu đối chứng.","Select AgNO3. Hold the dropper, drag it and release over a cup. Compare both controls."):L("Quan sát cặn trong ba cốc. Mở cốc để đọc kết quả; Ghi số đo lưu lượt của bạn.","Observe all three cups. Select a cup to inspect; Record saves your own trial.");
            if(id=="B01")return Session.Phase==ExperimentPhase.Preparing?(Session.Operation==0?L("Chạm mẫu hành để lấy lớp biểu bì mỏng trên lam có nước.","Touch the onion to prepare a thin wet epidermis layer."):Session.Operation==1?L("Chạm lamen để hạ nghiêng lên mẫu và thấm phần dư.","Touch the coverslip to lower it at an angle and blot excess liquid."):Session.Operation==2?L("Chạm lam kính để đặt vào bàn kính và đóng hai kẹp.","Touch the slide to mount it and close both stage clips."):L("Giữ ốc lấy nét, kéo lên/xuống. Chạm thị kính để nhìn mẫu.","Drag a focus knob up/down. Select the eyepiece to observe.")):L("Qua thị kính, chạm vùng quan sát rồi tự chọn tên cấu trúc. Chỉnh nét trước khi đổi vật kính.","In the eyepiece, select a region and identify its structure. Focus before changing objectives.");
            return Session.Phase==ExperimentPhase.Preparing||Session.Phase==ExperimentPhase.Ready?L("Chạm bánh 12/24 răng để chọn đầu vào, bánh 36 để chọn đầu ra. Chạm motor để chạy; chọn tốc kế để đọc.","Select the 12/24-tooth input or 36-tooth output gear. Touch the motor to run; inspect the tachometer."):L("Quan sát chiều quay và tốc kế. Chạm motor để tạm dừng/tiếp tục. Ghi lượt rồi làm lại để đổi bộ răng.","Observe direction and speed. Touch the motor to pause/resume. Record, then repeat to change the gears.");
        }
        string FlagshipPurpose(EquipmentHandle item)
        {
            string key=item.assetId;
            if(Session.spec.id=="C04")return key=="reagent"?L("AgNO₃ là thuốc thử. Mỗi lần chọn dùng cho một lần chuyển.","AgNO3 is the reagent. Select it again before each transfer."):key=="dropper"?L("Giữ để cầm; kéo đầu ống tới miệng cốc rồi thả.","Hold to pick up; drag the tip over a cup, then release."):L("Mẫu cần quan sát; so hiện tượng với các cốc đối chứng.","Observe this sample and compare the control cups.");
            if(Session.spec.id=="B01")return key=="microscope"?L("Giữ ốc để chỉnh nét; chạm thị kính để nhìn. Chỉ đổi vật kính sau khi đã rõ.","Drag a focus knob; select the eyepiece to look. Focus before increasing magnification."):key=="onion"?L("Mẫu hành cung cấp lớp biểu bì mỏng để ánh sáng đi qua.","Onion provides a thin epidermis layer for transmitted light."):key=="coverslip"?L("Lamen giữ mẫu phẳng; hạ nghiêng để hạn chế bọt khí.","A coverslip flattens the sample; lower it at an angle to limit air bubbles."):L("Lam kính đỡ mẫu và được cố định trên bàn kính.","The slide supports the specimen and is secured to the stage.");
            if(Session.spec.id=="M01")return key=="motor"?L("Chạm để chạy, tạm dừng hoặc tiếp tục; không đổi bộ răng trong lượt đang đo.","Touch to run, pause or resume; do not change the gears during a trial."):key=="gears"?L("Các răng ăn khớp truyền chuyển động. Chọn trực tiếp bánh 12/24/36 trên mô hình.","Meshing teeth transmit motion. Select the actual 12/24/36-tooth gear."):key=="tach"?L("Tốc kế đọc tốc độ từ góc quay của trục đã quan sát.","The tachometer reads speed from the observed shaft rotation."):L("Bảng lắp giữ các trục đúng vị trí và khoảng tâm.","The mounting board holds shaft positions and centre distances.");
            return "";
        }
        static bool Flagship(string id)=>id=="P01"||id=="C04"||id=="B01"||id=="M01";
        EquipmentHandle flagshipDrag;Vector3 flagshipRest;Quaternion flagshipRotation;Plane flagshipPlane;Vector3 flagshipLast;float flagshipDistance;bool flagshipFine;Ray lastInteractionRay;
        void CancelFlagshipDrag(){if(flagshipDrag&&Apparatus&&Apparatus.EndFlagshipGrab(flagshipDrag)&&(Session?.spec.id=="C04"||Session?.spec.id=="C06"))flagshipDrag.transform.SetPositionAndRotation(flagshipRest,flagshipRotation);flagshipDrag=null;}
        bool FlagshipOperate(string action)
        {
            if(!Apparatus.Driver.Operate(action,out var why)){ActionFailure(why);return false;}
            Guide().Applied(action);VlabAudio.Action(action,flagshipDrag?flagshipDrag.transform.position:Apparatus.Worktop);Save();RefreshActivity();return true;
        }
        bool StartObservedFlagship()
        {
            for(int n=0;n<Session.spec.operationsEn.Length&&Session.Phase==ExperimentPhase.Preparing;n++)if(!Apparatus.PrepareNext(out var why)){ActionFailure(why);return false;}
            if(!Session.Start(out var error)){ActionFailure(error);return false;}VlabAudio.Play(VlabSound.Experiment);Save();RefreshActivity();return true;
        }
        bool BeginMicroscopeObservation()
        {
            bool Reject(string message){if(modal&&inspectionStatus)inspectionStatus.text=message;else Toast(message);return false;}
            if(Session.Phase==ExperimentPhase.Preparing)
            {
                if(Session.Operation!=3)return Reject(L("Chuẩn bị và đặt lam lên kính trước khi quan sát.","Prepare and mount the slide before observing."));
                if(!Apparatus.PrepareNext(out var why))return Reject(LessonGuide.ExplainFailure(why,Vi));
            }
            if(Session.Phase==ExperimentPhase.Ready&&!Session.Start(out var error))return Reject(LessonGuide.ExplainFailure(error,Vi));
            Save();RefreshActivity();return true;
        }
        bool BeginFlagship(EquipmentHandle item,RaycastHit hit,ActionFrame frame)
        {
            if(!item||item.Owner!=EquipmentGrabOwner.None)return false;
            string id=Session.spec.id,key=item.assetId,part=hit.collider.name;
            if(!Flagship(id))return BeginWorkbenchDrag(item,hit,frame);
            if(!Apparatus.Ready){ActionFailure(L("Lắp đủ dụng cụ trước khi thao tác.","Install the required apparatus first."));return true;}
            if(id=="P01")
            {
                if(Session.Phase!=ExperimentPhase.Preparing||Session.Operation>=3)return false;
                bool expected=Session.Operation==0&&(key=="stand"||key=="clamp")||Session.Operation==1&&(key=="string"||key=="pendulum")||Session.Operation==2&&(key=="pendulum"||key=="timer"||key=="ruler");
                if(!expected)ActionFailure(L("Kiểm tra giá, dây/quả rồi mốc cân bằng theo thứ tự.","Check the support, string/bob, then the equilibrium timing mark in order."));
                else if(!Apparatus.PrepareNext(out var why))ActionFailure(why);else{Save();RefreshActivity();}return true;
            }
            if(id=="C04")
            {
                if(key=="reagent"){if(Session.Operation==0&&!Apparatus.PrepareNext(out var why)){ActionFailure(why);return true;}FlagshipOperate("selectAg");return true;}
                if(key=="dropper")
                {if(!Apparatus.BeginFlagshipGrab(item))return false;flagshipDrag=item;flagshipRest=item.transform.position;flagshipRotation=item.transform.rotation;flagshipDistance=Mathf.Clamp(hit.distance,.3f,3f);return true;}
                if(key=="nacl"||key=="nitrate"||key=="water"){if(Session.Phase==ExperimentPhase.Running||Session.Phase==ExperimentPhase.Measured)return false;ActionFailure(L("Chạm chai AgNO₃, rồi giữ ống nhỏ giọt và kéo thả trên cốc cần thử.","Select the AgNO3 bottle, then drag and release the dropper over a sample cup."));return true;}
            }
            if(id=="B01")
            {
                if(key=="onion"||key=="coverslip"||key=="slide")
                {
                    string expected=Session.Operation==0?"onion":Session.Operation==1?"coverslip":Session.Operation==2?"slide":"";
                    if(key!=expected){ActionFailure(L("Làm theo thứ tự: lớp biểu bì → lamen → đặt lam lên kính.","Use this order: epidermis layer, coverslip, then mount the slide."));return true;}
                    if(!Apparatus.PrepareNext(out var why))ActionFailure(why);else{Save();RefreshActivity();}return true;
                }
                if(key=="microscope"&&(part.Contains("Fine")||part.Contains("Coarse")))
                {if(!Apparatus.BeginFlagshipGrab(item))return false;flagshipDrag=item;flagshipFine=part.Contains("Fine");flagshipPlane=new Plane(View.transform.forward,hit.point);flagshipLast=hit.point;return true;}
                if(key=="microscope"&&part.Contains("Eyepiece"))
                {ShowInstrument(item);if(BeginMicroscopeObservation()){inspectionTab="view";ShowInstrument(item);}return true;}
            }
            if(id=="M01")
            {
                if(key=="gears")
                {
                    string parameter=part.Contains("36")?"driven":"driver";double teeth=part.Contains("36")?36:part.Contains("24")?24:12;
                    if(!Session.SetParameter(parameter,teeth,out var why))ActionFailure(why);else{Apparatus.RefreshConfiguration();Save();RefreshActivity();PointAtEquipment(item);}return true;
                }
                if(key=="motor")
                {if(Session.Phase==ExperimentPhase.Running){Session.Pause();RefreshActivity();}else if(Session.Phase==ExperimentPhase.Paused){Session.Resume();RefreshActivity();}else StartObservedFlagship();return true;}
            }
            return false;
        }
        EquipmentHandle FindSampleCup(Ray ray)
        {
            var hits=Physics.RaycastAll(ray,3.5f).OrderBy(h=>h.distance).Where(h=>!flagshipDrag||h.collider.GetComponentInParent<EquipmentHandle>()!=flagshipDrag).ToArray();
            var first=hits.Length>0?hits[0].collider.GetComponentInParent<EquipmentHandle>():null;
            bool Sample(EquipmentHandle item)=>item&&(item.assetId=="nacl"||item.assetId=="nitrate"||item.assetId=="water");
            if(Sample(first))return first;
            EquipmentHandle chosen=null;float nearest=3.5f;
            foreach(var item in Apparatus.items.Where(Sample))
            {
                if(!ApparatusRig.TryVisibleBounds(item,out var bounds))continue;
                var mouth=new Plane(Vector3.up,new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
                if(!mouth.Raycast(ray,out float distance)||distance>nearest)continue;var point=ray.GetPoint(distance)-bounds.center;
                float radius=Mathf.Min(bounds.extents.x,bounds.extents.z)+.004f;
                if(point.x*point.x+point.z*point.z>radius*radius||hits.Any(h=>h.distance<distance-.01f&&h.collider.GetComponentInParent<EquipmentHandle>()!=item))continue;
                chosen=item;nearest=distance;
            }
            return chosen;
        }
        void UpdateFlagshipDrag(ActionFrame frame)
        {
            if(!flagshipDrag)return;
            if(flagshipDrag.Owner!=EquipmentGrabOwner.Flagship){flagshipDrag=null;return;}
            if(frame.lost||frame.back||frame.menu){CancelFlagshipDrag();return;}
            if(UpdateWorkbenchDrag(frame))return;
            if(Session.spec.id=="C04")
            {
                flagshipDrag.transform.position=frame.pointer.GetPoint(flagshipDistance)+Vector3.up*.06f;
                if(frame.released)
                {
                    var cup=FindSampleCup(frame.pointer);
                    bool dosed=!PointerHitsUi(frame.pointer)&&cup&&FlagshipOperate("dose:"+cup.assetId);CancelFlagshipDrag();
                    if(!cup)ActionFailure(L("Thả đầu nhỏ giọt trên miệng một cốc mẫu.","Release the dropper over a sample cup."));
                    if(dosed){if(Session.Phase==ExperimentPhase.Preparing)Apparatus.PrepareNext(out _);if(Session.Phase==ExperimentPhase.Preparing)Apparatus.PrepareNext(out _);if(Session.Phase==ExperimentPhase.Ready)Session.Start(out _);Save();RefreshActivity();}
                }
            }
            else if(Session.spec.id=="B01")
            {
                if(flagshipPlane.Raycast(frame.pointer,out float d)){var point=frame.pointer.GetPoint(d);float delta=Vector3.Dot(point-flagshipLast,View.transform.up);if(Mathf.Abs(delta)>.012f){FlagshipOperate(flagshipFine?(delta>0?"focusFine+":"focusFine-"):(delta>0?"focus+":"focus-"));flagshipLast=point;}}
                if(frame.released)CancelFlagshipDrag();
            }
        }
        void ConfigureFlagshipTargets()
        {
            if(!Flagship(Session.spec.id))return;
            if(Session.spec.id=="B01"){var sheet=Apparatus.items.FirstOrDefault(x=>x.assetId=="specimen");if(sheet)foreach(var renderer in sheet.GetComponentsInChildren<Renderer>())renderer.forceRenderingOff=true;}
            if(Session.spec.id=="M01"){var motor=Apparatus.items.First(x=>x.assetId=="motor");if(ApparatusRig.TryVisibleBounds(motor,out var b)){var hit=motor.gameObject.AddComponent<SphereCollider>();hit.isTrigger=true;hit.center=motor.transform.InverseTransformPoint(b.center+Vector3.up*.025f);hit.radius=.035f;}}
            if(Session.spec.id=="M01")foreach(var renderer in Apparatus.items.First(x=>x.assetId=="gears").GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.EndsWith("_VIS",StringComparison.Ordinal)&&(r.name.Contains("Gear12")||r.name.Contains("Gear24")||r.name.Contains("Gear36"))))
            {var hit=renderer.gameObject.AddComponent<SphereCollider>();hit.isTrigger=true;hit.center=renderer.transform.InverseTransformPoint(renderer.bounds.center);hit.radius=Mathf.Max(renderer.bounds.extents.x,renderer.bounds.extents.y,renderer.bounds.extents.z)*.75f;}
            foreach(var item in Apparatus.items)foreach(var renderer in item.GetComponentsInChildren<MeshRenderer>())
                if(Session.spec.id=="C04"&&item.assetId=="dropper"||Session.spec.id=="B01"&&item.assetId=="microscope"&&(renderer.name.Contains("Fine")||renderer.name.Contains("Coarse")))
                {var hit=renderer.gameObject.AddComponent<SphereCollider>();hit.isTrigger=true;hit.center=renderer.transform.InverseTransformPoint(renderer.bounds.center);hit.radius=.022f;}
        }
        void ActivateFixture(LabFixture fixture)
        {
            if(fixture.purpose!=LabFixture.Purpose.Exit){fixture.Activate();return;}
            Confirm(L("Rời phòng thí nghiệm? Tiến độ đã lưu sẽ được giữ lại.","Leave the laboratory? Your saved progress will be kept."),()=>{if(Save())ShowBrowser();else Toast(status);});
        }
        void ZoomPhone(float amount)
        {
            if(Input.VrMode||Input.SimulatorMode||flagshipDrag||Apparatus?.IsPullingBob==true)return;
            float requested=Mathf.Clamp(View.fieldOfView+amount,20,90);
            if(Profile.settings.focusAssist&&Apparatus&&!Apparatus.IsCarrying)
            {
                var item=Apparatus.Selected;
                if(!item){string key=CurrentGuide()?.key;item=Apparatus.items.FirstOrDefault(part=>part.assetId==key&&part.gameObject.activeInHierarchy);if(!item&&Flagship(Session.spec.id))item=Apparatus.items.FirstOrDefault(part=>part.installed&&part.gameObject.activeInHierarchy&&part.assetId!="specimen"&&!part.assetId.StartsWith("solid",StringComparison.Ordinal));}
                if(item&&TryFocusBounds(item,out var bounds)){if(amount<0)ApparatusRig.TryVisibleBounds(item,out bounds);FitFocusBounds(bounds,ActivityObservationArea());float fitted=View.fieldOfView;View.fieldOfView=Mathf.Clamp(requested,fitted,90);return;}else FocusStation();
            }
            View.fieldOfView=requested;
        }
        bool TryFocusBounds(EquipmentHandle item,out Bounds bounds,bool includeSetup=true)
        {
            if(!ApparatusRig.TryVisibleBounds(item,out bounds))return false;
            if(includeSetup&&Session?.spec.id=="C04"&&Session.Phase==ExperimentPhase.Running)
            {
                bool found=false;
                foreach(var sample in Apparatus.items.Where(x=>x.assetId=="nacl"||x.assetId=="nitrate"||x.assetId=="water"))
                    if(ApparatusRig.TryVisibleBounds(sample,out var sampleBounds)){if(found)bounds.Encapsulate(sampleBounds);else{bounds=sampleBounds;found=true;}}
                return found;
            }
            if(Session?.spec.id=="B01"&&Session.Operation>=3){var microscope=Apparatus.items.First(x=>x.assetId=="microscope");if(ApparatusRig.TryVisibleBounds(microscope,out bounds))bounds.Expand(.04f);return true;}
            if(Session?.spec.id=="M01"){var gears=Apparatus.items.FirstOrDefault(x=>x.assetId=="gears");if(gears&&ApparatusRig.TryVisibleBounds(gears,out bounds)){var motor=Apparatus.items.FirstOrDefault(x=>x.assetId=="motor");if(motor&&ApparatusRig.TryVisibleBounds(motor,out var drive))bounds.Encapsulate(drive);bounds.Encapsulate(new Vector3(bounds.center.x,Apparatus.Worktop.y,bounds.center.z));bounds.Expand(.035f);}return true;}
            if(includeSetup&&Session!=null&&Session.spec.id!="P01"&&Flagship(Session.spec.id))foreach(var part in Apparatus.items.Where(x=>x.installed&&x.gameObject.activeInHierarchy&&x.assetId!="specimen"&&!x.assetId.StartsWith("solid",StringComparison.Ordinal)&&!(Session.spec.id=="B01"&&Session.Operation<2&&x.assetId=="microscope")))if(ApparatusRig.TryVisibleBounds(part,out var whole))bounds.Encapsulate(whole);
            if(Session?.spec.id=="P01"&&item.installed&&item.assetId=="pendulum")
            {
                float length=(float)Session.parameters["length"],angle=(float)Math.Max(15,Session.parameters["angle"])*Mathf.Deg2Rad;
                if(Session.Operation<2)bounds.Encapsulate(Apparatus.Pivot);
                else
                {
                    var rest=Apparatus.Pivot+Vector3.down*length;var arc=Apparatus.Pivot+Vector3.down*(Mathf.Cos(angle)*length);
                    bounds.Encapsulate(rest);bounds.Encapsulate(arc+Vector3.right*(Mathf.Sin(angle)*length));bounds.Encapsulate(arc-Vector3.right*(Mathf.Sin(angle)*length));
                }
                bounds.Expand(.06f);
                if(includeSetup&&Session.Phase==ExperimentPhase.Running)
                {
                    var timer=Apparatus.items.FirstOrDefault(x=>x.assetId=="timer"&&x.installed);
                    if(timer&&ApparatusRig.TryVisibleBounds(timer,out var clock))bounds.Encapsulate(clock);
                }
            }
            // Friction is observed as a connected rig: keep the block and its travel surface beside the force scale.
            if(Session?.spec.id=="P03")
                foreach(var linked in Apparatus.items)
                    if(linked.installed&&linked.gameObject.activeInHierarchy&&(linked.assetId=="surface"||linked.assetId=="block"||linked.assetId=="meter"||linked.assetId=="cord")&&ApparatusRig.TryVisibleBounds(linked,out var part))bounds.Encapsulate(part);
            return true;
        }
        Rect ActivityObservationArea()
        {
            // Match the clear center between the guide and rail in actual safe-area pixels.
            ui.PhoneViewport.Refresh();var content=ui.PhoneViewport.ContentPixels;
            return new Rect((content.x+content.width*.30f)/Screen.width,(content.y+content.height*.18f)/Screen.height,content.width*.47f/Screen.width,content.height*.64f/Screen.height);
        }
        void FocusInstrument(EquipmentHandle item,bool besideControls)
        {
            if(!Profile.settings.focusAssist||Input.VrMode||Input.SimulatorMode||!item||!TryFocusBounds(item,out var bounds,false))return;
            FitFocusBounds(bounds,besideControls?new Rect(.03f,.06f,.46f,.88f):new Rect(.04f,.06f,.92f,.88f));
        }
        void FocusInventoryItem(EquipmentHandle item)
        {
            if(!Profile.settings.focusAssist||Input.VrMode||Input.SimulatorMode||Screen.width<=Screen.height||!item||!item.gameObject.activeInHierarchy||!TryFocusBounds(item,out var bounds,false))return;
            FitFocusBounds(bounds,new Rect(.60f,.10f,.36f,.78f));
        }
        void FitFocusBounds(Bounds bounds,Rect area)
        {
            if(flagshipDrag||Input.VrMode||Input.SimulatorMode||Apparatus?.IsCarrying==true||Apparatus?.IsPullingBob==true)return;
            // Use a real front-of-bench viewpoint before adjusting the lens; never move a tracked head.
            float fov=Mathf.Clamp(Profile.settings.cameraFov,40,80),tangentFit=Mathf.Tan(fov*.5f*Mathf.Deg2Rad);
            float distance=Mathf.Clamp(Mathf.Max(bounds.size.y/area.height,bounds.size.x/(Mathf.Max(.5f,View.aspect)*area.width))/(2*tangentFit)+bounds.extents.z+.22f,.45f,2.6f);
            var eye=new Vector3(bounds.center.x,Mathf.Max(Apparatus?Apparatus.Worktop.y+.22f:bounds.center.y,bounds.center.y+.22f),Mathf.Min(bounds.min.z-.25f,bounds.center.z-distance));
            if(!Physics.CheckSphere(eye,.08f,~0,QueryTriggerInteraction.Ignore))origin.position=eye;
            Input.FocusAtWorldTarget(bounds.center);
            View.fieldOfView=Mathf.Clamp(Profile.settings.cameraFov,20,90);
            // Fit projected corners after each aim change: world-axis extents underfit diagonal apparatus.
            for(int pass=0;pass<48;pass++)
            {
                Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
                for(int corner=0;corner<8;corner++)
                {
                    var world=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var v=View.WorldToViewportPoint(world);min=Vector2.Min(min,v);max=Vector2.Max(max,v);
                }
                var center=(min+max)*.5f;var delta=(center-area.center)*.45f;
                float depth=Mathf.Max(.15f,Vector3.Dot(bounds.center-View.transform.position,View.transform.forward));
                float tangent=Mathf.Tan(View.fieldOfView*.5f*Mathf.Deg2Rad);
                var aim=View.transform.position+View.transform.forward*depth+View.transform.right*(delta.x*2*depth*tangent*View.aspect)+View.transform.up*(delta.y*2*depth*tangent);
                Input.FocusAtWorldTarget(aim);
                float ratio=Mathf.Max((max.x-min.x)/area.width,(max.y-min.y)/area.height);
                float target=Mathf.Clamp(2*Mathf.Atan(tangent*Mathf.Max(.25f,ratio)*1.015f)*Mathf.Rad2Deg,20,90);
                View.fieldOfView=Mathf.Lerp(View.fieldOfView,target,.2f);
                if(delta.sqrMagnitude<.000001f&&ratio>.96f&&ratio<1)break;
            }
        }
        string lastFocusedTaskTarget;
        void FocusStepTarget(GuideTask task)
        {
            bool comparing=Session?.spec.id=="C04"&&Session.Phase==ExperimentPhase.Running;
            if(Input.PhoneGestureActive||flagshipDrag||(task==null&&!comparing)||Apparatus==null||!Apparatus.Ready||Apparatus.IsCarrying||Apparatus.IsPullingBob||modal||changing||Session.spec.id=="P02"||
                Input.VrMode||Input.SimulatorMode||!Profile.settings.focusAssist)return;
            string key=comparing?"nacl":Session.spec.id=="P01"&&Session.Phase==ExperimentPhase.Running?"pendulum":task.key;
            string focus=key+"|"+Session.Operation+"|"+Session.Phase;
            if(string.IsNullOrEmpty(key)||focus==lastFocusedTaskTarget)return;
            var item=Apparatus.items.FirstOrDefault(x=>x.assetId==key&&x.installed&&x.gameObject.activeInHierarchy);
            if(!item||!TryFocusBounds(item,out var bounds))return;
            lastFocusedTaskTarget=focus;
            FitFocusBounds(bounds,ActivityObservationArea());
        }
        int framedInstalledCount=-1;
        void MoveToWorkstation()
        {
            if(!Apparatus||Session==null)return;
            string key=Session.spec.id=="P01"?"pendulum":Session.spec.id=="B01"?"microscope":Session.spec.id=="M01"?"gears":null;
            var item=Apparatus.items.FirstOrDefault(x=>x.assetId==key&&x.gameObject.activeInHierarchy);
            var target=item&&ApparatusRig.TryVisibleBounds(item,out var bounds)?bounds.center:Apparatus.Worktop;
            var surface=Apparatus.WorkSurface;
            // Move the level parent outside the table; the SDK continues to own the local head pose.
            var eye=new Vector3(Mathf.Clamp(target.x,surface.min.x+.18f,surface.max.x-.18f),Mathf.Max(surface.max.y+.48f,target.y+.18f),surface.min.z-.38f);
            for(int attempt=0;attempt<4;attempt++,eye.z-=.15f)
            {
                if(Physics.CheckSphere(eye,.16f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
                Apparatus.CancelGrab();CancelFlagshipDrag();
                origin.position=eye-origin.TransformVector(View.transform.localPosition);
                float heading=Quaternion.LookRotation(new Vector3(target.x-eye.x,0,target.z-eye.z),Vector3.up).eulerAngles.y;
                origin.rotation=Quaternion.Euler(0,heading-View.transform.localEulerAngles.y,0);
                origin.position=eye-origin.TransformVector(View.transform.localPosition);
                return;
            }
            Toast(L("Điểm đứng đang bị chắn. Hãy thử lại sau khi đặt dụng cụ xuống.","The work position is blocked. Put the tool down and retry."));
        }
        void FocusStation()
        {
            if(!Apparatus)return;
            if(Input.VrMode||Input.SimulatorMode){MoveToWorkstation();RecenterInterface();return;}
            View.fieldOfView=Profile.settings.cameraFov;var worktop=FindWorktop();
            Bounds kit=default;bool found=false,installed=Apparatus.items.Any(item=>item.installed&&item.gameObject.activeInHierarchy);
            foreach(var item in Apparatus.items)
            {
                if(!item.gameObject.activeInHierarchy||(installed&&!item.installed)||!ApparatusRig.TryVisibleBounds(item,out var bounds))continue;
                var delta=bounds.center-worktop;if(new Vector2(delta.x,delta.z).magnitude>3)continue;
                if(found)kit.Encapsulate(bounds);else{kit=bounds;found=true;}
            }
            if(found&&Flagship(Session.spec.id)&&Profile.settings.focusAssist){if(Session.spec.id=="M01")TryFocusBounds(Apparatus.items.First(x=>x.assetId=="gears"),out kit);else if(Session.spec.id=="C04"&&Session.Phase==ExperimentPhase.Running)TryFocusBounds(Apparatus.items.First(x=>x.assetId=="nacl"),out kit);FitFocusBounds(kit,ActivityObservationArea());return;}
            var target=found?kit.center:worktop+new Vector3(-.15f,.3f,0);
            float distance=found?Mathf.Clamp(Mathf.Max(kit.size.y,kit.size.x/Mathf.Max(.5f,View.aspect))*1.1f,.7f,2.6f):1.8f;
            origin.position=new Vector3(target.x,Mathf.Max(worktop.y+.3f,target.y+.3f),target.z-distance);
            Input.FocusAtWorldTarget(target);
        }
        void ShowPhoneSettings(bool movement)
        {
            var box=Modal(L("Điện thoại · Góc nhìn & di chuyển","Phone · Camera & movement"),1100,900);page.gameObject.SetActive(false);
            Button(box,L("Góc nhìn","Camera"),40,108,496,72,()=>ShowPhoneSettings(false),!movement?Accent:(Color?)null);
            Button(box,L("Di chuyển","Movement"),564,108,496,72,()=>ShowPhoneSettings(true),movement?Accent:(Color?)null);
            Action<Action> edit=change=>{change();dirty=true;ShowPhoneSettings(movement);};
            if(!movement)
            {
                SettingsSwitch(box,L("Đảo chiều ngang","Invert horizontal look"),draft.invertLookX,204,()=>edit(()=>draft.invertLookX=!draft.invertLookX));
                SettingsSwitch(box,L("Đảo chiều dọc","Invert vertical look"),draft.invertLookY,290,()=>edit(()=>draft.invertLookY=!draft.invertLookY));
                SettingsSwitch(box,L("Xoay máy để nhìn (cần cảm biến)","Look by rotating phone (sensor required)"),draft.phoneGyro,376,()=>edit(()=>draft.phoneGyro=!draft.phoneGyro));
                SettingsSwitch(box,L("Tự hướng về bàn khi vào bài","Face workstation when entering"),draft.focusAssist,462,()=>edit(()=>draft.focusAssist=!draft.focusAssist));
                SettingsValueBar(box,L("Độ nhạy: ","Look sensitivity: ")+draft.mouseLookSensitivity.ToString("0.0")+"×",null,552,(draft.mouseLookSensitivity-.5f)/1.5f,"−","+",null,null,()=>edit(()=>draft.mouseLookSensitivity=Mathf.Max(.5f,draft.mouseLookSensitivity-.1f)),()=>edit(()=>draft.mouseLookSensitivity=Mathf.Min(2,draft.mouseLookSensitivity+.1f)));
                SettingsValueBar(box,L("Góc nhìn: ","Field of view: ")+draft.cameraFov.ToString("0")+"°",null,652,(draft.cameraFov-45)/40,"−","+","Phone FOV minus","Phone FOV plus",()=>edit(()=>draft.cameraFov=Mathf.Max(45,draft.cameraFov-5)),()=>edit(()=>draft.cameraFov=Mathf.Min(85,draft.cameraFov+5)));
                ui.Text(box,draft.phoneGyro&&!Input.PhoneGyroAvailable?L("Thiết bị chưa cung cấp cảm biến xoay; bạn vẫn có thể vuốt để nhìn.","Rotation sensor unavailable; swipe look remains available."):L("Vuốt để nhìn; Căn lại đặt lại mốc cảm biến. Kính VR dùng theo dõi đầu.","Swipe to look; Recenter resets the sensor reference. VR uses head tracking."),48,742,1004,42,20,VlabUi.Secondary);
            }
            else
            {
                SettingsSwitch(box,L("Hiện cần di chuyển ảo","Show virtual movement stick"),draft.showPhoneJoystick,230,()=>edit(()=>draft.showPhoneJoystick=!draft.showPhoneJoystick));
                SettingsValueBar(box,L("Tốc độ: ","Movement speed: ")+draft.moveSpeed.ToString("0.0")+" m/s",null,344,(draft.moveSpeed-.3f)/1.7f,"−","+","Phone movement speed minus","Phone movement speed plus",()=>edit(()=>draft.moveSpeed=Mathf.Max(.3f,draft.moveSpeed-.1f)),()=>edit(()=>draft.moveSpeed=Mathf.Min(2,draft.moveSpeed+.1f)));
                SettingsValueBar(box,L("Kích thước cần: ","Stick size: ")+(draft.joystickScale*100).ToString("0")+"%",null,450,(draft.joystickScale-.75f)/.5f,"−","+","Phone joystick size minus","Phone joystick size plus",()=>edit(()=>draft.joystickScale=Mathf.Max(.75f,draft.joystickScale-.05f)),()=>edit(()=>draft.joystickScale=Mathf.Min(1.25f,draft.joystickScale+.05f)));
                ui.Text(box,L("Giữ cần ở góc dưới trái để di chuyển. Vuốt phần nền bên phải để nhìn. Có thể dùng hai ngón cùng lúc. Khi mở bảng, di chuyển dừng lại.\n\nKính VR dùng hướng đầu; các thiết lập vuốt này chỉ áp dụng màn hình thường.","Hold the lower-left stick to move. Drag the right-side background to look. Two fingers can work together. Movement stops while a dialog is open.\n\nVR uses head tracking; swipe settings apply only to the flat screen."),48,554,1004,220,25,VlabUi.Secondary);
            }
            Button(box,L("Mặc định camera","Reset camera defaults"),40,794,496,68,()=>{draft.invertLookX=draft.invertLookY=draft.phoneGyro=false;draft.showPhoneJoystick=draft.focusAssist=true;draft.moveSpeed=1;draft.joystickScale=1;draft.cameraFov=65;draft.mouseLookSensitivity=1;dirty=true;ShowPhoneSettings(movement);});
            Button(box,L("Xong · Chưa áp dụng","Done · Apply in Settings"),564,794,496,68,()=>{CloseModal();ShowSettings();},Accent);
        }
        void ShowFeedback()
        {
            NewPage("feedback");Header(L("Phản hồi & hỗ trợ","Feedback & support"),ShowHome);var box=ui.Panel(page,"Feedback",250,170,1100,670);
            ui.Text(box,L("Bạn gặp vấn đề ở đâu?","What happened?"),48,38,1004,68,38);
            ui.Text(box,L("Gửi tên thí nghiệm, bước đang làm, model điện thoại và ảnh hoặc video lỗi. Bạn có thể sao chép email rồi phản hồi khi có mạng.","Include the experiment, current step, phone model, and a screenshot or video. Copy the email and send your report when online."),48,130,1004,160,30,VlabUi.Secondary);
            ui.Text(box,"nguamanvlab@gmail.com",48,310,1004,72,38,null,TextAlignmentOptions.Center);
            var copied=ui.Text(box,"",48,510,1004,72,26,VlabUi.Secondary,TextAlignmentOptions.Center);
            Button(box,L("Sao chép email","Copy email"),48,414,480,80,()=>{GUIUtility.systemCopyBuffer="nguamanvlab@gmail.com";copied.text=L("Đã sao chép email.","Email copied.");}).name="Copy feedback email";
            Button(box,L("Mở ứng dụng email","Open email app"),572,414,480,80,()=>Application.OpenURL("mailto:nguamanvlab@gmail.com?subject=VLAB%20feedback%20"+Uri.EscapeDataString(Application.version)),Accent);
        }

        void RecenterInterface(){ui.Recenter(View);PlaceReadableSurface(page,Route!="explore");PlaceReadableSurface(modal,true);}
        void RecenterHead(){Input.Recenter();StartCoroutine(AlignUiAfterRecenter());}
        IEnumerator AlignUiAfterRecenter(){yield return null;yield return null;RecenterInterface();}
        IEnumerator AlignAfterTracking(){yield return null;yield return null;if(ui.canvas.renderMode==RenderMode.WorldSpace){if(Apparatus&&(Route=="activity"||Route=="pause"||Route=="settings"))MoveToWorkstation();RecenterInterface();}}
        void OnApplicationPause(bool paused){if(paused){Session?.Pause();CancelFlagshipDrag();Apparatus?.CancelGrab();modalResumeActivity=activityReturnRunning=false;SyncAuthority();Save();}else if(Input)Input.Gameplay=!changing&&(Route=="activity"||Route=="explore");}
        void OnApplicationFocus(bool focused){if(!focused){Session?.InputLost();CancelFlagshipDrag();Apparatus?.CancelGrab();modalResumeActivity=activityReturnRunning=false;SyncAuthority();Save();}}
        void OnApplicationQuit(){Session?.Pause();Save();}
    }
}
