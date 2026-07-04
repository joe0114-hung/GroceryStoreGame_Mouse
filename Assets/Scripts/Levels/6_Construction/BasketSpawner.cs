using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 腳本功能：負責在遊戲畫面下方，根據題目需求自動生成對應的水果籃。
/// 掛載對象：場景中的 Managers / BasketSpawner 物件。
/// 運作原理：LevelManager 載入題目時會呼叫此腳本，自動過濾掉重複的水果，並在畫面上生成不重複的籃子。
/// </summary>
public class BasketSpawner : MonoBehaviour
{
    // 自訂資料結構：定義「水果種類」與「籃子 Prefab」的對應關係
    [System.Serializable]
    public struct BasketMapping
    {
        public FruitType fruitType;
        public GameObject basketPrefab;
    }

    [Header("生成位置")]
    [Tooltip("請將畫面上的 FruitBasketRoot (掛有 Horizontal Layout Group 的物件) 拖曳到這裡")]
    public Transform basketSpawnParent; 
    
    [Header("籃子資料庫")]
    [Tooltip("請在這裡設定所有種類的水果與對應的籃子 Prefab")]
    public List<BasketMapping> availableBaskets;

    /// <summary>
    /// 接收題目的水果陣列，並在畫面上生成對應的籃子。
    /// </summary>
    /// <param name="requiredFruits">目前題目需要的所有水果 (包含重複項目)</param>
    public void SpawnBaskets(FruitType[] requiredFruits)
    {
        // 1. 過濾重複的水果，並排除 None
        List<FruitType> uniqueFruits = requiredFruits
            .Where(f => f != FruitType.None)
            .Distinct()
            .ToList();

        Debug.Log($"[BasketSpawner] 題目需要生成 {uniqueFruits.Count} 種籃子。");

        // 2. 清空畫面上舊的籃子
        foreach (Transform child in basketSpawnParent)
        {
            Destroy(child.gameObject);
        }

        // 3. 根據不重複的清單，生成對應的籃子
        foreach (FruitType type in uniqueFruits)
        {
            GameObject prefabToSpawn = GetBasketPrefab(type);
            
            if (prefabToSpawn != null)
            {
                // 生成籃子並設為 basketSpawnParent 的子物件，讓 Layout Group 自動幫它們排版
                Instantiate(prefabToSpawn, basketSpawnParent);
                Debug.Log($"[BasketSpawner] 生成了 {type} 籃子");
            }
            else
            {
                Debug.LogWarning($"[BasketSpawner] 找不到名為 {type} 的籃子 Prefab！請檢查 Inspector 設定。");
            }
        }
    }

    /// <summary>
    /// 透過比對對應表，找出特定水果的籃子 Prefab。
    /// </summary>
    private GameObject GetBasketPrefab(FruitType type)
    {
        foreach (var mapping in availableBaskets)
        {
            if (mapping.fruitType == type)
            {
                return mapping.basketPrefab;
            }
        }
        return null;
    }
}