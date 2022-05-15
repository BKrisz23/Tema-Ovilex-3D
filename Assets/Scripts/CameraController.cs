using UnityEngine;
/// Controleză camera
public class CameraController : MonoBehaviour {
    /// Referință și valori legate de urmarirea vehicului
    [Header("Follow Target")]
    [SerializeField] Vector3 carFollowOffset;
    [SerializeField] Vector3 truckFollowOffset;
    Vector3 followOffset;
    [SerializeField] float followSpeed = 5f;
    Transform target;
    Transform cameratT;

    void Start() {
        cameratT = Camera.main.transform;
        followOffset = carFollowOffset;
    }
    /// Actualizează poziția camerei
    void FixedUpdate() {
        if(target == null) return;
        cameratT.position = getCameraPosition(target);
    }
    /// Setează targetul pentru urmarire
    public void SetFollowTarget(Transform t){
        if(t == null) return;
        target = t;
    }
    /// Logica de urmărire
    Vector3 getCameraPosition(Transform target){
        Vector3 result = Vector3.zero;
        Vector3 currentPosition = cameratT.position;
        Vector3 nextPosition = target.position + followOffset;
        
        float timeStep = followSpeed * Time.deltaTime;
        result.x = Mathf.Lerp(currentPosition.x, nextPosition.x, timeStep);
        result.y = Mathf.Lerp(currentPosition.y, nextPosition.y, timeStep);
        result.z = Mathf.Lerp(currentPosition.z, nextPosition.z, timeStep);

        return result;
    }
    /// Setează offsettul pentru truck
    public void SetFollowOffsetTruck(){
        followOffset = truckFollowOffset;
    }
    //// Setează offsettul pentru car și moto
    public void SetFollowOffsetCar(){
        followOffset = carFollowOffset;
    }
}