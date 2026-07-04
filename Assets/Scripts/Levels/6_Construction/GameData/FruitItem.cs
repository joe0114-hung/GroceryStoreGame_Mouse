using UnityEngine;

/// <summary>
/// 腳本功能：水果實體的專屬身分證。
/// 掛載對象：所有水果的 Prefab (如 Fruit_apple, Fruit_banana 等) 身上。
/// 運作原理：當玩家把水果放進紙箱時，GiftBox 會讀取此腳本的 fruitType 來記錄玩家放了什麼。
/// </summary>
public class FruitItem : MonoBehaviour
{
    public FruitType fruitType;
}