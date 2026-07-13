using System;
using System.IO;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// 暫時的簡化存檔機制，先只記住「目前是誰在玩」，存成本機 JSON 檔案。
    ///
    /// TODO: 這是很簡化的版本，因為 Data/PlayData.cs、Data/DataManager.cs（真正的遊玩進度資料）
    /// 目前都還沒有人做。等那些做好後，這裡應該要擴充成存「目前玩到第幾關、今天六個向度裡
    /// 哪些已經完成」等完整進度，讀取時才能真正「繼續遊玩」，而不是只重新登入同一個人。
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public string playerName;
        public string savedAtUtc;
    }

    public static class GameSaveStore
    {
        private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "game_save.json");

        public static void Save(string playerName)
        {
            var data = new GameSaveData
            {
                playerName = playerName,
                savedAtUtc = DateTime.UtcNow.ToString("O")
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }

        public static bool TryLoad(out GameSaveData data)
        {
            if (!File.Exists(FilePath))
            {
                data = null;
                return false;
            }

            string json = File.ReadAllText(FilePath);
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }
    }
}
