using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CozyThirdPersonController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool allowKeyboardFallback = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private float verticalVelocity;

    public MobileJoystick Joystick
    {
        get => joystick;
        set => joystick = value;
    }

    public Transform CameraTransform
    {
        get => cameraTransform;
        set => cameraTransform = value;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 desiredMove = GetCameraRelativeMove(moveInput);

        Vector3 targetVelocity = desiredMove * moveSpeed;
        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            acceleration * Time.deltaTime);

        if (desiredMove.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredMove, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = currentVelocity;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = joystick != null ? joystick.Input : Vector2.zero;

        if (allowKeyboardFallback)
        {
            input.x += UnityEngine.Input.GetAxisRaw("Horizontal");
            input.y += UnityEngine.Input.GetAxisRaw("Vertical");
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }

        return Vector3.ClampMagnitude((right * input.x) + (forward * input.y), 1f);
    }
}
