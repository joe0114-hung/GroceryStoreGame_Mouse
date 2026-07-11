using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StampPaperIntroAnimator : MonoBehaviour
{
    [Header("Paper")]
    [SerializeField] private RawImage paperImage;
    [SerializeField] private Texture closedTexture;
    [SerializeField] private Texture halfTexture;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip rollingSound;

    [Header("Timing")]
    [SerializeField] private float closedHoldTime = 0.18f;
    [SerializeField] private float halfHoldTime = 0.18f;

    private Coroutine routine;
    private Texture currentTexture;

    private void Awake()
    {
        if (paperImage == null)
        {
            paperImage = GetComponent<RawImage>();
        }

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

        if (paperImage != null)
        {
            currentTexture = paperImage.texture;
        }

        if (closedTexture != null)
        {
            SetTexture(closedTexture);
        }
    }

    private void Start()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        PlayRollingSound();

        if (closedTexture != null)
        {
            SetTexture(closedTexture);
        }

        yield return new WaitForSeconds(closedHoldTime);

        if (halfTexture != null)
        {
            SetTexture(halfTexture);
        }

        yield return new WaitForSeconds(halfHoldTime);

        if (currentTexture != null)
        {
            SetTexture(currentTexture);
        }

        routine = null;
    }

    private void PlayRollingSound()
    {
        if (sfxSource != null && rollingSound != null)
        {
            sfxSource.PlayOneShot(rollingSound);
        }
    }

    private void SetTexture(Texture texture)
    {
        if (paperImage != null && texture != null)
        {
            paperImage.texture = texture;
        }
    }
}
