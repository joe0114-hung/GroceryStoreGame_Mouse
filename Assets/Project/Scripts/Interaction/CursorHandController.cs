using UnityEngine;
using UnityEngine.UI; // 引入 UI 控制 Image 的射線

/// <summary>
/// 腳本功能：負責控管「虛擬游標（手套）」的狀態，包含抓取水果、確認手上有無物品、以及清空雙手。
/// 掛載對象：場景中的 Cursor 游標主物件 (全域 DontDestroyOnLoad)。
/// </summary>
public class CursorHandController : MonoBehaviour
{
    // 單例模式 (Singleton)
    public static CursorHandController Instance;

    [Header("UI References (介面綁定)")]
    [Tooltip("游標的最高層級容器")]
    [SerializeField] private RectTransform cursorRoot;
    
    [Tooltip("專門用來裝水果的圖層 (放在這個圖層下才能確保水果跟著手套移動)")]
    [SerializeField] private RectTransform holdingLayer;

    // 內部變數：記錄現在手上拿著什麼東西
    private GameObject currentHolding;
    private RectTransform currentRect;

    private void Awake()
    {
        // 強化版單例防呆：避免跨場景切換時產生多隻手套打架
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[CursorHand] 偵測到重複的全域游標，已自動銷毀多餘的分身！");
            Destroy(gameObject); 
            return;
        }
    }

    /// <summary>
    /// 讓游標「抓起」指定的水果。
    /// </summary>
    public void Hold(GameObject fruitPrefab)
    {
        // 1. 防呆機制：如果手上已經有拿東西了，先把它銷毀掉
        if (currentHolding != null)
            Destroy(currentHolding);

        // 2. 生成實體並設定父層
        currentHolding = Instantiate(fruitPrefab);
        currentHolding.transform.SetParent(holdingLayer, false);
        
        // 3. 重置排版，確保水果對齊手套中心
        currentRect = currentHolding.GetComponent<RectTransform>();
        if (currentRect != null)
        {
            currentRect.anchorMin = new Vector2(0.5f, 0.5f);
            currentRect.anchorMax = new Vector2(0.5f, 0.5f);
            currentRect.pivot = new Vector2(0.5f, 0.5f);
            currentRect.anchoredPosition = Vector2.zero;
            //currentRect.localScale = Vector3.one;
            // 將原本的 Vector3.one 改成指定縮放比例 ( 0.5 倍)
            currentRect.localScale = new Vector3(0.5f, 0.5f, 1f);
        }

        //  4. 強迫關閉水果的射線阻擋，避免它擋住 Dwell 系統的眼睛！
        Image img = currentHolding.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = false;
        }
        
        // 如果水果 Prefab 底下還有其他裝飾圖片，也一併關閉
        foreach (Image childImg in currentHolding.GetComponentsInChildren<Image>())
        {
            childImg.raycastTarget = false;
        }

        Debug.Log($"[CursorHand] 成功抓取 {fruitPrefab.name}，並已解鎖射線阻擋！");
    }

    /// <summary>
    /// 取得目前手上的水果物件實體。
    /// </summary>
    public GameObject GetHolding()
    {
        return currentHolding;
    }

    /// <summary>
    /// 檢查手上是否有拿著東西。
    /// </summary>
    public bool HasHolding()
    {
        return currentHolding != null;
    }

    /// <summary>
    /// 清空雙手。
    /// </summary>
    public void Clear()
    {
        if (currentHolding != null)
        {
            Destroy(currentHolding);
            currentHolding = null;
            currentRect = null;

            Debug.Log("[CursorHand] 雙手已清空");
        }
    }
}