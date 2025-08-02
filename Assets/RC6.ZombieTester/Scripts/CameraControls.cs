using UnityEngine;

public class CameraControls : MonoBehaviour
{
    [Tooltip("Transform to rotate around its local X axis when dragging mouse up/down.")]
    public Transform CameraRotateX;

    [Tooltip("Transform to rotate around its local Y axis when dragging mouse left/right.")]
    public Transform CameraRotateY;

    [Tooltip("Transform to move along its local Z axis when using the scroll wheel.")]
    public Transform CameraDolly;

    [Tooltip("Minimum local Z position for the CameraDolly transform.")]
    [SerializeField]
    private float dollyMin = 1.7f;

    [Tooltip("Maximum local Z position for the CameraDolly transform.")]
    [SerializeField]
    private float dollyMax = 5.6f;

    [Tooltip("Sensitivity for horizontal (Y-axis) rotation (degrees per pixel).")]
    [SerializeField]
    private float horizontalRotationSensitivity = 0.5f;

    [Tooltip("Sensitivity for vertical (X-axis) rotation (degrees per pixel).")]
    [SerializeField]
    private float verticalRotationSensitivity = 0.5f;

    [Tooltip("Sensitivity for scroll wheel dolly movement.")]
    [SerializeField]
    private float dollySensitivity = 2f;

    private Vector3 initialXRotation;
    private Vector3 initialYRotation;
    private bool isDragging;

    void Start()
    {
        // Store initial rotations to preserve Y and Z for X, and X and Z for Y
        if (CameraRotateX != null)
        {
            initialXRotation = CameraRotateX.localEulerAngles;
        }
        if (CameraRotateY != null)
        {
            initialYRotation = CameraRotateY.localEulerAngles;
        }
    }

    void Update()
    {
        // Handle mouse drag for rotation
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X") * horizontalRotationSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * verticalRotationSensitivity;

            if (CameraRotateX != null)
            {
                // Rotate CameraRotateX around local X axis, preserve Y and Z
                float newXAngle = CameraRotateX.localEulerAngles.x - mouseY;
                CameraRotateX.localEulerAngles = new Vector3(newXAngle, initialXRotation.y, initialXRotation.z);
            }

            if (CameraRotateY != null)
            {
                // Rotate CameraRotateY around local Y axis, preserve X and Z
                float newYAngle = CameraRotateY.localEulerAngles.y + mouseX;
                CameraRotateY.localEulerAngles = new Vector3(initialYRotation.x, newYAngle, initialYRotation.z);
            }
        }

        // Handle scroll wheel for dolly
        if (CameraDolly != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * dollySensitivity;
            Vector3 localPos = CameraDolly.localPosition;
            localPos.z = Mathf.Clamp(localPos.z - scroll, dollyMin, dollyMax);
            CameraDolly.localPosition = localPos;
        }
    }

    void OnValidate()
    {
        // Ensure min and max dolly values are sensible
        if (dollyMin > dollyMax)
        {
            dollyMax = dollyMin;
        }
    }
}