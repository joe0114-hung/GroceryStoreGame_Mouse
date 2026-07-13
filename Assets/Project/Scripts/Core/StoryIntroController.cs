using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the story intro dialogue before entering the tutorial flow.
/// </summary>
public class StoryIntroController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject dialogueBox;

    [Header("Story")]
    [TextArea(2, 4)]
    [SerializeField] private string[] storyLines =
    {
        "老闆！週末的水果訂單來啦！",
        "請按照顧客畫的配置圖，把水果裝進紙盒裡",
        "儘快趁水果還新鮮，看好哪格放哪格喔～"
    };

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 16f;
    [SerializeField] private int typingSoundEveryCharacters = 2;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxPlayer;
    [SerializeField] private AudioClip nextSound;
    [SerializeField] private float nextSoundVolume = 0.8f;
    [SerializeField] private AudioClip[] typingSounds;
    [SerializeField] private float typingSoundVolume = 0.18f;
    [SerializeField] private float typingSoundFadeDuration = 1.25f;

    [Header("Character")]
    [SerializeField] private StoryIntroCharacterAnimator characterAnimator;
    [SerializeField] private RectTransform[] characterRoots;
    [SerializeField] private float characterIntroDuration = 2.4f;
    [SerializeField] private Vector2 characterIntroStartOffset = new Vector2(900f, 0f);
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private float walkSoundVolume = 0.18f;

    [Header("Next Stage")]
    [SerializeField] private GameFlowController.GameState nextState = GameFlowController.GameState.Tutorial;
    [SerializeField] private string fallbackTutorialSceneName = "Controller_Palm";

    private int currentLineIndex;
    private Coroutine typingRoutine;
    private Coroutine introRoutine;
    private Coroutine pageTurnRoutine;
    private bool isTyping;
    private bool isTurningPage;

    private void Awake()
    {
        if (dialogueText == null)
        {
            dialogueText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (nextButton == null)
        {
            InteractableUI interactable = GetComponentInChildren<InteractableUI>();
            if (interactable != null)
            {
                nextButton = interactable.gameObject;
            }
        }

        if (dialogueBox == null && dialogueText != null)
        {
            dialogueBox = dialogueText.transform.parent != null ? dialogueText.transform.parent.gameObject : null;
        }

        if (sfxPlayer == null)
        {
            sfxPlayer = GetComponent<AudioSource>();
        }

        if (characterAnimator == null)
        {
            characterAnimator = FindObjectOfType<StoryIntroCharacterAnimator>();
        }

    }

    private void OnEnable()
    {
        InteractionEvents.OnSelect += HandleSelect;
    }

    private void OnDisable()
    {
        InteractionEvents.OnSelect -= HandleSelect;
    }

    private void HandleSelect(GameObject selectedObject)
    {
        if (!IsNextButtonSelection(selectedObject))
        {
            return;
        }

        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        if (isTurningPage)
        {
            return;
        }

        if (pageTurnRoutine != null)
        {
            StopCoroutine(pageTurnRoutine);
        }

        pageTurnRoutine = StartCoroutine(AdvanceAfterPageTurn());
    }

    private bool IsNextButtonSelection(GameObject selectedObject)
    {
        if (selectedObject == null || nextButton == null)
        {
            return false;
        }

        if (selectedObject == nextButton)
        {
            return true;
        }

        return selectedObject.transform.IsChildOf(nextButton.transform);
    }

    private void Start()
    {
        currentLineIndex = 0;
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        introRoutine = StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        AudioSource walkAudioSource = null;
        GameObject walkSoundObject = null;

        if (characterRoots != null && characterRoots.Length > 0 && characterRoots[0] != null)
        {
            Vector2 endPosition = characterRoots[0].anchoredPosition;
            Vector2 startPosition = endPosition + characterIntroStartOffset;
            for (int i = 0; i < characterRoots.Length; i++)
            {
                if (characterRoots[i] != null)
                {
                    characterRoots[i].anchoredPosition = startPosition;
                }
            }

            if (walkSound != null)
            {
                walkSoundObject = new GameObject("WalkSound");
                walkSoundObject.transform.SetParent(transform, false);
                walkAudioSource = walkSoundObject.AddComponent<AudioSource>();
                walkAudioSource.clip = walkSound;
                walkAudioSource.loop = true;
                walkAudioSource.playOnAwake = false;
                walkAudioSource.spatialBlend = 0f;
                walkAudioSource.volume = walkSoundVolume;
                walkAudioSource.Play();
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, characterIntroDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);
                Vector2 currentPosition = Vector2.LerpUnclamped(startPosition, endPosition, smoothT);
                for (int i = 0; i < characterRoots.Length; i++)
                {
                    if (characterRoots[i] != null)
                    {
                        characterRoots[i].anchoredPosition = currentPosition;
                    }
                }
                yield return null;
            }

            for (int i = 0; i < characterRoots.Length; i++)
            {
                if (characterRoots[i] != null)
                {
                    characterRoots[i].anchoredPosition = endPosition;
                }
            }
        }

        if (walkAudioSource != null)
        {
            walkAudioSource.Stop();
        }

        if (walkSoundObject != null)
        {
            Destroy(walkSoundObject);
        }

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        ShowCurrentLine();
        introRoutine = null;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= storyLines.Length)
        {
            EnterTutorial();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogueText == null || storyLines.Length == 0)
        {
            return;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeLine(storyLines[currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        
        // 開始打字時，隱藏「下一步」按鈕，讓長輩專心看字
        if (nextButton != null) nextButton.SetActive(false);

        dialogueText.text = "";
        if (characterAnimator != null)
        {
            characterAnimator.SetTalking(true);
        }

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;
        int typedCharacters = 0;
        foreach (char character in line)
        {
            dialogueText.text += character;
            if (!char.IsWhiteSpace(character))
            {
                typedCharacters++;
                PlayTypingSound(typedCharacters);
            }

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        isTyping = false;
        
        // 整句話打完後，才顯示「下一步」按鈕允許點擊
        if (nextButton != null) nextButton.SetActive(true);

        if (characterAnimator != null)
        {
            characterAnimator.SetTalking(false);
        }
        typingRoutine = null;
    }

    private void CompleteCurrentLine()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;
        
        // 🌟 確保強制完成句子時，按鈕也會正常顯示出來
        if (nextButton != null) nextButton.SetActive(true);

        if (characterAnimator != null)
        {
            characterAnimator.SetTalking(false);
        }
        dialogueText.text = storyLines[currentLineIndex];
    }

    private void PlayNextSound()
    {
        if (sfxPlayer != null && nextSound != null)
        {
            sfxPlayer.PlayOneShot(nextSound, nextSoundVolume);
        }
    }

    private IEnumerator AdvanceAfterPageTurn()
    {
        isTurningPage = true;
        
        // 長輩一觸發「下一步」，立刻把按鈕藏起來，防止音效播放期間被連續誤觸
        if (nextButton != null) nextButton.SetActive(false);
        
        PlayNextSound();

        float waitTime = 0f;
        if (nextSound != null)
        {
            float pitch = sfxPlayer != null ? Mathf.Max(0.01f, sfxPlayer.pitch) : 1f;
            waitTime = nextSound.length / pitch;
        }

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        ShowNextLine();
        isTurningPage = false;
        pageTurnRoutine = null;
    }

    private void PlayTypingSound(int typedCharacters)
    {
        if (sfxPlayer == null || typingSounds == null || typingSounds.Length == 0)
        {
            return;
        }

        if (typingSoundEveryCharacters <= 0 || typedCharacters % typingSoundEveryCharacters != 0)
        {
            return;
        }

        int clipIndex = Random.Range(0, typingSounds.Length);
        AudioClip typingClip = typingSounds[clipIndex];
        if (typingClip != null)
        {
            StartCoroutine(PlayTypingClip(typingClip));
        }
    }

    private IEnumerator PlayTypingClip(AudioClip typingClip)
    {
        GameObject soundObject = new GameObject("TypingSound");
        soundObject.transform.SetParent(transform, false);

        AudioSource typingSource = soundObject.AddComponent<AudioSource>();
        typingSource.clip = typingClip;
        typingSource.volume = typingSoundVolume;
        typingSource.spatialBlend = 0f;
        typingSource.playOnAwake = false;
        typingSource.loop = false;
        typingSource.Play();

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, typingSoundFadeDuration);
        while (typingSource != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            typingSource.volume = Mathf.Lerp(typingSoundVolume, 0f, t);
            yield return null;
        }

        if (typingSource != null)
        {
            typingSource.Stop();
        }

        if (soundObject != null)
        {
            Destroy(soundObject);
        }
    }

    private void EnterTutorial()
    {
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(nextState);
            return;
        }

        SceneManager.LoadScene(fallbackTutorialSceneName);
    }
}
