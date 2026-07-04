using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 腳本功能：負責控管「虛擬游標（手套）」的狀態，包含抓取水果、確認手上有無物品、以及清空雙手。
/// 掛載對象：場景中的 Cursor 游標主物件。
/// 交接注意：此腳本處理的是「邏輯上的拿取」。
///         未來 OAK-D 攝影機接管時，只需負責移動 Cursor 物件的 (X,Y) 座標，
///         抓取與放下的邏輯直接呼叫此腳本的方法即可，無須重寫。
/// </summary>
public class CursorHandController : MonoBehaviour
{
    // 單例模式 (Singleton)：讓其他腳本可以隨時透過 CursorHandController.Instance 呼叫它，不用慢慢找物件。
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
        Instance = this; // 遊戲一開始時，把自己的身分登記到 Instance，方便別人呼叫
    }

    /// <summary>
    /// 讓游標「抓起」指定的水果。
    /// 會自動複製一顆該水果的 Prefab，並將其綁定在手套的 HoldingLayer 底下。
    /// </summary>
    /// <param name="fruitPrefab">要抓取的水果 Prefab</param>
    public void Hold(GameObject fruitPrefab)
    {
        // 1. 防呆機制：如果手上已經有拿東西了，先把它銷毀掉，確保手上只會拿一個
        if (currentHolding != null)
            Destroy(currentHolding);

        // 2. 生成實體：複製傳進來的水果 Prefab
        currentHolding = Instantiate(fruitPrefab);
        
        // 3. 設定父層：把複製出來的水果，變成 holdingLayer 的小孩 (false 代表不保留世界座標，跟著 UI 走)
        currentHolding.transform.SetParent(holdingLayer, false);
        
        // 4. 取得該水果的 UI 排版組件
        currentRect = currentHolding.GetComponent<RectTransform>();

        if (currentRect != null)
        {
            // 強制將錨點 (Anchor) 與軸心 (Pivot) 設為中心點 (0.5, 0.5)
            currentRect.anchorMin = new Vector2(0.5f, 0.5f);
            currentRect.anchorMax = new Vector2(0.5f, 0.5f);
            currentRect.pivot = new Vector2(0.5f, 0.5f);

            // 讓水果完美對齊 holdingLayer 的中心點
            currentRect.anchoredPosition = Vector2.zero;

            // 確保縮放比例是原始的 1 倍，不會變形
            currentRect.localScale = Vector3.one;
        }

        Debug.Log($"[CursorHand] 成功抓取 {fruitPrefab.name}");
    }

    /// <summary>
    /// 取得目前手上的水果物件實體。
    /// (給 GiftBox 判斷玩家正要把什麼放進紙箱時使用)
    /// </summary>
    public GameObject GetHolding()
    {
        return currentHolding;
    }

    /// <summary>
    /// 檢查手上是否有拿著東西。
    /// 回傳 true 代表有拿，false 代表空手。
    /// </summary>
    public bool HasHolding()
    {
        return currentHolding != null;
    }

    /// <summary>
    /// 清空雙手。
    /// (當玩家成功把水果放入紙箱，或是答錯要重來時呼叫)
    /// </summary>
    public void Clear()
    {
        if (currentHolding != null)
        {
            // 銷毀手上的水果實體
            Destroy(currentHolding);
            
            // 將記憶體指標清空，代表雙手正式淨空
            currentHolding = null;
            currentRect = null;

            Debug.Log("[CursorHand] 雙手已清空");
        }
    }
}