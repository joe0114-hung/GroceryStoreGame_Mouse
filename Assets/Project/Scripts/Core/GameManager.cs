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
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("=== 全域輸入模式 (滑鼠/相機) ===")]
        [Tooltip("在這裡統一控制要使用滑鼠測試還是 OAK 相機，切換場景時會自動套用到該場景的 OAKInputReceiver")]
        [SerializeField] 
        private OAKInputReceiver.InputMode globalInputMode = OAKInputReceiver.InputMode.MouseOnly_滑鼠測試模式;

        [Header("=== 基礎核心流程場景 ===")]
        [SerializeField] private string loginSceneName = "01_Login";
        [SerializeField] private string openingStorySceneName = "02_OpeningStory";
        
        [Header("=== 過場與遊戲核心流程場景 ===")]
        // 🌟 1. 解開這裡的註解，把 CoreGameScene 加入變數清單
        [SerializeField] private string coreGameSceneName = "CoreGameScene"; 
        [SerializeField] private string storyIntroSceneName = "StoryIntroScene";
        [SerializeField] private string waitStartSceneName = "WaitStartScene";
        [SerializeField] private string countdownSceneName = "CountdownScene";
        [SerializeField] private string stampCheckSceneName = "StampCheckScene";
        
        [Header("=== 獨立測試場景 ===")]
        [SerializeField] private string controllerPalmSceneName = "Controller_Palm";

        [Header("開發測試模式")]//控制遊戲的「隨機抽題系統 (LevelRandomizer)」，因為目前只有一關
        [SerializeField] private bool devModeConstructionOnly = true;

        [Header("大關設定（Level_1 / Level_2 / Level_3）")]
        [SerializeField] private List<BigLevelConfig> bigLevelConfigs = new List<BigLevelConfig>();
        [SerializeField] private int currentBigLevelIndex = 0;

        public string CurrentPlayerName { get; private set; }
        public GameFlowState CurrentFlowState { get; private set; } = GameFlowState.Boot;
        public LevelDimension? CurrentDimension => _levelRandomizer.CurrentDimension;
        public event Action<GameFlowState> OnFlowStateChanged;

        private LevelRandomizer _levelRandomizer;

        // 開放屬性，讓外部（或 UI 按鈕）可以在遊戲執行中隨時切換模式
        public OAKInputReceiver.InputMode GlobalInputMode
        {
            get => globalInputMode;
            set
            {
                globalInputMode = value;
                SyncInputModeToOAK();
            }
        }

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
        }

        private void Start()
        {
            SetFlowState(GameFlowState.Boot);
        }

        // ==========================================
        // 註冊場景載入事件，確保每次切場景都強制同步輸入模式
        // ==========================================
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SyncInputModeToOAK();
        }

        /// <summary>
        /// 將 GameManager 設定的模式，強制覆寫給場景中的 OAKInputReceiver
        /// </summary>
        private void SyncInputModeToOAK()
        {
            if (OAKInputReceiver.Instance != null)
            {
                OAKInputReceiver.Instance.currentMode = this.globalInputMode;
                Debug.Log($"[GameManager] 已將場景中的輸入模式強制同步為: {this.globalInputMode}");
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SyncInputModeToOAK();
            }
        }

        // ------------------------------------------------------------
        // 給各場景 / 流程呼叫的公開入口
        // ------------------------------------------------------------
        public void NotifyBootCompleted() { SetFlowState(GameFlowState.Login); LoadScene(loginSceneName); }
        public void NotifyLoginCompleted(string playerName) { CurrentPlayerName = playerName; SetFlowState(GameFlowState.OpeningStory); LoadScene(openingStorySceneName); }
        
        // 🌟 2. 暖身結束後，現在改為先進入 CoreGameScene
        public void NotifyOpeningStoryCompleted() { SetFlowState(GameFlowState.OpeningStory); LoadScene(coreGameSceneName); }

        // 🌟 3. 新增這個！在你的 CoreGame 結束時呼叫它，就會進入 StoryIntroScene
        public void NotifyCoreGameCompleted() { LoadScene(storyIntroSceneName); }

        // 🌟 4. 原本的 StoryIntro 結束後，繼續往 WaitStart 走 (流程不變)
        public void NotifyStoryIntroCompleted() { LoadScene(waitStartSceneName); }
        
        public void NotifyWaitStartCompleted() { LoadScene(countdownSceneName); }
        
        public void NotifyCountdownCompleted()
        {
            if (bigLevelConfigs.Count == 0) return;
            var config = bigLevelConfigs[Mathf.Clamp(currentBigLevelIndex, 0, bigLevelConfigs.Count - 1)];
            _levelRandomizer.StartNewRound(config);
            SetFlowState(GameFlowState.LevelPlaying);
            LoadNextLevelOrFinishRound();
        }

        public void NotifyLevelCompleted() { LoadNextLevelOrFinishRound(); }
        public void NotifyStampCheckCompleted() { CurrentPlayerName = null; SetFlowState(GameFlowState.Login); LoadScene(loginSceneName); }
        public void ReturnToHome() { CurrentPlayerName = null; SetFlowState(GameFlowState.Login); LoadScene(loginSceneName); }
        public void LoadControllerPalmTest() { LoadScene(controllerPalmSceneName); }

        // ------------------------------------------------------------
        // 內部流程與場景加載保護
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
                LoadScene(stampCheckSceneName);
            }
        }

        private void SetFlowState(GameFlowState newState)
        {
            CurrentFlowState = newState;
            OnFlowStateChanged?.Invoke(newState);
        }

        public (int played, int total) GetRoundProgress()
        {
            return (_levelRandomizer.PlayedCount, _levelRandomizer.TotalInRound);
        }

        private void LoadScene(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[GameManager] 場景「{sceneName}」還沒被加進 Build Settings 或檔名不符，先跳過");
            }
        }
    }
}