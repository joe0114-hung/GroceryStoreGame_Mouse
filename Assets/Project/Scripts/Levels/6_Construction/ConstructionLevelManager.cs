using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 腳本功能：第六關 (建構力) 的關卡核心大腦。
/// 負責控管題目的隨機生成、通知 UI 更新，以及判定玩家放入紙箱的水果是否正確。
/// </summary>
public class ConstructionLevelManager : MonoBehaviour
{
    [Header("資料庫")]
    [Tooltip("請放入第六關的專屬題庫")]
    [SerializeField] private QuestionDatabase database; 

    [Tooltip("每次遊戲要從題庫中抽出幾題？(預設5題)")]
    [SerializeField] private int maxQuestionsPerGame = 5;

    [Header("核心模組綁定")]
    [SerializeField] private OrderUI orderUI;
    [SerializeField] private BasketSpawner basketSpawner;
    [SerializeField] private GiftBoxController giftBox; 
    [SerializeField] private AnswerChecker answerChecker;
    [SerializeField] private GameFlowController.GameState nextState = GameFlowController.GameState.StampCheck;

    // 內部狀態追蹤
    private List<QuestionData> currentRoundQuestions = new List<QuestionData>();
    private int currentProgressIndex = 0;

    private void Start()
    {
        if (database != null && database.questions != null && database.questions.Length > 0)
        {
            InitializeRandomQuestions();
            GenerateLevel(currentProgressIndex);
        }
        else
        {
            Debug.LogWarning("[LevelManager] 題庫是空的，或沒有綁定 QuestionDatabase！");
        }
    }

    private void InitializeRandomQuestions()
    {
        List<QuestionData> allQuestions = database.questions.ToList();
        allQuestions = allQuestions.OrderBy(x => Random.value).ToList();
        int questionCount = Mathf.Min(maxQuestionsPerGame, allQuestions.Count);
        currentRoundQuestions = allQuestions.Take(questionCount).ToList();
    }

    public void GenerateLevel(int index)
    {
        if (index < 0 || index >= currentRoundQuestions.Count) return;

        var question = currentRoundQuestions[index];
        if (basketSpawner != null) basketSpawner.SpawnBaskets(question.order);
        if (orderUI != null) orderUI.ShowOrder(question.order);
    }

    /// <summary>
    /// 接收答案提交 (可由 UI 按鈕呼叫，未來由 OAK-D 雙手舉高事件呼叫)
    /// </summary>
    public void SubmitAnswer()
    {
        // 安全檢查：若系統核心遺失則跳出
        if (currentRoundQuestions == null || currentRoundQuestions.Count <= currentProgressIndex) return;
        if (answerChecker == null || giftBox == null)
        {
            Debug.LogError("[LevelManager] 系統元件遺失，無法判定答案！");
            return;
        }

        // 偵測 OAKInputReceiver 是否在場景中 (方便除錯)
        if (OAKInputReceiver.Instance != null)
        {
            Debug.Log("[LevelManager] 偵測到 OAK-D 接收器，正在處理影像提交指令...");
        }
        // 強制清空雙手，確保進入下一題或答錯重來時手是乾淨的
        if (CursorHandController.Instance != null)
        {
            CursorHandController.Instance.Clear();
        }
        bool isCorrect = answerChecker.CheckAnswer(currentRoundQuestions[currentProgressIndex]);

        if (isCorrect)
        {
            giftBox.ClearBox(); 
            currentProgressIndex++;
            
            if (currentProgressIndex < currentRoundQuestions.Count)
            {
                GenerateLevel(currentProgressIndex);
            }
            else
            {
                CompleteLevel();
            }
        }
        else
        {
            giftBox.ClearBox();
        }
    }

    private void CompleteLevel()
    {
        Debug.Log("🎉 [LevelManager] 恭喜！關卡結束");
        if (StampManager.Instance != null)
        {
            StampManager.Instance.QueuePendingStamp(6);
        }
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(nextState);
        }
    }
}