using UnityEngine;

public class VehicleCamera : MonoBehaviour, IVehicleCamera
{
    [SerializeField] GameObject[] cameras;
    int index;
    int maxIndex;

    GameObject currentCam;
    void Awake()
    {
        if (cameras.Length <= 0) return;

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].SetActive(false);
        }

        currentCam = cameras[0];
        maxIndex = cameras.Length - 1;
        index = 0;
    }

    public void SetNextCamera(){
        currentCam.SetActive(false);
        cameras[index].SetActive(true);
        currentCam = cameras[index];

        index++;

        if (index > maxIndex)
            index = 0;
    }
    public void DisableCameraSystem(){
        currentCam.SetActive(false);
        index = 0;
    }

    public void EnableCameraSystem(){
        currentCam = cameras[index];
        currentCam.SetActive(true);
        index++;
    }

    public GameObject GetActiveCam()
    {
        return currentCam;
    }
}