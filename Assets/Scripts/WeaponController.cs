using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    public bool isRecalling = false;
    public float speed = 20f;

    Rigidbody rb; 


    private WeaponController weaponController;
    private bool hasCollided = false;
    private float launchTime;
    private const float COLLISION_IGNORE_TIME = 0.2f; // Ignore player collisions for 0.2 seconds after launch
    private bool isInsidePlayerCollider = true; // Start assuming weapon is inside player collider


    void Start()
    {

        rb = GetComponent<Rigidbody>();

    }

    void Update()
    {
        if(hasCollided)
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
        if(!hasCollided)
        {
           rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime); 
        }
    }


    public void Initialize(WeaponController controller)
    {
        weaponController = controller;
        launchTime = Time.time;
        isRecalling = false;
        isInsidePlayerCollider = true;
    }

    public void SetRecalling(bool recalling)
    {
        isRecalling = recalling;
    }

    void OnTriggerExit(Collider other)
    {
        // Check if weapon is exiting player's collider
        if (other.gameObject == weaponController.gameObject && isInsidePlayerCollider)
        {
            isInsidePlayerCollider = false;
            //weaponController.OnWeaponLeftPlayerCollider();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the weapon hit the player
        if (other.gameObject == weaponController.gameObject)
        {
            // Only allow player collision if weapon is being recalled AND enough time has passed since launch
            if (isRecalling && Time.time - launchTime > COLLISION_IGNORE_TIME && !isInsidePlayerCollider)
            {
                //weaponController.OnWeaponReturnedToPlayer();
                return;
            }
            // Track when weapon enters player collider area
            if (isRecalling)
            {
                isInsidePlayerCollider = true;
            }
            return;
        }

        // If not player collision and hasn't collided yet, unfreeze Y position
        if (!hasCollided && !isInsidePlayerCollider)
        {
            hasCollided = true;
            //weaponController.OnWeaponCollision();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only process collisions if weapon is no longer a trigger
        Collider weaponCollider = GetComponent<Collider>();
        if (weaponCollider != null && weaponCollider.isTrigger)
            return;

        // Check if the weapon hit the player
        if (collision.gameObject == weaponController.gameObject)
        {
            // Only allow player collision if weapon is being recalled AND enough time has passed since launch
            if (isRecalling && Time.time - launchTime > COLLISION_IGNORE_TIME)
            {
                //weaponController.OnWeaponReturnedToPlayer();
                return;
            }
            // Ignore player collision if not recalling or too soon after launch
            return;
        }

        // If not player collision and hasn't collided yet, unfreeze Y position
        if (!hasCollided)
        {
            hasCollided = true;
            //weaponController.OnWeaponCollision();
        }
    }
}

