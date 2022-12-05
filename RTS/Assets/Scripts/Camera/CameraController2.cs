using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController2 : MonoBehaviour
{
    [SerializeField]
    private float speed;
    [SerializeField]
    private float zoomSpeed;
    [SerializeField]
    private float rotationSpeed;

    [SerializeField]
    private float maxHeight;
    [SerializeField]
    private float minHeight;

    [SerializeField]
    private float maxForward;
    [SerializeField]
    private float maxBack;

    [SerializeField]
    private float maxLeft;
    [SerializeField]
    private float maxRight;

    private CameraControlActions cameraControlActions;
    private InputAction movement;
    private Transform cameraTransform;


    void Start()
    {
        cameraControlActions = new CameraControlActions();
        cameraTransform = Camera.main.transform;
    }

    
    void Update()
    {
        float horizontalSpeed = speed * Mouse.current.position.x.ReadValue();
        float verticalSpeed = speed * Mouse.current.position.y.ReadValue();
        float scrollSpeed = -zoomSpeed * Mouse.current.scroll.ReadValue().y;

        Vector3 verticalMove = new Vector3(0, scrollSpeed, 0);
        //Vector3 lateralMove = 0f;
    }
}
