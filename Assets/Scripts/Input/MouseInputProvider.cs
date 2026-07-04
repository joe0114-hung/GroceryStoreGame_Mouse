using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 腳本功能：讀取實體滑鼠的座標，並將其轉換為 UI 畫布上的游標座標。
/// 掛載對象：場景中的 InputManager (或任何獨立的控制物件)。
/// 交接注意：【給 OAK-D 開發者的臨摹範本】
///         未來開發 OAK-D 訊號接收時，請另外建立一個 OakDInputProvider.cs，
///         只需讀取攝影機的 (X,Y) 座標，並用相同的方式賦值給 cursor.anchoredPosition 即可，
///         完全不需要修改遊戲的任何核心邏輯！
/// </summary>
public class MouseInputProvider : MonoBehaviour
{
    [Header("游標綁定")]
    [Tooltip("請放入場景上的 Cursor 游標最高層級物件")]
    [SerializeField] private RectTransform cursor;

    void Update()
    {
        // 1. 先確認目前有沒有接上滑鼠，避免程式報錯當機
        if (Mouse.current != null)
        {
            // 2. 讀取實體滑鼠在螢幕上的像素座標
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // 3. 將螢幕座標轉換為 Canvas 裡的相對 UI 座標
            // (cursor.parent as RectTransform 確保游標會在 Canvas 的正確範圍內移動)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cursor.parent as RectTransform,
                mousePos,
                null,
                out Vector2 localPos
            );

            // 4. 更新游標位置
            cursor.anchoredPosition = localPos;
        }
    }
}

