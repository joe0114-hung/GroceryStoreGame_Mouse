namespace Project.Flow
{
    /// <summary>
    /// 六個固定向度，對應 Scenes/1_Store_1/Level_1/ 底下的 1_1 ~ 1_6 關卡場景。
    /// 每一回合（一天/一次開店）會把這六個向度都玩過一次、且順序隨機、不重複。
    /// </summary>
    public enum LevelDimension
    {
        Orientation,            // 1_1 定向力
        AttentionCalculation,   // 1_2 注意力與計算
        Memory,                 // 1_3 記憶力
        Language,               // 1_4 語言能力
        ComprehensionBehavior,  // 1_5 理解與行為判斷
        Construction            // 1_6 建構能力
    }
}
