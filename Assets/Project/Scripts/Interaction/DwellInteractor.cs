using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 腳本功能：無障礙懸停 (Dwell) 觸發系統。
/// 只要游標停留在帶有 InteractableUI 的物件上超過指定時間，就會自動發出 OnSelect 事件。
/// 掛載對象：場景中的 InteractionManager。
/// 加入冷卻機制：觸發後強制休息指定秒數，避免連續誤觸 (Double Trigger)。
/// </summary>
public class DwellInteractor : MonoBehaviour
{
    [Header("懸停觸發設定")]
    [Tooltip("要追蹤的游標物件 (若為空，會自動尋找名為 Cursor 的全域游標)")]
    [SerializeField] private RectTransform cursor;
    
    [Tooltip("需要停留多少秒才會觸發點擊？")]
    [SerializeField] private float dwellTime = 2f;

    [Tooltip("觸發成功後，系統要強制休息幾秒？(給長輩移開手的反應時間)")]
    [SerializeField] private float cooldownDuration = 1.5f;

    // 內部狀態追蹤
    private GameObject currentTarget; // 目前游標停留在誰身上
    private float timer;              // 目前停留了幾秒
    private bool hasSelected;         // 是否已經觸發過選取 (避免連續觸發)
    private float cooldownTimer;      //  冷卻計時器

    // UI 射線偵測專用變數
    private PointerEventData pointerData;
    private readonly List<RaycastResult> raycastResults = new();

    [Header("UI 反饋")]
    [Tooltip("綁定畫面上跟隨游標的進度條圓圈 Image (填滿類型需為 Filled)")]
    [SerializeField] private UnityEngine.UI.Image progressRing;

    private void Awake()
    {
        Debug.Log("[Dwell] 懸停系統已啟動");
        pointerData = new PointerEventData(EventSystem.current);
    }

    private void Start()
    {
        //  防呆：如果 Inspector 沒綁定游標，就自動去全域找
        if (cursor == null)
        {
            GameObject globalCursor = GameObject.Find("Cursor");
            if (globalCursor != null) cursor = globalCursor.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (cursor == null || EventSystem.current == null) return;

        //  1. 檢查是否在「冷卻防護」狀態中
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime; // 倒數冷卻時間

            // 冷卻期間強制把進度條歸零隱藏
            if (progressRing != null) progressRing.fillAmount = 0f;

            // 強制脫離目前的目標，這樣冷卻結束後，長輩必須重新把視線對準目標才會開始計算
            if (currentTarget != null)
            {
                InteractionEvents.OnHoverExit?.Invoke(currentTarget);
                currentTarget = null;
            }
            
            return; // 只要還在冷卻，就直接 Return，下方所有的射線與進度條都不執行！
        }

        // 2. 將虛擬射線的發射點，對齊現在的游標位置
        pointerData.position = cursor.position;

        // 3. 清空上一次的射線結果，並發射新射線打穿游標底下的所有 UI
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        GameObject newTarget = null;

        // 4. 從射線打到的物件中，找出第一個身上帶有 InteractableUI 的可互動物件
        foreach (var result in raycastResults)
        {
            // 注意：因為你有些 InteractableUI 可能掛在子物件，所以用 GetComponentInParent 是對的
            InteractableUI ui = result.gameObject.GetComponentInParent<InteractableUI>();
            
            // 確保這個標籤是開啟 (enabled) 的才算數！(配合我們剛剛水果籃的防呆機制)
            if (ui != null && ui.enabled)
            {
                newTarget = ui.gameObject;
                break; 
            }
        }

        // 5. 判斷游標是不是「換目標」了 (包含移到空白處)
        if (newTarget != currentTarget)
        {
            GameObject previousTarget = currentTarget;
            currentTarget = newTarget;
            timer = 0f;          // 只要換目標，計時器就歸零
            hasSelected = false; // 重置觸發狀態

            if (progressRing != null)
            {
                progressRing.fillAmount = 0f;
            }

            if (previousTarget != null)
            {
                InteractionEvents.OnHoverExit?.Invoke(previousTarget);
            }

            if (currentTarget != null)
            {
                Debug.Log($"[Hover] 游標進入：{currentTarget.name}");
                InteractionEvents.OnHoverEnter?.Invoke(currentTarget);
            }
        }

        // 6. 如果現在沒有指著任何可互動物件，或已經觸發過了，就不繼續計時
        if (currentTarget == null || hasSelected)
            return;

        // 7. 累積停留時間
        timer += Time.deltaTime;

        if (progressRing != null)
        {
            progressRing.fillAmount = timer / dwellTime;
        }

        // 8. 達到指定時間，正式觸發選取事件！
        if (timer >= dwellTime)
        {
            hasSelected = true;
            Debug.Log($"[Select] 懸停觸發成功：{currentTarget.name}");

            // 廣播事件，通知其他系統
            InteractionEvents.OnSelect?.Invoke(currentTarget);

            //  9. 成功觸發後，立刻啟動冷卻防護罩！
            cooldownTimer = cooldownDuration;
        }
    }
}