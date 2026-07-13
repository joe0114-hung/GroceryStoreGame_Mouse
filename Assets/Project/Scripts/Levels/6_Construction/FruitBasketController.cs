using UnityEngine;

/// <summary>
/// 腳本功能：單一水果籃的互動控制器。當玩家懸停/點擊此籃子時，會將對應的水果 Prefab 交給游標。
/// 掛載對象：各種 FruitBasket (水果籃) 的 Prefab 身上。
/// 加入防呆機制：若手上已拿相同水果則不顯示進度條；若拿不同水果則允許直接替換。
/// </summary>
public class FruitBasketController : MonoBehaviour
{
    [Header("籃子基本設定")]
    [Tooltip("這個籃子專屬的水果 Prefab (例如蘋果籃就放 Fruit_apple)")]
    [SerializeField] private GameObject fruitPrefab;

    [Header("生成測試區 (目前未使用)")]
    [Tooltip("若需在籃子旁生成實體水果，指定的父物件節點")]
    [SerializeField] private Transform spawnParent;
    [Tooltip("生成的偏移位置")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(150, 0);

    private int spawnCount = 0;
    
    // 抓取自己身上的互動標籤
    private InteractableUI myInteractableTag;

    private void Awake()
    {
        // 取得標籤組件，用來動態控制要不要讓眼睛(Dwell)看到
        myInteractableTag = GetComponent<InteractableUI>();
    }

    private void OnEnable()
    {
        InteractionEvents.OnSelect += OnSelect;
    }

    private void OnDisable()
    {
        InteractionEvents.OnSelect -= OnSelect;
    }

    private void Update()
    {
        // 防呆邏輯：動態決定這個籃子要不要理會游標的射線
        if (CursorHandController.Instance != null && myInteractableTag != null)
        {
            GameObject currentHeld = CursorHandController.Instance.GetHolding();

            if (currentHeld != null && fruitPrefab != null)
            {
                // 判斷手上的水果是不是跟我這個籃子要給的一樣
                bool isHoldingSameFruit = currentHeld.name.Replace("(Clone)", "") == fruitPrefab.name;

                // 如果拿著一樣的，就關閉標籤不給互動；如果不一樣，就開啟標籤允許替換
                myInteractableTag.enabled = !isHoldingSameFruit;
            }
            else
            {
                // 手是空的：當然可以互動
                myInteractableTag.enabled = true;
            }
        }
    }

    /// <summary>
    /// 接收 Dwell 懸停或滑鼠點擊的事件。
    /// </summary>
    private void OnSelect(GameObject obj)
    {
        // 確認被點擊的物件是不是自己 (這個籃子)
        if (obj != gameObject)
            return;

        // 呼叫游標控制器，把設定好的水果 Prefab 交給游標去實體化
        // (CursorHandController 內部已處理掉舊水果的銷毀)
        if (fruitPrefab != null)
        {
            CursorHandController.Instance.Hold(fruitPrefab);
        }
    }

    /// <summary>
    /// (備用功能) 在籃子旁邊實體化一顆水果。
    /// 目前的流程是直接讓游標 Hold()，此方法可作為未來擴充或測試保留。
    /// </summary>
    private void SpawnFruit()
    {
        GameObject fruit = Instantiate(fruitPrefab, spawnParent);

        RectTransform rt = fruit.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchoredPosition = spawnOffset + new Vector2(0, -120 * spawnCount);
        }

        spawnCount++;

        Debug.Log($"[Fruit Spawn] 籃子旁生成第 {spawnCount} 顆水果");
    }
}