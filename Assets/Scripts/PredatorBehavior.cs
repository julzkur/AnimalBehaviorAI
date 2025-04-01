using System.Collections;
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
    public float wanderRadius = 15f;
    public float wanderDistance = 10f;
    public float wanderingMoveSpeed = 4f;
    private Vector3 wanderTarget;
    public float rotationSpeed = 5f; // Speed of rotation to face the target direction
    public LayerMask obstacleLayerMask; // Layer mask for obstacles
    public float jitterAmount = 0.5f; // Amount of jitter to apply to the wander target
    



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

            nextDetectionTime = Time.time + detectionInterval;


            HandleState();
        }

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
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        agent.speed = wanderingMoveSpeed;
        agent.isStopped = false;

        Debug.Log("Wandering...");

        activeCoroutine = StartCoroutine(WanderRoutine());
    }

    IEnumerator WanderRoutine()
    {
        while (predatorState == PredatorState.Wandering)
        {
            Vector3 wanderDirection = transform.forward * wanderDistance;
            Vector3 jitter = new Vector3(Random.Range(-jitterAmount, jitterAmount), 0, Random.Range(-jitterAmount, jitterAmount));
            Vector3 targetPosition = transform.position + wanderDirection + jitter;

            Vector3 avoidance = AvoidObstacles();
            targetPosition += avoidance * 3f;

            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                Debug.Log("Target position not on NavMesh, adjusting...");
                if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    Vector3 AvoidObstacles()
    {
        Debug.Log("Avoiding obstacles...");
        RaycastHit hit;
        Vector3 avoidance = Vector3.zero;
        float detectionDistance = 10f; 

        if (Physics.Raycast(transform.position, transform.forward, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance += Vector3.Reflect(transform.forward, hit.normal);
            Debug.Log("Obstacle ahead! Steering away.");
        }

        if (Physics.Raycast(transform.position, transform.right, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance -= transform.right * 1.5f; 
            Debug.Log("Obstacle detected on the right! Steering left.");
        }

        if (Physics.Raycast(transform.position, -transform.right, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance += transform.right * 1.5f; 
            Debug.Log("Obstacle detected on the left! Steering right.");
        }

        return avoidance.normalized; 
    }

    
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
        Gizmos.DrawWireSphere(transform.position, sightDistance); 


        // Draw a ray toward the prey
        // if (prey != null)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawLine(transform.position, prey.position);
        // }


        
    }

}
