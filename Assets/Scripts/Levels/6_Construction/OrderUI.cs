using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 腳本功能：負責更新畫面上方的「訂單需求」圖示。
/// 掛載對象：場景中的 GameplayUI / OrderUI 物件。
/// 交接注意：圖示 Prefab 必須是「純圖片」，不可掛載任何互動或碰撞腳本，以免干擾 Dwell 射線偵測。
/// </summary>
public class OrderUI : MonoBehaviour
{
    // 自訂資料結構：定義「水果種類」與「單純顯示用圖示 Prefab」的對應關係
    [System.Serializable]
    public struct OrderIconMapping
    {
        public FruitType fruitType;
        public GameObject fruitIconPrefab; // 注意：這裡放的是「純顯示」的圖片 Prefab，不要掛互動腳本！
    }

    [Header("UI 綁定")]
    [Tooltip("請把 Gird 底下的 Slot_0 到 Slot_3 依序拖進來")]
    public Transform[] orderSlots;

    [Header("圖示設定")]
    [Tooltip("設定水果對應的訂單小圖示")]
    public List<OrderIconMapping> iconMappings;

    /// <summary>
    /// 接收題庫陣列，並將圖示顯示在看板上。
    /// </summary>
    /// <param name="requiredFruits">目前題目需要的水果陣列</param>
    public void ShowOrder(FruitType[] requiredFruits)
    {
        // 1. 先清空所有格子裡上一題留下來的舊圖示
        foreach (Transform slot in orderSlots)
        {
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }
        }

        // 2. 依照題目陣列，依序把水果圖示放進對應的格子裡
        for (int i = 0; i < requiredFruits.Length; i++)
        {
            FruitType currentFruit = requiredFruits[i];

            // 如果題目是 None，就跳過這個格子(留空)
            if (currentFruit == FruitType.None) continue;

            // 確保題目的數量沒有超出我們準備的格子數量 (4個)
            if (i < orderSlots.Length)
            {
                GameObject iconPrefab = GetIconPrefab(currentFruit);
                
                if (iconPrefab != null)
                {
                    // 生成圖示並設為格子的子物件
                    GameObject newIcon = Instantiate(iconPrefab, orderSlots[i]);
                    
                    // 取得 UI 變形組件並進行安全重置
                    RectTransform rt = newIcon.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        
                        rt.anchoredPosition = Vector2.zero;// 強制把座標歸零（對齊格子的正中心）
                        rt.localScale = Vector3.one;// 強制把大小重置為 1 倍（避免突然變超大或超小）
                    }
                }
                else
                {
                    Debug.LogWarning($"[OrderUI] 找不到 {currentFruit} 的訂單圖示！");
                }
            }
        }
    }

    /// <summary>
    /// 透過比對對應表，找出特定水果的圖示 Prefab。
    /// </summary>
    private GameObject GetIconPrefab(FruitType type)
    {
        foreach (var mapping in iconMappings)
        {
            if (mapping.fruitType == type) return mapping.fruitIconPrefab;
        }
        return null;
    }
}