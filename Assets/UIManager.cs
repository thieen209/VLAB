using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using VLAB.PhysicsLab.SceneFlow;

public class UIManager : MonoBehaviour
{
    private string selectedScene = PhysicsLabSceneNames.Base;
    private string selectedLabTitle = "Phòng Thí nghiệm Vật lý";
    private bool joining;
    [Header("Panels Main")]
    public GameObject homePanel;
    public GameObject labListPanel;
    public GameObject settingsPanel;

    [Header("Lab Detail Popup")]
    public GameObject labDetailPanel;
    public TMP_Text labTitleText;
    public TMP_Text labDescriptionText;

    [Header("Kit Connection Mockup (Mới)")]
    public GameObject statusPanel; // Khung thông báo trạng thái kiểm tra
    public TMP_Text statusText;     // Chữ hiển thị thông báo trạng thái

    private void Start()
    {
        // Keep the existing menu objects and click bindings, but make their choices legible.
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;
            string title = null;
            switch (button.name)
            {
                case "PhysicsLabButton": title = "Vật lý"; break;
                case "ChemistryLabButton": title = "Hóa học"; break;
                case "BiologyLabButton": title = "Sinh học"; break;
                case "MechanicalLabButton": title = "Kỹ thuật"; break;
                case "CloseButton": label.text = "Đóng"; break;
                case "JoinNowButton": label.text = "Vào phòng"; break;
                case "BackButton": label.text = "Quay lại"; break;
            }
            if (title == null) continue;
            label.text = title;
            ((RectTransform)button.transform).sizeDelta = new Vector2(300, 42);
        }
    }

    // --- Các hàm chuyển Panel ---
    public void OpenHome()
    {
        homePanel.SetActive(true);
        labListPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void OpenLabList()
    {
        homePanel.SetActive(false);
        labListPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        homePanel.SetActive(false);
        labListPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // --- Các hàm Pop-up thông tin Lab ---
    public void OpenLabDetail(string labName, string description)
    {
        labTitleText.text = labName;
        labDescriptionText.text = description;
        labDetailPanel.SetActive(true);
        if (!labDetailPanel.transform.IsChildOf(labListPanel.transform)) labListPanel.SetActive(false);
    }

    public void CloseLabDetail()
    {
        labDetailPanel.SetActive(false);
        labListPanel.SetActive(true);
    }

    // Các hàm mở từng môn
    public void ClickPhysicsLab()
    {
        selectedScene = PhysicsLabSceneNames.Base;
        selectedLabTitle = "Phòng Thí nghiệm Vật lý";
        OpenLabDetail("PHÒNG THÍ NGHIỆM VẬT LÝ", "Thực hành các bài đo gia tốc, chuyển động và lực cơ học.");
    }

    public void ClickChemistryLab()
    {
        selectedScene = PhysicsLabSceneNames.Base;
        selectedLabTitle = "Phòng Thí nghiệm Vật lý";
        OpenLabDetail("PHÒNG THÍ NGHIỆM HÓA HỌC", "Thực hiện các phản ứng hóa học.");
    }

    public void ClickBiologyLab()
    {
        selectedScene = "BiologyLab";
        selectedLabTitle = "Phòng Thí nghiệm Sinh học";
        OpenLabDetail("PHÒNG THÍ NGHIỆM SINH HỌC", "Quan sát mẫu vật dưới kính hiển vi và cấu trúc tế bào.");
    }

    public void ClickMechanicalLab()
    {
        selectedScene = "EngineeringLab";
        selectedLabTitle = "Phòng Thí nghiệm Kỹ thuật";
        OpenLabDetail("PHÒNG THÍ NGHIỆM KỸ THUẬT", "Thiết kế và lắp ráp mạch LED an toàn: chọn điện trở, nối mạch và quan sát dòng điện.");
    }

    // --- Xử lý nút Join Now & Giả lập kết nối ---
    public void OnJoinNowClicked()
    {
        if (joining) return;
        StartCoroutine(SimulateKitCheckRoutine());
    }

    private IEnumerator SimulateKitCheckRoutine()
    {
        joining = true;
        if (labDetailPanel != null)
            labDetailPanel.SetActive(false);

        if (statusPanel != null)
            statusPanel.SetActive(true);

        if (statusText != null)
            statusText.text = "Đang mở " + selectedLabTitle + "...";

        yield return new WaitForSecondsRealtime(0.35f);
        SceneManager.LoadScene(selectedScene, LoadSceneMode.Single);
    }
}
