using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;

    [Header("Lực nhảy & Trọng lực")]
    public float jumpHeight = 1.2f;   // Độ cao lực nhảy (mét)
    public float gravity = -9.81f;    // Gia tốc trọng lực

    [Header("Độ nhạy chuột")]
    public float mouseSensitivity = 2.0f;
    public float maxUpAngle = 80f;
    public float maxDownAngle = -80f;

    [Header("Thành phần")]
    public Transform cameraTransform;
    private CharacterController controller;

    private float cameraPitch = 0f;
    private Vector3 velocity;         // Biến lưu vận tốc rơi/nhảy theo phương Y
    private bool isGrounded;          // Kiểm tra xem nhân vật có đang chạm đất không

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, maxDownAngle, maxUpAngle);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        // 1. Kiểm tra va chạm mặt đất
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            // Đặt vận tốc âm nhỏ để nhân vật bám chắc mặt đất
            velocity.y = -2f;
        }

        // 2. Di chuyển WASD + Shift
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speed = isRunning ? runSpeed : walkSpeed;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

        // 3. Xử lý Nhảy (Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Công thức tính vận tốc nhảy dựa trên độ cao: v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Tính toán trọng lực kéo nhân vật xuống theo thời gian
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}