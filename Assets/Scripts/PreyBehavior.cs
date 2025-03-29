using UnityEngine;
using UnityEngine.AI;

public class PreyBehavior : Animal
{
    
    public enum PreyState { Idle, Grazing, Herding, Alert, Fleeing }
    [Header("Setup")]
    public PreyState preyState;
    public Transform predator;
    public Transform herdLeader;

    [Header("Grazing Behavior")]
    public float grazingRadius = 10f;
    public float grazingMoveSpeed = 2f;  // Speed for moving around
    public float grazingTime = 5f; // Time spent eating before moving to a new spot
    private float lastGrazingTime = 0f;
    private Vector3 grazingPoint;   // Current grazing point

    [Header("Predator Detection")]
    public float fieldofView = 90f;
    public float sightDistance = 20f;
    public float predatorCheckInterval = 5f;
    private float lastPredatorCheckTime = 0f;
    

    [Header("Alert Behavior")]
    
    public float alertDistance = 20f;
    public float alertTime = 5f; // move away walking once this time passes and predator does not get closer

    public float fleeDistance = 10f;
    public float migratingMoveSpeed = 4f;
    public float fleeingSpeed = 8f;


    protected override void Start()
    {
        base.Start();
        preyState = PreyState.Grazing;
        predator = GameObject.Find("Predator").transform;
        herdLeader = GameObject.Find("HerdLeader").transform;
    }

    protected override void HandleState()
    {
        switch (preyState)
        {
            case PreyState.Grazing:
                GrazingBehavior();
                break;
            case PreyState.Herding:
                HerdingBehavior();
                break;
            case PreyState.Alert:
                AlertBehavior();
                break;
            case PreyState.Fleeing:
                FleeBehavior();
                break;
        }
    }

    void HerdingBehavior()
    {
        // if leader moves too far away, follow leader
        // if is baby, follow mom
        // if mom is dead, follow leader
        // if mom and leader is out of sight, follow closest herd member
        // if herd leader is dead, next in line becomes herd leader.
    }
    void GrazingBehavior()
    {
        // idle behavior, at intervals, do animation for grazing, walking x meters to new graze spot.
        // if is baby, stay with mom, interval drinking from mom up to age x
        // start transitioning between drinking/grazing
        // if is leader, add function to check on herd.

        agent.speed = grazingMoveSpeed;

        

        if (Time.time - lastGrazingTime > grazingTime)
        {
            grazingPoint = GetRandomPointInRadius();
            lastGrazingTime = Time.time;
            MoveTo(grazingPoint);
            agent.isStopped = false;
            Debug.Log("Moving to new grazing point: " + grazingPoint);
        }

        if (Vector3.Distance(transform.position, grazingPoint) < 2f)
        {
            agent.isStopped = true;
            Debug.Log("Reached grazing point: " + grazingPoint);

            if (Time.time - lastPredatorCheckTime > predatorCheckInterval)
            {
                lastPredatorCheckTime = Time.time;
                Debug.Log("I should check for predators!");
                CheckForPredator();
            }
        }
        // if (HasReachedDestination())
        // {
        //     // Play grazing animation
        //     Debug.Log("Grazing at: " + grazingPoint);
        // }
        
    }

    Vector3 GetRandomPointInRadius()
    {
        Vector3 randomDirection = Random.insideUnitSphere * grazingRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, grazingRadius, NavMesh.AllAreas);
        return hit.position;
    }
    
    void AlertBehavior()
    {
        Debug.Log("Alerting herd members!");
        // listen for alerts from other herd members
        // animation head up, standing still, looking for predator, rotating line of sight
        // if sees predator, alert other herd members
    }
    void FleeBehavior()
    {
        // if is predator is spotted, use predator location to flee away from it, preferably moving with herd leader
        // check for predator regularly
        // after certain distance from predator or until predator is no longer in line of sight, stop fleeing
        // go into alert mode for x amount of time
        // if is caught, fight (percentage chance to escape)
    }

    void CheckForPredator()
    /// <summary>
    /// Checks for the presence of a predator within a certain distance.
    /// If a predator is spotted, the prey enters Alert mode.
    /// If there is no predator in sight, the prey continues grazing.
    /// </summary>
    {
        Debug.Log("Checking for predator...");

        Vector3 directionToPredator = predator.position - transform.position;
        float angleToPredator = Vector3.Angle(transform.forward, directionToPredator);
        
        // check if predator is within field of view and distance
        if (angleToPredator < fieldofView / 2f && directionToPredator.magnitude < sightDistance)
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, directionToPredator.normalized, out hit, alertDistance))
            {
                if (hit.transform.CompareTag("Predator"))
                {
                    Debug.Log("Predator spotted! Alert!");
                    preyState = PreyState.Alert;
                }
                else 
                {
                    Debug.Log("No predator in sight, grazing...");
                    return;
                }
            }
        }
        Debug.Log("Predator spotted but not close enough to alert, grazing...");
        return;
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

        // Draw a ray toward the predator
        if (predator != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, predator.position);
        }
        
    }

}
