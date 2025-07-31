using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycast : MonoBehaviour
{
    public float maxDistance = 10f;
    public UIManager uiManager;
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public bool isLeft;

    private LineRenderer lineRenderer;

    // Track if trigger is held for dragging
    private bool isTriggerHeld = false;
    private GameObject currentDragObject = null;
    private PointerEventData pointerEventData;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        pointerEventData = new PointerEventData(eventSystem);
    }

    void Update()
    {
        if (!uiManager.GetOpen())
        {
            lineRenderer.enabled = false;
            if (isTriggerHeld && currentDragObject != null)
            {
                // Release drag if menu closes while dragging
                ExecuteEvents.Execute(currentDragObject, pointerEventData, ExecuteEvents.pointerUpHandler);
                isTriggerHeld = false;
                currentDragObject = null;
            }
            return;
        }

        lineRenderer.enabled = true;

        Ray ray = new(transform.position, transform.forward);
        Vector3 endPoint = ray.origin + ray.direction * maxDistance;

        // Update pointer event position to the canvas screen point under ray
        pointerEventData.position = WorldPointToCanvasScreenPoint(ray.origin, ray.direction);

        // Raycast UI elements under pointer
        List<RaycastResult> results = new();
        raycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            GameObject hitGO = results[0].gameObject;
            RectTransform rect = hitGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                endPoint = rect.position;

                // Handle input
                bool triggerDown = (OVRInput.GetDown(OVRInput.Button.Three) && isLeft) || (OVRInput.GetDown(OVRInput.Button.One) && !isLeft);
                bool triggerUp = (OVRInput.GetUp(OVRInput.Button.Three) && isLeft) || (OVRInput.GetUp(OVRInput.Button.One) && !isLeft);
                bool triggerHeld = (OVRInput.Get(OVRInput.Button.Three) && isLeft) || (OVRInput.Get(OVRInput.Button.One) && !isLeft);

                // First, check if UIManager recognizes this UI for hover/press
                if (uiManager.TryRaycastHover(hitGO))
                {
                    if (triggerDown)
                        uiManager.RaycastPress(hitGO);
                }
                else
                {
                    // For things not handled by UIManager, e.g. grab bar, send pointer events manually
                    pointerEventData.pointerCurrentRaycast = results[0];

                    if (triggerDown)
                    {
                        ExecuteEvents.Execute(hitGO, pointerEventData, ExecuteEvents.pointerDownHandler);
                        isTriggerHeld = true;
                        currentDragObject = hitGO;
                    }
                    else if (triggerUp && isTriggerHeld && currentDragObject != null)
                    {
                        ExecuteEvents.Execute(currentDragObject, pointerEventData, ExecuteEvents.pointerUpHandler);
                        isTriggerHeld = false;
                        currentDragObject = null;
                    }
                    else if (triggerHeld && isTriggerHeld && currentDragObject != null)
                    {
                        ExecuteEvents.Execute(currentDragObject, pointerEventData, ExecuteEvents.dragHandler);
                    }
                }
            }
        }

        // Draw the laser line
        lineRenderer.SetPosition(0, ray.origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    Vector2 WorldPointToCanvasScreenPoint(Vector3 origin, Vector3 direction)
    {
        Ray ray = new(origin, direction);
        Plane canvasPlane = new(-raycaster.transform.forward, raycaster.transform.position);

        if (canvasPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            return Camera.main.WorldToScreenPoint(hitPoint);
        }

        return Vector2.zero;
    }
}
