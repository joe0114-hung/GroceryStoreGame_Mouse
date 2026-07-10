using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual feedback for objects selected by the virtual hand dwell system.
/// </summary>
public class InteractableHoverFeedback : MonoBehaviour
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = new Color(1f, 0.86f, 0.48f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.95f, 0.62f, 1f);
    [SerializeField] private float hoverScale = 1.08f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        ApplyNormal();
    }

    private void OnEnable()
    {
        InteractionEvents.OnHoverEnter += HandleHoverEnter;
        InteractionEvents.OnHoverExit += HandleHoverExit;
    }

    private void OnDisable()
    {
        InteractionEvents.OnHoverEnter -= HandleHoverEnter;
        InteractionEvents.OnHoverExit -= HandleHoverExit;
    }

    private void HandleHoverEnter(GameObject target)
    {
        if (target != gameObject)
        {
            return;
        }

        transform.localScale = originalScale * hoverScale;

        if (targetGraphic != null)
        {
            targetGraphic.color = hoverColor;
        }
    }

    private void HandleHoverExit(GameObject target)
    {
        if (target != gameObject)
        {
            return;
        }

        ApplyNormal();
    }

    private void ApplyNormal()
    {
        transform.localScale = originalScale;

        if (targetGraphic != null)
        {
            targetGraphic.color = normalColor;
        }
    }
}
