using UnityEngine;
using UnityEngine.UI; 
using TMPro;        

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform cameraTransform; // Kéo Main Camera vào đây
    public float interactDistance = 3f; // Khoảng cách có thể tương tác
    public LayerMask interactableLayer; // Layer của đồ vật (sẽ thiết lập sau)

    [Header("Pickup Settings")]
    public Transform holdPoint;       // Kéo Object HoldPoint vào đây
    public float throwForce = 10f;    // Lực vứt đồ vật (Q)

    [Header("UI Settings")]
    public GameObject interactPromptUI; // Kéo object Text tương tác vào đây

    private InteractableItem currentTargetItem; // Đồ vật đang nhìn vào
    private InteractableItem heldItem;          // Đồ vật đang cầm trên tay

    void Update()
    {
        // 1. CHỨC NĂNG NHẬN DIỆN & HIỆN "BẤM E"
        CheckForInteractable();

        // 2. CHỨC NĂNG CẦM LÊN (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTargetItem != null && heldItem == null)
            {
                PickupItem(currentTargetItem);
            }
        }

        // 3. CHỨC NĂNG VỨT XUỐNG (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (heldItem != null)
            {
                DropItem();
            }
        }
    }

    // Hàm bắn Raycast để kiểm tra đồ vật trước mặt
    void CheckForInteractable()
    {
        // Nếu đang cầm đồ, không cần hiện prompt tương tác nữa
        if (heldItem != null)
        {
            if (interactPromptUI.activeSelf) interactPromptUI.SetActive(false);
            currentTargetItem = null;
            return;
        }

        RaycastHit hit;
        // Bắn một tia từ camera về phía trước
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactDistance, interactableLayer))
        {
            // Kiểm tra xem vật bị bắn trúng có script InteractableItem không
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null)
            {
                currentTargetItem = item;
                // Hiện UI "Bấm E"
                if (!interactPromptUI.activeSelf) interactPromptUI.SetActive(true);
            }
            else
            {
                // Trúng vật khác không phải đồ vật tương tác
                currentTargetItem = null;
                if (interactPromptUI.activeSelf) interactPromptUI.SetActive(false);
            }
        }
        else
        {
            // Không trúng gì cả
            currentTargetItem = null;
            if (interactPromptUI.activeSelf) interactPromptUI.SetActive(false);
        }
    }

    void PickupItem(InteractableItem item)
    {
        heldItem = item;

        // Tắt vật lý để nó không rơi khi đang cầm
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Tắt vật lý (isKinematic = true)
        }

        // Di chuyển đồ vật về điểm holdPoint và đặt làm con của holdPoint
        item.transform.position = holdPoint.position;
        item.transform.rotation = holdPoint.rotation;
        item.transform.SetParent(holdPoint);

        // Ẩn prompt tương tác ngay khi cầm
        if (interactPromptUI.activeSelf) interactPromptUI.SetActive(false);
    }

    void DropItem()
    {
        // Bỏ làm con của holdPoint
        heldItem.transform.SetParent(null);

        // Bật lại vật lý
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Bật lại vật lý

            // Thêm lực để vứt ra phía trước (Q)
            rb.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);
        }

        heldItem = null;
    }
}