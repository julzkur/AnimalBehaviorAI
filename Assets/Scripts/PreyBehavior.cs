using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PreyBehavior : Animal
{
    
    public enum PreyState { Grazing, Herding, Alert, Fleeing }
    private Coroutine activeCoroutine;

    [Header("Setup")]
    public PreyState preyState;
    public Transform predator;
    public Transform herdLeader;
    public LayerMask preyLayer;
    private bool isDead = false;

    [Header("Steering")]
    public float safeDistance = 25f; // Distance to keep from the predator
    public float jitterAmount = 0.5f; // Amount of random movement
    public LayerMask obstacleLayerMask; // Layer mask for obstacles


    [Header("Grazing Behavior")]

    public bool isGrazing = false;
    public float grazingRadius = 10f;
    public float grazingMoveSpeed = 1f;  // Speed for moving around
    public float grazingTime = 5f; // Time spent eating before moving to a new spot
    private Vector3 grazingPoint;   // Current grazing point


    [Header("Predator Detection")]
    public float fieldofView = 90f;
    public float sightDistance = 20f;
    private float nextDetectionTime = 0f;
    public float detectionInterval = 0.2f;
    private Vector3 lastKnownPredatorPosition = Vector3.zero;
    private float lastKnownPredatorDistance = 0f;
    

    [Header("Alert Behavior")]
    public bool isAlert = false;
    public float herdAlertRadius = 20f;
    public float alertDistance = 20f;
    public float alertTime = 0f; // move away walking once this time passes and predator does not get closer


    [Header("Flee Behavior")]
    public float fleeRadius = 15f;
    public float fleeingSpeed = 6f;
    public float avoidanceBlendFactor = 0.5f; // how much you want avoidance to affect fleeing



    [Header("Herding Behavior")]
    public float migratingMoveSpeed = 2f;


    protected override void Start()
    {
        base.Start();
        SetState(PreyState.Grazing);
        predator = GameObject.FindGameObjectWithTag("Predator").transform;
        //herdLeader = GameObject.Find("HerdLeader").transform;
        HandleState();
        
    }

    protected override void Update()
    {
        base.Update();
        // continuous detection of predator (every 0.2 seconds for performance)
        if (!isDead)
        {
            if (Time.time >= nextDetectionTime)
            {
                if (preyState == PreyState.Grazing || preyState == PreyState.Herding)
                {
                    nextDetectionTime = Time.time + detectionInterval;

                    bool predatorDetected = DetectPredator();

                    if (predatorDetected && preyState != PreyState.Alert)
                    {
                        isGrazing = false;
                        agent.isStopped = true;
                        StopAllCoroutines();
                        SetState(PreyState.Alert);
                        return; 
                    }

                    if (!predatorDetected && preyState == PreyState.Alert)
                    {
                        SetState(PreyState.Grazing);
                    }

                    if (Vector3.Distance(transform.position, predator.position) < fleeRadius)
                    {
                        isGrazing = false;
                        StopAllCoroutines();
                        SetState(PreyState.Fleeing);
                        return; 
                        
                    }
                }
                if (preyState == PreyState.Fleeing)
                {
                    AlertHerd();
                }
            }

            if (Time.time >= nextDetectionTime && preyState == PreyState.Alert && lastKnownPredatorDistance < fleeRadius)
            {
                Debug.Log("Predator too close! Switching to Fleeing state.");
                StopAllCoroutines();
                SetState(PreyState.Fleeing);
            }

        }
        else
        {
            // If the prey is dead, stop all coroutines and disable the NavMeshAgent
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }
    } 

    void SetState(PreyState newState)
    {
        if (preyState == newState) return;

        Debug.Log("Prey state changed: " + preyState + " -> " + newState);

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        fleeRadius = 15f;
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

                if (angleToPredator < fieldofView / 2f && directionToPredator.magnitude < sightDistance)
                {
                    RaycastHit rayHit;
                    if (Physics.Raycast(transform.position, directionToPredator.normalized, out rayHit, alertDistance))
                    {
                        if (rayHit.transform.CompareTag("Predator"))
                        {
                            predator = hit.transform;
                            lastKnownPredatorDistance = Vector3.Distance(transform.position, predator.position);
                            lastKnownPredatorPosition = predator.position;

                            predator.GetComponent<PredatorBehavior>().isSpotted = true;
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
        agent.speed = grazingMoveSpeed;
        agent.acceleration = 7f;
        agent.angularSpeed = 120f;
        isGrazing = true;

        Debug.Log("Grazing...");
        activeCoroutine = StartCoroutine(GrazingRoutine());
    }

    IEnumerator GrazingRoutine()
    {
        while (preyState == PreyState.Grazing)
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
        fleeRadius = 18f;
        
        AlertHerd();
        activeCoroutine = StartCoroutine(AlertRoutine());

    }

    void AlertHerd()
    {
        Collider[] herdMembers = Physics.OverlapSphere(transform.position, herdAlertRadius, preyLayer);
        foreach (Collider member in herdMembers)
        {
            PreyBehavior otherDeer = member.GetComponent<PreyBehavior>();
            if (otherDeer != null && otherDeer.preyState == PreyState.Grazing)
            {
                otherDeer.AlertedByOtherPrey();
            }
        }
    }

    void AlertedByOtherPrey()
    {
        Debug.Log("Alerted by other prey!");

        float alertTimer = 0f;
        if (preyState != PreyState.Alert)
        {
            // look at predator for 2 seconds and then enter alert state
            while (alertTimer < 2f && Vector3.Distance(transform.position, predator.position) > alertDistance)
            {
                alertTimer += Time.deltaTime;
                agent.isStopped = true;
                LookAtPredator();
                if (Vector3.Distance(transform.position, predator.position) < alertDistance)
                {
                    Debug.Log("OMG Predator! FLEEING!");
                    isAlert = false;
                    SetState(PreyState.Fleeing);
                    return;
                }
            }
            SetState(PreyState.Alert);
        }
    }

    IEnumerator AlertRoutine()
    {
        
        // rotate FOV straight towards predator and keep it there for 3 seconds
        LookAtPredator();

        while (alertTime < 3f) 
        {
            alertTime += Time.deltaTime;

            if (!DetectPredator()) 
            {
                Debug.Log("Predator out of sight. Returning to grazing.");
                SetState(PreyState.Grazing);
                agent.isStopped = false;
                yield break;
            }
            float distanceToPredator = Vector3.Distance(transform.position, predator.position);
            if (distanceToPredator < fleeRadius)
            {
                Debug.Log("Predator too close! FLEEING!");
                isAlert = false;
                SetState(PreyState.Fleeing);
                yield break;
            }

            yield return null;
            // keep raycasting towards predator, FOV fixed towards predator
            // if predator does not move, migrate herd away from it slightly (with migrate speed)
            // if predator enters chase mode, enter Flee state
            // if predator moves, keep raycasting towards it until it is out of sight/out of Alert range
             // once predator is out of alert distance, stop alerting and return to Grazing state
        }
        
        yield return MoveToSafeDistance();
    }

    IEnumerator MoveToSafeDistance()
    {
        Debug.Log("Migrating away from predator... Just to be safe.");

        agent.isStopped = false;
        agent.speed = migratingMoveSpeed;
        
        while (Vector3.Distance(transform.position, predator.position) < alertDistance)
        {
            Vector3 directionAwayFromPredator = (transform.position - predator.position).normalized;
            Vector3 targetPosition = transform.position + directionAwayFromPredator * alertDistance;

            MoveTo(targetPosition);

            Debug.Log("Moving further away...");

            // Wait a bit before checking again
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Safe distance reached. Returning to grazing.");
        isAlert = false;
        SetState(PreyState.Grazing);
    }

    void FleeBehavior()
    {
        Debug.Log("Fleeing from predator!");
        agent.speed = fleeingSpeed;
        agent.isStopped = false;
        agent.acceleration = 10f;
        predator.GetComponent<PredatorBehavior>().isSpotted = true;

        StartCoroutine(FleeRoutine());
    
    }

    IEnumerator FleeRoutine()
    {

        while (Vector3.Distance(transform.position, predator.position) < safeDistance)
        {
            Vector3 fleeDirection = (transform.position - predator.position).normalized;
            Vector3 jitter = new Vector3(Random.Range(-jitterAmount, jitterAmount), 0, Random.Range(-jitterAmount, jitterAmount));
            Vector3 targetPosition = transform.position + fleeDirection * 10f + jitter;

            Vector3 avoidance = AvoidObstacles();
            Vector3 blendedDirection = fleeDirection + avoidance * avoidanceBlendFactor;
            targetPosition = transform.position + blendedDirection * 7f;

            // Ensure target position is on the NavMesh
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                Debug.Log("Flee target position not on NavMesh, adjusting...");
                if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("Safe distance reached.");
        //look towards predator for a moment to make sure he's not following
        LookAtPredator();
        yield return new WaitForSeconds(2f);
        Debug.Log("Predator not following. Alert mode activated.");
        SetState(PreyState.Alert);
    }

    void LookAtPredator()
    {
        Quaternion rotationTowPred = Quaternion.LookRotation(predator.position - transform.position);
        float rotationSpeed = 3f; 

        transform.rotation = Quaternion.Slerp(transform.rotation, rotationTowPred, Time.deltaTime * rotationSpeed);
    }

    Vector3 AvoidObstacles()
    {
        RaycastHit hit;
        Vector3 avoidance = Vector3.zero;
        float detectionDistance = 7f; 

        if (Physics.Raycast(transform.position, transform.forward, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance += Vector3.Reflect(transform.forward, hit.normal);
        }

        if (Physics.Raycast(transform.position, transform.right, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance -= transform.right * 1.5f;
        }

        if (Physics.Raycast(transform.position, -transform.right, out hit, detectionDistance, obstacleLayerMask))
        {
            avoidance += transform.right * 1.5f;
        }

        return avoidance.normalized;
    }

    public void Die()
    {
        Debug.Log("Prey is dead!");
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false; // Disable NavMeshAgent to stop movement
        gameObject.SetActive(false); // Deactivate the prey object
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
