using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loops between idle and talking character frames while dialogue is typing.
/// </summary>
public class StoryIntroCharacterAnimator : MonoBehaviour
{
    [SerializeField] private RawImage idleImage;
    [SerializeField] private RawImage talkingImage;
    [SerializeField] private float frameInterval = 0.18f;

    private Coroutine animationRoutine;
    private bool isTalking;
    private bool showTalkingFrame;

    private void Awake()
    {
        ApplyIdleFrame();
    }

    private void OnEnable()
    {
        if (isTalking)
        {
            StartAnimation();
        }
    }

    private void OnDisable()
    {
        StopAnimation();
        ApplyIdleFrame();
    }

    public void SetTalking(bool talking)
    {
        if (isTalking == talking)
        {
            return;
        }

        isTalking = talking;

        if (isTalking)
        {
            StartAnimation();
        }
        else
        {
            StopAnimation();
            ApplyIdleFrame();
        }
    }

    private void StartAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(TalkLoop());
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private IEnumerator TalkLoop()
    {
        showTalkingFrame = false;

        while (isTalking)
        {
            showTalkingFrame = !showTalkingFrame;
            ApplyFrame(showTalkingFrame);
            yield return new WaitForSeconds(frameInterval);
        }
    }

    private void ApplyIdleFrame()
    {
        ApplyFrame(false);
    }

    private void ApplyFrame(bool talkingFrame)
    {
        if (idleImage != null)
        {
            idleImage.enabled = !talkingFrame;
        }

        if (talkingImage != null)
        {
            talkingImage.enabled = talkingFrame;
        }
    }
}
