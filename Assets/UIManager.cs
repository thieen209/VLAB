using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using VLAB.PhysicsLab.SceneFlow;

public class UIManager : MonoBehaviour
{
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
    }

    public void CloseLabDetail()
    {
        labDetailPanel.SetActive(false);
    }

    // Các hàm mở từng môn
    public void ClickPhysicsLab()
    {
        OpenLabDetail("PHÒNG THÍ NGHIỆM VẬT LÝ", "Thực hành các bài đo gia tốc, chuyển động và lực cơ học.");
    }

    public void ClickChemistryLab()
    {
        OpenLabDetail("PHÒNG THÍ NGHIỆM HÓA HỌC", "Thực hiện các phản ứng hóa học.");
    }

    public void ClickBiologyLab()
    {
        OpenLabDetail("PHÒNG THÍ NGHIỆM SINH HỌC", "Quan sát mẫu vật dưới kính hiển vi và cấu trúc tế bào.");
    }

    public void ClickMechanicalLab()
    {
        OpenLabDetail("PHÒNG THÍ NGHIỆM CƠ KHÍ", "Sử dụng dụng cụ cầm tay, lắp ráp thiết bị và mô hình kỹ thuật.");
    }

    // --- Xử lý nút Join Now & Giả lập kết nối ---
    public void OnJoinNowClicked()
    {
        StartCoroutine(SimulateKitCheckRoutine());
    }

    private IEnumerator SimulateKitCheckRoutine()
    {
        if (labDetailPanel != null)
            labDetailPanel.SetActive(false);

        if (statusPanel != null)
            statusPanel.SetActive(true);

        if (statusText != null)
            statusText.text = "Đang mở Phòng Thí nghiệm Vật lý...";

        yield return new WaitForSecondsRealtime(0.35f);
        SceneManager.LoadScene(PhysicsLabSceneNames.Base, LoadSceneMode.Single);
    }
}
