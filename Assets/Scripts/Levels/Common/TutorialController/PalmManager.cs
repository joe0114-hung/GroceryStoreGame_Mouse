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

    // 當 Event Bus 廣播選取事件時，會自動觸發此方法
    private void HandleObjectSelected(GameObject selectedObj)
    {
        // 安全檢查：只有在等待選取狀態，且被盯著看 2 秒的是「瓶子」時才執行
        if (currentState == TutorialState.WaitForSelect && selectedObj == bottle.gameObject)
        {
            Debug.Log("【狀態切換】Dwell 系統觸發成功！進入拖移中。");
            currentState = TutorialState.Dragging;

            // 通知特效腳本：播放拿起特效與聲音
            if (effectsManager != null)
            {
                effectsManager.PlayGrabEffects(bottle);
            }
        }
    }

    void HandleDragging()
    {
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

    // 在 HandleDragging() 的外面，新增這個小方法給 Invoke 呼叫
    private void GoToNextStage()
    {
        GameFlowController.Instance.ChangeState(GameFlowController.GameState.WaitForStart);
    }
}
