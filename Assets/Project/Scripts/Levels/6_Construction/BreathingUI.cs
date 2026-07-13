using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 腳本功能：讓指定的 UI 圖片產生「呼吸燈」般的透明度閃爍效果，並可選擇性讓物件微微縮放。
/// </summary>
public class BreathingUI : MonoBehaviour
{
    [Header("發光設定")]
    [Tooltip("請將 Glow_Background (發光背板) 拖進來")]
    public Image glowImage;
    
    [Tooltip("呼吸變化的速度 (數值越大閃得越快)")]
    public float breatheSpeed = 1.5f;
    
    [Tooltip("最暗的透明度 (0~1)")]
    public float minAlpha = 0.2f;
    
    [Tooltip("最亮的透明度 (0~1)")]
    public float maxAlpha = 0.8f;

    [Header("動態縮放 (選填)")]
    [Tooltip("如果想讓春聯本身也跟著微微放大縮小，把春聯本體拖進來")]
    public Transform targetToScale;
    
    [Tooltip("最大縮放比例 (例如 1.05 代表放大 5%)")]
    public float maxScale = 1.05f;

    void Update()
    {
        // 核心魔法：使用 Mathf.PingPong 產生 0 到 1 之間平滑來回跳動的數值
        float pingPongValue = Mathf.PingPong(Time.time * breatheSpeed, 1f);

        // 1. 更新發光背板的透明度 (Alpha)
        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, pingPongValue);
            glowImage.color = c;
        }

        // 2. (選填) 讓春聯本體微微跟著呼吸縮放，吸引力倍增！
        if (targetToScale != null)
        {
            float currentScale = Mathf.Lerp(1f, maxScale, pingPongValue);
            targetToScale.localScale = new Vector3(currentScale, currentScale, 1f);
        }
    }
}