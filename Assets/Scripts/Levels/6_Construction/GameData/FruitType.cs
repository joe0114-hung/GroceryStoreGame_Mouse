using UnityEngine;

/// <summary>
/// 水果種類的列舉 (Enum)。
/// 作為系統中所有水果的統一「身分證字號」，方便 LevelManager 與 GiftBox 進行快速比對。
/// </summary>
public enum FruitType
{
    None,
    Apple,
    Grape,
    Orange
}
