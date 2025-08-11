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
    private GameObject highlightBox;

    public void Start()
    {
    lineRenderer = GetComponent<LineRenderer>();
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
                    if (button != highlightBox)
                    {
                        HideHighlight();
                        ShowHighlight(button);
                        highlightBox = button; 
                    }
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
    private void ShowHighlight(GameObject targetRect)
    {
        Transform highlightTransform = targetRect.transform.Find("Highlight");
        if (highlightTransform != null)
        {
            highlightTransform.gameObject.SetActive(true);
        }
    }

    // Hide highlight box
    private void HideHighlight()
    {
        if (highlightBox != null)
        {
            //unhighlight graphically and logically
            Transform highlightTransform = highlightBox.transform.Find("Highlight");
            if (highlightTransform != null)
            {
                highlightTransform.gameObject.SetActive(false);
            }
            highlightBox = null; 
        } 
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

