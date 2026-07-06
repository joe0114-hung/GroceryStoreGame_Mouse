using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.UI
{
    /// <summary>
    /// 停留選取游標：滑鼠（之後換成 OAK-D 手掌位置）停在按鈕上一段時間，自動觸發該按鈕的點擊，
    /// 停留期間游標旁邊會顯示一個圓形進度條，填滿時觸發選取。
    ///
    /// 掛在 01_Login 場景裡一個叫 "DwellCursor" 的 GameObject 上，
    /// 透過 DontDestroyOnLoad 存活到整個 App 結束，所以每個場景都會自動套用，不用個別設置。
    ///
    /// 目前用滑鼠位置（Input.mousePosition）模擬手掌位置。
    /// TODO: Input/OakDInputManager.cs、Input/MotionCursor.cs 做好後，
    /// 把 GetPointerScreenPosition() 裡面的來源換成 OAK-D 手掌位置換算出的螢幕座標即可，
    /// 其他停留計時、觸發點擊的邏輯完全不用改。
    ///
    /// 一般滑鼠點擊仍然正常運作，這個系統是「額外多一種選取方式」，不會取代點擊。
    /// </summary>
    public class DwellCursor : MonoBehaviour
    {
        public static DwellCursor Instance { get; private set; }

        [Header("停留時間設定")]
        [Tooltip("停留在同一個按鈕上多少秒後自動觸發選取")]
        [SerializeField] private float dwellDuration = 2f;

        [Header("視覺元件")]
        [Tooltip("跟隨游標位置移動的圖示（一個小圓點或指標圖片即可）")]
        [SerializeField] private RectTransform cursorVisual;
        [Tooltip("圓形進度條，Image 元件的 Image Type 要設成 Filled，Fill Method 設成 Radial 360")]
        [SerializeField] private Image progressRingImage;

        private Button _currentTarget;
        private float _hoverTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 隱藏系統游標，畫面上改顯示自訂的 cursorVisual
            Cursor.visible = false;

            if (progressRingImage != null)
            {
                progressRingImage.fillAmount = 0f;
                progressRingImage.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Vector2 pointerPosition = GetPointerScreenPosition();

            UpdateCursorVisualPosition(pointerPosition);

            Button hoveredButton = GetButtonUnderPointer(pointerPosition);

            if (hoveredButton != _currentTarget)
            {
                // 換了目標（或移到空白處），重新計時
                _currentTarget = hoveredButton;
                _hoverTimer = 0f;
            }

            if (_currentTarget != null && _currentTarget.IsActive() && _currentTarget.interactable)
            {
                // 用 unscaledDeltaTime，這樣即使 MainMenuController 把 Time.timeScale 設成 0（暫停中），
                // 停留選取還是能正常運作，玩家才能用同一套方式選「繼續」「回主頁」等選單按鈕
                _hoverTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_hoverTimer / dwellDuration);

                if (progressRingImage != null)
                {
                    progressRingImage.gameObject.SetActive(true);
                    progressRingImage.fillAmount = progress;
                }

                if (_hoverTimer >= dwellDuration)
                {
                    var triggered = _currentTarget;
                    _hoverTimer = 0f;
                    _currentTarget = null; // 清空目標，避免同一個按鈕被連續觸發，玩家要移開再移回才能再選一次

                    if (progressRingImage != null) progressRingImage.gameObject.SetActive(false);

                    triggered.onClick.Invoke();
                }
            }
            else
            {
                _hoverTimer = 0f;
                if (progressRingImage != null) progressRingImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// TODO: OAK-D 手掌位置追蹤做好後，把這裡換成從 Input/MotionCursor.cs 拿到的螢幕座標。
        /// 目前先用滑鼠位置模擬。
        ///
        /// 這裡同時支援新版 Input System 跟舊版 Input Manager，
        /// 是依專案的 Project Settings > Player > Active Input Handling 設定自動切換，
        /// 不需要手動決定用哪一種。
        /// </summary>
        private Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private void UpdateCursorVisualPosition(Vector2 screenPosition)
        {
            if (cursorVisual != null)
            {
                cursorVisual.position = screenPosition;
            }

            // 之前這裡漏掉了 progressRingImage，導致它一直停留在編輯器裡擺放的位置不會動，
            // 現在讓它跟 cursorVisual 一樣，每一幀都跟著游標移動
            if (progressRingImage != null)
            {
                progressRingImage.rectTransform.position = screenPosition;
            }
        }

        private Button GetButtonUnderPointer(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return null;

            var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                var button = result.gameObject.GetComponentInParent<Button>();
                if (button != null) return button;
            }

            return null;
        }
    }
}
