using UnityEngine;
using System.Collections; 
using UnityEngine.UI;
using TMPro;

public class PalmEffectsManager : MonoBehaviour
{
    [Header("UI 文字提示")]
    public TextMeshProUGUI statusText; 

    [Header("引導特效綁定")]
    public GameObject guideHandHint;
    public GameObject bottleGlow;
    public CanvasGroup dragDashedLineGroup;

    [Header("音效設定")]
    public AudioSource sfxPlayer; 
    public AudioClip grabSound;   
    public AudioClip dropSound;

    [Header("動畫細節設定")]
    public float shrinkDuration = 0.5f; 
    public Vector3 iconScale = new Vector3(0.3f, 0.3f, 0.3f); 
    public float dropHeightOffset = 150f;

    // 遊戲一開始的初始視覺狀態
    public void InitEffects()
    {
        if (guideHandHint != null) guideHandHint.SetActive(true);
        if (bottleGlow != null) bottleGlow.SetActive(true);
        if (dragDashedLineGroup != null) dragDashedLineGroup.gameObject.SetActive(false);
        if (statusText != null) statusText.text = "選取";
    }

    // 處理「拿起」時的所有特效與音效
    public void PlayGrabEffects(RectTransform bottle)
    {
        if (statusText != null) statusText.text = "拖移";

        if (sfxPlayer != null && grabSound != null)
        {
            sfxPlayer.PlayOneShot(grabSound);
        }

        if (guideHandHint != null) guideHandHint.SetActive(false);
        if (bottleGlow != null) bottleGlow.SetActive(false);
        
        if (bottle != null)
        {
            bottle.localScale = new Vector3(1.2f, 1.2f, 1.2f); 
        }

        if (dragDashedLineGroup != null) 
        {
            dragDashedLineGroup.gameObject.SetActive(true);
            dragDashedLineGroup.alpha = 0f;
            StartCoroutine(FadeInDashedLine()); 
        }
    }

    // 處理「放入箱子」時的所有特效與音效
    public void PlayDropEffects(RectTransform bottle, RectTransform box)
    {
        if (dragDashedLineGroup != null) dragDashedLineGroup.gameObject.SetActive(false);
        if (statusText != null) statusText.text = "放置";
        
        // 啟動縮小動畫
        StartCoroutine(ShrinkIntoIcon(bottle, box));
    }

    private IEnumerator FadeInDashedLine()
    {
        float fadeDuration = 0.5f; 
        float currentTime = 0f;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            dragDashedLineGroup.alpha = Mathf.Lerp(0f, 1f, currentTime / fadeDuration);
            yield return null; 
        }
        dragDashedLineGroup.alpha = 1f;
    }

    private IEnumerator ShrinkIntoIcon(RectTransform bottle, RectTransform box)
    {
        Vector3 startScale = bottle.localScale;
        Vector3 startPosition = bottle.position;
        Vector3 aboveBoxPosition = box.position + new Vector3(0, dropHeightOffset, 0);
        Vector3 centerBoxPosition = box.position; 

        float halfTime = shrinkDuration / 2f;
        float elapsedTime = 0f;

        // 階段一：飛到上方並縮小
        while (elapsedTime < halfTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfTime; 
            bottle.localScale = Vector3.Lerp(startScale, iconScale, t);
            bottle.position = Vector3.Lerp(startPosition, aboveBoxPosition, t);
            yield return null; 
        }

        // 階段二：垂直掉進箱子
        elapsedTime = 0f; 
        while (elapsedTime < halfTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfTime; 
            bottle.position = Vector3.Lerp(aboveBoxPosition, centerBoxPosition, t);
            yield return null; 
        }

        bottle.localScale = iconScale;
        bottle.position = centerBoxPosition;
        
        if (sfxPlayer != null && dropSound != null)
        {
            sfxPlayer.PlayOneShot(dropSound);
        }

        Debug.Log("特效播放完畢！");
    }
}
