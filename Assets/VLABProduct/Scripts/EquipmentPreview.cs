using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vlab.Product
{
    // Copies geometry only: thumbnail capture must never run instrument behaviour.
    public sealed class EquipmentPreview:MonoBehaviour
    {
        readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        public Texture2D Get(EquipmentHandle item)
        {
            if(!item)return null;
            if(cache.TryGetValue(item.assetId,out var image))return image;
            var root=new GameObject("Offline equipment preview");root.SetActive(false);
            var cameraGo=new GameObject("Equipment preview camera");var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;
            RenderTexture texture=null;var previous=RenderTexture.active;
            try
            {
                Bounds bounds=default;bool found=false;
                foreach(var source in item.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=source.GetComponent<MeshRenderer>();if(!source.sharedMesh||!renderer||!renderer.enabled||!VisiblePart(source.transform,item.transform))continue;
                    var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(root.transform,false);part.layer=31;
                    part.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);part.transform.localScale=source.transform.lossyScale;
                    part.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;var copy=part.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=ShadowCastingMode.Off;
                    if(!found){bounds=copy.bounds;found=true;}else bounds.Encapsulate(copy.bounds);
                }
                foreach(var source in item.GetComponentsInChildren<LineRenderer>(true))
                {
                    if(!source.enabled||source.positionCount<2||!VisiblePart(source.transform,item.transform))continue;
                    var part=new GameObject(source.name,typeof(LineRenderer));part.transform.SetParent(root.transform,false);part.layer=31;
                    var copy=part.GetComponent<LineRenderer>();copy.useWorldSpace=false;copy.positionCount=source.positionCount;copy.sharedMaterials=source.sharedMaterials;copy.widthCurve=source.widthCurve;copy.widthMultiplier=Mathf.Max(.003f,source.widthMultiplier);copy.colorGradient=source.colorGradient;
                    for(int n=0;n<source.positionCount;n++){var point=source.useWorldSpace?source.GetPosition(n):source.transform.TransformPoint(source.GetPosition(n));copy.SetPosition(n,point);if(!found){bounds=new Bounds(point,Vector3.one*.003f);found=true;}else bounds.Encapsulate(point);}
                }
                if(!found)return null;
                root.transform.position=new Vector3(500,500,500)-bounds.center;root.SetActive(true);
                camera.orthographic=true;camera.orthographicSize=Mathf.Max(.025f,bounds.size.magnitude*.55f);camera.aspect=1;camera.cullingMask=1<<31;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.82f,.88f,.89f,1);camera.nearClipPlane=.01f;camera.farClipPlane=20;
                camera.transform.position=new Vector3(500,500,500)+new Vector3(1,.65f,-1).normalized*(bounds.size.magnitude+1);camera.transform.LookAt(new Vector3(500,500,500));
                texture=RenderTexture.GetTemporary(192,192,24,RenderTextureFormat.ARGB32);camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                image=new Texture2D(192,192,TextureFormat.RGB24,false);image.name="Preview "+item.assetId;image.ReadPixels(new Rect(0,0,192,192),0,0);image.Apply(false,true);cache.Add(item.assetId,image);return image;
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;if(texture)RenderTexture.ReleaseTemporary(texture);
                root.SetActive(false);cameraGo.SetActive(false);Destroy(root);Destroy(cameraGo);
            }
        }
        static bool VisiblePart(Transform part,Transform item)
        {for(var node=part;node&&node!=item;node=node.parent)if(!node.gameObject.activeSelf)return false;return true;}
        void OnDestroy(){foreach(var image in cache.Values)if(image)Destroy(image);cache.Clear();}
    }
}
