using System.Collections;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius;
    [Range(0, 360)]
    public float angle;

    public GameObject target;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public bool canSeeTarget;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Animal");
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            DetectTargetsinFOV();
        }
    }

    private void DetectTargetsinFOV()
    {
        Collider[] targetsInView = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (targetsInView.Length > 0)
        {
            Transform targetInView = targetsInView[0].transform;
            Vector3 directionToTarget = (targetInView.position - transform.position).normalized;

            if (Vector3.Angle(transform.position, directionToTarget) < angle/2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetInView.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleMask))
                {
                    canSeeTarget = true;
                    target = targetInView.gameObject;
                }
                else
                {
                    canSeeTarget = false;
                }
            }
            else
            {
                canSeeTarget = false;
            }
        }
        else if (target != null)
        {
            canSeeTarget = false;
        }
    }
}
