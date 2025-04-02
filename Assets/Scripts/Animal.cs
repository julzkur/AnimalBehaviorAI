using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Animal : MonoBehaviour
{
    protected NavMeshAgent agent;
    public enum AnimalState { Idle, Moving, Dead }
    public AnimalState currentState;


    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent is missing on " + gameObject.name);
        }
        currentState = AnimalState.Idle;
    }

    protected virtual void Update()
    {

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

    protected int GetRandomIndex()
    {
        return Random.Range(0, 5);  // Get a random index between 0 and 5
    }
}
