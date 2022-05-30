using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Am decis să fie combined mesh pentru că salva 10 batches
public class StreetLampController : MonoBehaviour
{
    [Header("Refferences")]
    [SerializeField] GameController gameController;

    [Header("Lamp Refference")]
    [Tooltip("Combined Mashes Here")]
    [SerializeField] Transform[] lampParents;

    MeshRenderer[] lampMeshRenderers;

    void Start() {
        initialize();
        if(gameController == null){
            #if UNITY_EDITOR
                Debug.LogError($"Missing Game Controller: { this.name }");
            #endif
                return;
        }

        setRendererState(false);
        gameController.OnDayTimeChange += toggleRenderers;
    }
    void toggleRenderers(DayTime dayTime){
        switch(dayTime){
            case DayTime.Day: setRendererState(false); break;
            case DayTime.Night: setRendererState(true); break;
        }
    }
    void setRendererState(bool state){
        int rendererLenght = lampMeshRenderers.Length;
        if(rendererLenght <= 0) {
            #if UNITY_EDITOR
            Debug.LogError($"Missing Lamp Mesh Renderers => setRendererState(bool) { this.name } ");
            #endif
            return;
        }

        for (int i = 0; i < rendererLenght; i++)
        {
            if(lampMeshRenderers[i] == null) {
                #if UNITY_EDITOR
                Debug.LogError($"Missing Lamp Mesh Instance => setRendererState(bool) { this.name } ");
                #endif
                return;
            }

            lampMeshRenderers[i].enabled = state;
        }
    }
    void initialize(){
        if(lampParents.Length <= 0) {
            #if UNITY_EDITOR
                Debug.LogError($"Missing Lamp Parents: { this.name } ");
            #endif
            return;
        }

        int lampParentLeght = lampParents.Length;
        lampMeshRenderers = new MeshRenderer[lampParentLeght];

        for (int i = 0; i < lampParentLeght; i++)
        {
            lampMeshRenderers[i] = lampParents[i].GetComponent<MeshRenderer>();
        }
    }
}
