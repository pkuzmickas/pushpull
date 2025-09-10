using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // Assign player via editor

    [Header("AI Settings")]
    public float attackDistance = 2f; // Distance at which enemy starts attacking
    public float moveSpeed = 3.5f; // Enemy movement speed

    [Header("Public State - Debug")]
    public bool isRunning = false; // Public animator state for visibility
    public bool isAttacking = false; // Public animator state for visibility
    public float distanceToPlayer = 0f; // Public distance for debugging
    public bool isPlayingAttackAnimation = false; // Debug visibility for attack animation state

    public enum State
    {
        Idle,
        Attacking
    }
    public State currentState = State.Idle;

    private NavMeshAgent navMeshAgent;
    private Animator animator;

    void Start()
    {
        // Get required components
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Configure NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = attackDistance;
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        if (navMeshAgent != null && currentState == State.Attacking)
        {
            // Calculate distance to player
            distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Check if attack animation is currently playing
            isPlayingAttackAnimation = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

            //// Don't move if attack animation is playing
            //if (isPlayingAttackAnimation)
            //{
            //    navMeshAgent.isStopped = true;
            //    isRunning = false;
            //    isAttacking = true;
            //}
            //else
            //{
            //navMeshAgent.isStopped = false;

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
}
