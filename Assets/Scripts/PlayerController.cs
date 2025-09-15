using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // Speed at which the character rotates
    public float jumpForce = 10f; // Force applied when jumping
    public LayerMask groundLayerMask = 1; // Layer mask for ground detection
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    public GameObject weaponPrefab; // The weapon prefab to spawn
    public Transform weaponSpawnPoint; // Where to spawn the weapon (usually on player)
    public Transform weaponRecallPoint; // Where to spawn the weapon (usually on player)
    public bool playerHasWeapon;

    private Camera mainCamera;
    private Rigidbody rb;
    private Animator animator;
    private Vector3 movementDirection;
    private bool isMoving;
    private bool isGrounded;
    private GameObject currentWeapon;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerHasWeapon = true;

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

        HandleMovement();

        HandleMouseInput();
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

    void ShootWeapon()
    {
        if (animator != null && playerHasWeapon)
        {
            animator.SetTrigger("shootTrigger");
            currentWeapon = Instantiate(weaponPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation);
        }
    }

    void HandleMovement()
    {
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
    void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        // Left mouse click - make player face direction immediately
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

            // Get mouse position and make player face that direction immediately
            Vector3 mouseWorldPosition = GetMouseWorldPosition();

            if (mouseWorldPosition != Vector3.zero)
            {
                // Calculate direction from player to mouse position (DON'T normalize here)
                Vector3 clickDirection = (mouseWorldPosition - transform.position);

                // Only proceed if we have a meaningful direction
                if (clickDirection.sqrMagnitude > 0.0001f)
                {
                    MakePlayerFaceDirection(clickDirection);
                    ShootWeapon();
                }
            }
        }

        //// Right mouse click - recall weapon to player
        //if (Mouse.current.rightButton.wasReleasedThisFrame)
        //{
        //    RecallWeaponToPlayer();
        //}
    }
    //void RecallWeaponToPlayer()
    //{
    //    // Only recall if player doesn't have weapon and weapon exists and has been launched
    //    if (!playerHasWeapon && currentWeapon != null && weaponLaunched && weaponRb != null)
    //    {
    //        // Calculate direction from weapon to player
    //        Vector3 recallDirection = (weaponRecallPoint.position - currentWeapon.transform.position).normalized;

    //        // Launch weapon towards player
    //        weaponRb.linearVelocity = recallDirection * weaponSpeed;

    //        // Set the weapon handler to recall mode
    //        WeaponHandler weaponHandler = currentWeapon.GetComponent<WeaponHandler>();
    //        if (weaponHandler == null)
    //        {
    //            weaponHandler = currentWeapon.AddComponent<WeaponHandler>();
    //            weaponHandler.Initialize(this);
    //        }
    //        weaponHandler.SetRecalling(true);
    //        weaponEffects.SetActive(true);
    //    }
    //}

    //public void OnWeaponCollision()
    //{
    //    // Called when weapon collides with something - unfreeze Y position
    //    if (weaponRb != null)
    //    {
    //        weaponRb.constraints = RigidbodyConstraints.FreezeRotation; // Keep rotation frozen but allow all position movement
    //        weaponEffects.SetActive(false);
    //    }
    //}

    //public void OnWeaponLeftPlayerCollider()
    //{
    //    // Called when weapon exits player's collision area
    //    if (currentWeapon != null)
    //    {
    //        Collider weaponCollider = currentWeapon.GetComponent<Collider>();
    //        if (weaponCollider != null && weaponCollider.isTrigger)
    //        {
    //            weaponCollider.isTrigger = false;
    //        }
    //    }
    //}

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

        // Cast a ray from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPosition);

        // First try to hit any colliders in the scene
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return hit.point;
        }

        // If no hit, project to a plane at the player's Y level
        Plane playerPlane = new Plane(Vector3.up, transform.position);
        if (playerPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            return worldPos;
        }

        // Fallback: use a reasonable fixed distance
        Vector3 fallbackPos = ray.GetPoint(20f);
        return fallbackPos;
    }

    void MakePlayerFaceDirection(Vector3 direction)
    {
        // Remove Y component to keep player upright
        Vector3 lookDirection = new Vector3(direction.x, 0f, direction.z);

        lookDirection.Normalize();

        // Calculate target rotation and apply immediately
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        // Temporarily disable rigidbody interpolation to prevent rotation smoothing
        // Causes jittery movement if not disabled
        Rigidbody rb = GetComponent<Rigidbody>();
        RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
        if (rb != null)
        {
            originalInterpolation = rb.interpolation;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        // Apply rotation instantly - no interpolation
        transform.rotation = targetRotation;

        // Re-enable interpolation after a short delay
        if (rb != null)
        {
            StartCoroutine(RestoreInterpolation(rb, originalInterpolation, 0.1f));
        }

    }
    private System.Collections.IEnumerator RestoreInterpolation(Rigidbody rb, RigidbodyInterpolation originalInterpolation, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.interpolation = originalInterpolation;
        }
    }

}
