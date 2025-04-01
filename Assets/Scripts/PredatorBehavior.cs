using UnityEngine;
using UnityEngine.AI;

public class PredatorBehavior : Animal
{
    public enum PredatorState { Wandering, Stalking, Chasing, Kill }

    private Coroutine activeCoroutine;

    [Header("Setup")]
    public PredatorState predatorState;
    public Transform prey;
    private bool isDead = false;
    

    [Header("Wandering Behavior")]
    public bool isWandering = false;
    public float wanderRadius = 10f;
    public float wanderDistance = 5f;
    public float wanderingMoveSpeed = 4f;
    private Vector3 velocity; // Current velocity of the predator
    private Vector3 wanderTarget;
    public float rotationSpeed = 5f; // Speed of rotation to face the target direction


    [Header("Steering")]
    public float maxSpeed = 4f; 
    public float maxForce = 10f;
    private float jitter = 3f;
    [SerializeField] float steeringWeight = 1.0f;
    public LayerMask obstacleLayerMask;
    public float obstacleAvoidanceRadius = 2f;
    public float obstacleDetectionRadius = 10f;


    [Header("Prey Detection")]
    public float fieldofView = 120f;
    public float sightDistance = 25f;
    private float nextDetectionTime = 0f;
    public float detectionInterval = 0.2f;
    

    [Header("Stalking Behavior")]
    public Transform hidingSpot;
    public bool isHiding = false;
    public float stalkingDistance = 25f;
    public float stalkTime = 0f; 
    private bool isSpotted = false;


    [Header("Chasing Behavior")]
    public float chaseSpeed = 5f;
    public float chaseDistance;
    public float stamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRecoveryRate = 5f;


    [Header("Kill Behavior")]
    public float killDistance;
    private float killChance = 0.5f;

    // private float targetUpdateInterval = 2.0f; // Every 2 seconds
    // private float lastTargetUpdateTime = 0f;
    // Vector3 currentSteeringForce = Vector3.zero; // Current steering force applied to the predator
    // private float movementCommitTime = 1.5f; // Move in the same direction for 1.5s
    // private float lastMoveTime = 0f;
    private float nextWanderUpdateTime = 0f; // Timer for wander updates
    public float wanderUpdateInterval = 2f; // Time interval between updates

    protected override void Start()
    {
        base.Start();
        SetState(PredatorState.Wandering);
        wanderTarget = transform.position + Random.insideUnitSphere * wanderRadius;
        //prey = GameObject.FindGameObjectWithTag("Prey").transform;
        
    }

    protected override void Update()
    {
        if (Time.time >= nextDetectionTime)
        {
            //DetectPrey();
            nextDetectionTime = Time.time + detectionInterval;
            // if (predatorState == PredatorState.Chasing && stamina > 0)
            // {
            //     stamina -= staminaDrainRate * Time.deltaTime;
            //     agent.speed = chaseSpeed;
            // }
            // else if (predatorState == PredatorState.Wandering)
            // {
            //     stamina += staminaRecoveryRate * Time.deltaTime;
            //     agent.speed = wanderingMoveSpeed;
            // }

            HandleState();
        }

        // if (Time.time - lastTargetUpdateTime > targetUpdateInterval)
        // {
        //     lastTargetUpdateTime = Time.time;
        //     WanderSteering(); // Only update occasionally
        // }

        // ApplySteering(currentSteeringForce); // Keep applying last computed force
    }

    void SetState(PredatorState newState)
    {
        Debug.Log("SetState called with newState: " + newState + ", current state: " + predatorState);
        if (predatorState == newState) return;

        Debug.Log("Predator state changed: " + predatorState + " -> " + newState);

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        predatorState = newState;

        HandleState();

    }

    protected override void HandleState() 
    {
        switch (predatorState)
        {
            case PredatorState.Chasing:
                ChasingBehavior();
                break;
            case PredatorState.Stalking:
                StalkingBehavior();
                break;
            case PredatorState.Wandering:
                WanderingBehavior();
                break;
            case PredatorState.Kill:
                KillBehavior();
                break;
        }
    }

    void WanderingBehavior()
    {
        agent.speed = wanderingMoveSpeed;
        agent.isStopped = false; // Ensure the agent is not stopped
        WanderSteering();
    }

    void WanderSteering()
    {
        wanderTarget += new Vector3(Random.Range(-jitter, jitter), 0, Random.Range(-jitter, jitter));
        wanderTarget = Vector3.ClampMagnitude(wanderTarget, wanderRadius);
        Vector3 targetPosition = transform.position + transform.forward * wanderDistance + wanderTarget;

        Vector3 steeringForce = CalculateSteeringForce(targetPosition);
        ApplySteering(steeringForce);
    }

    Vector3 CalculateSteeringForce(Vector3 target)
    {
        Vector3 desiredVelocity = (target - transform.position).normalized * maxSpeed;

        Vector3 steeringForce = desiredVelocity - velocity;
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

        Vector3 avoidanceForce = AvoidObstacles();
        steeringForce += avoidanceForce * steeringWeight;

        return steeringForce;
    }

    Vector3 AvoidObstacles()
    {
        Vector3 avoidance = Vector3.zero;
        Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleDetectionRadius, obstacleLayerMask);

