using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Data;

namespace Project.Login
{
    /// <summary>
    /// 註冊畫面的控制器。建議做成 01_Login 場景裡的一個獨立 Panel，
    /// 跟登入畫面的 Panel 互相切換顯示（不用另外開新場景，比較單純）。
    ///
    /// 註冊成功後，資料會存成 Data/PlayerData.cs 定義的格式，
    /// 透過 LocalAccountStore 暫存到本機 JSON 檔案，並自動切回登入畫面。
    /// </summary>
    public class RegisterController : MonoBehaviour
    {
        [Header("註冊表單")]
        [SerializeField] private TMP_InputField accountInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField confirmPasswordInputField;
        [SerializeField] private TMP_InputField displayNameInputField;
        [SerializeField] private Button registerButton;
        [Tooltip("表單驗證錯誤時顯示的提示文字，可留空不接")]
        [SerializeField] private TMP_Text errorText;

        [Header("畫面切換")]
        [Tooltip("註冊畫面的 Panel（通常掛這個腳本的物件本身，或它的父物件）")]
        [SerializeField] private GameObject registerPanel;
        [Tooltip("登入畫面的 Panel，註冊成功、或按下返回時要切回去顯示")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private Button backToLoginButton;

        private void Awake()
        {
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);

            if (backToLoginButton != null)
                backToLoginButton.onClick.AddListener(ShowLoginPanel);

            if (passwordInputField != null)
                passwordInputField.contentType = TMP_InputField.ContentType.Password;

            if (confirmPasswordInputField != null)
                confirmPasswordInputField.contentType = TMP_InputField.ContentType.Password;

            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }

        private void OnRegisterButtonClicked()
        {
            string account = accountInputField != null ? accountInputField.text.Trim() : string.Empty;
            string password = passwordInputField != null ? passwordInputField.text : string.Empty;
            string confirmPassword = confirmPasswordInputField != null ? confirmPasswordInputField.text : string.Empty;
            string displayName = displayNameInputField != null ? displayNameInputField.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(displayName))
            {
                ShowError("帳號、密碼、姓名都要填寫喔");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("兩次輸入的密碼不一樣，請再確認一次");
                return;
            }

            var newAccount = new PlayerData
            {
                account = account,
                password = password,
                displayName = displayName,
                registeredAtUtc = DateTime.UtcNow.ToString("O")
            };

            if (LocalAccountStore.TryAddAccount(newAccount, out string error))
            {
                ClearForm();
                ShowLoginPanel();
            }
            else
            {
                ShowError(error);
            }
        }

        private void ClearForm()
        {
            if (accountInputField != null) accountInputField.text = string.Empty;
            if (passwordInputField != null) passwordInputField.text = string.Empty;
            if (confirmPasswordInputField != null) confirmPasswordInputField.text = string.Empty;
            if (displayNameInputField != null) displayNameInputField.text = string.Empty;

            if (errorText != null) errorText.gameObject.SetActive(false);
        }

        /// <summary>切回登入畫面，由「返回」按鈕或註冊成功時呼叫</summary>
        private void ShowLoginPanel()
        {
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(true);
        }

        private void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }
}
