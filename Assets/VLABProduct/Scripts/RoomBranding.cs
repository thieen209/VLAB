using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Vlab.Product
{
    // Runtime repair only. The supplied scenes, logo PNG pixels and source assets stay unchanged.
    public static class RoomBranding
    {
        public const string ShaderName="VLAB/Room Brand Unlit";
        public const string ShaderResource="Branding/RoomBrand";
        public const string TextureResource="Branding/RoomBannerWhite";
        public const string OriginalArtworkSha256="2ee36b5f8be66c8a68b6645992f2d68ef63f25de18125f6e7fd9da8f6e899322";
        public static void SetVisible(Scene scene,bool visible)
        {
            if(!scene.IsValid()||!scene.isLoaded)return;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var mark in root.GetComponentsInChildren<RoomBrandingSurface>(true)){mark.IsVisible=visible;foreach(var renderer in mark.GetComponentsInChildren<Renderer>())renderer.enabled=visible;}
                foreach(var label in root.GetComponentsInChildren<MeshRenderer>(true))if(label.name=="Room-title"||label.name.StartsWith("Room-title.",StringComparison.Ordinal))label.enabled=false;
            }
        }
        // Measured alpha bounds of supplied banner white.png: top-down421,376..1480,707.
        // Only UVs crop its transparent padding; the source pixels remain unchanged.
        static readonly Rect ArtworkUv=new Rect(421f/1920f,373f/1080f,1059f/1920f,331f/1080f);
        public static string LaboratoryName(Scene scene,bool vi)
        {
            switch(scene.name)
            {case "Chemistry":return vi?"PHÒNG HÓA HỌC":"CHEMISTRY LAB";case "Biology":return vi?"PHÒNG SINH HỌC":"BIOLOGY LAB";case "Engineering":return vi?"PHÒNG KỸ THUẬT":"ENGINEERING LAB";default:return vi?"PHÒNG VẬT LÍ":"PHYSICS LAB";}
        }
        public static void SetLanguage(Scene scene,bool vi)
        {
            if(!scene.IsValid()||!scene.isLoaded)return;
            foreach(var root in scene.GetRootGameObjects())foreach(var mark in root.GetComponentsInChildren<RoomBrandingSurface>(true))
                if(mark.LaboratoryTitle){mark.Vietnamese=vi;mark.LaboratoryTitle.text=LaboratoryName(scene,vi);mark.LaboratoryTitle.ForceMeshUpdate();foreach(var renderer in mark.LaboratoryTitle.GetComponentsInChildren<Renderer>())renderer.enabled=mark.IsVisible;}
        }
        public static void Apply(Scene scene,bool vi=true)
        {
            if(!scene.IsValid()||!scene.isLoaded)return;
            var transforms=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            var anchor=transforms.FirstOrDefault(x=>x.name=="VLAB room identity");
            if(!anchor)return;
            if(transforms.Any(x=>x.GetComponent<RoomBrandingSurface>())){SetLanguage(scene,vi);return;}
            var shader=Resources.Load<Shader>(ShaderResource);var texture=Resources.Load<Texture2D>(TextureResource);
            if(!shader||!shader.isSupported||!texture)throw new InvalidOperationException("The retained room-brand shader/original banner texture is missing or unsupported.");
            foreach(var renderer in anchor.GetComponentsInChildren<Renderer>(true))
                if(renderer.name=="Official VLAB icon"||renderer.name=="VLAB wordmark")renderer.enabled=false;
            foreach(var label in transforms.Select(transform=>transform.GetComponent<MeshRenderer>()).Where(renderer=>renderer&&(renderer.name=="Room-title"||renderer.name.StartsWith("Room-title.",StringComparison.Ordinal))))label.enabled=false;
            var board=transforms.Select(transform=>transform.GetComponent<MeshRenderer>()).FirstOrDefault(renderer=>renderer&&(renderer.name=="Room-identity"||renderer.name.StartsWith("Room-identity.",StringComparison.Ordinal)));
            var position=anchor.position-anchor.forward*.002f;float width=.68f;
            if(board)
            {
                // The authored Physics ceiling is only2.8m; retain the stock
                // banner scale and fit actual visible pixels between its board
                // and the lowest ceiling/grid surface rather than raising it.
                var ceilings=transforms.Select(transform=>transform.GetComponent<MeshRenderer>()).Where(renderer=>renderer&&renderer.name.StartsWith("Ceiling",StringComparison.Ordinal)).ToArray();
                var north=transforms.Select(transform=>transform.GetComponent<MeshRenderer>()).FirstOrDefault(renderer=>renderer&&renderer.name=="Wall-north");
                float top=(ceilings.Length>0?ceilings.Min(renderer=>renderer.bounds.min.y):north?north.bounds.max.y:anchor.position.y+.11f)-.012f;
                float bottom=board.bounds.max.y+.012f;if(top-bottom<.04f)throw new InvalidOperationException("The supplied laboratory sign has no safe space below its ceiling.");
                float artWidth=Mathf.Min(.625f,(top-bottom)*(1059f/331f),board.bounds.size.x-.12f);width=artWidth+.055f;float halfHeight=artWidth/(1059f/331f)*.5f;
                position.x=board.bounds.center.x;position.y=Mathf.Clamp(anchor.position.y,bottom+halfHeight,top-halfHeight);
            }
            var main=BrandMark(scene,"VLAB repaired wall identity",position,anchor.rotation,width,shader,texture,true);
            main.SourceAnchor=anchor;
            if(board)AddLaboratoryTitle(scene,main,board,vi);
            // One restrained transparent mark on the known entrance wall; no plate or background.
            var door=transforms.FirstOrDefault(x=>x.name=="Working entrance");
            var wall=transforms.FirstOrDefault(x=>x.name=="Wall-south");
            var wallRenderer=wall?wall.GetComponent<Renderer>():null;
            var doorRenderers=door?door.GetComponentsInChildren<Renderer>():Array.Empty<Renderer>();
            if(!wallRenderer||doorRenderers.Length==0)return;
            var doorBounds=doorRenderers[0].bounds;foreach(var renderer in doorRenderers)doorBounds.Encapsulate(renderer.bounds);
            var wallBounds=wallRenderer.bounds;
            float x=doorBounds.max.x+.29f;
            if(x+.23f>wallBounds.max.x-.04f||x-.23f<wallBounds.min.x+.04f||1.55f<wallBounds.min.y||1.55f>wallBounds.max.y)return;
            var entrance=BrandMark(scene,"VLAB entrance brand mark",new Vector3(x,1.55f,wallBounds.max.z+.006f),Quaternion.Euler(0,180,0),.44f,shader,texture,false);
            entrance.SourceAnchor=wall;entrance.SurfaceBounds=wallBounds;
        }
        static void AddLaboratoryTitle(Scene scene,RoomBrandingSurface main,MeshRenderer board,bool vi)
        {
            var font=Resources.Load<TMP_FontAsset>("UI/VlabTitle");if(!font)throw new InvalidOperationException("The Vietnamese-capable laboratory-title font is missing.");
            main.SourceBoard=board;main.SurfaceBounds=board.bounds;main.Vietnamese=vi;
            var label=new GameObject("Localized laboratory name",typeof(RectTransform),typeof(TextMeshPro));label.transform.SetParent(main.transform,false);
            label.transform.position=new Vector3(board.bounds.center.x,board.bounds.center.y,board.bounds.min.z-.006f);label.transform.rotation=main.transform.rotation;
            var title=label.GetComponent<TextMeshPro>();main.LaboratoryTitle=title;title.font=font;title.fontStyle=FontStyles.Normal;title.color=VlabUi.Hex("F7F7F2");title.alignment=TextAlignmentOptions.Center;title.textWrappingMode=TextWrappingModes.NoWrap;title.overflowMode=TextOverflowModes.Overflow;
            title.rectTransform.sizeDelta=new Vector2(Mathf.Max(.2f,board.bounds.size.x-.12f),Mathf.Max(.08f,board.bounds.size.y-.025f));title.enableAutoSizing=true;title.fontSizeMin=.6f;title.fontSizeMax=2.4f;title.fontSize=2;title.text=LaboratoryName(scene,vi);title.ForceMeshUpdate();
            var renderer=title.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        static RoomBrandingSurface BrandMark(Scene scene,string name,Vector3 position,Quaternion rotation,float width,Shader shader,Texture2D texture,bool main)
        {
            var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(position,rotation);
            var surface=go.AddComponent<RoomBrandingSurface>();surface.IsMain=main;surface.OriginalTexture=texture;
            float artWidth=width-.055f,artHeight=artWidth/(1059f/331f);
            Quad(surface,"Original VLAB artwork",artWidth,artHeight,Vector3.zero,shader,texture,Color.white,ArtworkUv);
            return surface;
        }
        static void Quad(RoomBrandingSurface owner,string name,float width,float height,Vector3 position,Shader shader,Texture2D texture,Color tint,Rect uv)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(owner.transform,false);go.transform.localPosition=position;
            var mesh=new Mesh{name="Room brand quad"};mesh.vertices=new[]{new Vector3(-width/2,-height/2,0),new Vector3(-width/2,height/2,0),new Vector3(width/2,height/2,0),new Vector3(width/2,-height/2,0)};
            mesh.uv=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMax,uv.yMin)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var material=new Material(shader){name="VLAB retained original brand"};material.SetTexture("_MainTex",texture);material.SetColor("_Tint",tint);
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            owner.Owned.Add(mesh);owner.Owned.Add(material);
        }
    }
    public sealed class RoomBrandingSurface:MonoBehaviour
    {
        public bool IsMain,IsVisible=true,Vietnamese=true;public Transform SourceAnchor;public Bounds SurfaceBounds;public Texture2D OriginalTexture;public MeshRenderer SourceBoard;public TextMeshPro LaboratoryTitle;
        internal readonly List<UnityEngine.Object> Owned=new List<UnityEngine.Object>();
        void OnDestroy(){foreach(var item in Owned)if(item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}}
    }
}
