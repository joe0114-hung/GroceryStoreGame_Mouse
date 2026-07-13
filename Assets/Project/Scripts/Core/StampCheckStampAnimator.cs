using System.Collections;
using UnityEngine;

public class StampCheckStampAnimator : MonoBehaviour
{
    [Header("Scene Links")]
    [SerializeField] private StampPaperProgressController stampPaperController;
    [SerializeField] private GameObject pressingVisual;
    [SerializeField] private GameObject pressedVisual;
    [SerializeField] private RectTransform effectRoot;
    [SerializeField] private StoryIntroCelebrationAnimator celebrationAnimator;
    [SerializeField] private StampCheckContinueButton continueButton;
    [SerializeField] private AudioClip stampStartSound;
    [SerializeField] private AudioClip stampSound;
    [SerializeField] private AudioClip crowdCheerSound;
    [SerializeField] private AudioSource sfxSource;

    [Header("Timing")]
    [SerializeField] private float entryStartY = -900f;
    [SerializeField] private float entryMoveDuration = 1f;
    [SerializeField] private float pressingHoldTime = 0.45f;
    [SerializeField] private float pressedHoldTime = 0.12f;

    private Coroutine animationRoutine;
    private Vector2 restingAnchoredPosition;

    private void Awake()
    {
        if (effectRoot == null)
        {
            effectRoot = transform as RectTransform;
        }

        if (stampPaperController == null)
        {
            stampPaperController = FindObjectOfType<StampPaperProgressController>();
        }

        if (pressingVisual == null)
        {
            pressingVisual = transform.Find("UI_Stamp_Pressing")?.gameObject;
        }

        if (pressedVisual == null)
        {
            pressedVisual = transform.Find("UI_Stamp_Pressed")?.gameObject;
        }

        if (celebrationAnimator == null)
        {
            celebrationAnimator = FindObjectOfType<StoryIntroCelebrationAnimator>();
        }

        if (continueButton == null)
        {
            StampCheckContinueButton[] buttons = Resources.FindObjectsOfTypeAll<StampCheckContinueButton>();
            if (buttons != null && buttons.Length > 0)
            {
                continueButton = buttons[0];
            }
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }

        if (effectRoot != null)
        {
            restingAnchoredPosition = effectRoot.anchoredPosition;
            effectRoot.anchoredPosition = new Vector2(restingAnchoredPosition.x, entryStartY);
        }

        SetVisualState(false, false);
    }

    private void Start()
    {
        if (stampPaperController != null)
        {
            stampPaperController.SyncFromManager();
        }

        TryPlayQueuedStamp();
    }

    public void PlayStamp1() => PlayStamp(1);
    public void PlayStamp2() => PlayStamp(2);
    public void PlayStamp3() => PlayStamp(3);
    public void PlayStamp4() => PlayStamp(4);
    public void PlayStamp5() => PlayStamp(5);
    public void PlayStamp6() => PlayStamp(6);

    public void PlayStamp(int stampIndex)
    {
        stampIndex = Mathf.Clamp(stampIndex, 1, 6);

        if (StampManager.Instance != null && StampManager.Instance.HasStamp(stampIndex))
        {
            Debug.Log($"[StampCheck] 印章 {stampIndex} 已經蓋過，略過重複動作。");
            return;
        }

        if (stampPaperController != null && stampPaperController.HasStamp(stampIndex))
        {
            Debug.Log($"[StampCheck] 印章 {stampIndex} 已經顯示在印章紙上，略過重複動作。");
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(PlayStampRoutine(stampIndex));
    }

    private IEnumerator PlayStampRoutine(int stampIndex)
    {
        SetVisualState(true, false);
        PlayStampStartSound();
        yield return MoveIntoPlace();
        yield return new WaitForSeconds(pressingHoldTime);

        SetVisualState(false, true);
        PlayStampSound();
        yield return new WaitForSeconds(pressedHoldTime);

        SetVisualState(false, false);

        bool registered = false;
        if (StampManager.Instance != null)
        {
            registered = StampManager.Instance.TryRegisterCompletedStamp(stampIndex);
        }
        else if (stampPaperController != null)
        {
            registered = stampPaperController.RevealStamp(stampIndex);
        }

        if (!registered && stampPaperController != null)
        {
            stampPaperController.RevealStamp(stampIndex);
        }

        if (celebrationAnimator != null)
        {
            yield return celebrationAnimator.PlayCelebration();
        }

        PlayCrowdCheerSound();

        if (continueButton != null)
        {
            continueButton.Reveal();
        }

        animationRoutine = null;
    }

    private IEnumerator MoveIntoPlace()
    {
        if (effectRoot == null)
        {
            yield break;
        }

        Vector2 startPosition = new Vector2(restingAnchoredPosition.x, entryStartY);
        Vector2 endPosition = restingAnchoredPosition;

        effectRoot.anchoredPosition = startPosition;

        float elapsed = 0f;
        while (elapsed < entryMoveDuration)
        {
            float t = Mathf.Clamp01(elapsed / entryMoveDuration);
            effectRoot.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        effectRoot.anchoredPosition = endPosition;
    }

    private void SetVisualState(bool showPressing, bool showPressed)
    {
        if (pressingVisual != null)
        {
            pressingVisual.SetActive(showPressing);
        }

        if (pressedVisual != null)
        {
            pressedVisual.SetActive(showPressed);
        }
    }

    private void PlayStampSound()
    {
        if (sfxSource == null || stampSound == null)
        {
            return;
        }

        sfxSource.PlayOneShot(stampSound);
    }

    private void PlayStampStartSound()
    {
        if (sfxSource == null || stampStartSound == null)
        {
            return;
        }

        sfxSource.PlayOneShot(stampStartSound);
    }

    private void PlayCrowdCheerSound()
    {
        if (sfxSource == null || crowdCheerSound == null)
        {
            return;
        }

        sfxSource.PlayOneShot(crowdCheerSound);
    }

    private void TryPlayQueuedStamp()
    {
        if (StampManager.Instance == null)
        {
            return;
        }

        if (!StampManager.Instance.TryConsumePendingStamp(out int stampIndex))
        {
            return;
        }

        if (stampIndex >= 1 && stampIndex <= 6)
        {
            PlayStamp(stampIndex);
        }
    }
}
