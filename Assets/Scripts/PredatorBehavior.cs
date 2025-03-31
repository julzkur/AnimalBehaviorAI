using UnityEngine;
using UnityEngine.AI;

public class PredatorBehavior : Animal
{
    public enum PredatorState { Idle, Wandering, Stalking, Chasing, Eating, Resting }
    public PredatorState predatorState;


    public Transform prey;
    public Transform hidingSpot;
    public Transform killSpot;
    public float killDistance;
    public float chaseDistance;

    protected override void Start()
    {
        base.Start();
        predatorState = PredatorState.Idle;
    }

    protected override void HandleState() {
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
            case PredatorState.Eating:
                EatingBehavior();
                break;
            case PredatorState.Resting:
                Resting();
                break;
        }
    }

    void WanderingBehavior()
    {
        // idly wandering set path, add function to check for prey
    }
    void StalkingBehavior()
    {
        // after spotting prey, if predator has not been spotted (if prey is not fleeing), hide
        // use objects to put between prey and predator, maybe add grass which predator
        // hides in and obscures view/hides predator
        // moves between hiding spots as close as possible, if next hiding spot is closer to prey
        // than last hiding spot, move (crouch) to next hiding spot.
        // if spotted, go into chase.
        // if no other hiding spot is closer, go into chase
    }
    void ChasingBehavior()
    {
        // run after closest OR youngest/smallest prey
        // if loses line of sight, go to next one
        // add stamina? if stamina out, slow down if within x distance, otherwise go back into stalking/tracking
        // if captures prey, kill - x chance to succeed
        // add kill function
        // if prey escapes, back to chase.
    }
    void EatingBehavior()
    {
        // after kill, eat.
        // nice to have: drag prey into obscured area, or back home
        // after eating, go home/rest
    }
    void Resting()
    {
        // resting? sitting
        // hunger bar
        // stamina bar
        // rest bar
    }
}
