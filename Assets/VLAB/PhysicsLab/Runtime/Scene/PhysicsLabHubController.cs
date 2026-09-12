using UnityEngine;

namespace VLAB.PhysicsLab.SceneFlow
{
    public sealed class PhysicsLabHubController : MonoBehaviour
    {
        private Material promptMaterial;
        private void Awake()
        {
            var existingText=GetComponentInChildren<TMPro.TMP_Text>(true);
            var font=existingText!=null?existingText.font:null;
            // The hub is now an empty workbench. Experiment selection lives in the shared pause menu.
            foreach(var canvas in GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
            var console=transform.Find("VLAB_ExperimentConsole");
            if(console!=null)
            {
                console.gameObject.SetActive(false);
            }
            var note=new GameObject("Physics entry prompt",typeof(RectTransform),typeof(Canvas));
            note.transform.SetParent(transform,false);
            note.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            note.transform.SetPositionAndRotation(new Vector3(0,1.22f,-.25f),Quaternion.identity);
            note.transform.localScale=Vector3.one*.002f;
            ((RectTransform)note.transform).sizeDelta=new Vector2(720,130);
            var label=new GameObject("Open menu guidance").AddComponent<TMPro.TextMeshProUGUI>();
            if(font!=null)label.font=font;
            promptMaterial=new Material(label.fontSharedMaterial);
            promptMaterial.SetColor(TMPro.ShaderUtilities.ID_FaceColor,Color.white);label.fontSharedMaterial=promptMaterial;
            label.transform.SetParent(note.transform,false);
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            label.text="Mở Menu / Esc để chọn bài thí nghiệm Vật lí.";
            label.fontSize=34;label.alignment=TMPro.TextAlignmentOptions.Center;
            label.color=new Color(.7f,.94f,1);label.raycastTarget=false;
        }
        private void OnDestroy(){if(promptMaterial!=null)Destroy(promptMaterial);}
        public void OpenPendulum() => Load(PhysicsLabSceneNames.Pendulum);
        public void OpenProjectile() => Load(PhysicsLabSceneNames.Projectile);
        public void OpenFriction() => Load(PhysicsLabSceneNames.Friction);
        public void OpenPhotogateMotion() => Load(PhysicsLabSceneNames.PhotogateMotion);
        public void OpenSpring() => Load(PhysicsLabSceneNames.Spring);
        public void OpenAirTrackMomentum() => Load(PhysicsLabSceneNames.AirTrackMomentum);
        public void BackToMainMenu() => PhysicsLabSceneFlow.Instance?.ReturnToMainMenu();

        private static void Load(string sceneName)
        {
            if (PhysicsLabSceneFlow.Instance == null)
            {
                Debug.LogError("PhysicsLabSceneFlow is not available. Load PhysicsLab_Base before hub content.");
                return;
            }
            PhysicsLabSceneFlow.Instance.LoadContent(sceneName);
        }
    }
}
