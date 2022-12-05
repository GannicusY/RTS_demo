using UnityEngine.InputSystem;
using UnityEngine;
using ExtensionMethods;

public class CameraController : MonoBehaviour
{
    [Header("Horizontal Translation")]
    [SerializeField] private float smoothTime = 0.5f;
    [SerializeField] private float speedDivider = 50f;
    [SerializeField] private float maxLeft = 0f;
    [SerializeField] private float maxRight = 10000f;
    [SerializeField] private float maxFoward = 10000f;
    [SerializeField] private float maxBack = 0f;
    
    [Header("Zoom")]
    [SerializeField] private float initialStepSize = 2f;    
    [SerializeField] private float zoomDampingTime = 0.5f;
    [SerializeField] private float minHeight = 1f;    
    [SerializeField] private float maxHeight = 2000f;
    [SerializeField] private float zoomExponention = 1.07f;
    [SerializeField] private float zoomZAxisDisplacement = 5f;

    [Header("Rotation")]
    [SerializeField] private float maxRotationSpeed = 3f;

    [Header("Edge Movement")]
    [SerializeField] [Range(0f, 0.1f)] private float edgeTolerance = 0.05f;

    private CameraControlActions cameraControlActions;
    private InputAction movement;
    private Transform cameraTransform;

    private float zoomHeight;
    private float cachedZoomHeight;
    private float stepSize;
    private Vector3 zoomTarget;
    private Vector3 verticalZoomVelocity = Vector3.zero;

    private Vector3 targetPosition;
    private Vector3 targetVelocity = Vector3.zero;

    private Quaternion targetRotation;
    private Vector3 rotationVelocity = Vector3.zero;

    private void Awake()    
    {
        cameraControlActions = new CameraControlActions();
        cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        zoomHeight = minHeight;
        targetPosition = transform.position;

        cameraTransform.LookAt(this.transform);
        zoomTarget = new Vector3(cameraTransform.localPosition.x, zoomHeight, cameraTransform.localPosition.z - zoomHeight);
        stepSize = initialStepSize;
        
        movement = cameraControlActions.CameraActionMap.Movement;
        cameraControlActions.CameraActionMap.Rotation.performed += RotateCamera;
        cameraControlActions.CameraActionMap.Zoom.performed += ZoomCamera;
        cameraControlActions.CameraActionMap.Enable();
    }

    private void OnDisable()
    {
        cameraControlActions.CameraActionMap.Rotation.performed -= RotateCamera;
        cameraControlActions.CameraActionMap.Zoom.performed -= ZoomCamera;
        cameraControlActions.CameraActionMap.Disable();
    }

    private void Update()
    {
        GetKeyboardMovement();
        CheckMouseAtScreenEdge();
    }

    private void LateUpdate()
    {
        UpdateCameraRigPosition();
        UpdateCameraRigRotation();
        UpdateCameraPosition();
    }
    
    private void ZoomCamera(InputAction.CallbackContext callbackContext)
    {
        float inputValue = -callbackContext.ReadValue<Vector2>().y / 100f;
        
        cachedZoomHeight = zoomHeight;
        
        if (inputValue > 0.1f)
        {
            stepSize = Mathf.Pow(stepSize, zoomExponention);
            
            zoomHeight += inputValue * stepSize;
        }
            
        else if (inputValue < -0.1f)
        {
            zoomHeight += inputValue * stepSize;

            stepSize = Mathf.Pow(stepSize, 1f / zoomExponention);
        }

        if (zoomHeight < minHeight)
        {
            stepSize = initialStepSize;
            zoomHeight = minHeight;
        }
            
        else if (zoomHeight > maxHeight)
        {
            stepSize = Mathf.Pow(stepSize, 1f / zoomExponention);
            zoomHeight = cachedZoomHeight;
        }

        
        if (callbackContext.ReadValue<Vector2>().y > 0)
        {
            CalculateCameraRigPositionUpdateAfterZoomDown();
        }

        float targetZPosition = CalculateTargetZPosition();

        zoomTarget = new Vector3(cameraTransform.localPosition.x, zoomHeight, targetZPosition);
    }

    private float CalculateTargetZPosition()
    {
        float targetZPosition = cameraTransform.localPosition.z - zoomZAxisDisplacement * (zoomHeight - cameraTransform.localPosition.y);

        return targetZPosition;
    }

    private void CalculateCameraRigPositionUpdateAfterZoomDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 desiredPosition = hit.point;

            float distanceFromCameraRigToMouseCursor = Vector3.Distance(transform.position, desiredPosition);

