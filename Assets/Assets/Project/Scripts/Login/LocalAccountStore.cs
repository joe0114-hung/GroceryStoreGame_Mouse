using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Project.Data;

namespace Project.Login
{
    /// <summary>
    /// 暫時的帳號本機儲存機制，把註冊的長者帳號存成一份 JSON 檔案（存在 Application.persistentDataPath）。
    ///
    /// TODO: 等 Data/DataManager.cs、Data/LocalDataStorage.cs 做好後，把這個 class 整個換掉，
    /// LoginController / RegisterController 呼叫的方法名稱（TryLogin / TryAddAccount）盡量維持不變，
    /// 這樣替換的時候呼叫端幾乎不用改。
    /// </summary>
    public static class LocalAccountStore
    {
        private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "player_accounts.json");

        [Serializable]
        private class PlayerDataListWrapper
        {
            public List<PlayerData> accounts = new List<PlayerData>();
        }

        /// <summary>讀取所有已註冊的帳號</summary>
        public static List<PlayerData> LoadAll()
        {
            if (!File.Exists(FilePath))
            {
                return new List<PlayerData>();
            }

            string json = File.ReadAllText(FilePath);
            var wrapper = JsonUtility.FromJson<PlayerDataListWrapper>(json);
            return wrapper != null && wrapper.accounts != null ? wrapper.accounts : new List<PlayerData>();
        }

        private static void SaveAll(List<PlayerData> accounts)
        {
            var wrapper = new PlayerDataListWrapper { accounts = accounts };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(FilePath, json);
        }

        /// <summary>嘗試新增一個帳號，帳號重複的話會失敗並回傳錯誤訊息</summary>
        public static bool TryAddAccount(PlayerData newAccount, out string errorMessage)
        {
            var all = LoadAll();

            foreach (var existing in all)
            {
                if (existing.account == newAccount.account)
                {
                    errorMessage = "這個帳號已經被註冊過了";
                    return false;
                }
            }

            all.Add(newAccount);
            SaveAll(all);
            errorMessage = null;
            return true;
        }

        /// <summary>驗證帳號密碼，成功的話回傳對應的 PlayerData</summary>
        public static bool TryLogin(string account, string password, out PlayerData matched)
        {
            var all = LoadAll();

            foreach (var existing in all)
            {
                if (existing.account == account && existing.password == password)
                {
                    matched = existing;
                    return true;
                }
            }

            matched = null;
            return false;
        }

        /// <summary>
        /// 方便開發測試用：如果目前完全沒有任何帳號資料，先塞兩筆預設測試帳號進去，
        /// 這樣不用每次都要先手動註冊過一次才能測登入。
        /// </summary>
        public static void EnsureDefaultTestAccountsSeeded()
        {
            var all = LoadAll();
            if (all.Count > 0) return;

            all.Add(new PlayerData { account = "elder01", password = "1234", displayName = "王奶奶", registeredAtUtc = DateTime.UtcNow.ToString("O") });
            all.Add(new PlayerData { account = "elder02", password = "1234", displayName = "陳爺爺", registeredAtUtc = DateTime.UtcNow.ToString("O") });
            SaveAll(all);
        }
    }
}
