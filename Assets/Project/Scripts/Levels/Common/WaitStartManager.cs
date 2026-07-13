using UnityEngine;
using UnityEngine.UI; // 如果未來鈴鐺要做簡單的變色或動畫會用到

public class WaitStartManager : MonoBehaviour
{
    [Header("測試設定")]
    [Tooltip("在滑鼠模式下，按下此按鍵模擬長輩『舉手拍鈴鐺』成功")]
    public KeyCode debugTriggerKey = KeyCode.Space;

    // 防止長輩手一直舉著，或狂按按鈕導致重複觸發
    private bool hasTriggered = false;

    void Update()
    {
        // 使用 New Input System 的寫法來偵測空白鍵
        if (!hasTriggered && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("⌨️ [測試模式] 按下空白鍵，模擬拍到鈴鐺！");
            OnHandRaisedDetected();
        }
    }

    /// <summary>
    /// 當 OAK-D 偵測到單手高舉，或是測試模式按下空白鍵時觸發
    /// </summary>
    public void OnHandRaisedDetected()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            Debug.Log("🔔 叮鈴鈴！長輩成功敲響鈴鐺，準備進入倒數！");
            
            // TODO: 未來可以在這裡加入播放鈴鐺音效的程式碼
            // GetComponent<AudioSource>().Play();

            // 稍微延遲 0.5 秒再切換場景，讓長輩聽完鈴聲、看見回饋
            Invoke(nameof(TriggerCountdownScene), 0.5f);
        }
    }

    /// <summary>
    /// 綁定在「再複習一次」(記帳本) 按鈕上
    /// </summary>
    public void GoBackToTutorial()
    {
        if (!hasTriggered)
        {
            hasTriggered = true; // 鎖住狀態，避免切換中途又去拍鈴鐺
            Debug.Log("🔙 長輩選擇看看記帳本，回到教學關卡！");

            if (GameFlowController.Instance != null)
            {
                // 無縫疊加切換回教學關卡
                GameFlowController.Instance.ChangeState(GameFlowController.GameState.Tutorial);
            }
        }
    }

    private void TriggerCountdownScene()
    {
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ChangeState(GameFlowController.GameState.Countdown);
        }
        else
        {
            Debug.LogError("[WaitStartManager] 找不到 GameFlowController！");
        }
    }
}