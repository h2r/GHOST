using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycast : MonoBehaviour
{
    public float maxDistance = 15f;
    public UIManager uiManager;
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public bool isLeft;

    private LineRenderer lineRenderer;

    // Highlight box for hovered UI element
    private GameObject highlightBox;

    public void Start()
    {
    lineRenderer = GetComponent<LineRenderer>();

    // Create highlight box (Image with white border)
    highlightBox = new GameObject("HighlightBox");
    var image = highlightBox.AddComponent<UnityEngine.UI.Image>();
    image.color = new Color(1, 1, 1, 0); // Transparent fill
    image.raycastTarget = false;
    var outline = highlightBox.AddComponent<UnityEngine.UI.Outline>();
    outline.effectColor = Color.white;
    outline.effectDistance = new Vector2(4, 4);
    highlightBox.SetActive(false);
    }

    public void Update()
    {
        if (uiManager.GetOpen())
        {
            lineRenderer.enabled = true;

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
                    // Show highlight box for hovered UI element 
                    ShowHighlight(rect);
                    if ((OVRInput.GetDown(OVRInput.Button.Three) && isLeft) ||
                        (OVRInput.GetDown(OVRInput.Button.One) && !isLeft))
                        uiManager.RaycastPress(button);
                }
                else
                {
                    HideHighlight();
                }
            }
            else
            {
                HideHighlight();
            }

            // Draw the ray
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, endPoint);
        }
        else
        {
            lineRenderer.enabled = false;
            HideHighlight();
        }
    }
    // Show highlight box around the hovered UI element
    private void ShowHighlight(RectTransform targetRect)
    {
        if (highlightBox == null || targetRect == null) return;
        highlightBox.transform.SetParent(targetRect.parent, false);
        var highlightRect = highlightBox.GetComponent<RectTransform>();
        if (highlightRect == null)
            highlightRect = highlightBox.AddComponent<RectTransform>();
        highlightRect.anchorMin = targetRect.anchorMin;
        highlightRect.anchorMax = targetRect.anchorMax;
        highlightRect.pivot = targetRect.pivot;
        highlightRect.sizeDelta = targetRect.sizeDelta;
        highlightRect.anchoredPosition = targetRect.anchoredPosition;
        highlightBox.SetActive(true);
    }

    // Hide highlight box
    private void HideHighlight()
    {
        if (highlightBox != null)
            highlightBox.SetActive(false);
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

