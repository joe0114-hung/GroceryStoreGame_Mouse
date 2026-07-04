using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 腳本功能：負責紙箱 (GiftBox) 的核心邏輯。管理 4 個格子的狀態、放水果、拿水果以及對答案前的資料彙整。
/// 掛載對象：場景中的 GiftBox 主物件。
/// </summary>
public class GiftBoxController : MonoBehaviour
{
    [Header("Slots (UI 位置綁定)")]
    [Tooltip("請依序拖入紙箱內的 4 個格子 (Slot_0 到 Slot_3)")]
    [SerializeField] private RectTransform[] slotRoots; 

    // 內部陣列紀錄：目前 4 個格子裝了什麼水果的實體與型別
    private GameObject[] slotFruits = new GameObject[4];
    private FruitType[] slotTypes = new FruitType[4];

    // 紀錄目前游標正指著哪一個格子 (-1 代表沒有指著任何格子)
    private int hoverSlotIndex = -1;

    private void OnEnable()
    {
        InteractionEvents.OnSelect += OnSelect;
    }

    private void OnDisable()
    {
        InteractionEvents.OnSelect -= OnSelect;
    }

    /// <summary>
    /// 處理懸停觸發時的動作分配 (放水果 vs 拿水果)
    /// </summary>
    private void OnSelect(GameObject obj)
    {
        if (obj == null) return;

        // 用 parent 判斷，確保打到的是 GiftBox 或它底下的 Slot
        if (!obj.transform.IsChildOf(transform))
            return;

        Debug.Log($"[GiftBox] 觸發互動：{obj.name}");

        // 取得目前游標上的狀態
        GameObject holding = CursorHandController.Instance.GetHolding();

        // 情境 A：手上是空的 ➔ 嘗試從格子裡拿出水果
        if (holding == null)
        {
            if (hoverSlotIndex != -1 && slotFruits[hoverSlotIndex] != null)
            {
                GameObject fruitInSlot = slotFruits[hoverSlotIndex];
                CursorHandController.Instance.Hold(fruitInSlot);
                TakeFruit(hoverSlotIndex);
                
                Debug.Log($"[GiftBox] 成功從 Slot {hoverSlotIndex} 拿出水果！");
            }
            else
            {
                Debug.Log("[GiftBox] 操作無效：手上沒東西，且格子也是空的");
            }
            return; 
        }

        // 情境 B：手上有拿水果 ➔ 嘗試放進格子裡
        bool isPlaced = PutFruit(holding);
        
        // 如果放置成功，才把游標手上的水果實體清空
        if (isPlaced)
        {
            CursorHandController.Instance.Clear();
        }
    }

    /// <summary>
    /// 將水果放入指定的格子中。
    /// </summary>
    /// <returns>放置是否成功</returns>
    public bool PutFruit(GameObject fruitPrefab)
    {
        if (hoverSlotIndex == -1)
        {
            Debug.Log("[GiftBox] 放置失敗：沒有對準任何格子");
            return false;
        }

        if (slotFruits[hoverSlotIndex] != null)
        {
            Debug.Log("[GiftBox] 放置失敗：該格子已經有水果了");
            return false;
        }

        if (fruitPrefab == null) return false;

        // 複製水果實體並放在格子的 UI 節點下
        GameObject fruit = Instantiate(fruitPrefab, slotRoots[hoverSlotIndex]);
        fruit.name = fruitPrefab.name.Replace("(Clone)", ""); 

        // 重置排版大小
        RectTransform rt = fruit.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // 更新陣列紀錄
        slotFruits[hoverSlotIndex] = fruit;
        FruitItem item = fruitPrefab.GetComponent<FruitItem>();
        slotTypes[hoverSlotIndex] = (item != null) ? item.fruitType : FruitType.None;

        Debug.Log($"[GiftBox] 成功將 {fruit.name} 放入 Slot {hoverSlotIndex}");
        return true;
    }

    /// <summary>
    /// 從指定的格子中取出並銷毀水果實體。
    /// </summary>
    public GameObject TakeFruit(int index)
    {
        if (index < 0 || index >= slotFruits.Length) return null;

        GameObject temp = slotFruits[index];

        if (temp != null)
        {
            Destroy(temp);
            slotFruits[index] = null;
            slotTypes[index] = FruitType.None;
        }

        Debug.Log($"[GiftBox] 已從 Slot {index} 移除水果");
        return temp;
    }

    /// <summary>
    /// 更新目前游標懸停的格子索引 (由 SlotDetector 呼叫)
    /// </summary>
    public void SetHoverSlot(int index)
    {
        if (hoverSlotIndex != index)
        {
            hoverSlotIndex = index;
            if (index != -1)
                Debug.Log($"[GiftBox] Hover Slot {index}");
        }
    }
    
    // --------------------------------------------------------
    // 以下為提供給 LevelManager (大腦) 對答案用的公開方法 API
    // --------------------------------------------------------

    /// <summary> 檢查紙箱內是否至少有一顆水果 </summary>
    public bool HasAnyFruit()
    {
        for (int i = 0; i < slotFruits.Length; i++)
        {
            if (slotFruits[i] != null) return true;
        }
        return false;
    }

    /// <summary> 取得 4 個格子內的水果名稱陣列 </summary>
    public string[] GetAllFruits()
    {
        string[] result = new string[4];
        for (int i = 0; i < slotFruits.Length; i++)
        {
            if (slotFruits[i] != null)
                result[i] = slotFruits[i].name;
        }
        return result;
    }

    /// <summary> 取得 4 個格子內的水果列舉 (Enum) 陣列 </summary>
    public FruitType[] GetAllFruitTypes()
    {
        return slotTypes;
    }
    
    /// <summary> 徹底清空紙箱 (換題或答錯時使用) </summary>
    public void ClearBox()
    {
        for (int i = 0; i < slotFruits.Length; i++)
        {
            if (slotFruits[i] != null)
            {
                Destroy(slotFruits[i]); 
                slotFruits[i] = null;   
                slotTypes[i] = FruitType.None; 
            }
        }
        Debug.Log("[GiftBox] 紙箱已全面清空！");
    }


}