            float distanceFromCameraToCameraRig = Vector3.Distance(Vector3.zero, cameraTransform.localPosition);

            float targetZPosition = CalculateTargetZPosition();

            Vector3 updatedCameraLocalPosition = new Vector3(cameraTransform.localPosition.x, zoomHeight,
                targetZPosition);

            float updatedDistanceFromCameraToCameraRig = Vector3.Distance(Vector3.zero, updatedCameraLocalPosition);

            float cameraRigShiftDistance = updatedDistanceFromCameraToCameraRig * distanceFromCameraRigToMouseCursor / distanceFromCameraToCameraRig;

            float updatedDistanceFromCameraRigToMouseCursor = distanceFromCameraRigToMouseCursor - cameraRigShiftDistance;

            float lambda = updatedDistanceFromCameraRigToMouseCursor / cameraRigShiftDistance;

            targetPosition.x = (transform.position.x + lambda * desiredPosition.x) / (1f + lambda);
            targetPosition.z = (transform.position.z + lambda * desiredPosition.z) / (1f + lambda);
        }

        else
        {
            targetPosition.x = transform.position.x;
            targetPosition.z = transform.position.z;
        }
    }

    private void RotateCamera(InputAction.CallbackContext callbackContext)
    {
        if (!Mouse.current.middleButton.isPressed) return;               

        float inputValueX = callbackContext.ReadValue<Vector2>().x;
        float inputValueY = callbackContext.ReadValue<Vector2>().y;
        
        float angle = transform.rotation.eulerAngles.x;
        angle = (angle > 180) ? angle - 360 : angle;

        float value = angle + inputValueY * maxRotationSpeed;
                
        if (value <= -90f || value >= 90f) inputValueY = 0f;
        
        targetRotation = Quaternion.Euler(inputValueY * maxRotationSpeed + transform.rotation.eulerAngles.x,
         inputValueX * maxRotationSpeed + transform.rotation.eulerAngles.y, 0f);
    }
    
    private void GetKeyboardMovement()
    {
        Vector3 inputValue = movement.ReadValue<Vector2>().x * GetCameraRight() + movement.ReadValue<Vector2>().y * GetCameraForward();

        inputValue = inputValue.normalized;

        if (inputValue.sqrMagnitude > 0.1f) targetPosition += inputValue * Mathf.Log(25f * zoomHeight + 0.1f, 7f) * Mathf.Sqrt(10f * zoomHeight) / speedDivider;
    }    

    private void CheckMouseAtScreenEdge()
    {        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;
                
        if (mousePosition.x < edgeTolerance * Screen.width) moveDirection -= GetCameraRight();
        else if (mousePosition.x > (1f - edgeTolerance) * Screen.width) moveDirection += GetCameraRight();
                
        if (mousePosition.y < edgeTolerance * Screen.height) moveDirection -= GetCameraForward();
        else if (mousePosition.y > (1f - edgeTolerance) * Screen.height) moveDirection += GetCameraForward();
                
        targetPosition += moveDirection * Mathf.Log(25f * zoomHeight + 0.1f, 7f) * Mathf.Sqrt(10f * zoomHeight) / speedDivider;
    }

    private void UpdateCameraRigPosition()
    {
        if (targetPosition.x < maxLeft) targetPosition = new Vector3(maxLeft, targetPosition.y, targetPosition.z);
        
        if (targetPosition.x > maxRight) targetPosition = new Vector3(maxRight, targetPosition.y, targetPosition.z);
        
        if (targetPosition.z < maxBack) targetPosition = new Vector3(targetPosition.x, targetPosition.y, maxBack);
        
        if (targetPosition.z > maxFoward) targetPosition = new Vector3(targetPosition.x, targetPosition.y, maxFoward);
        
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref targetVelocity, smoothTime);
    }

    private void UpdateCameraRigRotation()
    {
        transform.rotation = transform.rotation.SmoothDamp(targetRotation, ref rotationVelocity, 0.5f);
    }

    private void UpdateCameraPosition()
    {
        cameraTransform.localPosition = Vector3.SmoothDamp(cameraTransform.localPosition, zoomTarget, ref verticalZoomVelocity, zoomDampingTime);
    }

    private Vector3 GetCameraForward()
    {
        Vector3 forward = (cameraTransform.forward + cameraTransform.up) * 0.75f;
        forward.y = 0f;
        return forward;
    }

    private Vector3 GetCameraRight()
    {
        Vector3 right = cameraTransform.right;
        right.y = 0f;
        return right;
    }
}

