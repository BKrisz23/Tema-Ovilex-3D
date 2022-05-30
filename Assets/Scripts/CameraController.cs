using UnityEngine;
using Unity.Mathematics;
/// Controleză camera
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour {
    Transform m_Transform;
    Camera m_camera;
    Transform targetT;

    readonly float xBoundPercent = .25f;
    readonly float yBoundPercent = .4f;
    float deltaX;
    float deltaY;

    [Header("Config")]
    [SerializeField] [Range(1f,5f)] float rotationMultiplier = 1f;
    [SerializeField] [Range(1f,20f)] float singleStep = 5f;
    [SerializeField] [Range(1f,20f)] float minMagnitude = 1f;
    [SerializeField] int maximumXrotation = 30;

    Vector3 screenCenter;
    Vector3 initialTouchPosition;
    Vector3 currentTouchPosition;
    Vector2 swipeDirection;
    Vector3 remapedInput;
    Vector3 nextRotation;
    float magnitude;
    float rotationMagnitude;
    Quaternion initialRotation;

    //clamp min max

    bool isMoving;
    void Start() {
        m_Transform = this.transform;
        m_camera = m_Transform.GetComponent<Camera>();
        targetT = new GameObject("Cam Target").transform;
        targetT.localRotation = Quaternion.Euler(15f,0f,0f);
        Transform parentT = m_Transform.GetTopmostParentComponent<Transform>();
        targetT.SetParent(parentT,false);
        m_Transform.SetParent(targetT,false);

        deltaX = m_camera.pixelWidth * xBoundPercent;
        deltaY = m_camera.pixelHeight * yBoundPercent;
    }

    void Update() {
        int touchCount = Input.touchCount;
        if(touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);

        setScreenCenter();

        if(touch.phase == TouchPhase.Began) {
            initialTouchPosition = touch.position;
            currentTouchPosition = initialTouchPosition;
            initialRotation = targetT.rotation;
            isMoving = true;
        }else if(touch.phase == TouchPhase.Moved){
            currentTouchPosition = touch.position;
        }else if(touch.phase == TouchPhase.Ended){
            initialRotation = Quaternion.identity;
            initialTouchPosition = Vector3.zero;
            currentTouchPosition = Vector3.zero;
            swipeDirection = Vector3.zero;
            remapedInput = Vector3.zero;
            magnitude = 0;
            isMoving = false;
        }

        if(!isInBounds(initialTouchPosition)) isMoving = false;
        if(!isInBounds(currentTouchPosition)) isMoving = false;

        if(!isMoving) return;


        swipeDirection = (currentTouchPosition - initialTouchPosition).normalized;
        magnitude = (currentTouchPosition - initialTouchPosition).magnitude;

        if(magnitude < minMagnitude) return;

        remapedInput = new Vector3(-swipeDirection.y, swipeDirection.x, 0f) * rotationMultiplier * magnitude;
        nextRotation = math.lerp(initialRotation.eulerAngles, initialRotation.eulerAngles + remapedInput, singleStep * Time.fixedDeltaTime);
        
        nextRotation.x = math.clamp(nextRotation.x, 0, 90);

        Quaternion rotation = Quaternion.Euler(nextRotation);
        targetT.rotation = rotation;
    }
    bool isInBounds(Vector3 inputPosition){ //refaktor unnecesary variables
        bool xAxis = false;
        bool yAxis = false;
        if(inputPosition.x <= screenCenter.x && inputPosition.x > screenCenter.x - deltaX ||
           inputPosition.x > screenCenter.x && inputPosition.x < screenCenter.x + deltaX) xAxis = true;
        
        if(inputPosition.y <= screenCenter.y && inputPosition.y > screenCenter.y - deltaY ||
           inputPosition.y > screenCenter.y && inputPosition.y < screenCenter.y + deltaY) yAxis = true;
           
        if(xAxis && yAxis) return true;
        return false;
    }
    void setScreenCenter(){
        screenCenter = new Vector3(m_camera.pixelWidth * .5f ,m_camera.pixelHeight *.5f, m_Transform.position.z);
    }
}