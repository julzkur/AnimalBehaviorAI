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


    [Header("Steering")]

    public float rotationSpeed = 5f; 
    public LayerMask obstacleLayerMask; 
    public float jitterAmount = 0.5f; 
    float preyPredictionFactor = 0.5f; // predicts prey movement
    float obstacleAvoidanceStrength = 2f;
    float stuckTimeThreshold = 2f;  // Time threshold to detect stuck state
    float stuckTime = 0f;
    

    [Header("Wandering Behavior")]
    public bool isWandering = false;
    public float wanderRadius = 15f;
    public float wanderDistance = 10f;
    public float wanderingMoveSpeed = 3f;
    private Vector3 wanderTarget;
    

    [Header("Prey Detection")]
    public float fieldofView = 120f;
    public float sightDistance = 25f;
    private float nextDetectionTime = 0f;
    public float detectionInterval = 0.2f;
    

    [Header("Stalking Behavior")]
    public float stalkingDistance = 25f;
    public float stalkingSpeed = 2f;
    public float stalkTime = 0f; 
    public bool isSpotted = false;
    public LayerMask preyLayerMask; 
    private Vector3 lastKnownPreyPosition; 


    [Header("Chasing Behavior")]
    public float chaseSpeed = 5f;
    public float staminaModifier = 1.5f;
    public float chaseDistance = 10f;
    public float stamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRecoveryRate = 5f;
    float timeSinceLastSeen = 0f;  
    float lostSightDuration = 3f;  // predator gives up and goes back to wandering after x seconds


    [Header("Kill Behavior")]
    public float killDistance = 8.5f;
    private float killChance = 0.5f;
    private float nextWanderUpdateTime = 0f; 
    public float wanderUpdateInterval = 2f; 

    protected override void Start()
    {
        base.Start();
        SetState(PredatorState.Wandering);
        wanderTarget = transform.position + Random.insideUnitSphere * wanderRadius;
        HandleState();
        
    }

    protected override void Update()
    {
        if (prey == null || !prey.gameObject.activeInHierarchy)
        {
            DetectPrey();
        }

        if (agent.velocity.sqrMagnitude < 0.1f)
        {
            stuckTime += Time.deltaTime;
            if (stuckTime > stuckTimeThreshold)
            {
                // Recalculate the path
                Vector3 newDestination = transform.position + Random.insideUnitSphere * wanderRadius;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(newDestination, out hit, 4f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    Debug.LogWarning("Failed to find a valid new destination on the NavMesh.");
                }
                stuckTime = 0f;
            }
        }
        else
        {
            stuckTime = 0f;  // Reset stuck time if agent is moving
        }

        if (Time.time >= nextDetectionTime)
        {
            nextDetectionTime = Time.time + detectionInterval;

            bool preyDetected = DetectPrey();

            switch (predatorState)
            {
                case PredatorState.Wandering:
                    if (preyDetected && !isSpotted && Vector3.Distance(transform.position, prey.position) < stalkingDistance)
                    {
                        isWandering = false;
                        StopAllCoroutines();
                        SetState(PredatorState.Stalking);
                        return; 
                    }
                    break;

                case PredatorState.Stalking:
                    if (preyDetected && isSpotted && Vector3.Distance(transform.position, prey.position) < stalkingDistance)
                    {
                        isSpotted = true; // Prey is now spotted
                        StopAllCoroutines();
                        SetState(PredatorState.Chasing);
                    }
                    break;

                case PredatorState.Chasing:
                    if (!preyDetected)
                    {
                        timeSinceLastSeen += Time.deltaTime;

                        if (timeSinceLastSeen > lostSightDuration)
                        {
                            Debug.Log("Lost sight of prey! Switching to wandering.");
                            SetState(PredatorState.Wandering);
                        }
                    }
                    else
                    {
                        timeSinceLastSeen = 0f;
                    }
                    break;

                    
            }
            if (Vector3.Distance(transform.position, prey.position) < chaseDistance && !isSpotted)
            {
                SetState(PredatorState.Chasing);
            }

            if (!preyDetected && predatorState != PredatorState.Wandering)
            {
                SetState(PredatorState.Wandering);
            }
        }
        
        if (Vector3.Distance(transform.position, prey.position) < stalkingDistance && prey.GetComponent<PreyBehavior>().preyState == PreyBehavior.PreyState.Fleeing)
        {
            isWandering = false;
            SetState(PredatorState.Chasing);
        }


    }

    void SetState(PredatorState newState)
    {
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

    bool DetectPrey()
    {
        // OverlapSphere for less performance-heavy detection, only shoots ray if prey is nearby
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sightDistance + 2f);
        Transform closestPrey = null;
        float closestDistance = sightDistance;

        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Prey"))
            {
                Vector3 directionToPrey = hit.transform.position - transform.position;
                float angleToPredator = Vector3.Angle(transform.forward, directionToPrey);

                // If prey is in FOV and within distance
                if (angleToPredator < fieldofView / 2f && directionToPrey.magnitude < closestDistance)
                {
                    RaycastHit rayHit;
                    if (Physics.Raycast(transform.position, directionToPrey.normalized, out rayHit, sightDistance))
                    {
                        if (rayHit.transform.CompareTag("Prey"))
                        {
                            closestPrey = rayHit.transform; 
                            closestDistance = directionToPrey.magnitude;
                            lastKnownPreyPosition = closestPrey.position; 
                        }
                    }
                }
            }
        }
        if (closestPrey != null)
        {
            prey = closestPrey;
            return true;
        }
        prey = null;
        return false;
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
        agent.acceleration = 8f;
        RecoverStamina();

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

            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
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
        RaycastHit hit;
        Vector3 avoidance = Vector3.zero;
        float detectionDistance = 10f; 

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

    
    void StalkingBehavior()
    {  
        Debug.Log("Stalking...");
        agent.speed = stalkingSpeed;

        Vector3 preyPosition = GetClosestPreyPosition();
        if (Vector3.Distance(agent.destination, preyPosition) > 0.5f) 
        {
            agent.SetDestination(preyPosition);
        }

        if (Vector3.Distance(transform.position, preyPosition) < chaseDistance)
        {
            SetState(PredatorState.Chasing);
        }
        
    }

    Vector3 GetClosestPreyPosition()
    {
        Collider[] preyInRange = Physics.OverlapSphere(transform.position, sightDistance, preyLayerMask);
        // if prey not in range, go to last known position of prey
        if (preyInRange.Length == 0)
        {
            return lastKnownPreyPosition; // Return current position if no prey is found
        }

        Transform closestPrey = null;
        float closestDistance = Mathf.Infinity;

        foreach (var preyCollider in preyInRange)
        {
            float distanceToPrey = Vector3.Distance(transform.position, preyCollider.transform.position);
            if (distanceToPrey < closestDistance)
            {
                closestDistance = distanceToPrey;
                closestPrey = preyCollider.transform;
            }
        }
        
        if (closestPrey != null)
        {
            prey = closestPrey; // Update prey reference
            lastKnownPreyPosition = closestPrey.position;
        }

        return lastKnownPreyPosition;
    }

    void ChasingBehavior()
    {
        Debug.Log("Chasing prey!");
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.acceleration = 10f;

        activeCoroutine = StartCoroutine(ChaseRoutine());
    }

    IEnumerator ChaseRoutine()
    {
        
        while (predatorState == PredatorState.Chasing)
        {

            if (stamina > 70f)
            {
                stamina -= staminaDrainRate * Time.deltaTime; // Recover stamina when above 50
                agent.speed = chaseSpeed + staminaModifier;  // Boost speed when stamina is not low
            }
            else if (stamina <= 70f && stamina > 30f)
            {
                stamina -= staminaRecoveryRate * Time.deltaTime; // Drain stamina when above 30
                agent.speed = chaseSpeed;  // Normal speed when stamina is low
            }
            else if (stamina <= 30f)
            {
                stamina += staminaDrainRate * Time.deltaTime;
                agent.speed = chaseSpeed / 2f;
            }

            stamina = Mathf.Clamp(stamina, 0f, 100f);

            Debug.Log("Stamina: " + stamina);

            Vector3 preyPosition = GetClosestPreyPosition();

            if (!DetectPrey())
            {
                agent.SetDestination(lastKnownPreyPosition); 
                timeSinceLastSeen += Time.deltaTime;

                if (timeSinceLastSeen > lostSightDuration)
                {
                    Debug.Log("Lost sight of prey! Switching to wandering.");
                    SetState(PredatorState.Wandering);
                    yield break; 
                }
            }
            else
            {
                timeSinceLastSeen = 0f; 
                lastKnownPreyPosition = preyPosition;
            }

            Vector3 preyVelocity = (preyPosition - lastKnownPreyPosition) / Time.deltaTime;
            Vector3 predictedPreyPosition = preyPosition + preyVelocity * preyPredictionFactor;

            Vector3 avoidance = AvoidObstacles();
            Vector3 targetPosition = predictedPreyPosition + avoidance * obstacleAvoidanceStrength;

            if (Vector3.Distance(agent.destination, targetPosition) > 0.5f) 
            {
                agent.SetDestination(targetPosition);
            }

            if (Vector3.Distance(transform.position, prey.position) <= killDistance)
            {
                Debug.Log("Prey caught! Rolling dice to see if predator kills it.");
                float randomValue = Random.Range(0f, 1f);
                if (randomValue < killChance)
                {
                    Debug.Log("Success!");
                    SetState(PredatorState.Kill);
                }
                else
                {
                    Debug.Log("Predator failed to kill the prey.");
                    yield return new WaitForSeconds(1f); // Wait for a second before trying again
                }
            }

            yield return null; 
        }

        // run after closest prey
        // add staminaModifier (1f) to speed for x seconds
        // if stamina out, slow down to chase speed until stamina recovers
        // if loses sight of prey (!DetectPrey()), go to last known position, if no prey, back to wandering
        // if distance to prey is less than killDistance, SetState(PredatorState.Kill);
    }
    void RecoverStamina()
    {
        if (stamina < 100f)
        {
            stamina += staminaRecoveryRate * Time.deltaTime;
        }
        else if (stamina > 100f)
        {
            stamina = 100f; // Clamp stamina to a maximum of 100
        }
        stamina = Mathf.Clamp(stamina, 0f, 100f); 
    }

    void KillBehavior()
    {
        Debug.Log("Killing prey!");
        prey.GetComponent<PreyBehavior>().Die();
        
        SetState(PredatorState.Wandering); 

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


        if (prey != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, prey.position);
        }


        
    }

}
