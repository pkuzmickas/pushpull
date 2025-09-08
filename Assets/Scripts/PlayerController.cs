using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // Speed at which the character rotates
    public float jumpForce = 10f; // Force applied when jumping
    public LayerMask groundLayerMask = 1; // Layer mask for ground detection
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    
    private Camera mainCamera;
    private Rigidbody rb;
    private Animator animator;
    private Vector3 movementDirection;
    private bool isMoving;
    private bool isGrounded;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Configure rigidbody for character movement
        if (rb != null)
        {
            rb.freezeRotation = true; // Prevent the physics system from rotating our player
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        // Check if character is grounded
        CheckGrounded();
        
        // Get input in Update for responsive input detection
        Vector3 input = Vector3.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.z += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.z -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x += 1;
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                Jump();
            }
        }

        if (mainCamera != null)
        {
            // Get the camera's forward and right vectors, but remove the Y component
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = mainCamera.transform.right;
            right.y = 0;
            right.Normalize();

            // Store movement direction for use in FixedUpdate
            movementDirection = (right * input.x + forward * input.z).normalized;
        }

        // Check if character is moving and update animator
        isMoving = movementDirection != Vector3.zero;
        if (animator != null)
        {
            animator.SetBool("isRunning", isMoving);
        }

        // Handle character rotation based on movement direction
        if (isMoving)
        {
            // Calculate the target rotation based on movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);

            // Smoothly rotate the character towards the movement direction
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (rb != null && movementDirection != Vector3.zero)
        {
            // Calculate the next position
            Vector3 targetPosition = rb.position + movementDirection * moveSpeed * Time.fixedDeltaTime;

            // Use MovePosition for physics-based movement
            rb.MovePosition(targetPosition);
        }
    }
    
    void CheckGrounded()
    {
        // Cast a ray downward from the character's position to check if grounded
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // Start slightly above ground
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance + 0.1f, groundLayerMask);
    }
    
    void Jump()
    {
        if (rb != null && isGrounded)
        {
            // Apply upward force for jumping
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            // Trigger jump animation if animator exists
            if (animator != null)
            {
                animator.SetTrigger("jumpTrigger");
            }
        }
    }
}
