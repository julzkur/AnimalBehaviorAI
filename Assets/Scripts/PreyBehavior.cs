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
        // check for predator at an interval
        CheckForPredator();
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
    }
    void AlertBehavior()
    {
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
    {
        // rotate line of sight, trigger
        // add resting ?
    }
}
