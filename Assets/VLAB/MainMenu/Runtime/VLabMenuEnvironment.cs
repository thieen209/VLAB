using System.Collections.Generic;
using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>A low-overdraw menu stage inspired by the supplied floating cyan interfaces.</summary>
    public sealed class VLabMenuEnvironment : MonoBehaviour
    {
        private readonly List<Object> owned = new List<Object>();
        private void Awake()
        {
            var camera = Camera.main;
            if (camera == null) return;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root == gameObject) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                    if(renderer.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()==null)renderer.enabled = false;
                foreach (var light in root.GetComponentsInChildren<Light>()) light.enabled = false;
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.012f,.026f,.048f);
            camera.allowHDR = false;
            RenderSettings.fog = true;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .035f;
            var center = camera.transform.position;
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            var origin = center + forward * 6;
            var brand=new GameObject("VLAB holographic identity").AddComponent<TMPro.TextMeshPro>();
            brand.transform.SetParent(transform,false);
            brand.transform.SetPositionAndRotation(center+camera.transform.forward*3.7f+camera.transform.up*1.35f,camera.transform.rotation);
            var font=Resources.Load<VLABMenuAssets>("VLABMenuAssets")?.activityFont;
            if(font!=null)brand.font=font;
            brand.text="V L A B";brand.fontSize=7.2f;brand.fontStyle=TMPro.FontStyles.Bold;
            brand.alignment=TMPro.TextAlignmentOptions.Center;brand.rectTransform.sizeDelta=new Vector2(3.8f,.65f);
            var brandMaterial=new Material(brand.fontSharedMaterial);owned.Add(brandMaterial);
            brandMaterial.SetColor(TMPro.ShaderUtilities.ID_FaceColor,new Color(.86f,.98f,1));
            brandMaterial.SetColor(TMPro.ShaderUtilities.ID_OutlineColor,new Color(0,.7f,.85f));
            brandMaterial.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth,.12f);
            brand.fontSharedMaterial=brandMaterial;
            // One static mesh for all distant points; no particle updates or transparent quads.
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var random = new System.Random(209);
            for (int i=0;i<150;i++)
            {
                var point = center + new Vector3((float)random.NextDouble()*24-12, (float)random.NextDouble()*12-5, (float)random.NextDouble()*24-12);
                if ((point-center).sqrMagnitude<36) continue;
                int start = vertices.Count;
                float size=.012f+(float)random.NextDouble()*.015f;
                vertices.Add(point+Vector3.up*size); vertices.Add(point+Vector3.right*size);
                vertices.Add(point-Vector3.up*size); vertices.Add(point-Vector3.right*size);
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3,start+2,start+1,start,start+3,start+2,start});
            }
            var mesh = new Mesh { name="VLAB distant scientific points" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); owned.Add(mesh);
            var points = new GameObject("Distant points", typeof(MeshFilter),typeof(MeshRenderer));
            points.transform.SetParent(transform,false); points.GetComponent<MeshFilter>().sharedMesh=mesh;
            points.GetComponent<MeshRenderer>().sharedMaterial=Material(new Color(.08f,.35f,.43f));
            for(int ring=0;ring<3;ring++)
            {
                var go=new GameObject("Laboratory orbit "+ring,typeof(LineRenderer));go.transform.SetParent(transform,false);
                var line=go.GetComponent<LineRenderer>();line.sharedMaterial=Material(new Color(.018f,.14f,.2f));
                line.widthMultiplier=.008f;line.loop=true;line.positionCount=96;
                for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,new Vector3(center.x+Mathf.Cos(a)*(4+ring*3),center.y-1.65f,center.z+Mathf.Sin(a)*(4+ring*3)));}
            }
        }
        private Material Material(Color color)
        {
            var material=new Material(Shader.Find("Unlit/Color")){color=color};owned.Add(material);return material;
        }
        private void OnDestroy(){foreach(var item in owned)if(item!=null)Destroy(item);}
    }
}
