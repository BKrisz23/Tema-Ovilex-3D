using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class SetCOMRB :MonoBehaviour
    {
        public Vector3 ComPos;
        // Start is called before the first frame update
        void Start ()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.centerOfMass = ComPos;
            }
            else
            {
                Debug.LogErrorFormat ("[{0}.SetCOMRB] Without Rigidbody", name);
                Destroy (this);
            }
        }

        private void OnDrawGizmosSelected ()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere (transform.TransformPoint(ComPos), 0.3f);
        }
    }
}
