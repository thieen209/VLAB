using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>Serialized boundary between the permanent room and guided apparatus.</summary>
    public sealed class VLabExperimentStation : MonoBehaviour
    {
        [SerializeField] private GameObject[] guidedContent = new GameObject[0];
        public GameObject[] GuidedContent => guidedContent;
        public void Configure(Vector3 position, GameObject[] content)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            guidedContent = content;
        }
    }
}
