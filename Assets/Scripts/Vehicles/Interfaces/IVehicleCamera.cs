using UnityEngine;
public interface IVehicleCamera
{
    void SetNextCamera();
    void DisableCameraSystem();
    void EnableCameraSystem();
    GameObject GetActiveCam();
}
