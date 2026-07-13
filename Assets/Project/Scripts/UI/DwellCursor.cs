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

            Vector2 pointerPosition = GetPointerScreenPosition();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cursorVisual.parent as RectTransform, pointerPosition, null, out Vector2 localPos);
            cursorVisual.anchoredPosition = localPos;

            //  檢查是否在「冷卻防護」狀態中
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.unscaledDeltaTime; // 倒數冷卻時間
                
                // 強制脫離目標並隱藏進度條
                if (_currentTarget != null)
                {
                    InteractionEvents.OnHoverExit?.Invoke(_currentTarget);
                    _currentTarget = null;
                }
                ResetDwellUI();
                
                return; //  只要還在冷卻，就直接 Return，不發射線也不讀條！
            }

            GameObject hoveredObj = GetTargetUnderPointer(pointerPosition);

            // 當游標切換目標時的處理
            if (hoveredObj != _currentTarget)
            {
                if (_currentTarget != null) InteractionEvents.OnHoverExit?.Invoke(_currentTarget);

                _currentTarget = hoveredObj;
                _hoverTimer = 0f;
                ResetDwellUI();

                if (_currentTarget != null) InteractionEvents.OnHoverEnter?.Invoke(_currentTarget);
            }

            // 如果有目標，開始讀條
            if (_currentTarget != null)
            {
                _hoverTimer += Time.unscaledDeltaTime;
                
                if (progressRingImage != null)
                {
                    progressRingImage.gameObject.SetActive(true);
                    progressRingImage.rectTransform.localPosition = Vector3.zero;
                    progressRingImage.fillAmount = Mathf.Clamp01(_hoverTimer / dwellDuration);
                }

                // 達到觸發時間
                if (_hoverTimer >= dwellDuration)
                {
                    GameObject triggered = _currentTarget;
                    _hoverTimer = 0f;
                    _currentTarget = null;
                    ResetDwellUI();

                    // 執行觸發事件
                    Button btn = triggered.GetComponent<Button>();
                    if (btn != null && btn.interactable) btn.onClick.Invoke();

                    InteractableUI interactable = triggered.GetComponent<InteractableUI>();
                    if (interactable != null)
                    {
                        Debug.Log($"[Select] 懸停觸發成功：{triggered.name}");
                        InteractionEvents.OnSelect?.Invoke(triggered);
                    }

                    //  成功觸發後，立刻啟動冷卻防護罩！
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