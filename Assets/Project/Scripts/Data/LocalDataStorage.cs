using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// 對應架構圖 Data/LocalDataStorage.cs：「先把資料存在本機 JSON，未來再改接資料庫」。
    ///
    /// 目前先只實作「玩家帳號」這一塊（給 Login/LoginController.cs、Login/RegisterController.cs 使用），
    /// 把註冊的長者帳號存成一份 JSON 檔案，存在 Application.persistentDataPath。
    ///
    /// TODO: 這是簡化版，正式的 Data/DataManager.cs 做好後，
    /// 這裡的邏輯應該會被整合進去（或者這個檔案繼續存在，專門負責跟本機檔案讀寫，
    /// DataManager 負責更高層的業務邏輯，兩者分工）。
    /// 之後如果要存其他資料（PlayData 關卡紀錄、TimeData 時間紀錄等），
    /// 可以照同樣的模式在這個檔案裡擴充，或另外拆檔案，
    /// 但請保留 TryLogin / TryAddAccount 這兩個方法名稱，
    /// 這樣 LoginController / RegisterController 呼叫端不用改。
    /// </summary>
    public static class LocalDataStorage
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
