using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Core;
using Project.Data;

// TODO: 完整版登入應該要有：
//   - Login/LoginManager.cs：統一管理登入流程，帳號密碼要對真正的資料庫驗證（例如透過 Data/DataManager.cs）
//   - Login/MockLoginProvider.cs、Login/FaceLoginProvider.cs：其他登入方式（例如人臉辨識）
// 這些檔案目前都還沒有人做，所以目前的帳密驗證是透過 LocalAccountStore（暫時存本機 JSON 檔案）處理。
// 等真正的帳號資料庫（LoginManager.cs / DataManager.cs）做好後，
// 把 OnLoginButtonClicked() 裡呼叫 LocalAccountStore 的地方，
// 改成呼叫真正的資料庫驗證即可，其他程式碼（按鈕事件、GameManager 呼叫方式）都不需要改。

namespace Project.Login
{
    /// <summary>
    /// 01_Login.unity 場景的控制器。
    /// 掛在場景裡一個叫 "LoginController" 的 GameObject 上。
    ///
    /// 提供：
    ///   1. 真實登入：長者輸入帳號 + 密碼，按「登入」，會對照 LocalAccountStore 裡的資料驗證
    ///   2. 測試用登入：開發/測試階段用，按「(測試用)」按鈕直接略過帳密驗證登入
    ///   3. 切換到註冊畫面：按「註冊」按鈕，切換顯示 RegisterController 負責的註冊 Panel
    /// </summary>
    public class LoginController : MonoBehaviour
    {
        [Header("真實登入（帳號 + 密碼）")]
        [SerializeField] private TMP_InputField accountInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private Button loginButton;
        [Tooltip("帳密錯誤或欄位空白時顯示的提示文字。可以留空不接，這樣就不會有錯誤提示功能")]
        [SerializeField] private TMP_Text errorText;

        [Header("測試用登入（跳過帳密驗證，直接用固定帳號登入，方便開發測試）")]
        [SerializeField] private Button testLoginButton;
        [SerializeField] private string testPlayerName = "測試用玩家";

        [Header("切換到註冊畫面")]
        [SerializeField] private Button goToRegisterButton;
        [Tooltip("登入畫面的 Panel（通常掛這個腳本的物件本身，或它的父物件）")]
        [SerializeField] private GameObject loginPanel;
        [Tooltip("註冊畫面的 Panel，按下「註冊」後要切換過去顯示")]
        [SerializeField] private GameObject registerPanel;

        private void Awake()
        {
            // 方便開發測試：如果目前完全沒有任何已註冊帳號，先塞兩筆預設測試帳號
            LocalAccountStore.EnsureDefaultTestAccountsSeeded();

            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);

            if (testLoginButton != null)
                testLoginButton.onClick.AddListener(OnTestLoginButtonClicked);

            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

            if (passwordInputField != null)
                passwordInputField.contentType = TMP_InputField.ContentType.Password;

            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }

        /// <summary>「登入」按鈕：讀取帳號密碼欄位，對照 LocalAccountStore 裡的資料驗證</summary>
        private void OnLoginButtonClicked()
        {
            if (accountInputField == null || passwordInputField == null)
            {
                Debug.LogWarning("[LoginController] 沒有指定 accountInputField / passwordInputField，請在 Inspector 接上輸入框");
                return;
            }

            string account = accountInputField.text.Trim();
            string password = passwordInputField.text;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                ShowError("請輸入帳號和密碼");
                return;
            }

            if (LocalAccountStore.TryLogin(account, password, out PlayerData matched))
            {
                Login(matched.displayName);
            }
            else
            {
                ShowError("帳號或密碼錯誤，請再確認一次");
            }
        }

        /// <summary>「(測試用)」按鈕：跳過帳密驗證，直接用固定的測試帳號登入</summary>
        private void OnTestLoginButtonClicked()
        {
            Login(testPlayerName);
        }

        /// <summary>「註冊」按鈕：切換到註冊畫面</summary>
        private void ShowRegisterPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(true);
        }

        private void Login(string playerName)
        {
            GameManager.Instance.NotifyLoginCompleted(playerName);
        }

        private void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }
}
