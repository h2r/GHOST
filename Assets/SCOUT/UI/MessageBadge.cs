using System.Collections;
using TMPro;
using UnityEngine;

namespace SCOUT
{
    public class MessageBadge : MonoBehaviour
    {
        public static MessageBadge Instance { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI messageText;
        public CanvasGroup canvasGroup;

        [Header("Positioning")]
        public Transform cameraRig;
        public Vector3 offsetFromCamera = new Vector3(0f, 0f, 1.5f);
        public bool followCamera = true;
        public float followSpeed = 5f;

        [Header("Animation Settings")]
        public float fullOpacity = 0.5f;
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.3f;
        public float displayDuration = 2.5f;

        private Coroutine activeDisplayCoroutine;
        private bool isShowing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (cameraRig == null)
            {
                GameObject rig = GameObject.Find("OVRCameraRig") ?? GameObject.Find("CameraRig");
                if (rig != null)
                    cameraRig = rig.transform;
            }
            ShowMessage("Started!!");
        }

        private void LateUpdate()
        {
            if (followCamera && cameraRig != null && isShowing)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            Vector3 forward = cameraRig.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPos = cameraRig.position + forward * offsetFromCamera.z
                               + cameraRig.right * offsetFromCamera.x
                               + Vector3.up * offsetFromCamera.y;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            Vector3 lookDirection = cameraRig.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
        }

        public void ShowMessage(string text, float? duration = null)
        {
            if (activeDisplayCoroutine != null)
                StopCoroutine(activeDisplayCoroutine);

            activeDisplayCoroutine = StartCoroutine(ShowCoroutine(text, duration ?? displayDuration));
        }

        public void Hide()
        {
            if (activeDisplayCoroutine != null)
            {
                StopCoroutine(activeDisplayCoroutine);
                activeDisplayCoroutine = null;
            }

            StartCoroutine(FadeOut());
        }

        private IEnumerator ShowCoroutine(string text, float duration)
        {
            isShowing = true;

            if (messageText != null)
                messageText.text = text;

            if (cameraRig != null)
                UpdatePosition();

            yield return StartCoroutine(FadeIn());
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeOut());

            isShowing = false;
            activeDisplayCoroutine = null;
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, fullOpacity, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = fullOpacity;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
