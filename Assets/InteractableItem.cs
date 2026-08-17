using UnityEngine;
using TMPro;

public class InteractableItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Đồ vật không tên";
    public TextMeshProUGUI nameTagText;

    void Start()
    {
        if (nameTagText != null)
        {
            nameTagText.text = itemName;
        }
    }
}