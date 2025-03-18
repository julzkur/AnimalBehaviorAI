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
        agent.SetDestination(destination);
    }
}
