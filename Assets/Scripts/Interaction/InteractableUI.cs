using UnityEngine;

/// <summary>
/// 腳本功能：可互動物件的「身分標籤 (Marker Component)」。
/// 掛載對象：任何希望被 Dwell (懸停系統) 偵測並觸發的 UI 物件 (如 GiftBox, 籃子等)。
/// 交接注意：這是一個空腳本，僅作為 Tag 使用。
///         DwellInteractor 的射線只會對帶有此腳本的物件進行倒數計時。
/// </summary>
public class InteractableUI : MonoBehaviour
{
    // 不需任何程式碼，只要掛著就有作用！
}