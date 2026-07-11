using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StampCheckDebugButtonPanel : MonoBehaviour
{
    [SerializeField] private StampCheckStampAnimator stampAnimator;
    [SerializeField] private Vector2 buttonSize = new Vector2(88f, 40f);
    [SerializeField] private Vector2 buttonSpacing = new Vector2(8f, 8f);
    [SerializeField] private int columns = 3;

    private void Awake()
    {
        if (stampAnimator == null)
        {
            stampAnimator = FindObjectOfType<StampCheckStampAnimator>();
        }

        EnsureGridLayout();
        BuildButtons();
    }

    private void EnsureGridLayout()
    {
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = buttonSize;
        grid.spacing = buttonSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(0, 0, 0, 0);
    }

    private void BuildButtons()
    {
        if (transform.childCount > 0)
        {
            return;
        }

        Sprite buttonSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        for (int i = 1; i <= 6; i++)
        {
            int stampIndex = i;
            GameObject buttonObject = new GameObject($"TestStamp_{stampIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.94f, 0.89f, 0.72f, 0.92f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                if (stampAnimator != null)
                {
                    stampAnimator.PlayStamp(stampIndex);
                }
            });

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = fontAsset;
            label.text = $"測{stampIndex}";
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.28f, 0.18f, 0.08f, 1f);
            label.raycastTarget = false;
        }
    }
}
