using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vlab.Product
{
    // A monoscopic photograph wrapped around the learner, not a second live room.
    public sealed class LabPanoramaPreview:MonoBehaviour
    {
        [Serializable] public sealed class CapturePose {public string room;public float yaw;public Vector3 position;}
        public bool ReducedMotion;
        public string CurrentRoom{get;private set;}
        public Texture2D CurrentTexture=>texture;
        public Transform Shell=>shell?shell.transform:null;
        public float Alpha=>alpha;
        public int TriangleCount=>mesh?mesh.triangles.Length/3:0;
        Camera view;Mesh mesh;MeshRenderer shell;Material material;Texture2D texture;float alpha,target;bool visible=true;
        public void Initialize(Camera camera,bool reducedMotion=false)
        {
            view=camera;ReducedMotion=reducedMotion;
            if(shell)return;
            var shader=Resources.Load<Shader>("UI/LabPanorama");
            if(!shader)throw new InvalidOperationException("Missing Resources/UI/LabPanorama.shader.");
            var go=new GameObject("Static room panorama",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.layer=2;
            mesh=BuildSphere();go.GetComponent<MeshFilter>().sharedMesh=mesh;shell=go.GetComponent<MeshRenderer>();
            material=new Material(shader){name="VLAB static panorama"};shell.sharedMaterial=material;shell.shadowCastingMode=ShadowCastingMode.Off;shell.receiveShadows=false;shell.lightProbeUsage=LightProbeUsage.Off;shell.reflectionProbeUsage=ReflectionProbeUsage.Off;shell.sortingOrder=9;shell.enabled=false;
        }
        public bool Show(string room)
        {
            if(room!="Home"&&room!="Physics"&&room!="Chemistry"&&room!="Biology"&&room!="Engineering")return false;
            if(!shell||!view)return false;
            if(CurrentRoom!=room)
            {
                var next=Resources.Load<Texture2D>("UI/Panorama_"+room);if(!next){Clear();return false;}
                if(next!=texture){ReleaseTexture();texture=next;alpha=0;}CurrentRoom=room;material.mainTexture=texture;
                var data=Resources.Load<TextAsset>("UI/Panorama_"+room+"_pose");var pose=data?JsonUtility.FromJson<CapturePose>(data.text):null;
                // Align once at focus; subsequent head yaw does not rotate the photograph.
                float sourceYaw=pose?.yaw??0;shell.transform.rotation=Quaternion.Euler(0,view.transform.eulerAngles.y-sourceYaw,0);
            }
            target=visible?1:0;return true;
        }
        public void Clear(){CurrentRoom=null;target=0;if(alpha<=0)ReleaseTexture();}
        public void SetVisible(bool value){visible=value;target=value&&!string.IsNullOrEmpty(CurrentRoom)?1:0;}
        void LateUpdate()
        {
            if(!shell||!view)return;shell.transform.position=view.transform.position;
            alpha=ReducedMotion?target:Mathf.MoveTowards(alpha,target,Time.unscaledDeltaTime/.22f);material.SetFloat("_Opacity",alpha);shell.enabled=alpha>.001f&&texture;
            if(alpha==0&&CurrentRoom==null)ReleaseTexture();
        }
        void ReleaseTexture(){if(material)material.mainTexture=null;if(texture){Resources.UnloadAsset(texture);texture=null;}}
        static Mesh BuildSphere()
        {
            const int rows=20,columns=40;var vertices=new Vector3[(rows+1)*(columns+1)];var triangles=new int[rows*columns*6];int t=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float latitude=Mathf.PI*y/rows,longitude=2*Mathf.PI*x/columns;
                vertices[y*(columns+1)+x]=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude))*8;
                if(y==rows||x==columns)continue;int a=y*(columns+1)+x,b=a+columns+1;triangles[t++]=a;triangles[t++]=b;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=b;triangles[t++]=b+1;
            }
            var result=new Mesh{name="VLAB 1600-triangle panorama shell"};result.vertices=vertices;result.triangles=triangles;result.RecalculateBounds();return result;
        }
        void OnDestroy(){ReleaseTexture();if(material)Destroy(material);if(mesh)Destroy(mesh);}
    }
}
