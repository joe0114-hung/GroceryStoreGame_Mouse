using UnityEngine;

/// <summary>
/// 資料結構：單一題目的考卷設定。
/// 這是一份純資料表，不需要掛載在場景物件上。
/// </summary>
[System.Serializable] // 必須保留此標籤，Unity 的 Inspector 才能顯示並編輯它
public class QuestionData
{
    [Header("題目答案設定")]
    [Tooltip("請設定這題 4 個格子應該放什麼水果。\n陣列長度請設為 4，對應 Slot 0~3。")]
    public FruitType[] order;
}