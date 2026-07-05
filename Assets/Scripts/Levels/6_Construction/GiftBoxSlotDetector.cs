using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
// ⚠️ 移除了 UnityEngine.InputSystem，因為我們不再依賴實體滑鼠了！

/// <summary>
/// 腳本功能：紙箱格子感應器。持續發射射線偵測「虛擬游標」目前停留在哪一個格子上，並回報給 GiftBox。
/// 掛載對象：場景中的 GiftBox / SlotRoot 物件。
/// 交接注意：已將實體滑鼠 (Mouse.current) 徹底解耦！
///         未來 OAK-D 接管虛擬游標時，此腳本不需任何修改即可繼續精準判定。
/// </summary>
public class GiftBoxSlotDetector : MonoBehaviour
{
    [Header("核心綁定")]
    [Tooltip("請綁定場景中的虛擬游標 (Cursor)")]
    [SerializeField] private RectTransform cursor;
    
    [Tooltip("請綁定負責接收判定結果的 GiftBoxController")]
    [SerializeField] private GiftBoxController giftBox;
    
    [Header("格子陣列")]
    [Tooltip("請把 4 個 Slot 依序拖曳到這裡")]
    [SerializeField] private RectTransform[] slots;

    // 效能優化：預先建立好變數，避免在 Update 中瘋狂 new 產生記憶體垃圾
    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Start()
    {
        // 初始化射線資料 (只需執行一次)
        if (EventSystem.current != null)
        {
            pointerData = new PointerEventData(EventSystem.current);
        }
    }

    private void Update()
    {
        // 防呆保護
        if (giftBox == null || cursor == null || pointerData == null) return;

        // 取得目前游標懸停的格子，並回報給紙箱
        int index = GetHoveredSlot();
        giftBox.SetHoverSlot(index);
    }

    private int GetHoveredSlot()
    {
        // 1. 將射線發射點對齊「虛擬游標」的當前位置 (取代了原本寫死的 Mouse.current)
        pointerData.position = cursor.position;

        // 2. 清空舊結果，發射新射線打穿所有 UI
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        // 3. 檢查射線打到的所有物件
        foreach (var result in raycastResults)
        {
            // 拿出我們準備好的 4 個 Slot 來比對
            for (int i = 0; i < slots.Length; i++)
            {
                // 🔥 關鍵判定：只要打到的物件「是 Slot 本身」或「是 Slot 的子層」，都直接判定成功！
                if (slots[i] != null && result.gameObject.transform.IsChildOf(slots[i]))
                {
                    return i; 
                }
            }
        }

        return -1; // 如果都沒有打到格子，回傳 -1
    }
}