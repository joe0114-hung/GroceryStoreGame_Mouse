using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public TextMeshProUGUI countdownText;
    public RectTransform bellTransform; 

    [Header("搖擺設定")]
    public float swingAngle = 15f;      

    // 👇 就是缺了下面這三行 👇
    [Header("音效綁定")]
    public AudioSource audioSource;     // 剛剛加的喇叭
    public AudioClip bellSound;         // 搖鈴聲
    public AudioClip goSound;           // (選用) 最後「開始！」的音效

    void Start()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int count = 3;
        
        while (count > 0)
        {
            countdownText.text = count.ToString();
            
            // 播放搖鈴音效
            if (audioSource != null && bellSound != null)
            {
                audioSource.PlayOneShot(bellSound);
            }

            if (bellTransform != null)
            {
                StartCoroutine(SwingBellAnimation(swingAngle));
            }

            yield return new WaitForSeconds(1f);
            count--;
        }

        countdownText.text = "開始！";
        
        // 播放開始音效
        if (audioSource != null && goSound != null)
        {
            audioSource.PlayOneShot(goSound);
        }
        else if (audioSource != null && bellSound != null) 
        {
            audioSource.PlayOneShot(bellSound);
        }

        if (bellTransform != null)
        {
            StartCoroutine(SwingBellAnimation(swingAngle * 1.5f)); 
        }
        
        yield return new WaitForSeconds(1f);

        // 呼叫全域大腦切換場景
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(GameFlowController.GameState.PlayingLevel);
        }
    }

    private IEnumerator SwingBellAnimation(float angle)
    {
        float duration = 0.15f; 
        float time = 0;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float z = Mathf.Lerp(0, angle, time / duration);
            bellTransform.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }
        
        time = 0;
        while (time < duration * 2)
        {
            time += Time.deltaTime;
            float z = Mathf.Lerp(angle, -angle, time / (duration * 2));
            bellTransform.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }

        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float z = Mathf.Lerp(-angle, 0, time / duration);
            bellTransform.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }
    }
}