using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// 掛在任何 UI Graphic（例如按鈕的 Image）上，讓它呈現「上緣亮、下緣暗」的漸層效果，
    /// 模擬立體浮起、有光澤感的按鈕外觀，不需要額外準備圖片，純粹用頂點顏色計算。
    ///
    /// 使用方式：
    ///   1. 把這個腳本掛在按鈕的 Image 上（跟 Image 元件同一個物件）
    ///   2. 調整 Top Color / Bottom Color 兩個顏色即可即時看到效果
    ///   3. 想要套用到其他按鈕，用「Copy Component → Paste Component Values」快速複製設定
    /// </summary>
    [AddComponentMenu("UI/Effects/Gradient Overlay")]
    [RequireComponent(typeof(Graphic))]
    public class UIGradientOverlay : BaseMeshEffect
    {
        [Tooltip("按鈕上緣要疊加的顏色（選白色，讓上緣看起來比較亮）")]
        public Color topColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("按鈕下緣要疊加的顏色（選黑色，讓下緣看起來比較暗，產生立體感）")]
        public Color bottomColor = new Color(0f, 0f, 0f, 1f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);

            if (vertexList.Count == 0) return;

            float top = vertexList[0].position.y;
            float bottom = vertexList[0].position.y;

            for (int i = 0; i < vertexList.Count; i++)
            {
                float y = vertexList[i].position.y;
                if (y > top) top = y;
                if (y < bottom) bottom = y;
            }

            float height = top - bottom;

            for (int i = 0; i < vertexList.Count; i++)
            {
                var v = vertexList[i];
                float t = height > 0f ? (v.position.y - bottom) / height : 0f;
                Color grad = Color.Lerp(bottomColor, topColor, t);

                v.color = new Color32(
                    (byte)(v.color.r * grad.r),
                    (byte)(v.color.g * grad.g),
                    (byte)(v.color.b * grad.b),
                    (byte)(v.color.a * grad.a)
                );

                vertexList[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }
    }
}
