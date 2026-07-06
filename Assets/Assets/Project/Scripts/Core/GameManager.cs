using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Flow;

// ------------------------------------------------------------------
// 下面這幾個系統目前還沒有人做，等做好之後把對應的地方換回來即可
// （所有需要替換的地方都用 TODO 標記，直接搜尋 "TODO" 就能找到）：
//   - Core/ServiceLocator.cs, Core/SceneLoader.cs, Core/GameConfig.cs
//   - Data/DataManager.cs
//   - Login/PlayerProfile.cs, Login/UserRole.cs
//   - Flow/LevelResult.cs
// using Project.Core;
// using Project.Data;
// using Project.Login;
// ------------------------------------------------------------------

namespace Project.Core
{
    /// <summary>
    /// 整個遊戲最高層管理者。掛在目前你測試用的起始場景（現階段建議掛在 01_Login.unity）裡，
    /// 一個叫 "GameManager" 的 GameObject 上，透過 DontDestroyOnLoad 存活到整個 App 結束。
    ///
    /// 負責：
    ///   - 控制最高層流程：Login -> OpeningStory -> Level 迴圈(向度) -> ProgressSummary -> ClosingStory
    ///   - 記錄目前玩家、目前流程狀態
    ///   - 協調 LevelRandomizer 抽關、切換場景
    ///
    /// 【這一版的簡化說明】
    /// 因為 ServiceLocator / SceneLoader / DataManager / PlayerProfile / LevelResult
    /// 這些檔案目前都還沒有人做，所以：
    ///   - 場景切換先直接呼叫 Unity 內建的 SceneManager，不透過 SceneLoader
    ///   - 玩家先只用簡單的字串名字（CurrentPlayerName），不用完整的 PlayerProfile 物件
    ///   - 關卡結束先不存檔，只負責推進到下一關
    ///   - 因為只有 1_6_Level_Construction 這關做好了，devModeConstructionOnly 開著的話，
    ///     這回合就只會抽到 Construction 這一關，這樣才能真的玩起來測試
    /// 等對應檔案做好後，把標記 TODO 的地方換掉，其他程式碼（Login/OpeningStory/各關卡）都不用再改。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("流程場景名稱（對應 Scenes/ 底下的檔名，不含 .unity）")]
        [SerializeField] private string loginSceneName = "01_Login";
        [SerializeField] private string openingStorySceneName = "02_OpeningStory";
        [SerializeField] private string progressSummarySceneName = "03_ProgressSummary";
        [SerializeField] private string closingStorySceneName = "04_ClosingStory";

        [Header("開發測試模式")]
        [Tooltip("目前只有 1_6_Level_Construction 這一關做好了，勾選後這回合只會抽到 Construction 這一關，方便先測整條流程。等其他 5 個向度的場景都做好後，記得關掉這個選項。")]
        [SerializeField] private bool devModeConstructionOnly = true;

        [Header("大關設定（Level_1 / Level_2 / Level_3）")]
        [Tooltip("留空的話，Awake 會自動套用預設值（依 devModeConstructionOnly 決定是只測 Construction，還是完整六個向度）")]
        [SerializeField] private List<BigLevelConfig> bigLevelConfigs = new List<BigLevelConfig>();
        [SerializeField] private int currentBigLevelIndex = 0;

        /// <summary>
        /// 目前登入使用中的玩家名稱。
        /// TODO: Login/PlayerProfile.cs 做好後，這裡改成完整的 PlayerProfile 物件（含 ID、年齡等資料），
        /// NotifyLoginCompleted 的參數也要一起換成 PlayerProfile。
        /// </summary>
        public string CurrentPlayerName { get; private set; }

        /// <summary>目前最高層流程狀態</summary>
        public GameFlowState CurrentFlowState { get; private set; } = GameFlowState.Boot;

        /// <summary>目前正在玩的向度，只有在 LevelPlaying 狀態時有意義</summary>
        public LevelDimension? CurrentDimension => _levelRandomizer.CurrentDimension;

        /// <summary>流程狀態改變時觸發，UI 可以監聽這個事件</summary>
        public event Action<GameFlowState> OnFlowStateChanged;

        private LevelRandomizer _levelRandomizer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _levelRandomizer = new LevelRandomizer();

            if (bigLevelConfigs == null || bigLevelConfigs.Count == 0)
            {
                bigLevelConfigs = new List<BigLevelConfig>
                {
                    devModeConstructionOnly
                        ? DefaultBigLevelConfigs.CreateConstructionOnlyForTesting()
                        : DefaultBigLevelConfigs.CreateLevel1Default()
                };
            }

