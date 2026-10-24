using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Blur the existing offline panorama only inside each circular utility, with no extra camera or full-screen pass.
    public sealed class FrostedMenuDisc:MonoBehaviour
    {
        RawImage image;Material owned;Canvas canvas;readonly Vector3[] corners=new Vector3[4];
        public static void Add(VlabUi ui,Transform parent)
        {var rect=ui.Rect(parent,"Frosted circular background",18,0,84,84);rect.gameObject.AddComponent<FrostedMenuDisc>().Initialize(ui.canvas);}
        void Initialize(Canvas owner)
        {
            canvas=owner;image=gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            var shader=Resources.Load<Shader>("UI/MenuDisc");if(!shader||!shader.isSupported){image.enabled=false;return;}
            owned=new Material(shader){name="Circular panorama frost"};image.material=owned;
        }
        void LateUpdate()
        {
            var app=VlabApp.Instance;if(!owned||!app||!app.Preview||!app.Preview.CurrentTexture||!app.Preview.Shell)return;
            image.texture=app.Preview.CurrentTexture;((RectTransform)transform).GetWorldCorners(corners);
            for(int i=0;i<4;i++)
            {
                Vector3 direction=canvas.renderMode==RenderMode.WorldSpace?corners[i]-app.View.transform.position:app.View.ScreenPointToRay(RectTransformUtility.WorldToScreenPoint(null,corners[i])).direction;
                owned.SetVector("_Ray"+i,app.Preview.Shell.InverseTransformDirection(direction).normalized);
            }
        }
        void OnDestroy(){if(owned)Destroy(owned);}
    }
}
