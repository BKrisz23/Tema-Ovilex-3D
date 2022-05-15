using UnityEngine;

/// Controlează lampa de pe stradă
public class Lamp : MonoBehaviour
{
    [SerializeField] MeshRenderer lampLight; // lumina
    [SerializeField] ParticleSystem starLight; //lumina rotundă

    void Start() {
        if(lampLight == null || starLight == null) return;

        GameController.OnDayTimeChange += toggleLampLight; //schimbă starea prin event
    }

    void toggleLampLight(DayTime dayTime){ //schimbă starea
        switch(dayTime){
            case DayTime.Day: lampLight.enabled = false;
                              starLight.Stop(); break;
            case DayTime.Night: lampLight.enabled = true;
                                starLight.Play(); break;
        }
    }
}
