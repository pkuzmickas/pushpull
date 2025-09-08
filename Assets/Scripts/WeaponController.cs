using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject weaponPrefab; // The weapon prefab to spawn
    public Transform weaponSpawnPoint; // Where to spawn the weapon (usually on player)
    public Transform weaponRecallPoint; // Where to spawn the weapon (usually on player)
    public float weaponSpeed = 10f; // Speed of the weapon when launched
    public LayerMask playerLayerMask = 1; // Layer mask for player detection

    [Header("Weapon State")]
    public bool playerHasWeapon = true; // Public flag indicating if player has the weapon

    [Header("Player Rotation Settings")]
    public float playerRotationSpeed = 10f; // Speed at which the player rotates to face click direction

    private GameObject currentWeapon; // Reference to the current weapon instance
    private Camera mainCamera;
    private Rigidbody weaponRb;
    private bool weaponLaunched = false;

    // Click handling variables
    private Coroutine currentRotationMonitor;
    private int clickSequenceId = 0;

    void Start()
    {
        mainCamera = Camera.main;

        // If weapon spawn point is not set, use this transform
        if (weaponSpawnPoint == null)
            weaponSpawnPoint = transform;
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        // Left mouse click - make player face direction immediately
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Increment click sequence to invalidate any previous pending operations
            clickSequenceId++;
            int currentClickId = clickSequenceId;

            Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();

            // Cancel any previous rotation monitoring
            if (currentRotationMonitor != null)
            {
                StopCoroutine(currentRotationMonitor);
            }

            // Get mouse position and make player face that direction immediately
            Vector3 mouseWorldPosition = GetMouseWorldPosition();

            if (mouseWorldPosition != Vector3.zero)
            {
                // Calculate direction from player to mouse position (DON'T normalize here)
                Vector3 clickDirection = (mouseWorldPosition - transform.position);

                // Only proceed if we have a meaningful direction
                if (clickDirection.sqrMagnitude > 0.0001f)
                {
                    MakePlayerFaceDirection(clickDirection, currentClickId);
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            SpawnWeaponIfNeeded();
            LaunchWeaponToMousePosition();
        }

        // Right mouse click - recall weapon to player
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            RecallWeaponToPlayer();
        }
    }

    void SpawnWeaponIfNeeded()
    {
        // Only spawn if player has weapon and no weapon instance exists
        if (playerHasWeapon && currentWeapon == null && weaponPrefab != null)
        {
            currentWeapon = Instantiate(weaponPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation);
            weaponRb = currentWeapon.GetComponent<Rigidbody>();

            weaponLaunched = false;
        }
    }

    void LaunchWeaponToMousePosition()
    {
        if (currentWeapon == null || weaponRb == null || weaponLaunched) return;

        // Get mouse position in world space
        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        if (mouseWorldPosition == Vector3.zero) return;

        // Calculate direction from weapon to mouse position
        Vector3 launchDirection = (mouseWorldPosition - currentWeapon.transform.position).normalized;

        // Freeze Y position while weapon is flying
        weaponRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        // Launch the weapon (player already facing the direction from the press event)
        weaponRb.linearVelocity = launchDirection * weaponSpeed;
        weaponLaunched = true;
        playerHasWeapon = false; // Player no longer has the weapon

        // Add unified weapon handler component
        WeaponHandler weaponHandler = currentWeapon.GetComponent<WeaponHandler>();
        if (weaponHandler == null)
        {
            weaponHandler = currentWeapon.AddComponent<WeaponHandler>();
        }
        weaponHandler.Initialize(this);
    }

    void MakePlayerFaceDirection(Vector3 direction, int clickId)
    {
        // Remove Y component to keep player upright
        Vector3 lookDirection = new Vector3(direction.x, 0f, direction.z);

        // Only rotate if there's a meaningful direction
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            lookDirection.Normalize();

            // Calculate target rotation and apply immediately
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            // Temporarily disable rigidbody interpolation to prevent rotation smoothing
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
    }

    private System.Collections.IEnumerator RestoreInterpolation(Rigidbody rb, RigidbodyInterpolation originalInterpolation, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.interpolation = originalInterpolation;
        }
    }

    void RecallWeaponToPlayer()
    {
        // Only recall if player doesn't have weapon and weapon exists and has been launched
        if (!playerHasWeapon && currentWeapon != null && weaponLaunched && weaponRb != null)
        {
            // Calculate direction from weapon to player
            Vector3 recallDirection = (weaponRecallPoint.position - currentWeapon.transform.position).normalized;

            // Launch weapon towards player
            weaponRb.linearVelocity = recallDirection * weaponSpeed;

            // Set the weapon handler to recall mode
            WeaponHandler weaponHandler = currentWeapon.GetComponent<WeaponHandler>();
            if (weaponHandler == null)
            {
                weaponHandler = currentWeapon.AddComponent<WeaponHandler>();
                weaponHandler.Initialize(this);
            }
            weaponHandler.SetRecalling(true);
        }
    }

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

    public void OnWeaponReturnedToPlayer()
    {
        // Called when weapon hits the player
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
            weaponRb = null;
        }

        playerHasWeapon = true;
        weaponLaunched = false;
    }

    public void OnWeaponCollision()
    {
        // Called when weapon collides with something - unfreeze Y position
        if (weaponRb != null)
        {
            weaponRb.constraints = RigidbodyConstraints.FreezeRotation; // Keep rotation frozen but allow all position movement
        }
    }

}

// Unified component for weapon collision detection and recall functionality
public class WeaponHandler : MonoBehaviour
{
    private WeaponController weaponController;
    private bool hasCollided = false;
    public bool isRecalling = false;
    private float launchTime;
    private const float COLLISION_IGNORE_TIME = 0.2f; // Ignore player collisions for 0.2 seconds after launch

    public void Initialize(WeaponController controller)
    {
        weaponController = controller;
        launchTime = Time.time;
        isRecalling = false;
    }

    public void SetRecalling(bool recalling)
    {
        isRecalling = recalling;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the weapon hit the player
        if (collision.gameObject == weaponController.gameObject)
        {
            // Only allow player collision if weapon is being recalled AND enough time has passed since launch
            if (isRecalling && Time.time - launchTime > COLLISION_IGNORE_TIME)
            {
                weaponController.OnWeaponReturnedToPlayer();
                return;
            }
            // Ignore player collision if not recalling or too soon after launch
            return;
        }

        // If not player collision and hasn't collided yet, unfreeze Y position
        if (!hasCollided)
        {
            hasCollided = true;
            weaponController.OnWeaponCollision();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the weapon hit the player
        if (other.gameObject == weaponController.gameObject)
        {
            // Only allow player collision if weapon is being recalled AND enough time has passed since launch
            if (isRecalling && Time.time - launchTime > COLLISION_IGNORE_TIME)
            {
                weaponController.OnWeaponReturnedToPlayer();
                return;
            }
            // Ignore player collision if not recalling or too soon after launch
            return;
        }

        // If not player collision and hasn't collided yet, unfreeze Y position
        if (!hasCollided)
        {
            hasCollided = true;
            weaponController.OnWeaponCollision();
        }
    }
}
