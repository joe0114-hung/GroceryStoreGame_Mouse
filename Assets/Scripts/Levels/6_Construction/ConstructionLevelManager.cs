using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 腳本功能：第六關 (建構力) 的關卡核心大腦。
/// 負責控管題目的隨機生成、通知 UI 更新，以及判定玩家放入紙箱的水果是否正確。
/// 掛載對象：Hierarchy 中的 Managers / ConstructionLevelController 物件。
/// 交接注意：目前的 SubmitAnswer() 是由畫面上的 Button 觸發。
///         未來接入 OAK-D 後，請由攝影機的「雙手提交動作」事件來呼叫此方法。
/// </summary>
public class ConstructionLevelManager : MonoBehaviour
{
    [Header("資料庫")]
    [Tooltip("請放入第六關的專屬題庫")]
    [SerializeField] private QuestionDatabase database; 

    [Tooltip("每次遊戲要從題庫中抽出幾題？(預設5題)")]
    [SerializeField] private int maxQuestionsPerGame = 5;

    [Header("核心模組綁定")]
    [Tooltip("負責顯示畫面上方的訂單需求")]
    [SerializeField] private OrderUI orderUI;
    [Tooltip("負責在畫面下方生成水果籃")]
    [SerializeField] private BasketSpawner basketSpawner;
    [Tooltip("玩家放水果的紙箱")]
    [SerializeField] private GiftBoxController giftBox; 
    [Tooltip("負責比對答案")]
    [SerializeField] private AnswerChecker answerChecker;
    [Tooltip("全部答完後要切換到的下一個狀態")]
    [SerializeField] private GameFlowController.GameState nextState = GameFlowController.GameState.StampCheck;

    // 內部狀態追蹤
    private List<QuestionData> currentRoundQuestions = new List<QuestionData>(); // 存放這回合抽出的 5 題
    private int currentProgressIndex = 0; // 紀錄目前答到第幾題 (0 ~ 4)

    private void Start()
    {
        // 遊戲開始，先檢查題庫是否正常
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

    /// <summary>
    /// 洗牌演算法：從總題庫中隨機抽出指定數量的題目，存入本局清單。
    /// </summary>
    private void InitializeRandomQuestions()
    {
        // 1. 將題庫轉為 List
        List<QuestionData> allQuestions = database.questions.ToList();

        // 2. 隨機打亂題庫順序 (使用 LINQ 與 Unity 的 Random.value)
        allQuestions = allQuestions.OrderBy(x => Random.value).ToList();

        // 3. 挑選指定數量的題目
        // (防呆機制：如果未來題庫少於 5 題，就以題庫最大數量為準，避免報錯)
        int questionCount = Mathf.Min(maxQuestionsPerGame, allQuestions.Count);
        
        // 4. 擷取前面的 N 題，變成這回合真正的考卷
        currentRoundQuestions = allQuestions.Take(questionCount).ToList();

        Debug.Log($"[LevelManager] 題庫準備完畢！共 {allQuestions.Count} 題，已隨機抽出 {currentRoundQuestions.Count} 題。");
    }

    public void GenerateLevel(int index)
    {
        // 防呆：確保 index 沒有超出我們抽出的題目清單
        if (index < 0 || index >= currentRoundQuestions.Count) return;

        // 從「本回合抽出的題單」中拿出題目，而不是從總題庫拿
        var question = currentRoundQuestions[index];
        Debug.Log($"[LevelManager] 載入本局第 {index + 1} / {currentRoundQuestions.Count} 題");

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
        if (currentRoundQuestions == null || currentRoundQuestions.Count <= currentProgressIndex) return;
        if (answerChecker == null || giftBox == null)
        {
            Debug.LogError("[LevelManager] 系統元件遺失，無法判定答案！");
            return;
        }

        Debug.Log("[LevelManager] 收到提交指令，批改中...");

        // 1. 取得目前這題的完整考卷資料
        QuestionData currentQuestion = currentRoundQuestions[currentProgressIndex];

        // 2. 呼叫 AnswerChecker 進行批改
        bool isCorrect = answerChecker.CheckAnswer(currentQuestion);

        // 3. 根據批改結果，控制遊戲流程
        if (isCorrect)
        {
            // 答對了：清空紙箱，題號 +1
            giftBox.ClearBox(); 
            currentProgressIndex++;
            
            if (currentProgressIndex < currentRoundQuestions.Count)
            {
                // 載入下一題
                GenerateLevel(currentProgressIndex);
            }
            else
            {
                Debug.Log("🎉 [LevelManager] 恭喜！ 5題全部答對了！ 關卡結束");
                if (StampManager.Instance != null)
                {
                    StampManager.Instance.QueuePendingStamp(6);
                }
                if (GameFlowController.Instance != null)
                {
                    GameFlowController.Instance.ChangeState(nextState);
                }
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
