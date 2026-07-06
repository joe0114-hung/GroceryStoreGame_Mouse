using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Project.Core;
using Project.Data;

namespace Project.UI
{
    /// <summary>
    /// 主選單控制器，掛在 01_Login 場景裡一個叫 "MainMenuController" 的 GameObject 上，
    /// 透過 DontDestroyOnLoad 存活到整個 App 結束，所以只要放一次，之後每個場景都會自動帶著它。
    ///
    /// 功能：
    ///   - 右上角（或你自訂的位置）有一顆常駐的「選單」按鈕，隨時可以點開
    ///   - 點開後顯示暫停選單：繼續 / 儲存遊戲 / 回主頁
    ///   - 暫停時 Time.timeScale = 0（遊戲內所有跟時間有關的邏輯會停住，UI 按鈕仍可正常點擊）
    ///
    /// TODO: 存檔目前只是簡化版（見 Data/GameSaveStore.cs），
    /// 等 Data/DataManager.cs、Data/PlayData.cs 做好後要擴充成完整進度存檔。
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        [Header("常駐選單按鈕（任何畫面都會顯示，點下去開啟選單）")]
        [SerializeField] private Button openMenuButton;

        [Header("選單面板")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button homeButton;
        [Tooltip("存檔後顯示「已儲存」之類的提示，可留空不接")]
        [SerializeField] private TMP_Text saveFeedbackText;

        /// <summary>目前選單是否開啟中（遊戲是否處於暫停狀態）</summary>
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (openMenuButton != null) openMenuButton.onClick.AddListener(OpenMenu);
            if (resumeButton != null) resumeButton.onClick.AddListener(CloseMenu);
            if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClicked);
            if (homeButton != null) homeButton.onClick.AddListener(OnHomeButtonClicked);

            if (menuPanel != null) menuPanel.SetActive(false);
            if (saveFeedbackText != null) saveFeedbackText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 換場景時如果選單還開著（理論上不太會發生，但保險起見），自動關閉、恢復時間流動，
        // 避免不小心讓下一個場景卡在暫停狀態
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPaused) CloseMenu();
        }

        public void OpenMenu()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            if (menuPanel != null) menuPanel.SetActive(true);
        }

        public void CloseMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        private void OnSaveButtonClicked()
        {
            if (string.IsNullOrEmpty(GameManager.Instance.CurrentPlayerName))
            {
                ShowSaveFeedback("目前沒有登入中的玩家，無法儲存");
                return;
            }

            GameSaveStore.Save(GameManager.Instance.CurrentPlayerName);
            ShowSaveFeedback("已儲存！");
        }

        private void OnHomeButtonClicked()
        {
            CloseMenu();
            GameManager.Instance.ReturnToHome();
        }

        private void ShowSaveFeedback(string message)
        {
            if (saveFeedbackText == null) return;

            saveFeedbackText.text = message;
            saveFeedbackText.gameObject.SetActive(true);

            CancelInvoke(nameof(HideSaveFeedback));
            Invoke(nameof(HideSaveFeedback), 1.5f);
        }

        private void HideSaveFeedback()
        {
            if (saveFeedbackText != null) saveFeedbackText.gameObject.SetActive(false);
        }
    }
}
