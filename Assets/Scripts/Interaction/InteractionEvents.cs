using System;
using UnityEngine;

/// <summary>
/// 腳本功能：全域互動事件廣播中心 (Event Bus)。
/// 運作原理：DwellInteractor (或未來的輸入端) 負責發布 OnSelect 事件。
///         任何需要被點擊的物件 (如 GiftBox, Basket) 只需訂閱此事件，即可做出反應。
/// </summary>
public static class InteractionEvents
{
    // 當有物件被游標「確定選取」時觸發，並傳遞被選取的 GameObject
    public static Action<GameObject> OnSelect;
}