        foreach (var obstacle in obstacles)
        {
            Vector3 obstacleDir = transform.position - obstacle.transform.position;
            float distance = obstacleDir.magnitude;
            if (distance > 0)
            {
                float strength = Mathf.Clamp01(1f - (distance / obstacleDetectionRadius)); // Reduce effect for far obstacles
                avoidance += obstacleDir.normalized * strength;
            }
        }

        return Vector3.ClampMagnitude(avoidance, maxForce * 0.5f); // Scale down the avoidance force
    }

    void ApplySteering(Vector3 force)
    {
        // Calculate the target position based on the steering force
        Vector3 targetPosition = transform.position + force.normalized * wanderDistance;

        // Use NavMeshAgent to move toward the target position
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            // Check if the target is near the edge of the NavMesh
            if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edgeHit, NavMesh.AllAreas))
            {
                float distanceToEdge = Vector3.Distance(hit.position, edgeHit.position);
                if (distanceToEdge < 3f) // Adjust threshold as needed
                {
                    Debug.Log("Target near NavMesh edge, adjusting...");
                    Vector3 directionToCenter = (transform.position - edgeHit.position).normalized;
                    targetPosition = hit.position + directionToCenter * 2f; // Push it away from the edge
                }
            }

            // Set the destination on the NavMeshAgent
            agent.SetDestination(hit.position);

            // Debugging
            Debug.Log($"Setting NavMeshAgent destination to: {hit.position}");
        }
        else
        {
            Debug.LogWarning("No valid NavMesh position found for target.");
        }
    }

    
    // bool DetectObstacleAhead(out Vector3 avoidanceDirection)
    // {
    //     RaycastHit hit;
    //     Vector3 forward = transform.forward * avoidanceDistance;

    //     if (Physics.Raycast(transform.position, forward, out hit, avoidanceDistance))
    //     {
    //         Debug.Log("Obstacle detected: " + hit.collider.name);

    //         // decide which direction to turn
    //         Vector3 leftDirection = Quaternion.Euler(0, -turnAngle, 0) * transform.forward;
    //         Vector3 rightDirection = Quaternion.Euler(0, turnAngle, 0) * transform.forward;

    //         // Check which side has more space
    //         bool leftClear = !Physics.Raycast(transform.position, leftDirection, avoidanceDistance);
    //         bool rightClear = !Physics.Raycast(transform.position, rightDirection, avoidanceDistance);

    //         if (leftClear && rightClear)
    //         {
    //             avoidanceDirection = Random.value > 0.5f ? leftDirection : rightDirection;
    //         }
    //         else if (leftClear)
    //         {
    //             avoidanceDirection = leftDirection;
    //         }
    //         else if (rightClear)
    //         {
    //             avoidanceDirection = rightDirection;
    //         }
    //         else
    //         {
    //             // no direction to go, go back
    //             avoidanceDirection = -transform.forward;
    //         }

    //         return true;
    //     }

    //     avoidanceDirection = Vector3.zero;
    //     return false; // no obstacle detected
    // }

    
    // bool IsNearNavMeshEdge(Vector3 position)
    // {
    //     NavMeshHit hit;
    //     return !NavMesh.SamplePosition(position, out hit, 10f, NavMesh.AllAreas);
    // }

    // Vector3 GetAdjustedCenterPoint()
    // {
    //     Vector3 mapCenter = new Vector3(120, 0, 120);
    //     Vector3 directionToCenter = (mapCenter - transform.position).normalized;
    //     Vector3 newTarget = transform.position + directionToCenter * 20f;

    //     NavMeshHit hit;
    //     if (NavMesh.SamplePosition(newTarget, out hit, 25f, NavMesh.AllAreas))
    //     {
    //         return hit.position;
    //     }

    //     return transform.position; // Fallback to current position if no valid point found
    // }
    
    
    void StalkingBehavior()
    {
        // after spotting prey, if predator has not been spotted (if prey is not fleeing), hide
        // use objects to put between prey and predator, maybe add grass which predator
        // hides in and obscures view/hides predator
        // moves between hiding spots as close as possible, if next hiding spot is closer to prey
        // than last hiding spot, move (crouch) to next hiding spot.
        // if spotted, go into chase.
        // if no other hiding spot is closer, go into chase
    }
    void ChasingBehavior()
    {
        // make it so it takes some time to reach max speed
        // run after closest prey
        // if loses line of sight, go to next one in line of sight, otherwise go to search?
        // if stamina out, slow down to wandering speed, go to last known poisition of prey
        // if captures prey, kill - x chance to succeed
        // if prey escapes, back to chase.
    }
    void KillBehavior()
    {
        // after kill, eat.
        // nice to have: drag prey into obscured area, or back home
        // after eating, go home/rest
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;  // FOV cone color
        Vector3 forward = transform.forward * sightDistance;

        // Draw field of view lines
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldofView / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldofView / 2, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawWireSphere(transform.position, sightDistance); // Show max sight range

        // Draw a ray toward the prey
        // if (prey != null)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawLine(transform.position, prey.position);
        // }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward * wanderDistance + wanderTarget, 1f);
        
    }

}
