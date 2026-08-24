using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để quản lý Scene

public class SceneLoader : MonoBehaviour
{
    // Hàm gọi chuyển sang scene PhysicsLab
    public void LoadPhysicsLab()
    {
        SceneManager.LoadScene("PhysicsLab");
    }
}