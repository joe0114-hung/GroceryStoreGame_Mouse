using System;

namespace Project.Data
{
    /// <summary>
    /// 長者的帳號資料（簡化版）。
    ///
    /// TODO: 這是暫時的簡化版本，正式的 PlayerData.cs 應該由負責 Data/ 資料夾的組員完成，
    /// 之後可能會擴充更多欄位（例如年齡、生日、頭像、緊急聯絡人、遊玩紀錄關聯 ID 等）。
    /// 擴充時直接在這個類別加欄位即可，account/password/displayName 這幾個現有欄位
    /// 已經被 LoginController / RegisterController 使用，請不要更動名稱，
    /// 或更動時記得同步通知使用到的人。
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string account;
        public string password; // TODO: 目前是明碼儲存，之後應該要加密雜湊處理，先求功能能動
        public string displayName;
        public string registeredAtUtc;
    }
}
