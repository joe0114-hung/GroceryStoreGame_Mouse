using UnityEngine;
using System.Linq;

/// <summary>
/// 腳本功能：第六關 (建構力) 的關卡核心大腦。
/// 負責控管題目的生成、通知 UI 更新，以及判定玩家放入紙箱的水果是否正確。
/// 掛載對象：Hierarchy 中的 Managers / ConstructionLevelController 物件。
/// 交接注意：目前的 SubmitAnswer() 是由畫面上的 Button 觸發。
///         未來接入 OAK-D 後，請由攝影機的「雙手提交動作」事件來呼叫此方法。
/// </summary>
public class ConstructionLevelManager : MonoBehaviour
{
    [Header("資料庫")]
    [Tooltip("請放入第六關的專屬題庫")]
    [SerializeField] private QuestionDatabase database; 

    [Header("核心模組綁定")]
    [Tooltip("負責顯示畫面上方的訂單需求")]
    [SerializeField] private OrderUI orderUI;
    [Tooltip("負責在畫面下方生成水果籃")]
    [SerializeField] private BasketSpawner basketSpawner;
    [Tooltip("玩家放水果的紙箱")]
    [SerializeField] private GiftBoxController giftBox; 
    [Tooltip("負責比對答案")]
    [SerializeField] private AnswerChecker answerChecker;

    // 紀錄目前進行到第幾題
    private int currentQuestionIndex = 0;

    private void Start()
    {
        // 遊戲開始先載入第一題
        if (database != null && database.questions != null && database.questions.Length > 0)
        {
            GenerateLevel(currentQuestionIndex);
        }
        else
        {
            Debug.LogWarning("[LevelManager] 題庫是空的，或沒有綁定 QuestionDatabase！");
        }
    }

    public void GenerateLevel(int index)
    {
        if (index < 0 || index >= database.questions.Length) return;

        var question = database.questions[index];
        Debug.Log($"[LevelManager] 載入第 {index} 題");

        if (basketSpawner != null) basketSpawner.SpawnBaskets(question.order);
        if (orderUI != null) orderUI.ShowOrder(question.order);
    }
    /// <summary>
    /// 接收玩家提交答案的指令。
    /// 會將目前考卷交給 AnswerChecker 批改，並根據結果決定進入下一題或重新挑戰。
    /// </summary>
    public void SubmitAnswer()
    {
        // 防呆：確保題庫與閱卷老師都在
        if (database == null || database.questions.Length <= currentQuestionIndex) return;
        if (answerChecker == null || giftBox == null)
        {
            Debug.LogError("[LevelManager] 找不到 AnswerChecker 或 GiftBox，無法判定答案！請檢查 Inspector。");
            return;
        }

        Debug.Log("[LevelManager] 收到提交指令，交給閱卷老師批改...");

        // 1. 取得目前這題的完整考卷資料
        QuestionData currentQuestion = database.questions[currentQuestionIndex];

        // 2. 呼叫 AnswerChecker 進行批改
        bool isCorrect = answerChecker.CheckAnswer(currentQuestion);

        // 3. 根據批改結果，控制遊戲流程
        if (isCorrect)
        {
            // 答對了：清空紙箱，題號 +1
            giftBox.ClearBox(); 
            currentQuestionIndex++;
            
            if (currentQuestionIndex < database.questions.Length)
            {
                // 載入下一題
                GenerateLevel(currentQuestionIndex);
            }
            else
            {
                Debug.Log("🎉 [LevelManager] 恭喜！所有題目都完成了！");
                // 這裡未來可以呼叫結算畫面、播放成功音效，或回到標題
            }
        }
        else
        {
            // 答錯了：只清空紙箱，題號不變，讓玩家重新挑戰這一題
            giftBox.ClearBox();
        }
    }
}