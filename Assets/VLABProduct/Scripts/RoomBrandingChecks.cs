using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vlab.Product
{
    public static class RoomBrandingChecks
    {
        public static void Run(Action<string,bool> check,Scene scene,bool? vietnamese=null)
        {
            bool vi=vietnamese??Surfaces(scene).FirstOrDefault(surface=>surface.IsMain)?.Vietnamese??true;
            RoomBranding.Apply(scene,vi);
            var initial=Surfaces(scene);RoomBranding.Apply(scene,vi);var current=Surfaces(scene);
            check("Room branding does not duplicate on repeated adoption",initial.Length==current.Length);
            check("Room has one repaired primary wall identity",current.Count(x=>x.IsMain)==1);
            check("Room branding adds at most one transparent entrance mark",current.Count(x=>!x.IsMain)<=1);
            var renderersInRoom=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
            var labels=renderersInRoom.Where(renderer=>renderer.name=="Room-title"||renderer.name.StartsWith("Room-title.",StringComparison.Ordinal)).ToArray();
            var boards=renderersInRoom.Where(renderer=>renderer.name=="Room-identity"||renderer.name.StartsWith("Room-identity.",StringComparison.Ordinal)).ToArray();var boardVisibility=boards.Select(renderer=>renderer.enabled).ToArray();
            check("Known room lettering and separate backing board are present",labels.Length==1&&boards.Length>=1);
            RoomBranding.SetVisible(scene,false);
            check("Home hides competing wall lettering while preserving the room board",labels.All(renderer=>!renderer.enabled)&&boards.Select(renderer=>renderer.enabled).SequenceEqual(boardVisibility));
            RoomBranding.SetVisible(scene,true);
            check("Leaving Home retains only localized lettering and preserves the board",labels.All(renderer=>!renderer.enabled)&&boards.Select(renderer=>renderer.enabled).SequenceEqual(boardVisibility));
            var main=current.Single(surface=>surface.IsMain);check("Room has exactly one legible localized laboratory name",main.LaboratoryTitle&&main.SourceBoard);
            foreach(bool language in new[]{false,true})
            {
                RoomBranding.SetLanguage(scene,language);var title=main.LaboratoryTitle;
                string expected=scene.name=="Chemistry"?(language?"PHÒNG HÓA HỌC":"CHEMISTRY LAB"):scene.name=="Biology"?(language?"PHÒNG SINH HỌC":"BIOLOGY LAB"):scene.name=="Engineering"?(language?"PHÒNG KỸ THUẬT":"ENGINEERING LAB"):(language?"PHÒNG VẬT LÍ":"PHYSICS LAB");
                check("Live room language changes the single laboratory title: "+(language?"VI":"EN"),title.text==expected&&!title.text.Contains("/")&&!title.text.Contains("\n"));
                check("Localized room title contains all native font glyphs",title.font.HasCharacters(expected));
                title.ForceMeshUpdate();var bounds=title.GetComponent<MeshRenderer>().bounds;var board=main.SourceBoard.bounds;
                check("Localized room title remains centred inside the supplied board: "+(language?"VI":"EN"),!title.isTextOverflowing&&bounds.size.y>.035f&&bounds.min.x>=board.min.x-.005f&&bounds.max.x<=board.max.x+.005f&&bounds.min.y>=board.min.y-.005f&&bounds.max.y<=board.max.y+.005f&&Mathf.Abs(title.transform.position.x-board.center.x)<.001f&&Mathf.Abs(title.transform.position.y-board.center.y)<.001f);
            }
            RoomBranding.SetVisible(scene,false);RoomBranding.SetLanguage(scene,!vi);
            check("Changing locale while branding is hidden does not expose it in a photograph or Home",current.All(surface=>surface.GetComponentsInChildren<Renderer>().All(renderer=>!renderer.enabled)));
            RoomBranding.SetLanguage(scene,vi);RoomBranding.SetVisible(scene,true);
            foreach(var plaque in current)
            {
                check(plaque.name+" is attached to a known scene surface",plaque.SourceAnchor&&plaque.SourceAnchor.gameObject.scene==scene);
                check(plaque.name+" adds no collider or interaction target",plaque.GetComponentsInChildren<Collider>(true).Length==0);
                var renderers=plaque.GetComponentsInChildren<MeshRenderer>();var art=renderers.Where(renderer=>renderer.name=="Original VLAB artwork").ToArray();check(plaque.name+" has exactly one original logo and no added backing",art.Length==1&&renderers.All(renderer=>renderer.name=="Original VLAB artwork"||renderer.GetComponentInParent<TMPro.TextMeshPro>()==plaque.LaboratoryTitle));
                foreach(var renderer in art)
                {
                    var material=renderer.sharedMaterial;
                    check(plaque.name+" / "+renderer.name+" uses a supported retained URP shader",material&&material.shader&&material.shader.name==RoomBranding.ShaderName&&material.shader.isSupported);
                    check(plaque.name+" / "+renderer.name+" has visible nondegenerate bounds",renderer.enabled&&renderer.bounds.size.x>.01f&&renderer.bounds.size.y>.01f&&renderer.bounds.size.magnitude<1f);
                }
                var artwork=renderers.FirstOrDefault(x=>x.name=="Original VLAB artwork");
                check(plaque.name+" uses original texture and untinted pixels",artwork&&artwork.sharedMaterial.GetTexture("_MainTex")==plaque.OriginalTexture&&artwork.sharedMaterial.GetColor("_Tint")==Color.white);
                check(plaque.name+" has no solid-colour background material",art.All(x=>x.sharedMaterial.GetTexture("_MainTex")==plaque.OriginalTexture));
                check(plaque.name+" preserves original texture dimensions",plaque.OriginalTexture&&plaque.OriginalTexture.width==1920&&plaque.OriginalTexture.height==1080);
                check(plaque.name+" original PNG has transparent surrounding pixels",HasTransparentCorners(plaque.OriginalTexture));
                check(plaque.name+" uses the supplied white wordmark rather than the black variant",HasWhiteWordmark(plaque.OriginalTexture));
#if UNITY_EDITOR
                string asset=UnityEditor.AssetDatabase.GetAssetPath(plaque.OriginalTexture);using(var hash=System.Security.Cryptography.SHA256.Create()){string digest=BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(asset))).Replace("-","").ToLowerInvariant();check(plaque.name+" original white PNG is byte-identical to supplied banner white.png",digest==RoomBranding.OriginalArtworkSha256);}
#endif
                if(artwork)check(plaque.name+" keeps cropped artwork UVs inside original pixels",artwork.GetComponent<MeshFilter>().sharedMesh.uv.All(uv=>uv.x>0&&uv.x<1&&uv.y>0&&uv.y<1));
                if(plaque.IsMain)
                {
                    var legacy=plaque.SourceAnchor.GetComponentsInChildren<Renderer>(true).Where(x=>x.name=="Official VLAB icon"||x.name=="VLAB wordmark").ToArray();
                    check("Legacy icon and wordmark cannot double the repaired logo",legacy.Length>0&&legacy.All(x=>!x.enabled));
                    check("Primary white banner aligns above the existing room-name board",Mathf.Abs(plaque.transform.position.x-plaque.SourceBoard.bounds.center.x)<.001f&&Mathf.Abs(plaque.transform.position.z-(plaque.SourceAnchor.position-plaque.SourceAnchor.forward*.002f).z)<.001f&&Quaternion.Angle(plaque.transform.rotation,plaque.SourceAnchor.rotation)<.01f);
                    var ceiling=renderersInRoom.Where(renderer=>renderer.name.StartsWith("Ceiling",StringComparison.Ordinal)).ToArray();
                    check("White banner pixels clear both the actual ceiling grid and the room-name board",artwork&&ceiling.Length>0&&artwork.bounds.max.y<=ceiling.Min(renderer=>renderer.bounds.min.y)-.009f&&artwork.bounds.min.y>=plaque.SourceBoard.bounds.max.y+.009f);
                }
                else check("Transparent entrance mark remains on its known wall",Mathf.Abs(plaque.transform.position.z-plaque.SurfaceBounds.max.z-.006f)<.001f);
            }
        }
        static bool HasWhiteWordmark(Texture2D texture)
        {
            if(!texture)return false;var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(256,144,0,RenderTextureFormat.ARGB32);var sample=new Texture2D(1,1,TextureFormat.RGBA32,false,true);
            try{Graphics.Blit(texture,target);RenderTexture.active=target;sample.ReadPixels(new Rect(138,77,1,1),0,0);sample.Apply();var pixel=sample.GetPixel(0,0);return pixel.a>.8f&&pixel.r>.8f&&pixel.g>.8f&&pixel.b>.8f;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(Application.isPlaying)UnityEngine.Object.Destroy(sample);else UnityEngine.Object.DestroyImmediate(sample);}
        }
        static bool HasTransparentCorners(Texture2D texture)
        {
            if(!texture)return false;
            var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(4,4,0,RenderTextureFormat.ARGB32);
            var sample=new Texture2D(4,4,TextureFormat.RGBA32,false,true);
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;sample.ReadPixels(new Rect(0,0,4,4),0,0);sample.Apply();
                return sample.GetPixel(0,0).a<.01f&&sample.GetPixel(3,0).a<.01f&&sample.GetPixel(0,3).a<.01f&&sample.GetPixel(3,3).a<.01f;
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(Application.isPlaying)UnityEngine.Object.Destroy(sample);else UnityEngine.Object.DestroyImmediate(sample);}
        }
        static RoomBrandingSurface[] Surfaces(Scene scene)=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RoomBrandingSurface>(true)).ToArray();
    }
}
