namespace Project.Core
{
    /// <summary>
    /// 整個 App 最高層的流程狀態，對應到 Scenes/ 底下的主要場景：
    /// Boot -> Login -> OpeningStory -> LevelPlaying(重複跑6個向度) -> ProgressSummary -> ClosingStory -> (回到 Login)
    /// </summary>
    public enum GameFlowState
    {
        Boot,            // 00_Boot
        Login,           // 01_Login
        OpeningStory,    // 02_OpeningStory（開店前情境、今日任務說明、前置練習）
        LevelPlaying,    // 1_1 ~ 1_6_Level_XXX（單一向度關卡進行中）
        ProgressSummary, // 03_ProgressSummary
        ClosingStory     // 04_ClosingStory
    }
}
