using UnityEngine;
using UnityEngine.EventSystems;
using Oculus.Platform; // If you're using OVRInput (adjust namespace if different)

public class UIGrabHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Transform rootCanvas; // The whole menu panel to move
    public OVRInput.Button grabButton = OVRInput.Button.Three; // X button on left controller by default

    private bool isDragging = false;
    private Vector3 initialControllerPos;
    private Vector3 initialCanvasPos;
    private Transform controllerTransform;

    void Update()
    {
        if (isDragging)
        {
            // Get current controller position
            Vector3 currentControllerPos = controllerTransform.position;

            // Calculate delta movement in world space
            Vector3 delta = currentControllerPos - initialControllerPos;

            // Move the canvas/menu panel by that delta
            rootCanvas.position = initialCanvasPos + delta;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // We only start dragging if X button is currently held down (on left or right controller)
        if (IsGrabButtonHeld())
        {
            isDragging = true;

            // Find the controller that triggered this pointer event
            controllerTransform = eventData.pointerPressRaycast.gameObject.transform; // fallback

            // Better approach: get controller transform from eventData.pointerDrag or eventData.pointerEnter
            // But safest is to manually assign controller transforms in your raycast script and pass as pointer data
            // For now, just fallback to main camera if not found:
            if (controllerTransform == null)
                controllerTransform = Camera.main.transform;

            initialControllerPos = controllerTransform.position;
            initialCanvasPos = rootCanvas.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // We do actual movement in Update() for smoothness, so no need here.
    }

    private bool IsGrabButtonHeld()
    {
        // Check both left and right controllers for X button
        return OVRInput.Get(OVRInput.Button.Three) || OVRInput.Get(OVRInput.Button.One);
    }
}
