using System;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Presentation only: the source schematic/mask, scientific state and ID mapping stay intact.
    public sealed class MicroscopePresentation:MonoBehaviour
    {
        RawImage image;BiologyDriver driver;Material focusMaterial,previousMaterial;
        public float BlurRadiusSourcePixels{get;private set;}
        public static string Notice(bool vi)=>vi?"Sơ đồ minh họa: phóng to/lấy nét, chưa có thước hiệu chuẩn.":"Illustrative schematic: zoom/focus only. No calibrated scale.";
        public void Bind(RawImage target,BiologyDriver source)
        {
            if(!target||source==null||!source.HasB01Presentation)return;
            image=target;driver=source;
            if(!focusMaterial)
            {
                var shader=Resources.Load<Shader>("UI/MicroscopeFocus");if(!shader)throw new InvalidOperationException("Missing Resources/UI/MicroscopeFocus.shader.");
                previousMaterial=image.material;focusMaterial=new Material(shader){name="Illustrative microscope focus"};
            }
            image.material=focusMaterial;RefreshNow();
        }
        void LateUpdate(){RefreshNow();}
        public void RefreshNow()
        {
            if(!image||driver==null||!focusMaterial||!driver.HasB01Presentation)return;
            Rect uv=driver.InspectionUv("microscope");image.uvRect=uv;
            BlurRadiusSourcePixels=(1-driver.MicroscopeFocusQuality)*36*uv.width;
            focusMaterial.SetFloat("_BlurPixels",BlurRadiusSourcePixels);focusMaterial.SetVector("_UvBounds",new Vector4(uv.xMin,uv.yMin,uv.xMax,uv.yMax));
        }
        void OnDestroy(){if(image&&image.material==focusMaterial)image.material=previousMaterial;if(focusMaterial)Destroy(focusMaterial);}
#if UNITY_EDITOR
        public static void CheckGeometry(Action<string,bool> check)
        {
            Vector2 center=new Vector2(.5f,.5f),source=new Vector2(.52f,.53f);float[] widths={1,.4f,.1f};
            for(int objective=0;objective<3;objective++)
            {
                Rect crop=BiologyDriver.B01InspectionCrop(objective,center);check("B01 objective "+objective+" has expected relative field",Mathf.Abs(crop.width-widths[objective])<.00001f&&crop.width==crop.height&&Vector2.Distance(crop.center,center)<.00001f);
                Vector2 local=new Vector2((source.x-crop.x)/crop.width,(source.y-crop.y)/crop.height),mapped=new Vector2(crop.x+local.x*crop.width,crop.y+local.y*crop.height);
                check("B01 objective "+objective+" maps to the same original source UV",local.x>=0&&local.x<=1&&local.y>=0&&local.y<=1&&Vector2.Distance(source,mapped)<.00001f);
                Rect edge=BiologyDriver.B01InspectionCrop(objective,new Vector2(.01f,.99f));check("B01 crop stays inside original atlas",edge.xMin>=0&&edge.yMin>=0&&edge.xMax<=1&&edge.yMax<=1);
            }
        }
        // Call against a real mounted B01 apparatus. No scientific action or knob is executed here.
        public static void CheckBinding(RawImage image,BiologyDriver driver,Action<string,bool> check)
        {
            string before=driver.SaveState();Texture original=image.texture;var component=image.GetComponent<MicroscopePresentation>()??image.gameObject.AddComponent<MicroscopePresentation>();component.Bind(image,driver);component.RefreshNow();
            check("B01 presentation reads the actual SDK without mutating lesson state",before==driver.SaveState());check("B01 presentation preserves original atlas texture",image.texture==original);
            Rect view=driver.InspectionUv("microscope");Vector2 point=view.center;bool found=false;int expected=0,region=0;
            for(int y=1;y<20&&!found;y++)for(int x=1;x<20&&!found;x++)
            {point=new Vector2(view.x+view.width*x/20,view.y+view.height*y/20);found=driver.TryB01SourceIdentity(point,out expected,out region);}
            check("B01 displayed crop contains a source mask structure",found);
            if(found)
            {
                // Check original global identity at each relative objective field without operating the lesson.
                for(int objective=0;objective<3;objective++)
                {
                    Rect crop=BiologyDriver.B01InspectionCrop(objective,point);Vector2 local=new Vector2((point.x-crop.x)/crop.width,(point.y-crop.y)/crop.height);Vector2 mapped=new Vector2(crop.x+local.x*crop.width,crop.y+local.y*crop.height);
                    bool valid=driver.TryB01SourceIdentity(mapped,out int cell,out int structure);check("B01 objective "+objective+" preserves global cell and structure identity",valid&&cell==expected&&structure==region);
                }
            }
            check("B01 presentation assertions do not select or classify a cell",before==driver.SaveState());
        }
#endif
    }
}
