using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Project.Core;
using TMPro;

// ------------------------------------------------------------------
// 下面這些檔案目前還沒有（負責的組員可能還沒推上來），
// 等他們把檔案加進來之後，搜尋這個檔案裡的 "TODO" 把對應的地方取消註解即可：
//   - NPC/DialogueManager.cs
//   - NPC/DialogueLine.cs
//   - NPC/NPCController.cs
//   - Input/OakDInputManager.cs
// using Project.NPC;
// using Project.InputSystem;
// ------------------------------------------------------------------

namespace Project.Flow
{
    /// <summary>
    /// 02_OpeningStory.unity 場景的控制器，掛在場景裡一個叫 "OpeningStoryController" 的 GameObject 上。
    ///
    /// 負責三個依序播放的階段：
    ///   1. Story        開店前情境提要（完整版要接 NPC 對話劇情，目前先用按鈕手動跳過）
    ///   2. TaskSummary   今日任務說明
    ///   3. PrePractice   前置練習（完整版要接 OAK-D 偵測，目前先用按鈕手動繼續）
    ///
    /// 三個階段都跑完後，呼叫 GameManager.Instance.NotifyOpeningStoryCompleted()。
    /// 這一版三個階段都是「按鈕手動點繼續」，等 NPC 對話與 OAK-D 偵測系統做好後，
    /// 再把對應的自動化邏輯接上去即可，不影響 GameManager 那邊的呼叫方式。
    /// </summary>
    public class OpeningStoryController : MonoBehaviour
    {
        private enum Stage { Story, TaskSummary, PrePractice, Done }

        [Header("三個階段的畫面 Panel")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private GameObject taskSummaryPanel;
        [SerializeField] private GameObject prePracticePanel;

        [Header("Story 階段：目前先用按鈕手動繼續（之後接 NPC 對話系統）")]
        [SerializeField] private Button storyContinueButton;

        [Header("TaskSummary 階段")]
        [SerializeField] private Button taskSummaryContinueButton;

        [Header("PrePractice 階段")]
        [SerializeField] private TMP_Text prePracticeStatusText; // 專案使用 TextMeshPro 因此用 TMP_Text
        [Tooltip("目前 OAK-D 偵測還沒做好，先靠這顆按鈕手動繼續")]
        [SerializeField] private Button prePracticeContinueButton;

        // ------------------------------------------------------------
        // TODO: NPC/DialogueManager.cs、NPC/DialogueLine.cs、NPC/NPCController.cs
        // 加進專案後，取消下面這幾行的註解，並把檔案最上面的 using Project.NPC; 打開，
        // 就可以把 Story 階段換成真正播放 NPC 對話，而不是只靠按鈕手動跳過
        // ------------------------------------------------------------
        // [Header("Story 階段：今日開店情境對話")]
        // [SerializeField] private List<DialogueLine> openingDialogueLines;
        // [SerializeField] private NPCController npc;

        private Stage _currentStage;

        private void Awake()
        {
            if (storyContinueButton != null)
                storyContinueButton.onClick.AddListener(EnterTaskSummaryStage);

            if (taskSummaryContinueButton != null)
                taskSummaryContinueButton.onClick.AddListener(EnterPrePracticeStage);

            if (prePracticeContinueButton != null)
                prePracticeContinueButton.onClick.AddListener(FinishOpeningStory);
        }

        private void Start()
        {
            EnterStoryStage();
        }

        // ------------------------------------------------------------
        // 1. Story：開店前情境提要
        // ------------------------------------------------------------
        private void EnterStoryStage()
        {
            _currentStage = Stage.Story;
            SetPanels(story: true, task: false, practice: false);

            // TODO: NPC 系統做好後，把下面這行的手動繼續拿掉，改成呼叫 DialogueManager 播放對話，
            // 播完再自動呼叫 EnterTaskSummaryStage()，範例：
            //
            // npc?.Enter();
            // if (DialogueManager.Instance != null && openingDialogueLines != null && openingDialogueLines.Count > 0)
            // {
            //     DialogueManager.Instance.PlayDialogue(openingDialogueLines, EnterTaskSummaryStage);
            // }
            // else
            // {
            //     EnterTaskSummaryStage();
            // }
        }

        // ------------------------------------------------------------
        // 2. TaskSummary：今日任務說明
        // ------------------------------------------------------------
        private void EnterTaskSummaryStage()
        {
            _currentStage = Stage.TaskSummary;
            SetPanels(story: false, task: true, practice: false);

            // TODO: npc?.Exit();（NPC 系統做好後打開）

            // TODO：在這裡顯示今天要玩的大關/向度概覽，目前只有 Construction 這一關做好，先顯示固定文字即可
        }

        // 由 taskSummaryContinueButton 呼叫
        private void EnterPrePracticeStage()
        {
            _currentStage = Stage.PrePractice;
            SetPanels(story: false, task: false, practice: true);

            // 目前還沒有 OAK-D 骨架偵測，先顯示固定文字，靠玩家自己按「開始遊戲」按鈕繼續
            if (prePracticeStatusText != null)
                prePracticeStatusText.text = "準備好了嗎？按下開始，遊玩今天的任務吧！";
        }

        // ------------------------------------------------------------
        // TODO: Input/OakDInputManager.cs 加進專案後，取消下面整個方法的註解，
        // 並在檔案最上面取消 using Project.InputSystem; 的註解，
        // 這樣前置練習畫面就能即時顯示「是否偵測到玩家」，而不只是靠按鈕手動繼續
        // ------------------------------------------------------------
        /*
        private void Update()
        {
            if (_currentStage != Stage.PrePractice) return;
            if (prePracticeStatusText == null) return;

            bool detected = OakDInputManager.Instance != null && OakDInputManager.Instance.IsPersonDetected;
            prePracticeStatusText.text = detected
                ? "偵測到您囉！準備好了嗎？"
                : "請站到攝影機前，讓我們準備好囉";
        }
        */

        // 由 prePracticeContinueButton 呼叫
        private void FinishOpeningStory()
        {
            if (_currentStage == Stage.Done) return; // 避免重複呼叫

            _currentStage = Stage.Done;
            GameManager.Instance.NotifyOpeningStoryCompleted();
        }

        private void SetPanels(bool story, bool task, bool practice)
        {
            if (storyPanel != null) storyPanel.SetActive(story);
            if (taskSummaryPanel != null) taskSummaryPanel.SetActive(task);
            if (prePracticePanel != null) prePracticePanel.SetActive(practice);
        }
    }
}
