using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof (TextMeshProUGUI))]
public class FPSCounter : MonoBehaviour
{
    [SerializeField] float UpdateTime = 0.3f;
    TextMeshProUGUI Text;
    
    int FpsCount= 0;
    float Timer = 0;

    void Start()
    {
        Text = GetComponent<TextMeshProUGUI> ();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Timer >= UpdateTime)
        {
            Text.text = (FpsCount / Timer).ToInt ().ToString();
            Timer = 0;
            FpsCount = 0;
        }
        else
        {
            Timer += Time.deltaTime;
            FpsCount++;
        }
    }
}
