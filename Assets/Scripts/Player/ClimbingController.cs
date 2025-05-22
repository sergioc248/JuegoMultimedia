using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ClimbingController : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;


    [Header("Climbing Settings")]
    [Tooltip("The exact tag your vine colliders use.")]
    public string vineTag = "Vine";

    [Tooltip("Vertical speed when climbing up/down the vine.")]
    public float climbSpeed = 3f;

    [Tooltip("Parameter name for the Animator bool to enter climb state.")]
    public string animClimbBool = "isClimbing";

    [Tooltip("Parameter name for the Animator float to drive climb animation speed.")]
    public string animClimbSpeed = "ClimbSpeed";

    private bool isClimbing = false;
    private Collider currentVine;

    void Start()
    {
        if (animator == null)
            Debug.LogError("VineClimbingController: Missing Animator on Player.");
        if (playerController == null)
            Debug.LogError("VineClimbingController: Missing PlayerController on Player.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(vineTag))
        {
            currentVine = other;
            StartClimbing();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == currentVine)
            StopClimbing();
    }

    void Update()
    {
        if (!isClimbing) return;
        HandleClimbing();
    }

    private void StartClimbing()
    {
        isClimbing = true;
        playerController.enabled = false;                    // disable normal movement
        animator.SetBool(animClimbBool, true);
        AlignWithVine();                                    // snap to vine center
    }

    private void StopClimbing()
    {
        isClimbing = false;
        animator.SetBool(animClimbBool, false);
        playerController.enabled = true;                     // restore control
    }

    private void AlignWithVine()
    {
        // Lock player XZ to the vine's local position
        Vector3 p = transform.position;
        p.x = currentVine.transform.position.x;
        p.z = currentVine.transform.position.z;
        transform.position = p;
    }

    private void HandleClimbing()
    {
        float v = Input.GetAxis("Vertical");     // Vertical input for climbing
        Vector3 climbMotion = Vector3.up * v * climbSpeed * Time.deltaTime;
        characterController.Move(climbMotion);

        // Drive climb animation speed
        animator.SetFloat(animClimbSpeed, Mathf.Abs(v));

        // Exit climb and perform jump if Jump pressed
        if (Input.GetButtonDown("Jump"))
        {
            // first stop climbing and re-enable normal controls
            StopClimbing();

            // perform jump impulse using CharacterController
            float jumpHeight = playerController.jumpHeight;
            float gravity = playerController.gravity;
            // calculate upward velocity equivalent
            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Vector3 jumpMotion = Vector3.up * jumpVelocity * Time.deltaTime;
            characterController.Move(jumpMotion);

            // trigger jump animation on PlayerController's animator
            playerController.SendMessage("OnExternalJump", SendMessageOptions.DontRequireReceiver);
        }
    }
}
