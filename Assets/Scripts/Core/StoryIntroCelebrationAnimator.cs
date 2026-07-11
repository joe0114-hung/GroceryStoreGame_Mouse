using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryIntroCelebrationAnimator : MonoBehaviour
{
    [SerializeField] private GameObject messageBox;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RawImage smileImage;
    [SerializeField] private RawImage cheerImage;
    [SerializeField] private RectTransform effectRoot;

    [Header("Timing")]
    [SerializeField] private float frameInterval = 0.18f;
    [SerializeField] private float celebrationDuration = 1.8f;

    [Header("Motion")]
    [SerializeField] private float bobAmplitude = 18f;
    [SerializeField] private float pulseScale = 0.06f;
    [SerializeField] private string celebrationMessage = "完成蓋章！";

    private Vector2 restingAnchoredPosition;
    private Vector3 restingScale;
    private Coroutine celebrationRoutine;

    private void Awake()
    {
        if (effectRoot == null)
        {
            effectRoot = transform as RectTransform;
        }

        if (messageBox == null)
        {
            messageBox = transform.Find("MessageBox")?.gameObject;
        }

        if (messageText == null && messageBox != null)
        {
            messageText = messageBox.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (smileImage == null)
        {
            smileImage = transform.Find("SmileFrame")?.GetComponent<RawImage>();
        }

        if (cheerImage == null)
        {
            cheerImage = transform.Find("CheerFrame")?.GetComponent<RawImage>();
        }

        if (effectRoot != null)
        {
            restingAnchoredPosition = effectRoot.anchoredPosition;
            restingScale = effectRoot.localScale;
        }

        SetVisible(true);
        SetFrame(false);
    }

    public IEnumerator PlayCelebration()
    {
        if (celebrationRoutine != null)
        {
            StopCoroutine(celebrationRoutine);
        }

        celebrationRoutine = StartCoroutine(PlayCelebrationRoutine());
        yield return celebrationRoutine;
        celebrationRoutine = null;
    }

    private IEnumerator PlayCelebrationRoutine()
    {
        float elapsed = 0f;
        float nextFlipTime = 0f;
        bool showCheer = false;

        SetVisible(true);
        if (messageText != null)
        {
            messageText.text = celebrationMessage;
        }
        SetFrame(false);

        while (elapsed < celebrationDuration)
        {
            if (elapsed >= nextFlipTime)
            {
                showCheer = !showCheer;
                SetFrame(showCheer);
                nextFlipTime += frameInterval;
            }

            ApplyMotion(elapsed / Mathf.Max(0.01f, celebrationDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetMotion();
        SetVisible(true);
        SetFrame(false);
    }

    private void SetVisible(bool visible)
    {
        if (messageBox != null)
        {
            messageBox.SetActive(visible);
        }

        if (smileImage != null)
        {
            smileImage.gameObject.SetActive(visible);
        }

        if (cheerImage != null)
        {
            cheerImage.gameObject.SetActive(visible);
        }
    }

    private void SetFrame(bool cheerVisible)
    {
        if (smileImage != null)
        {
            smileImage.gameObject.SetActive(!cheerVisible);
        }

        if (cheerImage != null)
        {
            cheerImage.gameObject.SetActive(cheerVisible);
        }
    }

    private void ApplyMotion(float normalizedTime)
    {
        if (effectRoot == null)
        {
            return;
        }

        float bob = Mathf.Sin(normalizedTime * Mathf.PI * 4f) * bobAmplitude;
        float pulse = Mathf.Sin(normalizedTime * Mathf.PI * 6f) * pulseScale;
        effectRoot.anchoredPosition = restingAnchoredPosition + new Vector2(0f, bob);
        effectRoot.localScale = restingScale * (1f + pulse);
    }

    private void ResetMotion()
    {
        if (effectRoot == null)
        {
            return;
        }

        effectRoot.anchoredPosition = restingAnchoredPosition;
        effectRoot.localScale = restingScale;
    }
}
