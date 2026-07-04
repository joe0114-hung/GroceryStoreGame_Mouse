using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 腳本功能：無障礙懸停 (Dwell) 觸發系統。
/// 只要游標停留在帶有 InteractableUI 的物件上超過指定時間，就會自動發出 OnSelect 事件。
/// 掛載對象：場景中的 InteractionManager。
/// 交接注意：此腳本 100% 獨立於硬體輸入！它是追蹤「虛擬游標 (cursor)」的位置。
///         因此接入 OAK-D 時，此腳本「完全不需要」任何修改即可正常運作。
/// </summary>
public class DwellInteractor : MonoBehaviour
{
    [Header("懸停觸發設定")]
    [Tooltip("要追蹤的游標物件 (通常是 Cursor)")]
    [SerializeField] private RectTransform cursor;
    
    [Tooltip("需要停留多少秒才會觸發點擊？")]
    [SerializeField] private float dwellTime = 2f;

    // 內部狀態追蹤
    private GameObject currentTarget; // 目前游標停留在誰身上
    private float timer;              // 目前停留了幾秒
    private bool hasSelected;         // 是否已經觸發過選取 (避免連續觸發)

    // UI 射線偵測專用變數
    private PointerEventData pointerData;
    private readonly List<RaycastResult> raycastResults = new();

    private void Awake()
    {
        Debug.Log("[Dwell] 懸停系統已啟動");
        // 初始化 EventSystem 的指標資料，用來模擬射線
        pointerData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        // 防呆：如果沒有綁定游標，或場景沒有 EventSystem，就不做事
        if (cursor == null || EventSystem.current == null)
            return;

        // 1. 將虛擬射線的發射點，對齊現在的游標位置
        pointerData.position = cursor.position;

        // 2. 清空上一次的射線結果，並發射新射線打穿游標底下的所有 UI
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        GameObject newTarget = null;

        // 3. 從射線打到的物件中，找出第一個身上帶有 InteractableUI 的可互動物件
        foreach (var result in raycastResults)
        {
            InteractableUI ui = result.gameObject.GetComponentInParent<InteractableUI>();
            if (ui != null)
            {
                newTarget = ui.gameObject;
                break; 
            }
        }

        // 4. 判斷游標是不是「換目標」了 (包含移到空白處)
        if (newTarget != currentTarget)
        {
            currentTarget = newTarget;
            timer = 0f;          // 只要換目標，計時器就歸零
            hasSelected = false; // 重置觸發狀態

            if (currentTarget != null)
                Debug.Log($"[Hover] 游標進入：{currentTarget.name}");
        }

        // 5. 如果現在沒有指著任何可互動物件，或已經觸發過了，就不繼續計時
        if (currentTarget == null || hasSelected)
            return;

        // 6. 累積停留時間
        timer += Time.deltaTime;

        // 7. 達到指定時間，正式觸發選取事件！
        if (timer >= dwellTime)
        {
            hasSelected = true;
            Debug.Log($"[Select] 懸停觸發成功：{currentTarget.name}");

            // 廣播事件，通知其他系統 (例如 GiftBox 或 CursorHand) 這個物件被選中了
            InteractionEvents.OnSelect?.Invoke(currentTarget);
        }
    }
}
