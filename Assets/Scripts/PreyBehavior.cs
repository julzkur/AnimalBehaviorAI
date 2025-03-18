using UnityEngine;

public class PreyBehavior : Animal
{
    public enum PreyState { Grazing, Herding, Alert, Fleeing }
    public PreyState preyState;

    public Transform predator;
    public Transform herdLeader;
    public float fleeDistance = 10f;
    public float alertDistance = 20f;
    public float grazingRadius = 5f;

    protected override void Start()
    {
        base.Start();
        preyState = PreyState.Grazing;
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

        CheckForPredator();
    }

    void HerdingBehavior()
    {

    }
    void GrazingBehavior()
    {
        
    }
    void AlertBehavior()
    {
        
    }
    void FleeBehavior()
    {
        
    }

    void CheckForPredator()
    {
        
    }
}
