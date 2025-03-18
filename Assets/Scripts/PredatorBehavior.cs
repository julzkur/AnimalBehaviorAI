using UnityEngine;

public class PredatorBehavior : Animal
{
    public enum PredatorState { Idle, Wandering, Stalking, Chasing, Eating }
    public PredatorState predatorState;

    protected override void Start()
    {
        base.Start();
        predatorState = PredatorState.Idle;
    }
}
