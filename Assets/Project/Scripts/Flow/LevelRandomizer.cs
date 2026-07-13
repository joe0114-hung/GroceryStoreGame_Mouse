using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Flow
{
    /// <summary>一個向度對應到哪個場景檔名（不含 .unity）</summary>
    [Serializable]
    public class LevelDimensionSceneEntry
    {
        public LevelDimension dimension;
        public string sceneName; // 例如 "1_1_Level_Orientation"
    }

    /// <summary>
    /// 一個「大關」的設定，對應 Scenes/1_Store_1/ 底下的 Level_1 / Level_2 / Level_3 資料夾。
    /// 每個大關底下都有固定六個向度的關卡場景。
    /// </summary>
    [Serializable]
    public class BigLevelConfig
    {
        [Tooltip("大關識別名稱，例如 Level_1 / Level_2 / Level_3")]
        public string bigLevelId;

        [Tooltip("這個大關底下，六個向度各自對應的場景名稱")]
        public List<LevelDimensionSceneEntry> dimensionScenes = new List<LevelDimensionSceneEntry>(6);

        public string GetSceneName(LevelDimension dimension)
        {
            foreach (var entry in dimensionScenes)
            {
                if (entry.dimension == dimension) return entry.sceneName;
            }
            Debug.LogError($"[BigLevelConfig] 找不到向度 {dimension} 對應的場景，請檢查 {bigLevelId} 的設定");
            return null;
        }
    }

    /// <summary>
    /// 方便測試用的預設大關設定，直接對應架構圖裡 Level_1/ 底下已知的六個場景檔名。
    /// 之後要加 Level_2 / Level_3，比照這個寫法另外提供設定即可，
    /// 或直接在 GameManager 的 Inspector 上手動填。
    /// </summary>
    public static class DefaultBigLevelConfigs
    {
        /// <summary>
        /// 開發測試用：目前只有 1_6_Level_Construction 這一關做好了，
        /// 這個設定讓一回合只會抽到 Construction 這一關，其他 5 個向度先不列入，
        /// 這樣才能在其他場景還沒做好之前，先把整條流程真的玩過一遍。
        /// 等其他向度場景都做好後，把 GameManager 的 devModeConstructionOnly 關掉，
        /// 改用 CreateLevel1Default() 即可，不用改這個 class 以外的任何程式碼。
        /// </summary>
        public static BigLevelConfig CreateConstructionOnlyForTesting()
        {
            return new BigLevelConfig
            {
                bigLevelId = "Level_1_DevTest_ConstructionOnly",
                dimensionScenes = new List<LevelDimensionSceneEntry>
                {
                    new LevelDimensionSceneEntry { dimension = LevelDimension.Construction, sceneName = "1_6_Level_Construction" },
                }
            };
        }

        public static BigLevelConfig CreateLevel1Default()
        {
            return new BigLevelConfig
            {
                bigLevelId = "Level_1",
                dimensionScenes = new List<LevelDimensionSceneEntry>
                {
                    new LevelDimensionSceneEntry { dimension = LevelDimension.Orientation, sceneName = "1_1_Level_Orientation" },
                    new LevelDimensionSceneEntry { dimension = LevelDimension.AttentionCalculation, sceneName = "1_2_Level_AttentionCalculation" },
                    new LevelDimensionSceneEntry { dimension = LevelDimension.Memory, sceneName = "1_3_Level_Memory" },
                    new LevelDimensionSceneEntry { dimension = LevelDimension.Language, sceneName = "1_4_Level_Language" },
                    new LevelDimensionSceneEntry { dimension = LevelDimension.ComprehensionBehavior, sceneName = "1_5_Level_ComprehensionBehavior" },
                    new LevelDimensionSceneEntry { dimension = LevelDimension.Construction, sceneName = "1_6_Level_Construction" },
                }
            };
        }
    }

    /// <summary>
    /// 判斷「大關」（Level_1 / Level_2 / Level_3...）與「level」（六個向度中的哪一關），
    /// 從尚未完成的六個向度中隨機抽下一關。
    ///
    /// 使用方式（由 GameManager 呼叫）：
    ///   1. 每次要開始新的一回合（02_OpeningStory 結束後）呼叫一次 StartNewRound(config)
    ///   2. 每次要知道下一關玩什麼，呼叫 TryGetNextLevel(out dimension, out sceneName)
    ///   3. TryGetNextLevel 回傳 false，代表這回合的六個向度都已經抽完，該去 03_ProgressSummary 了
    ///
    /// 這是一個純 C# class（不是 MonoBehaviour），由 GameManager 建立並持有，
    /// 不需要掛在任何 GameObject 上。
    /// </summary>
    public class LevelRandomizer
    {
        private readonly List<LevelDimension> _remainingDimensions = new List<LevelDimension>();
        private BigLevelConfig _currentConfig;

        /// <summary>目前這回合玩的是哪個大關</summary>
        public string CurrentBigLevelId => _currentConfig?.bigLevelId;

        public int TotalInRound { get; private set; }
        public int PlayedCount => TotalInRound - _remainingDimensions.Count;
        public bool IsRoundComplete => _remainingDimensions.Count == 0;

        /// <summary>目前已抽出、正在進行中的向度（尚未收到該關完成通知前都是這個值）</summary>
        public LevelDimension? CurrentDimension { get; private set; }

        /// <summary>
        /// 開始新的一回合：指定這回合要玩哪個大關，
        /// 並把該大關底下的六個向度洗牌放入待抽池。
        /// </summary>
        public void StartNewRound(BigLevelConfig config)
        {
            if (config == null || config.dimensionScenes == null || config.dimensionScenes.Count == 0)
            {
                Debug.LogError("[LevelRandomizer] BigLevelConfig 設定為空，無法開始回合");
                return;
            }

            _currentConfig = config;
            _remainingDimensions.Clear();

            foreach (var entry in config.dimensionScenes)
            {
                _remainingDimensions.Add(entry.dimension);
            }

            Shuffle(_remainingDimensions);
            TotalInRound = _remainingDimensions.Count;
            CurrentDimension = null;
        }

        /// <summary>
        /// 抽出下一個要玩的向度與對應場景名稱。
        /// 回傳 false 代表這回合六個向度都已經抽完。
        /// </summary>
        public bool TryGetNextLevel(out LevelDimension dimension, out string sceneName)
        {
            if (IsRoundComplete)
            {
                dimension = default;
                sceneName = null;
                return false;
            }

            dimension = _remainingDimensions[0];
            _remainingDimensions.RemoveAt(0);
            sceneName = _currentConfig.GetSceneName(dimension);
            CurrentDimension = dimension;
            return true;
        }

        /// <summary>
        /// 保留給「整關必須重來」這種例外情境使用（例如中途連線中斷）。
        /// 一般的「答錯重新出題」流程是在關卡內部處理，不會用到這個方法。
        /// </summary>
        public void RequeueCurrentDimension()
        {
            if (CurrentDimension.HasValue)
            {
                _remainingDimensions.Insert(0, CurrentDimension.Value);
            }
        }

        private void Shuffle(List<LevelDimension> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
