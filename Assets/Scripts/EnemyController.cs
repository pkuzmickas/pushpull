using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // Assign player via editor

    [Header("AI Settings")]
    public float attackDistance = 2f; // Distance at which enemy starts attacking
    public float moveSpeed = 3.5f; // Enemy movement speed

    [Header("Death Effects")]
    public GameObject bloodEffectPrefab; // Blood effect prefab to spawn on death
    public GameObject impactEffectPrefab; // Impact effect prefab to spawn on death
    public float effectLifetime = 3f; // How long effects stay before being destroyed

    [Header("Public State - Debug")]
    public bool isRunning = false; // Public animator state for visibility
    public bool isAttacking = false; // Public animator state for visibility
    public float distanceToPlayer = 0f; // Public distance for debugging

    public enum State
    {
        Idle,
        Attacking,
        Dead
    }
    public State currentState = State.Idle;

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private Collider enemyCollider;
    private Rigidbody rb;

    void Start()
    {
        // Get required components
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        // Configure NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = attackDistance;
        }
    }

    void Update()
    {
        if (player == null || currentState == State.Dead)
        {
            return;
        }

        if (navMeshAgent != null && currentState == State.Attacking)
        {
            // Calculate distance to player
            distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Check if attack animation is currently playing
            bool isPlayingAttackAnimation = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

            // Determine states based on distance and movement
            bool shouldBeRunning = navMeshAgent.velocity.sqrMagnitude > 0.1f && distanceToPlayer > attackDistance;
            bool shouldBeAttacking = distanceToPlayer <= attackDistance;

            if (!shouldBeAttacking && !isPlayingAttackAnimation)
            {
                navMeshAgent.SetDestination(player.position);
            }

            // Update states
            isRunning = shouldBeRunning;
            isAttacking = shouldBeAttacking;
            //}

            // Update animator parameters
            if (animator != null)
            {
                animator.SetBool("isRunning", isRunning);
                animator.SetBool("isAttacking", isAttacking);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the enemy was hit by a weapon
        WeaponHandler weaponHandler = other.GetComponent<WeaponHandler>();
        if (weaponHandler != null && currentState != State.Dead)
        {
            // Get collision point for effect positioning
            Vector3 collisionPoint = other.ClosestPoint(transform.position);
            TriggerDeath(collisionPoint);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the enemy was hit by a weapon (for non-trigger weapon colliders)
        WeaponHandler weaponHandler = collision.gameObject.GetComponent<WeaponHandler>();
        if (weaponHandler != null && currentState != State.Dead)
        {
            // Get collision point for effect positioning
            Vector3 collisionPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            TriggerDeath(collisionPoint);
        }
    }

    void TriggerDeath(Vector3 impactPoint)
    {
        // Spawn effects at impact point
        SpawnDeathEffects(impactPoint);

        // Change state to dead
        currentState = State.Dead;
        isRunning = false;
        isAttacking = false;
        rb.constraints = RigidbodyConstraints.FreezeAll; // Freeze rigidbody
        enemyCollider.isTrigger = true; // Make collider a trigger to avoid further collisions
        
        // Stop navigation
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            //navMeshAgent.enabled = false; // Disable NavMeshAgent to prevent interference
        }

        // Update animator to play death animation
        if (animator != null)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isAttacking", false);
            animator.SetTrigger("deathTrigger");
        }
    }

    void SpawnDeathEffects(Vector3 impactPoint)
    {
        // Spawn blood effect
        if (bloodEffectPrefab != null)
        {
            GameObject bloodEffect = Instantiate(bloodEffectPrefab, impactPoint, Quaternion.identity);
            
            // Destroy blood effect after specified lifetime
            Destroy(bloodEffect, effectLifetime);
        }

        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            
            // Destroy impact effect after specified lifetime
            Destroy(impactEffect, effectLifetime);
        }
    }
}
