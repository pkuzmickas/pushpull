using Unity.AppUI.UI;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public bool isRecalling = false;
    public float speed = 20f;
    public GameObject flyingEffects;

    private Rigidbody rb;
    private Collider weaponCollider;
    private const float PLAYER_LAYER = 6;
    private const float ENEMY_LAYER = 8;
    private bool hasCollidedWithWall = false;

    void Start()
    {
        weaponCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        isRecalling = false;
    }

    void Update()
    {
        if (hasCollidedWithWall)
        {
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
        else
        {
            if (rb != null)
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
        if (!isHitPlayer(collision.collider) && collision.gameObject.layer != ENEMY_LAYER)
        {
            hasCollidedWithWall = true;
            flyingEffects.SetActive(false);
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
            Vector3 directionToPlayer = (location - transform.position).normalized;
            rb.linearVelocity = directionToPlayer * speed;
            Vector3 velocity = directionToPlayer * speed;
            velocity.y *= 20f; // Double Y speed for faster vertical movement
            //TODO: fix recall Y position

            rb.linearVelocity = velocity;
            //transform.position = new Vector3(transform.position.x, location.y, transform.position.z); 
            isRecalling = true;
            hasCollidedWithWall = false;
            flyingEffects.SetActive(true);
        }
    }
}

