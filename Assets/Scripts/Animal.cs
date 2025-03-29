using UnityEngine;
using UnityEngine.AI;

public abstract class Animal : MonoBehaviour
{
    protected NavMeshAgent agent;
    protected enum AnimalState { Idle, Moving }
    protected AnimalState currentState;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = AnimalState.Idle;
    }

    protected virtual void Update()
    {
        HandleState();
    }

    protected abstract void HandleState();

    protected void MoveTo(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);  // Set destination for movement
            currentState = AnimalState.Moving;   // Update state to Moving
        }
    }

    protected void StopMoving()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            currentState = AnimalState.Idle;  // Set state to Idle when stopped
        }
    }

    protected bool HasReachedDestination()
    {
        if (agent != null)
        {
            return agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending;
        }
        return false;
    }
}
