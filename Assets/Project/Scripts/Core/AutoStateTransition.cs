using UnityEngine;
using System.Collections;

/// <summary>
/// 腳本功能：自動計時器。
/// 掛載對象：疊加場景（如 CountdownScene, WaitStartScene）中的空物件。
/// 運作邏輯：場景一載入就開始倒數，時間到後自動通知全域狀態機切換狀態。
/// </summary>
public class AutoStateTransition : MonoBehaviour
{
    [Header("流程設定")]
    [Tooltip("在這個場景要停留幾秒？")]
    [SerializeField] private float delayTime = 5f;

    [Tooltip("時間到之後，要通知系統切換到哪個狀態？")]
    // 這裡假設你的 GameFlowController 裡有一個叫 GameState 的 Enum
    [SerializeField] private GameFlowController.GameState nextState; 

    private void Start()
    {
        // 當這個場景被疊加進來時，Start 會自動執行，開始計時協程
        StartCoroutine(WaitAndTransitionRoutine());
    }

    private IEnumerator WaitAndTransitionRoutine()
    {
        // 暫停等待指定的秒數
        yield return new WaitForSeconds(delayTime);
        
        // 時間到，執行切換狀態的方法
        GoToNextStage();
    }

    private void GoToNextStage()
    {
        Debug.Log($"[場景計時器] {delayTime} 秒時間到！通知系統切換至狀態：{nextState}");
        
        // 透過單例模式 (Singleton) 呼叫核心場景的狀態機
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(nextState);
        }
        else
        {
            Debug.LogError("[場景計時器] 找不到 GameFlowController.Instance！請確認核心場景是否正常運作。");
        }
    }
}