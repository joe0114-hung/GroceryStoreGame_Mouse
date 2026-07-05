using UnityEngine;

/// <summary>
/// 腳本功能：閱卷老師。負責比對玩家紙箱裡的水果與題庫答案是否完全一致。
/// 掛載對象：場景中的 LevelManager 或 Manager 群組下。
/// 交接注意：未來 OAK-D 偵測到「雙手提交」動作時，應由關卡大腦呼叫此腳本的 CheckAnswer()。
/// </summary>
public class AnswerChecker : MonoBehaviour
{
    [Header("綁定物件")]
    [Tooltip("用來讀取玩家目前放了什麼水果的紙箱控制器")]
    [SerializeField] private GiftBoxController giftBox;
    
    /// <summary>
    /// 進行對答案比對。(請傳入當前關卡的正確題目資料)
    /// </summary>
    /// <param name="currentQuestion">目前這題的 QuestionData</param>
    /// <returns>全對回傳 true，有錯回傳 false</returns>
    public bool CheckAnswer(QuestionData currentQuestion)
    {
        // 1. 從紙箱取得玩家放的水果型別 (Enum)
        FruitType[] playerAnswers = giftBox.GetAllFruitTypes();
        
        // 2. 從題庫取得這題的正確解答 (Enum)
        FruitType[] correctAnswers = currentQuestion.order;

        // 防呆：確保題目設定的格子數量與紙箱一致 (通常是 4 格)
        if (playerAnswers.Length != correctAnswers.Length)
        {
            Debug.LogError("[AnswerChecker] ❌ 嚴重錯誤：題庫設定的長度與紙箱格子數不符！");
            return false;
        }

        // 3. 開始逐格比對
        bool isAllCorrect = true;
        string debugLog = "[AnswerChecker] 批改結果：";

        for (int i = 0; i < correctAnswers.Length; i++)
        {
            if (playerAnswers[i] == correctAnswers[i])
            {
                debugLog += $"[V 格子{i}] ";
            }
            else
            {
                debugLog += $"[X 格子{i} 應為:{correctAnswers[i]} 實為:{playerAnswers[i]}] ";
                isAllCorrect = false;
            }
        }

        // 4. 印出批改明細，方便開發除錯
        Debug.Log(debugLog);

        // 5. 回報最終結果
        if (isAllCorrect)
        {
            Debug.Log("🎉 判定結果：完全正確！準備進入下一題！");
        }
        else
        {
            Debug.Log("⚠️ 判定結果：有錯誤喔，請玩家再檢查看看！");
        }

        return isAllCorrect; // 將結果回傳給 LevelManager 決定下一步
    }
}