using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PreyBehavior : Animal
{
    
    public enum PreyState { Idle, Grazing, Herding, Alert, Fleeing }
    private Coroutine activeCoroutine;

    [Header("Setup")]
    public PreyState preyState;
    public Transform predator;
    public Transform herdLeader;
    

    [Header("Grazing Behavior")]

    public bool isGrazing = false;
    public float grazingRadius = 10f;
    public float grazingMoveSpeed = 2f;  // Speed for moving around
    public float grazingTime = 5f; // Time spent eating before moving to a new spot
    private float lastGrazingTime = 0f;
    private Vector3 grazingPoint;   // Current grazing point


    [Header("Predator Detection")]
    public float fieldofView = 90f;
    public float sightDistance = 20f;
    private float nextDetectionTime = 0f;
    public float detectionInterval = 0.2f;
    

    [Header("Alert Behavior")]
    public bool isAlert = false;
    public float alertDistance = 20f;
    public float alertTime = 0f; // move away walking once this time passes and predator does not get closer


    [Header("Flee Behavior")]
    public float fleeRadius = 10f;
    public float fleeingSpeed = 8f;


    [Header("Herding Behavior")]
    public float migratingMoveSpeed = 4f;


    protected override void Start()
    {
        base.Start();
        SetState(PreyState.Grazing);
        predator = GameObject.Find("Predator").transform;
        herdLeader = GameObject.Find("HerdLeader").transform;
        
    }

    protected override void Update()
    {
        base.Update();
        // continuous detection of predator (every 0.2 seconds for performance)
        if (Time.time >= nextDetectionTime && (preyState == PreyState.Grazing || preyState == PreyState.Herding))
        {
            nextDetectionTime = Time.time + detectionInterval;

            bool predatorDetected = DetectPredator();

            if (predatorDetected && preyState != PreyState.Alert)
            {
                isGrazing = false;
                StopAllCoroutines();
                SetState(PreyState.Alert);
                return; // Stop further processing
            }

            if (!predatorDetected && preyState == PreyState.Alert)
            {
                SetState(PreyState.Grazing);
            }
        }

        // listen for alerts from other herd members
    } 

    void SetState(PreyState newState)
    {
        if (preyState == newState) return;

        Debug.Log("State changed: " + preyState + " -> " + newState);

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        preyState = newState;

        HandleState();

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

    bool DetectPredator()
    {
        // OverlapSphere for less performance-heavy detection, only shoots ray if predator is nearby
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightDistance + 2f);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Predator"))
            {
                Vector3 directionToPredator = predator.position - transform.position;
                float angleToPredator = Vector3.Angle(transform.forward, directionToPredator);

                // If predator is in FOV and within distance
                if (angleToPredator < fieldofView / 2f && directionToPredator.magnitude < sightDistance)
                {
                    RaycastHit rayHit;
                    if (Physics.Raycast(transform.position, directionToPredator.normalized, out rayHit, alertDistance))
                    {
                        if (rayHit.transform.CompareTag("Predator"))
                        {
                            Debug.Log("Predator spotted! Alert!");
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }



    void GrazingBehavior()
    {
        // if is leader, add function to check on herd.

        agent.speed = grazingMoveSpeed;
        agent.angularSpeed = 120f;
        isGrazing = true;

        Debug.Log("Grazing...");
        activeCoroutine = StartCoroutine(GrazingRoutine());
    }

    IEnumerator GrazingRoutine()
    {
        while (preyState == PreyState.Grazing) // Keeps grazing in a loop
        {
            agent.isStopped = true; 
            
            yield return new WaitForSeconds(grazingTime); 

            yield return StartCoroutine(CheckForPredator());

            if (preyState != PreyState.Grazing)
            {
                Debug.Log("Stopping grazing.");
                isGrazing = false;
                yield break;
            }

            grazingPoint = GetRandomPointInRadius();
            Debug.Log("Moving to new grazing point: " + grazingPoint);
            MoveTo(grazingPoint); 
            agent.isStopped = false; 

            while (Vector3.Distance(transform.position, grazingPoint) > 1f)
            {
                yield return null; 
            }

            Debug.Log("Reached grazing point.");
        }
    }


    Vector3 GetRandomPointInRadius()
    {
        Vector3 randomDirection = Random.insideUnitSphere * grazingRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, grazingRadius, NavMesh.AllAreas);
        return hit.position;
    }

    IEnumerator CheckForPredator()

    {
        agent.isStopped = true;

        Debug.Log("Checking for predator...");

        // Rotations for scanning area in a 180 degree arc
        Quaternion originalRotation = transform.rotation;
        Quaternion leftRotation = Quaternion.Euler(0, -90, 0) * originalRotation;
        Quaternion rightRotation = Quaternion.Euler(0, 90, 0) * originalRotation;

  
        yield return RotateFOV(leftRotation);
        if (preyState == PreyState.Alert) yield break;

        yield return RotateFOV(rightRotation);
        if (preyState == PreyState.Alert) yield break;

        yield return RotateFOV(originalRotation);
        if (preyState == PreyState.Alert) yield break;

        agent.isStopped = false;
    }

    IEnumerator RotateFOV(Quaternion targetRotation)
    {
        float rotationDuration = 1f;
        float elapsedTime = 0;
        Quaternion startRotation = transform.rotation;

        while (elapsedTime < rotationDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationDuration);
            elapsedTime += Time.deltaTime;

            yield return null;
        }
        transform.rotation = targetRotation;
    }


    void AlertBehavior()
    {
        Debug.Log("Alerting herd members!");
        
        agent.isStopped = true;
        alertTime = 0f;
        isGrazing = false;
        isAlert = true;

        activeCoroutine = StartCoroutine(AlertRoutine());
        
    }

    IEnumerator AlertRoutine()
    {
        
        // rotate FOV straight towards predator and keep it there for 3 seconds
        Quaternion rotationTowPred = Quaternion.LookRotation(predator.position - transform.position);
        float rotationSpeed = 3f; // Adjust as needed
        while (Quaternion.Angle(transform.rotation, rotationTowPred) > 1f)
        {
            Debug.Log("Looking at predator...");
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationTowPred, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        while (alertTime < 3f) 
        {
            alertTime += Time.deltaTime;

            if (!DetectPredator()) 
            {
                Debug.Log("Predator out of sight. Returning to grazing.");

                preyState = PreyState.Grazing;
                agent.isStopped = false;
                yield break;
            }

            if (Vector3.Distance(transform.position, predator.position) < fleeRadius)
            {
                Debug.Log("Predator too close! FLEEING!");

                preyState = PreyState.Fleeing;
                agent.isStopped = false;
                yield break;
            }

            yield return null;
            // keep raycasting towards predator, FOV fixed towards predator
            // if predator does not move, migrate herd away from it slightly (with migrate speed)
            // if predator enters chase mode, enter Flee state
            // if predator moves, keep raycasting towards it until it is out of sight/out of Alert range
             // once predator is out of alert distance, stop alerting and return to Grazing state
        }
        
        Migrate();
    }

    void Migrate()
    {
        Debug.Log("Migrating away from predator... Just to be safe.");
        agent.isStopped = false;
        agent.speed = migratingMoveSpeed;

        Vector3 awayFromPredator = (transform.position - predator.position).normalized * agent.speed * Time.deltaTime;
        float distanceToPredator = Vector3.Distance(transform.position, predator.position);
        float bufferDistance = 5f;

        if (distanceToPredator < alertDistance + bufferDistance)
        {
            agent.Move(awayFromPredator);
        }
        else
        {
            Debug.Log("Preadtor out of range, stopping migration");
            StopCoroutine(AlertRoutine());
            preyState = PreyState.Grazing;
        }
    }

    // void AlertHerd()
    // {
    //     Collider[] herdMates = Physics.OverlapSphere(transform.position, herdAlertRadius, preyLayer);
    //     foreach (Collider mate in herdMates)
    //     {
    //         PreyAI otherDeer = mate.GetComponent<PreyAI>();
    //         if (otherDeer != null && otherDeer.preyState == PreyState.Grazing)
    //         {
    //             otherDeer.EnterAlertState();
    //         }
    //     }
    // }

    void FleeBehavior()
    {
        Debug.Log("Fleeing from predator!");
        agent.speed = fleeingSpeed;
        // if is predator is spotted, use predator location to flee away from it, preferably moving with herd leader
        // check for predator regularly
        // after certain distance from predator or until predator is no longer in line of sight, stop fleeing
        // go into alert mode for x amount of time
        // if is caught, fight (percentage chance to escape)
    }

        void HerdingBehavior() // Boids/Flocking behavior
    {
        // if leader moves too far away, follow leader
        // if is baby, follow mom
        // if mom is dead, follow leader
        // if mom and leader is out of sight, follow closest herd member
        // if herd leader is dead, next in line becomes herd leader.
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
