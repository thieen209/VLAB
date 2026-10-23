#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Focused native Home geometry/input checks; this is not an all-lesson visual audit.
    public static class HomePresentationChecks
    {
        static Button Button(VlabApp app,string name)=>app.GetComponentsInChildren<Button>().Single(b=>b.isActiveAndEnabled&&b.name==name);
        static Quaternion Calibration(VlabApp app)=>(Quaternion)typeof(MockControllerProvider).GetField("calibration",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(app.Input.Controller);
        static Quaternion Aim(Quaternion calibration,Vector3 direction)=>Quaternion.Inverse(calibration)*Quaternion.LookRotation(direction);
        static void Signal(VlabApp app,long sequence,Quaternion orientation,bool select,Action<string,bool> check)
        {check("Fresh Home VR sample "+sequence,app.Input.Controller.Submit(new ControllerSignal{sequence=sequence,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=select}));}
        static void CopyFits(TextMeshProUGUI text,string value,float font,Action<string,bool> check,string label)
        {
            string original=text.text;float originalFont=text.fontSize;
            try
            {
                text.text=value;text.fontSize=font;text.ForceMeshUpdate(true,true);
                float needed=text.GetPreferredValues(value,text.rectTransform.rect.width,float.PositiveInfinity).y;
                check(label+" fits its actual TMP rectangle",needed<=text.rectTransform.rect.height+.75f&&!text.isTextOverflowing);
            }
            finally{text.text=original;text.fontSize=originalFont;text.ForceMeshUpdate(true,true);}
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Covers(Mesh mesh,Vector2 point)
        {
            var vertices=mesh.vertices;var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                Vector2 a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]];if(Mathf.Abs(Cross(b-a,c-a))<.001f)continue;
                float ab=Cross(b-a,point-a),bc=Cross(c-b,point-b),ca=Cross(a-c,point-c);
                if((ab>=0&&bc>=0&&ca>=0)||(ab<=0&&bc<=0&&ca<=0))return true;
            }
            return false;
        }
        static void CheckPlateCut(SciencePortalGraphic plate,int subject,Action<string,bool> check)
        {
            Mesh sample=null;try
            {
                sample=UnityEngine.Object.Instantiate(plate.canvasRenderer.GetMesh());var r=plate.rectTransform.rect;
                var cut=new[]{new Vector2(146,330),new Vector2(187,370),new Vector2(210,329),new Vector2(207,378)}[subject];
                check("Native plate retains its opaque title field "+subject,Covers(sample,new Vector2(r.xMin+134,r.yMax-100)));
                check("Native science contour removes plate triangles rather than painting over them "+subject,!Covers(sample,new Vector2(r.xMin+cut.x,r.yMax-cut.y)));
                check("Lower-right card surface does not return behind the overflowing figure "+subject,!Covers(sample,new Vector2(r.xMin+253,r.yMax-430)));
                var vertices=sample.vertices;var indices=sample.triangles;float area=0;for(int i=0;i<indices.Length;i+=3)area+=Mathf.Abs(Cross((Vector2)vertices[indices[i+1]]-(Vector2)vertices[indices[i]],(Vector2)vertices[indices[i+2]]-(Vector2)vertices[indices[i]]))*.5f;
                check("Plate contains substantial real negative space and a usable body "+subject,area<r.width*r.height-2000&&area>r.width*r.height*.55f);
            }
            finally{if(sample)UnityEngine.Object.DestroyImmediate(sample);}
        }
        public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
        {
            check("Home review begins in Vietnamese simulator after plan flow",app&&app.Route=="home"&&app.Profile.locale=="vi"&&app.Input.SimulatorMode);
            check("Resting Home uses a capture from the current project's room",app.Preview.CurrentRoom=="Home");
            string profile=JsonUtility.ToJson(app.Profile);Quaternion calibration=Calibration(app);
            app.Input.UseMockController=true;yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
            check("Home background is its actual current-project wide room photograph",app.Preview.CurrentRoom=="Home"&&app.Preview.CurrentTexture&&app.Preview.CurrentTexture.name=="Panorama_Home"&&app.Preview.Alpha>.99f);
            var page=app.GetComponentInChildren<HomePortalArc>().GetComponent<RectTransform>();
            var logo=page.GetComponentsInChildren<RawImage>().Single(image=>image.name=="Original VLAB logo");
            Vector3 logoCentre=page.InverseTransformPoint(logo.rectTransform.TransformPoint(logo.rectTransform.rect.center));
            check("Original transparent VLAB wordmark is centred at logical x800",Mathf.Abs(logoCentre.x-page.rect.xMin-800)<.5f);
            check("Home has no redundant laboratory-selection heading",!page.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.text=="Chọn phòng thí nghiệm"||text.text=="Choose a laboratory"||text.text=="Ngua Man VLAB"));
            string[] codes={"P","C","B","M"},viTitles={"Vật lí","Hóa học","Sinh học","Kỹ thuật"},enTitles={"Physics","Chemistry","Biology","Engineering"};
            var titleFont=Resources.Load<TMP_FontAsset>("UI/VlabTitle");check("Home title font is an installed Vietnamese-capable bold face",titleFont&&titleFont.faceInfo.styleName.IndexOf("Bold",StringComparison.OrdinalIgnoreCase)>=0);
            var baselineSizes=new Vector3[4];
            for(int i=0;i<codes.Length;i++)
            {
                var card=Button(app,"Subject "+codes[i]);
                var cardRect=card.GetComponent<RectTransform>();
                check(codes[i]+" card has the same upright baseline plane and logical size",Vector3.Distance(cardRect.anchoredPosition3D,new Vector3(326+i*316,-468,0))<.1f&&Quaternion.Angle(cardRect.localRotation,Quaternion.identity)<.01f&&Vector2.Distance(cardRect.rect.size,new Vector2(268,432))<.1f);
                var corners=new Vector3[4];cardRect.GetWorldCorners(corners);var bounds=corners.Select(point=>app.View.WorldToViewportPoint(point)).ToArray();
                Debug.Log("VLAB Home card viewport corners "+codes[i]+": "+string.Join(", ",bounds.Select(point=>point.ToString("F4"))));
                check(codes[i]+" complete card stays inside the native view",bounds.All(point=>point.z>0&&point.x>=.02f&&point.x<=.98f&&point.y>=.02f&&point.y<=.98f));
                // Compare matching rows, not the AABB: a pitched shared canvas
                // shears outer cards sideways without changing their actual size.
                baselineSizes[i]=new Vector3(bounds[2].x-bounds[1].x,bounds[3].x-bounds[0].x,(bounds[1].y+bounds[2].y-bounds[0].y-bounds[3].y)*.5f);
                var graphics=card.GetComponentsInChildren<SciencePortalGraphic>();var shape=graphics.Single(graphic=>!graphic.IsFrame&&!graphic.IsPlate);var frame=graphics.Single(graphic=>graphic.IsFrame);var plate=graphics.Single(graphic=>graphic.IsPlate);
                check(codes[i]+" science shape is an opaque native vector mesh without a bitmap",shape.Subject==i&&shape.DrawnVertexCount>60&&shape.color.a==1&&!shape.raycastTarget&&card.GetComponentsInChildren<RawImage>().Length==0);
                check(codes[i]+" full box border is replaced by a native open frame",frame.DrawnVertexCount>0&&frame.DrawnBounds.width>=cardRect.rect.width&&frame.DrawnBounds.height>250&&frame.color.a==1&&!card.GetComponent<Outline>().enabled);
                check(codes[i]+" portrait silhouette retains its bounds and follows saved panel opacity",plate.DrawnVertexCount>=18&&Mathf.Abs(plate.color.a-UiTheme.Opacity)<.001f&&Vector2.Distance(plate.DrawnBounds.size,cardRect.rect.size)<.1f&&card.GetComponent<Image>().color.a==0);
                CheckPlateCut(plate,i,check);
                Mesh frameSample=null;try{frameSample=UnityEngine.Object.Instantiate(frame.canvasRenderer.GetMesh());var r=frame.rectTransform.rect;check(codes[i]+" lower-right rectangular outline is cut away behind the figure",!Covers(frameSample,new Vector2(r.xMin+254,r.yMax-430)));}finally{if(frameSample)UnityEngine.Object.DestroyImmediate(frameSample);}
                var shapeBounds=shape.DrawnBounds;Vector3 lowerRight=cardRect.InverseTransformPoint(shape.rectTransform.TransformPoint(new Vector3(shapeBounds.xMax,shapeBounds.yMin,0)));
                check(codes[i]+" drawn scientific shape crosses the right and bottom frame",lowerRight.x>cardRect.rect.xMax+2&&lowerRight.y<cardRect.rect.yMin-2);
                check(codes[i]+" silhouette overflow stays within half the nominal card gap",lowerRight.x-cardRect.rect.xMax<=24);
                check(codes[i]+" scientific silhouette remains unclipped",shape.GetComponentsInParent<Mask>().Length==0&&shape.GetComponentsInParent<RectMask2D>().Length==0&&card.GetComponentsInChildren<Mask>().Length==0);
                var texts=card.GetComponentsInChildren<TextMeshProUGUI>();var title=texts.Single(text=>text.text==viTitles[i]);
                check(codes[i]+" card contains only its title and vector elements",texts.Length==1&&!card.transform.Find("Portal arrow ring"));
                check(codes[i]+" title uses the actual new bold face without synthetic emboldening",title.font==titleFont&&title.fontStyle==FontStyles.Normal);
                Vector3 titleCentre=cardRect.InverseTransformPoint(title.rectTransform.TransformPoint(title.rectTransform.rect.center));
                check(codes[i]+" title is top-aligned and horizontally centred near the upper edge",title.fontSize>=36&&Mathf.Abs(titleCentre.x-cardRect.rect.center.x)<.1f&&Vector2.Distance(title.rectTransform.anchoredPosition,new Vector2(16,-20))<.1f&&title.rectTransform.rect.height==60&&title.alignment==TextAlignmentOptions.Top);
                foreach(bool large in new[]{false,true})foreach(bool vi in new[]{true,false})
                {
                    string scope=codes[i]+(vi?" VI":" EN")+(large?" large":" normal");float scale=large?1.12f:1;
                    CopyFits(title,vi?viTitles[i]:enTitles[i],36*scale,check,scope+" title");
                }
            }
            Debug.Log("VLAB Home matching-row projected sizes (top width, bottom width, height): "+string.Join(", ",baselineSizes.Select(size=>size.ToString("F4"))));
            check("All four resting cards have equal projected top edges, bottom edges and height",baselineSizes.All(size=>size.x>0&&size.y>0&&size.z>0)&&baselineSizes.Max(size=>size.x)-baselineSizes.Min(size=>size.x)<.003f&&baselineSizes.Max(size=>size.y)-baselineSizes.Min(size=>size.y)<.003f&&baselineSizes.Max(size=>size.z)-baselineSizes.Min(size=>size.z)<.003f);
            string[] labels={"Cài đặt","Trợ giúp","Giới thiệu","Sổ tay"};
            for(int i=0;i<labels.Length;i++)
            {
                var button=page.GetComponentsInChildren<Button>().Single(b=>b.transform.parent==page&&b.GetComponentInChildren<TextMeshProUGUI>()?.text==labels[i]);var rect=button.GetComponent<RectTransform>();
                check("Centred uniform circular Home utility "+labels[i],Vector2.Distance(rect.sizeDelta,new Vector2(120,120))<.1f&&Mathf.Abs(rect.anchoredPosition.x-(512+i*152))<.1f&&Mathf.Abs(rect.anchoredPosition.y+770)<.1f);
                var disc=button.GetComponentsInChildren<Image>().Single(image=>image.name=="Circular utility disc");
                Debug.Log("VLAB Home utility diagnostics: label="+labels[i]+" host="+button.name+" hostSize="+rect.sizeDelta+" hostAnchor="+rect.anchoredPosition+" disc="+disc.name+" discActive="+disc.isActiveAndEnabled+" discParent="+disc.transform.parent.name+" discSize="+disc.rectTransform.sizeDelta+" discAnchor="+disc.rectTransform.anchoredPosition+" discRaycast="+disc.raycastTarget+" sprite="+(disc.sprite?disc.sprite.name:"null")+" target="+(button.targetGraphic?button.targetGraphic.name:"null")+" targetParent="+(button.targetGraphic?button.targetGraphic.transform.parent.name:"null"));
                check("Utility has an84px circle and a separate readable label "+labels[i],disc.sprite&&Vector2.Distance(disc.rectTransform.sizeDelta,new Vector2(84,84))<.1f&&Vector2.Distance(disc.rectTransform.anchoredPosition,new Vector2(18,0))<.1f&&disc.raycastTarget&&button.targetGraphic==disc);
                var caption=button.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.text==labels[i]);
                check("Utility label fits below its circle "+labels[i],Mathf.Abs(caption.rectTransform.anchoredPosition.y+86)<.1f&&Mathf.Abs(caption.rectTransform.rect.height-34)<.1f);
                foreach(bool large in new[]{false,true})CopyFits(caption,labels[i],22*(large?1.12f:1),check,labels[i]+(large?" large":" normal"));
            }
            var book=Button(app,"Open journal").GetComponent<RectTransform>();
            check("Notebook is the fourth circular utility without a duplicate lower row",Vector2.Distance(book.sizeDelta,new Vector2(120,120))<.1f&&Mathf.Abs(book.anchoredPosition.x-968)<.1f&&page.GetComponentsInChildren<Button>().Count(button=>button.name=="Open journal")==1);
            check("Home omits the removed continue-lesson button",!page.GetComponentsInChildren<Button>().Any(button=>button.name=="Home resume"));
            var edges=codes.Select(code=>Button(app,"Subject "+code).GetComponent<RectTransform>()).Select(rect=>new[]{app.View.WorldToViewportPoint(rect.TransformPoint(new Vector3(rect.rect.xMin,rect.rect.center.y,0))),app.View.WorldToViewportPoint(rect.TransformPoint(new Vector3(rect.rect.xMax,rect.rect.center.y,0)))}).ToArray();
            var gaps=Enumerable.Range(0,3).Select(index=>edges[index+1][0].x-edges[index][1].x).ToArray();
            Debug.Log("VLAB Home projected mid-edge gaps: "+string.Join(", ",gaps.Select(gap=>gap.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
            check("Four upright Home cards have positive balanced projected gaps",edges.All(pair=>pair.All(point=>point.z>0))&&gaps.All(gap=>gap>0)&&gaps.Max()-gaps.Min()<.009f);
            for(int i=0;i<3;i++)
            {
                var shape=Button(app,"Subject "+codes[i]).GetComponentsInChildren<SciencePortalGraphic>().Single(graphic=>!graphic.IsFrame&&!graphic.IsPlate);
                var nativeMesh=shape.canvasRenderer.GetMesh();check(codes[i]+" spacing samples the actual rendered vector vertices",nativeMesh&&nativeMesh.vertexCount>0);
                var next=Button(app,"Subject "+codes[i+1]).GetComponent<RectTransform>();var corners=new Vector3[4];next.GetWorldCorners(corners);
                Vector3 bottom=app.View.WorldToViewportPoint(corners[0]),top=app.View.WorldToViewportPoint(corners[1]);var plane=new Plane(next.forward,next.position);Mesh sample=null;
                try
                {
                    // Copy only for inspection. Never destroy the mesh owned by the live renderer.
                    sample=UnityEngine.Object.Instantiate(nativeMesh);float minimumLogicalMargin=float.PositiveInfinity;int compared=0;bool projectionValid=top.y>bottom.y;
                    foreach(var vertex in sample.vertices.Distinct())
                    {
                        Vector3 point=app.View.WorldToViewportPoint(shape.transform.TransformPoint(vertex));
                        if(point.z<=0||point.y<bottom.y||point.y>top.y)continue;
                        float edgeX=Mathf.Lerp(bottom.x,top.x,(point.y-bottom.y)/(top.y-bottom.y));
                        var ray=app.View.ViewportPointToRay(new Vector3(edgeX,point.y,0));
                        if(!plane.Raycast(ray,out float distance)||distance<=0){projectionValid=false;continue;}
                        Vector3 local=next.InverseTransformPoint(ray.GetPoint(distance));
                        float eightPixels=app.View.WorldToViewportPoint(next.TransformPoint(local+Vector3.right*8)).x-edgeX;
                        if(eightPixels<=0){projectionValid=false;continue;}
                        minimumLogicalMargin=Mathf.Min(minimumLogicalMargin,(edgeX-point.x)/eightPixels*8);compared++;
                    }
                    Debug.Log("VLAB Home same-row vector clearance "+codes[i]+": vertices="+compared+" minimumLogicalPixels="+minimumLogicalMargin.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                    check(codes[i]+" vector keeps eight logical pixels clear of the next portal",projectionValid&&compared>0&&minimumLogicalMargin>=8);
                }
                finally{if(sample)UnityEngine.Object.DestroyImmediate(sample);}
            }
            long sequence=11000;
            for(int i=0;i<codes.Length;i++)
            {
                yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();var card=Button(app,"Subject "+codes[i]);var rect=card.GetComponent<RectTransform>();
                var nativeFrame=card.GetComponentsInChildren<SciencePortalGraphic>().Single(graphic=>graphic.IsFrame);float restingFrameWidth=nativeFrame.DrawnBounds.width;
                var nativePlate=card.GetComponentsInChildren<SciencePortalGraphic>().Single(graphic=>graphic.IsPlate);Color32 restingFill=nativePlate.canvasRenderer.GetMesh().colors32[0];Vector3 restingScale=card.transform.localScale;
                var direction=rect.TransformPoint(rect.rect.center)-app.View.transform.position;var orientation=Aim(calibration,direction);
                Signal(app,sequence++,orientation,false,check);yield return null;yield return null;yield return new WaitForSecondsRealtime(.25f);
                Canvas.ForceUpdateCanvases();check("VR focus strengthens the actual opaque vector frame "+codes[i],nativeFrame.color.a==1&&nativeFrame.DrawnBounds.width>restingFrameWidth+.5f);
                Color32 hoverFill=nativePlate.canvasRenderer.GetMesh().colors32[0];check("Real VR hover tints the inside without fading the figure "+codes[i],Mathf.Abs(hoverFill.a-Mathf.RoundToInt(UiTheme.Opacity*255))<=1&&(hoverFill.r!=restingFill.r||hoverFill.g!=restingFill.g||hoverFill.b!=restingFill.b));
                bool reduced=card.GetComponent<PortalFocus>().ReducedMotion;check("Hover respects gentle growth and reduced motion "+codes[i],reduced?Vector3.Distance(card.transform.localScale,restingScale)<.001f:card.transform.localScale.x>=restingScale.x*1.025f&&card.transform.localScale.x<=restingScale.x*1.036f);
                string room=new[]{"Physics","Chemistry","Biology","Engineering"}[i];
                check("Home focus uses a capture of the actual current-project "+room+" room",app.Preview.CurrentRoom==room&&app.Preview.CurrentTexture&&app.Preview.CurrentTexture.name=="Panorama_"+room);
                capture("47-nen-"+codes[i]+"-VI");
                Signal(app,sequence++,orientation,true,check);yield return null;yield return null;
                Signal(app,sequence++,Aim(calibration,Vector3.up),false,check);
                float deadline=Time.realtimeSinceStartup+3;while(app.Route!="browser"&&Time.realtimeSinceStartup<deadline)yield return null;
                check("Actual VR ray opens "+viTitles[i]+" through portal animation",app.Route=="browser");
                check("Selected browser retains its matching current-project room capture "+codes[i],app.Preview.CurrentRoom==room&&app.Preview.CurrentTexture&&app.Preview.CurrentTexture.name=="Panorama_"+room);
                var lessons=app.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.name.StartsWith("Browser lesson ",StringComparison.Ordinal)).ToArray();
                check("Browser exposes all eight lessons of the ray-selected subject "+codes[i],lessons.Length==8&&lessons.Select(button=>button.name).Distinct().Count()==8&&lessons.All(b=>b.name.StartsWith("Browser lesson "+codes[i],StringComparison.Ordinal)));
                var selectedTitle=app.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.name=="Browser selected title");
                var selectedGoal=app.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.name=="Browser selected goal");
                var photograph=Button(app,"Browser explore").GetComponentInChildren<UnityEngine.UI.RawImage>();
                check("Browser offers a labelled actual-room photo without another scene load "+codes[i],photograph&&photograph.texture&&photograph.texture.name=="Preview_"+room&&!photograph.raycastTarget);
                foreach(var lesson in lessons)
                {
                    string id=lesson.name.Substring("Browser lesson ".Length);var caption=lesson.GetComponentInChildren<TextMeshProUGUI>();
                    foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
                    {
                        float scale=large?1.12f:1;string label=id+(vi?" VI":" EN")+(large?" large":" normal");
                        CopyFits(caption,LessonGuide.ShortTitle(id,vi)+"\n<size=20>"+(vi?"Chưa có số đo":"No saved readings")+"</size>",26*scale,check,label+" browser grid title/status");
                        CopyFits(selectedTitle,LessonGuide.ShortTitle(id,vi),32*scale,check,label+" browser detail title");
                        CopyFits(selectedGoal,ReadinessView.Goal(id,vi),24*scale,check,label+" browser detail goal");
                    }
                }
                capture("63-danh-sach-8-thi-nghiem-"+codes[i]+"-VI");
                yield return new WaitForSecondsRealtime(.3f);Button(app,"Browser home").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
                check("Real browser Home callback restores four cards",app.Route=="home"&&app.GetComponentsInChildren<HomePortalArc>().Length==1);
            }
            Signal(app,11999,Aim(calibration,Vector3.up),false,check);yield return null;yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            check("Home exposes exactly one live VR mode control",app.GetComponentsInChildren<Button>().Count(button=>button.isActiveAndEnabled&&button.name=="VR mode")==1);
            check("Editor mode control describes leaving simulation",Button(app,"VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="Thoát VR");
            Button(app,"VR mode").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
            check("Actual mode callback leaves simulation and offers it again",!app.Input.SimulatorMode&&Button(app,"VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="VR");
            Button(app,"VR mode").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
            check("Actual mode callback restores the world-space Home",app.Input.SimulatorMode&&Button(app,"Subject P").GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace&&Button(app,"VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="Thoát VR");
            for(int i=0;i<codes.Length;i++)check("Mode roundtrip retains Home anchoring "+codes[i],Vector3.Distance(Button(app,"Subject "+codes[i]).GetComponent<RectTransform>().anchoredPosition3D,new Vector3(326+i*316,-468,0))<.1f);
            check("Home navigation preserves calibration quaternion",Quaternion.Angle(calibration,Calibration(app))<.0001f);
            check("Home checks preserve notes, measurements and settings",JsonUtility.ToJson(app.Profile)==profile);
        }
    }
}
#endif