            // TODO: Core/ServiceLocator.cs 做好後，取消下面這行註解，
            // 讓其他系統可以用 ServiceLocator.Get<GameManager>() 取得 GameManager
            // ServiceLocator.Register(this);
        }

        private void Start()
        {
            SetFlowState(GameFlowState.Boot);
        }

        // ------------------------------------------------------------
        // 給各場景 / 流程呼叫的公開入口
        // ------------------------------------------------------------

        /// <summary>00_Boot 場景初始化完成後呼叫（目前 00_Boot 還沒做，暫時用不到，先留著）</summary>
        public void NotifyBootCompleted()
        {
            SetFlowState(GameFlowState.Login);
            LoadScene(loginSceneName);
        }

        /// <summary>01_Login 完成登入後呼叫，記錄目前玩家並前往開店前情境</summary>
        public void NotifyLoginCompleted(string playerName)
        {
            CurrentPlayerName = playerName;
            SetFlowState(GameFlowState.OpeningStory);
            LoadScene(openingStorySceneName);
        }

        /// <summary>
        /// 02_OpeningStory（情境提要 + 今日任務說明 + 前置練習）全部跑完後呼叫。
        /// 會從目前大關的向度中洗牌，並自動載入第一關。
        /// </summary>
        public void NotifyOpeningStoryCompleted()
        {
            if (bigLevelConfigs.Count == 0)
            {
                Debug.LogError("[GameManager] bigLevelConfigs 是空的，請在 Inspector 設定至少一組大關設定");
                return;
            }

            var config = bigLevelConfigs[Mathf.Clamp(currentBigLevelIndex, 0, bigLevelConfigs.Count - 1)];
            _levelRandomizer.StartNewRound(config);

            SetFlowState(GameFlowState.LevelPlaying);
            LoadNextLevelOrFinishRound();
        }

        /// <summary>
        /// 單一關卡結束時呼叫（例如 ConstructionLevelController 答對後呼叫這個方法）。
        /// TODO: Flow/LevelResult.cs 和 Data/DataManager.cs 做好後，
        /// 改成 NotifyLevelCompleted(LevelResult result)，並在這裡把結果存檔。
        /// </summary>
        public void NotifyLevelCompleted()
        {
            // TODO: 存檔，例如 ServiceLocator.Get<DataManager>().SaveLevelResult(CurrentPlayerName, result);
            LoadNextLevelOrFinishRound();
        }

        /// <summary>03_ProgressSummary 播完進步動畫後呼叫，前往關店劇情（場景目前還沒做，暫時會被安全跳過）</summary>
        public void NotifyProgressSummaryCompleted()
        {
            SetFlowState(GameFlowState.ClosingStory);
            LoadScene(closingStorySceneName);
        }

        /// <summary>04_ClosingStory 結束後呼叫，回到登入畫面讓下一位使用者登入（場景目前還沒做，暫時會被安全跳過）</summary>
        public void NotifyClosingStoryCompleted()
        {
            // TODO: 存檔，例如 ServiceLocator.Get<DataManager>().SaveDailySessionComplete(CurrentPlayerName);
            CurrentPlayerName = null;

            SetFlowState(GameFlowState.Login);
            LoadScene(loginSceneName);
        }

        /// <summary>
        /// 玩家在遊戲中途，透過主選單主動按「回主頁」時呼叫。
        /// 跟 NotifyClosingStoryCompleted() 不同：這是玩家「中途主動退出」，
        /// 會直接跳過關店劇情，馬上回到登入畫面；NotifyClosingStoryCompleted() 是「六關都玩完、正常流程走完」才會走到。
        /// </summary>
        public void ReturnToHome()
        {
            CurrentPlayerName = null;
            SetFlowState(GameFlowState.Login);
            LoadScene(loginSceneName);
        }
        // ------------------------------------------------------------
        // 內部流程
        // ------------------------------------------------------------

        private void LoadNextLevelOrFinishRound()
        {
            if (_levelRandomizer.TryGetNextLevel(out var dimension, out var sceneName))
            {
                LoadScene(sceneName);
            }
            else
            {
                SetFlowState(GameFlowState.ProgressSummary);
                LoadScene(progressSummarySceneName);
            }
        }

        private void SetFlowState(GameFlowState newState)
        {
            CurrentFlowState = newState;
            OnFlowStateChanged?.Invoke(newState);
        }

        /// <summary>目前這回合第幾關 / 共幾關，給 UI 顯示用</summary>
        public (int played, int total) GetRoundProgress()
        {
            return (_levelRandomizer.PlayedCount, _levelRandomizer.TotalInRound);
        }

        /// <summary>
        /// TODO: Core/SceneLoader.cs 做好後，改成呼叫 ServiceLocator.Get&lt;SceneLoader&gt;().LoadScene(sceneName)，
        /// 現在先直接用 Unity 內建的 SceneManager 切場景，行為一樣，之後換掉不會影響其他程式碼的呼叫方式。
        ///
        /// 這裡多做了一個保護：如果場景還沒被加進 Build Settings（例如 03_ProgressSummary、04_ClosingStory、
        /// 其他 5 個向度的關卡場景目前都還沒做好），就只印警告訊息、不會讓遊戲當掉，
        /// 方便在其他場景還沒做好之前先測試已完成的部分。
        /// </summary>
        private void LoadScene(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[GameManager] 場景「{sceneName}」還沒被加進 Build Settings 或還沒做好，先跳過（測試階段屬正常現象）");
            }
        }
    }
}
