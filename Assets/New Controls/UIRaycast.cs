using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycast : MonoBehaviour
{
    public enum Hand { Left, Right }
    public Hand hand = Hand.Right;

    public float maxDistance = 10f;
    public UIManager uiManager;
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private LineRenderer lineRenderer;
    private OVRInput.Button clickButton;
    private OVRInput.Controller controller;

    // Ray colors
    private static readonly Color defaultColor = Color.white;
    private static readonly Color hoverColor = new Color(0.5f, 0.7f, 1f, 1f); // subtle blue

    private GameObject lastHoveredButton = null;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (!uiManager)
            Debug.LogWarning($"{name}: UIManager reference not set!");
        if (!raycaster)
            Debug.LogWarning($"{name}: GraphicRaycaster reference not set!");
        if (!eventSystem)
            Debug.LogWarning($"{name}: EventSystem reference not set!");
        if (!lineRenderer)
            Debug.LogWarning($"{name}: LineRenderer component not found!");

        if (hand == Hand.Left)
        {
            clickButton = OVRInput.Button.Three;
            controller = OVRInput.Controller.LTouch;
        }
        else
        {
            clickButton = OVRInput.Button.One;
            controller = OVRInput.Controller.RTouch;
        }
    }

    void Update()
    {
        if (uiManager == null || lineRenderer == null)
            return;

        if (uiManager.GetOpen())
        {
            lineRenderer.enabled = true;

            Ray ray = new(transform.position, transform.forward);
            var endPoint = ray.origin + ray.direction * maxDistance;

            bool hovering = false;
            GameObject hoveredButton = null;

            if (raycaster && eventSystem)
            {
                PointerEventData pointerEventData = new(eventSystem)
                {
                    position = WorldPointToCanvasScreenPoint(ray.origin, ray.direction)
                };
                List<RaycastResult> results = new();
                raycaster.Raycast(pointerEventData, results);

                if (results.Count > 0)
                {
                    var rect = results[0].gameObject.GetComponent<RectTransform>();
                    var button = results[0].gameObject.transform.parent.gameObject;
                    if (rect != null && uiManager.TryRaycastHover(button))
                    {
                        endPoint = rect.position;
                        hovering = true;
                        hoveredButton = button;

                        if (OVRInput.GetDown(clickButton))
                        {
                            uiManager.RaycastPress(button);
                            // Short, subtle haptic feedback
                            OVRInput.SetControllerVibration(0.1f, 0.2f, controller);
                        }
                    }
                }
            }

            // Set ray color
            lineRenderer.startColor = lineRenderer.endColor = hovering ? hoverColor : defaultColor;

            // Draw the ray
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, endPoint);

            // Button hover highlight
            if (hoveredButton != lastHoveredButton)
            {
                uiManager.HighlightButton(hoveredButton); // You need to add this method, see below
                lastHoveredButton = hoveredButton;
            }
        }
        else
        {
            lineRenderer.enabled = false;
            lastHoveredButton = null;
        }
    }

    Vector2 WorldPointToCanvasScreenPoint(Vector3 origin, Vector3 direction)
    {
        if (!raycaster)
            return Vector2.zero;

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
