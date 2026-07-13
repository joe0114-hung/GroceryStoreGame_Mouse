using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Project.UI
{
    public class DwellCursor : MonoBehaviour
    {
        public static DwellCursor Instance { get; private set; }

        [Header("停留時間設定")]
        [SerializeField] private float dwellDuration = 2f;
        
        // 冷卻時間設定
        [Tooltip("觸發成功後，系統要強制休息幾秒？(防連點防呆)")]
        [SerializeField] private float cooldownDuration = 1.5f;

        [Header("視覺元件")]
        [SerializeField] private RectTransform cursorVisual;
        [Tooltip("請拉入專屬按鈕的 ProgressRing_Button")]
        [SerializeField] private Image progressRingImage;

        private GameObject _currentTarget; 
        private float _hoverTimer;
        private bool _isPaused = false; 
        private float _cooldownTimer; // 冷卻計時器

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); 
                return;
            }
            Instance = this;
            Time.timeScale = 1f;

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "01_Login")
            {
                DontDestroyOnLoad(transform.root.gameObject);
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            }

            Cursor.visible = false;
            ResetDwellUI();
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SetCursorActive(bool isActive)
        {
            _isPaused = !isActive;
            if (cursorVisual != null) cursorVisual.gameObject.SetActive(isActive);
            if (!isActive)
            {
                if (_currentTarget != null) InteractionEvents.OnHoverExit?.Invoke(_currentTarget);
                _currentTarget = null;
                ResetDwellUI();
            }
        }

        private void Update()
        {
            if (_isPaused || cursorVisual == null) return; 

            //卡頓突波過濾器 (DeltaTime Spike Filter)
            // 如果畫面卡頓超過 0.1 秒，我們強制只算 0.1 秒，絕對不允許瞬間灌滿進度條！
            float safeDeltaTime = Time.unscaledDeltaTime;
            if (safeDeltaTime > 0.1f) safeDeltaTime = 0.1f;

            // 偵測到「舉手提交」動作時，強制閉眼！
            OAKInputReceiver oakBrain = OAKInputReceiver.Instance;
            if (oakBrain != null && oakBrain.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式)
            {
                // 如果雙手都舉到畫面上半部 (> 60%)，代表準備提交了
                if (oakBrain.LeftHandScreenPos.y > Screen.height * 0.6f && 
                    oakBrain.RightHandScreenPos.y > Screen.height * 0.6f)
                {
                    // 強制中斷目前的懸停進度
                    if (_currentTarget != null) InteractionEvents.OnHoverExit?.Invoke(_currentTarget);
                    _currentTarget = null;
                    ResetDwellUI();
                    
                    // 游標圖案還是要跟著手移動，但不准執行下方的射線點擊！
                    Vector2 pos = GetPointerScreenPosition();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        cursorVisual.parent as RectTransform, pos, null, out Vector2 safePos);
                    cursorVisual.anchoredPosition = safePos;
                    return; 
                }
            }

            Vector2 pointerPosition = GetPointerScreenPosition();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cursorVisual.parent as RectTransform, pointerPosition, null, out Vector2 localPos);
            cursorVisual.anchoredPosition = localPos;

            //  檢查是否在「冷卻防護」狀態中
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= safeDeltaTime; // 改用安全的流失時間
                
                if (_currentTarget != null)
                {
                    InteractionEvents.OnHoverExit?.Invoke(_currentTarget);
                    _currentTarget = null;
                }
                ResetDwellUI();
                return; 
            }

            GameObject hoveredObj = GetTargetUnderPointer(pointerPosition);

            if (hoveredObj != _currentTarget)
            {
                if (_currentTarget != null) InteractionEvents.OnHoverExit?.Invoke(_currentTarget);

                _currentTarget = hoveredObj;
                _hoverTimer = 0f;
                ResetDwellUI();

                if (_currentTarget != null) InteractionEvents.OnHoverEnter?.Invoke(_currentTarget);
            }

            if (_currentTarget != null)
            {
                _hoverTimer += safeDeltaTime; // 改用安全的流失時間
                
                if (progressRingImage != null)
                {
                    progressRingImage.gameObject.SetActive(true);
                    progressRingImage.rectTransform.localPosition = Vector3.zero;
                    progressRingImage.fillAmount = Mathf.Clamp01(_hoverTimer / dwellDuration);
                }

                if (_hoverTimer >= dwellDuration)
                {
                    GameObject triggered = _currentTarget;
                    _hoverTimer = 0f;
                    _currentTarget = null;
                    ResetDwellUI();

                    Button btn = triggered.GetComponent<Button>();
                    if (btn != null && btn.interactable) btn.onClick.Invoke();

                    InteractableUI interactable = triggered.GetComponent<InteractableUI>();
                    if (interactable != null)
                    {
                        Debug.Log($"[Select] 懸停觸發成功：{triggered.name}");
                        InteractionEvents.OnSelect?.Invoke(triggered);
                    }

                    _cooldownTimer = cooldownDuration;
                }
            }
        }

        private void ResetDwellUI()
        {
            _hoverTimer = 0f;
            if (progressRingImage != null)
            {
                progressRingImage.fillAmount = 0f;
                progressRingImage.gameObject.SetActive(false);
            }
        }

        private Vector2 GetPointerScreenPosition()
        {
            OAKInputReceiver oakBrain = OAKInputReceiver.Instance;
            if (oakBrain == null) oakBrain = FindAnyObjectByType<OAKInputReceiver>();

            if (oakBrain != null && oakBrain.currentMode == OAKInputReceiver.InputMode.OAKCamera_相機偵測模式)
            {
                return oakBrain.GeneratedScreenPosition;
            }

#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        // 只鎖定「目前有開啟 (isActiveAndEnabled)」的互動元件
        private GameObject GetTargetUnderPointer(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return null;
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (var result in results)
            {
                var button = result.gameObject.GetComponentInParent<Button>();
                if (button != null && button.interactable && button.isActiveAndEnabled) 
                    return button.gameObject;

                var interactable = result.gameObject.GetComponentInParent<InteractableUI>();
                if (interactable != null && interactable.isActiveAndEnabled) 
                    return interactable.gameObject;
            }
            return null;
        }
    }
}