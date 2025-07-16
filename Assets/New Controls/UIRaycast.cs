using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebSocketSharp;

public class UIRaycast : MonoBehaviour
{
    public float maxDistance = 10f;
    public UIManager uiManager;
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private LineRenderer lineRenderer;

    public void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        print(raycaster);
    }

    public void Update()
    {
        Ray ray = new(transform.position, transform.forward);
        var endPoint = ray.origin + ray.direction * maxDistance;

        // UI Raycast
        PointerEventData pointerEventData = new(eventSystem)
        {
            position = WorldPointToCanvasScreenPoint(ray.origin, ray.direction)
        };
        List<RaycastResult> results = new();
        raycaster.Raycast(pointerEventData, results);

        // If UI hit, set endpoint to the hit location
        if (results.Count > 0)
        {
            var rect = results[0].gameObject.GetComponent<RectTransform>();
            var button = results[0].gameObject.transform.parent.gameObject;
            if (rect != null && uiManager.TryRaycastHover(button))
            {
                endPoint = rect.position;
                if (OVRInput.Get(OVRInput.Button.Three))
                    uiManager.RaycastPress(button);
            }
        }

        // Draw the ray
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
