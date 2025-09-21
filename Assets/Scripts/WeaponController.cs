using Unity.AppUI.UI;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public bool isRecalling = false;
    public float speed = 20f;
    public GameObject flyingEffects;
    public GameObject wallImpactEffect;

    private Rigidbody rb;
    private Collider weaponCollider;
    private const float PLAYER_LAYER = 6;
    private const float ENEMY_LAYER = 8;
    private const float WALL_LAYER = 9;
    private bool hasCollidedWithWall = false;
    private bool isAdjustingHeight = false;
    private Vector3 targetPlayerLocation;
    private float targetY;

    void Start()
    {
        weaponCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        isRecalling = false;
    }

    void Update()
    {
        if (rb)
        {
            if (hasCollidedWithWall)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!hasCollidedWithWall && !isRecalling)
        {
            rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
        }
        else if (isRecalling && isAdjustingHeight)
        {
            // Smoothly move to target Y position
            float currentY = transform.position.y;
            float newY = Mathf.MoveTowards(currentY, targetY, speed * Time.fixedDeltaTime);

            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Check if we've reached the target Y position
            if (Mathf.Abs(newY - targetY) < 0.1f)
            {
                isAdjustingHeight = false;
                // Now start moving towards the player with fixed Y
                Vector3 directionToPlayer = (targetPlayerLocation - transform.position).normalized;
                directionToPlayer.y = 0; // Keep Y movement at 0
                rb.linearVelocity = directionToPlayer * speed;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if weapon is exiting player's collider
        if (isHitPlayer(other))
        {
            weaponCollider.isTrigger = false; // Disable trigger to allow collisions

        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isHitPlayer(collision.collider) && collision.gameObject.layer != ENEMY_LAYER && collision.gameObject.layer == WALL_LAYER)
        {
            hasCollidedWithWall = true;
            flyingEffects.SetActive(false);
            GameObject impactEffect = Instantiate(wallImpactEffect, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(impactEffect, 0.7f);
        }
    }

    bool isHitPlayer(Collider other)
    {
        return other.gameObject.layer == PLAYER_LAYER;
    }

    public void RecallWeapon(Vector3 location)
    {
        if (rb != null)
        {
            // Store target location and Y position
            targetPlayerLocation = location;
            targetY = location.y;

            // Start the recall process
            isRecalling = true;
            isAdjustingHeight = true;
            hasCollidedWithWall = false;
            flyingEffects.SetActive(true);

            // Remove rigidbody constraints temporarily to allow Y movement
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero; // Stop current movement
        }
    }
}

