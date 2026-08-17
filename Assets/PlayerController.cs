using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;

    [Header("Độ nhạy chuột")]
    public float mouseSensitivity = 2.0f;
    public float maxUpAngle = 80f;   // Giới hạn ngước mắt lên
    public float maxDownAngle = -80f; // Giới hạn cúi mắt xuống

    [Header("Thành phần")]
    public Transform cameraTransform;
    private CharacterController controller; // Dùng CharacterController sẽ mượt hơn Rigidbody

    private float cameraPitch = 0f; // Góc xoay lên/xuống của Camera

    void Start()
    {
        // Tự lấy CharacterController nếu chưa gắn
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // Tự lấy Main Camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Khóa con trỏ chuột vào giữa màn hình khi chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    // 1. Xử lý xoay góc nhìn bằng chuột 🖱️
    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Xoay nhân vật sang trái/phải theo chiều ngang chuột
        transform.Rotate(Vector3.up * mouseX);

        // Xoay Camera lên/xuống (Pitch)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, maxDownAngle, maxUpAngle); // Giới hạn góc nhìn

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    // 2. Xử lý di chuyển WASD 🚶
    void HandleMovement()
    {
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speed = isRunning ? runSpeed : walkSpeed;

        float moveX = Input.GetAxis("Horizontal"); // Phím A/D
        float moveZ = Input.GetAxis("Vertical");   // Phím W/S

        // Tính hướng di chuyển theo hướng mặt nhân vật đang nhìn
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Di chuyển bằng CharacterController
        controller.Move(move * speed * Time.deltaTime);
    }
}