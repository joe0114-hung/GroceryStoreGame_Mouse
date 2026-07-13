using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 腳本功能：遊戲全域流程控制器 (State Machine)。
/// 負責控管 教學 -> 舉手等待 -> 倒數 -> 正式關卡 的狀態切換，
/// 並自動處理對應場景的疊加載入 (Additive Load) 與卸載 (Unload)。
/// </summary>
public class GameFlowController : MonoBehaviour
{
    // 定義遊戲的所有狀態
    public enum GameState
    {
        None = 0,           // 初始空狀態
        Tutorial = 1,       // 教學階段
        WaitForStart = 2,   // 舉手開始階段
        Countdown = 3,      // 倒數階段
        PlayingLevel = 4,   // 關卡場景階段
        StoryIntro = 5,     // 故事開場階段
        StampCheck = 6      // 印章確認階段
    }

    public static GameFlowController Instance;

    [Header("目前狀態 (僅供觀察，請勿手動修改)")]
    public GameState currentState = GameState.None;

    [Header("各階段場景名稱設定")]
    public string storyIntroSceneName = "StoryIntroScene";
    public string stampCheckSceneName = "StampCheckScene";
    public string tutorialSceneName = "Controller_Palm";
    public string waitStartSceneName = "WaitStartScene"; // 請替換成你實際的場景名稱
    public string countdownSceneName = "CountdownScene"; // 請替換成你實際的場景名稱
    public string levelSceneName = "Construction";

    // 紀錄目前疊加在畫面上的是哪一個場景，方便之後卸載
    private string currentLoadedAdditiveScene = "";

    private void Awake()
    {
        // 確保全遊戲只有一個流程控制器
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 遊戲一啟動，先進入故事開場
        ChangeState(GameState.StoryIntro);
    }

    /// <summary>
    /// 給外部呼叫的狀態切換方法。
    /// 例如：教學結束時，呼叫 GameFlowController.Instance.ChangeState(GameState.WaitForStart);
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"[GameFlow] 準備從 {currentState} 切換至狀態：{newState}");
        currentState = newState;

        // 啟動協程來處理場景的切換 (先卸載舊的，再載入新的)
        StartCoroutine(SwitchSceneRoutine(GetSceneNameForState(newState)));
    }

    // 根據狀態，回傳對應的場景名稱
    private string GetSceneNameForState(GameState state)
    {
        switch (state)
        {
            case GameState.StoryIntro: return storyIntroSceneName;
            case GameState.StampCheck: return stampCheckSceneName;
            case GameState.Tutorial: return tutorialSceneName;
            case GameState.WaitForStart: return waitStartSceneName;
            case GameState.Countdown: return countdownSceneName;
            case GameState.PlayingLevel: return levelSceneName;
            default: return "";
        }
    }

    // 處理場景卸載與載入的非同步協程
    private IEnumerator SwitchSceneRoutine(string sceneToLoad)
    {
        // 1. 如果畫面上已經有疊加其他場景了，先把它卸載掉！
        if (!string.IsNullOrEmpty(currentLoadedAdditiveScene))
        {
            Debug.Log($"[GameFlow] 正在卸載舊場景：{currentLoadedAdditiveScene}");
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentLoadedAdditiveScene);
            
            // 等待直到卸載完全結束
            while (!unloadOp.isDone)
            {
                yield return null;
            }
        }

        // 2. 如果有設定新場景名稱，則把它疊加載入進來
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"[GameFlow] 正在疊加載入新場景：{sceneToLoad}");
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

            // 等待直到載入完全結束
            while (!loadOp.isDone)
            {
                yield return null;
            }

            // 更新目前載入的場景紀錄
            currentLoadedAdditiveScene = sceneToLoad;
            Debug.Log($"[GameFlow] 場景 {sceneToLoad} 載入完成！");
        }
    }
}
