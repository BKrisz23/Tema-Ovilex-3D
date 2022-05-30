using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveRB : MonoBehaviour
{
    [SerializeField] Vector3 StartPosition = Vector3.zero;
    [SerializeField] Vector3 EndPosition = Vector3.zero;
    [SerializeField] float Speed = 10;
    [SerializeField] bool UseRBMovePosition;

    Vector3 GlobalStartPosition;
    Vector3 GlobalEndPosition;

    Vector3 TargetPosition;
    Rigidbody RB;

    private void Start ()
    {
        if (transform.parent)
        {
            GlobalStartPosition = transform.parent.TransformPoint (StartPosition);
            GlobalEndPosition = transform.parent.TransformPoint (EndPosition);
        }
        else
        {
            GlobalStartPosition = StartPosition;
            GlobalEndPosition = EndPosition;
        }
        

        RB = GetComponent<Rigidbody> ();
        RB.MovePosition (GlobalStartPosition);
        TargetPosition = GlobalEndPosition;
    }

    // Update is called once per frame
    private void FixedUpdate ()
    {

        if (RB.position == TargetPosition)
        {
            TargetPosition = TargetPosition == GlobalStartPosition ? GlobalEndPosition : GlobalStartPosition;
        }

        if (UseRBMovePosition)
        {
            RB.MovePosition (Vector3.MoveTowards (RB.position, TargetPosition, Time.fixedDeltaTime * Speed));
        }
        else
        {
            RB.position = Vector3.MoveTowards (RB.position, TargetPosition, Time.fixedDeltaTime * Speed);
        }

    }
}
