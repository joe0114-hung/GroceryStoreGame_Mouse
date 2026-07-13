using UnityEngine;

public class PalmManager : MonoBehaviour
{
    public enum TutorialState { WaitForSelect, Dragging, Drop }
    
    [Header("目前狀態")]
    public TutorialState currentState = TutorialState.WaitForSelect;

    [Header("UI / 實體物件綁定")]
    public RectTransform virtualHand;
    public RectTransform bottle;
    public RectTransform box;

    [Header("外部組件連結")]
    [SerializeField] private PalmEffectsManager effectsManager; // 連結特效腳本

    private void OnEnable()
    {
        // 1. 訂閱全域事件：當有物件被選取時通知我
        InteractionEvents.OnSelect += HandleObjectSelected;
    }

    private void OnDisable()
    {
        // 務必取消訂閱
        InteractionEvents.OnSelect -= HandleObjectSelected;
    }

    void Start()
    {
        currentState = TutorialState.WaitForSelect;
        
        // 🌟 新增防呆機制：如果游標遺失，自動去尋找全域游標
        if (virtualHand == null)
        {
            GameObject globalCursor = GameObject.Find("Cursor"); // 確保你的全域游標名稱是 Cursor
            if (globalCursor != null)
            {
                virtualHand = globalCursor.GetComponent<RectTransform>();
                Debug.Log("[PalmManager] 🌟 成功自動跨場景綁定全域游標！");
            }
            else
            {
                Debug.LogError("[PalmManager] ❌ 找不到名為 Cursor 的全域游標！");
            }
        }

        // 初始化特效狀態
        if (effectsManager != null) 
            effectsManager.InitEffects();
    }

    void Update()
    {
        // 現在 Update 只需要處理拖移狀態，選取狀態已經交給 Dwell 系統了
        if (currentState == TutorialState.Dragging)
        {
            HandleDragging();
        }
    }

    private void HandleObjectSelected(GameObject selectedObj)
    {
        if (currentState == TutorialState.WaitForSelect && selectedObj == bottle.gameObject)
        {
            Debug.Log("【狀態切換】Dwell 系統觸發成功！進入拖移中。");
            currentState = TutorialState.Dragging;

            // 拿起後立刻關閉瓶子的 InteractableUI，
            // 避免瓶子跟著游標跑時被 DwellCursor 重複偵測、進度圈一直重轉
            var bottleInteractable = bottle.GetComponent<InteractableUI>();
            if (bottleInteractable != null)
            {
                bottleInteractable.enabled = false;
            }

            if (effectsManager != null)
            {
                effectsManager.PlayGrabEffects(bottle);
            }
        }
    }

    void HandleDragging()
    {
        // 防呆：確保游標有綁定成功才執行，避免當機
        if (virtualHand == null) return;

        // 讓瓶子跟隨虛擬游標
        bottle.position = virtualHand.position;

        // 偵測是否碰到紙箱
        if (RectTransformUtility.RectangleContainsScreenPoint(box, virtualHand.position))
        {
            Debug.Log("【狀態切換】碰到紙箱了！");
            currentState = TutorialState.Drop;
            
            // 通知特效腳本：執行落入箱子的動態特寫
            if (effectsManager != null)
            {
                effectsManager.PlayDropEffects(bottle, box);
            }

            // 延遲 1.5 秒後，才執行切換關卡 (秒數可根據你的特效長度調整)
            Invoke(nameof(GoToNextStage), 2.0f);
        }   
    }

    private void GoToNextStage()
    {
        GameFlowController.Instance.ChangeState(GameFlowController.GameState.WaitForStart);
    }
}