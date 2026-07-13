using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StampCheckContinueButton : MonoBehaviour
{
    [SerializeField] private GameFlowController.GameState nextState = GameFlowController.GameState.WaitForStart;
    [SerializeField] private string fallbackSceneName = "WaitStartScene";
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickSound;

    private bool hasTriggered;
    private bool hasRevealed;

    private void Awake()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        InteractionEvents.OnSelect += HandleSelect;
    }

    private void OnDisable()
    {
        InteractionEvents.OnSelect -= HandleSelect;
    }

    public void Reveal()
    {
        if (hasRevealed)
        {
            return;
        }

        hasRevealed = true;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        PlayPickSound();
    }

    private void HandleSelect(GameObject selectedObject)
    {
        if (hasTriggered || selectedObject == null)
        {
            return;
        }

        if (selectedObject != gameObject && !selectedObject.transform.IsChildOf(transform))
        {
            return;
        }

        hasTriggered = true;
        GoNext();
    }

    private void GoNext()
    {
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(nextState);
            return;
        }

        SceneManager.LoadScene(fallbackSceneName);
    }

    private void PlayPickSound()
    {
        if (sfxSource != null && pickSound != null)
        {
            sfxSource.PlayOneShot(pickSound);
        }
    }
}
