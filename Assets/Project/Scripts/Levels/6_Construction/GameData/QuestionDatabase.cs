using UnityEngine;

/// <summary>
/// 資料結構：題庫大表。
/// 可以在 Project 視窗點擊右鍵 -> Game -> Question Database 來建立實體題庫檔 (.asset)。
/// </summary>
[CreateAssetMenu(fileName = "QuestionDatabase", menuName = "Game/Question Database")]
public class QuestionDatabase : ScriptableObject
{
    [Header("關卡題庫")]
    [Tooltip("這份題庫裡包含的所有題目順序")]
    public QuestionData[] questions;
}