using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PG 
{
    public class DisappearingText :MonoBehaviour
    {
        public TextMeshPro Text;
        public float DisappearTime = 3;
        public DisappearAction Action;

        Coroutine DisappearCoroutine;

        private void OnTriggerEnter (Collider other)
        {
            if (DisappearCoroutine == null && Action == DisappearAction.OnTriggerEnter)
            {
                DisappearCoroutine = StartCoroutine (OnDisappear());
            }
        }

        private void OnTriggerExit (Collider other)
        {
            if (DisappearCoroutine == null && Action == DisappearAction.OnTriggerExit)
            {
                DisappearCoroutine = StartCoroutine (OnDisappear ());
            }
        }

        IEnumerator OnDisappear ()
        {
            var timer = DisappearTime;
            float normalizeTime;
            Color color;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                normalizeTime = timer / DisappearTime;
                color = Text.color;
                color.a = normalizeTime;
                Text.color = color;
                yield return null;
            }
            
            Destroy (gameObject);
        }

        public enum DisappearAction
        {
            OnTriggerEnter,
            OnTriggerExit,
        }
    }
}
