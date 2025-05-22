using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speed")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float rotationSpeed = 10f;
    public float mouseSensitivity = 1f;


    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    public float dashCooldown = 2f;
    public Slider dashCooldownSlider;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    // Internals
    private CharacterController characterController;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private float yaw;
    private Vector3 externalVelocity = Vector3.zero;

    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity;

    // Properties
    public bool isMoving { get; private set; }
    public Vector2 CurrentInput { get; private set; }
    public bool IsGrounded { get; private set; }
    private bool isInvulnerable = false;
    public float CurrentYaw => yaw;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize dash UI                                  
        if (dashCooldownSlider != null)
        {
            dashCooldownSlider.maxValue = dashCooldown;
            dashCooldownSlider.value = dashCooldown;
        }
    }

    void Update()
    {
        HandleDashInput();
        UpdateCooldownSlider();

        if (!isDashing)                        // Modified: skip normal movement while dashing
        {
            HandleMovement();
            HandleRotation();
        }
        UpdateAnimator();
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = (externalVelocity.y > -0.05f && externalVelocity.y < 0.05f) ? 0 : -2f;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = inputDirection.magnitude > 0.1f;

        Vector3 moveDirection = Vector3.zero;

        if (isMoving)
        {
            moveDirection = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * inputDirection;
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            characterController.Move(moveDirection * currentSpeed * Time.unscaledDeltaTime);
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator?.SetBool("isJumping", true);
        }

        velocity.y += gravity * Time.unscaledDeltaTime;

        Vector3 finalMovement = (moveDirection * currentSpeed + externalVelocity) * Time.unscaledDeltaTime;
        finalMovement.y += velocity.y * Time.unscaledDeltaTime;
        characterController.Move(finalMovement);

        if (isGrounded && velocity.y < 0f)
        {
            animator?.SetBool("isJumping", false);
        }


    }


    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        yaw += mouseX;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    void UpdateAnimator()
    {
        float speedPercent = isMoving ? (currentSpeed == sprintSpeed ? 1f : 0.5f) : 0f;
        animator?.SetFloat("Speed", speedPercent, 0.1f, Time.unscaledDeltaTime);
        animator?.SetBool("isGrounded", isGrounded);
        animator?.SetFloat("VerticalSpeed", velocity.y);
        animator?.SetBool("isDashing", isDashing);
    }

    public void SetExternalVelocity(Vector3 platformVelocity)
    {
        externalVelocity = platformVelocity;
    }

    public void SetInvulnerable(bool state) { isInvulnerable = state; }

    // —— DASH LOGIC START —— //
    void HandleDashInput()
    {
        // Right-click to dash if cooldown has passed
        if (Input.GetKeyDown(KeyCode.C) &&
            !isDashing &&
            Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        isDashing = true;
        UpdateAnimator();
        lastDashTime = Time.time;

        float dashEnd = Time.time + dashTime;
        Vector3 dashDir = transform.forward;

        while (Time.time < dashEnd)
        {
            characterController.Move(dashDir * dashSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        isDashing = false;
    }

    void UpdateCooldownSlider()
    {
        if (dashCooldownSlider == null) return;

        float elapsed = Time.time - lastDashTime;
        dashCooldownSlider.value = Mathf.Clamp(elapsed, 0f, dashCooldown);
    }
    // —— DASH LOGIC END —— //

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // BoxCollider
        var box = GetComponent<BoxCollider>();
        if (box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        // CharacterController
        var cc = GetComponent<CharacterController>();
        if (cc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            float radius = cc.radius * 2f; // diameter for drawing spheres
            Vector3 center = cc.center;
            float halfHeight = Mathf.Max(0, cc.height / 2f - cc.radius);
            // bottom sphere
            Gizmos.DrawWireSphere(center - Vector3.up * halfHeight, cc.radius);
            // top sphere
            Gizmos.DrawWireSphere(center + Vector3.up * halfHeight, cc.radius);
            // capsule sides (approximate by cuboid)
            Gizmos.DrawWireCube(center, new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f));
        }
    }

}
