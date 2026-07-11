using UnityEngine;
using System;

/// <summary>
/// Controls which stamp icons are visible on the stamp paper.
/// Hides everything on start and reveals one stamp at a time by index.
/// </summary>
public class StampPaperProgressController : MonoBehaviour
{
    public static StampPaperProgressController Instance;

    [Header("Auto Hide")]
    [SerializeField] private bool hideAllOnStart = true;

    private GameObject[] stampSlots;
    private GameObject[] stampIcons;
    private bool[] revealedStampSlots;

    private void Awake()
    {
        Instance = this;
        CacheStampIcons();
        revealedStampSlots = new bool[6];
    }

    private void Start()
    {
        if (hideAllOnStart)
        {
            ResetStampPaperState();
        }

        SyncFromManager();
    }

    public void HideAll()
    {
        if (stampSlots == null)
        {
            CacheStampIcons();
        }

        if (stampIcons == null)
        {
            CacheStampIcons();
        }

        foreach (GameObject stampSlot in stampSlots)
        {
            if (stampSlot != null)
            {
                stampSlot.SetActive(false);
            }
        }

        foreach (GameObject stampIcon in stampIcons)
        {
            if (stampIcon != null)
            {
                stampIcon.SetActive(false);
            }
        }
    }

    public void ResetStampPaperState()
    {
        HideAll();

        if (revealedStampSlots == null || revealedStampSlots.Length != 6)
        {
            revealedStampSlots = new bool[6];
            return;
        }

        Array.Clear(revealedStampSlots, 0, revealedStampSlots.Length);
    }

    public bool HasStamp(int index)
    {
        int zeroBasedIndex = index - 1;
        if (zeroBasedIndex < 0 || zeroBasedIndex >= revealedStampSlots.Length)
        {
            return false;
        }

        return revealedStampSlots[zeroBasedIndex];
    }

    public bool RevealStamp(int index)
    {
        if (stampIcons == null)
        {
            CacheStampIcons();
        }

        if (revealedStampSlots == null || revealedStampSlots.Length != 6)
        {
            revealedStampSlots = new bool[6];
        }

        int zeroBasedIndex = index - 1;
        if (zeroBasedIndex < 0 || zeroBasedIndex >= stampIcons.Length)
        {
            Debug.LogWarning($"[StampPaper] 無效的印章索引：{index}");
            return false;
        }

        if (revealedStampSlots[zeroBasedIndex])
        {
            return false;
        }

        revealedStampSlots[zeroBasedIndex] = true;

        if (stampSlots != null && stampSlots[zeroBasedIndex] != null)
        {
            stampSlots[zeroBasedIndex].SetActive(true);
        }

        if (stampIcons[zeroBasedIndex] != null)
        {
            stampIcons[zeroBasedIndex].SetActive(true);
        }

        return true;
    }

    public void RevealStamp1() => RevealStamp(1);
    public void RevealStamp2() => RevealStamp(2);
    public void RevealStamp3() => RevealStamp(3);
    public void RevealStamp4() => RevealStamp(4);
    public void RevealStamp5() => RevealStamp(5);
    public void RevealStamp6() => RevealStamp(6);

    public void SetCompletedCount(int count)
    {
        if (stampSlots == null)
        {
            CacheStampIcons();
        }

        if (stampIcons == null)
        {
            CacheStampIcons();
        }

        int visibleCount = Mathf.Clamp(count, 0, stampIcons.Length);
        for (int i = 0; i < stampIcons.Length; i++)
        {
            GameObject stampSlot = stampSlots != null ? stampSlots[i] : null;
            GameObject stampIcon = stampIcons[i];

            if (stampSlot != null)
            {
                stampSlot.SetActive(i < visibleCount);
            }

            if (stampIcon != null)
            {
                stampIcon.SetActive(i < visibleCount);
            }
        }
    }

    public void ShowOnlyStamp(int index)
    {
        ResetStampPaperState();
        RevealStamp(index);
    }

    public void SyncFromManager()
    {
        if (StampManager.Instance == null)
        {
            return;
        }

        ResetStampPaperState();

        for (int i = 1; i <= 6; i++)
        {
            if (StampManager.Instance.HasStamp(i))
            {
                RevealStamp(i);
            }
        }
    }

    private void CacheStampIcons()
    {
        stampSlots = new GameObject[6];
        stampIcons = new GameObject[6];
        Transform paper = transform;

        stampSlots[0] = FindStampSlot(paper, "StampSlot_1");
        stampSlots[1] = FindStampSlot(paper, "StampSlot_2");
        stampSlots[2] = FindStampSlot(paper, "StampSlot_3");
        stampSlots[3] = FindStampSlot(paper, "StampSlot_4");
        stampSlots[4] = FindStampSlot(paper, "StampSlot_5");
        stampSlots[5] = FindStampSlot(paper, "StampSlot_6");

        stampIcons[0] = FindStampIcon(paper, "StampSlot_1", "UI_Stamp_Calendar");
        stampIcons[1] = FindStampIcon(paper, "StampSlot_2", "UI_Stamp_Abacus");
        stampIcons[2] = FindStampIcon(paper, "StampSlot_3", "UI_Stamp_Brain");
        stampIcons[3] = FindStampIcon(paper, "StampSlot_4", "UI_Stamp_Megaphone");
        stampIcons[4] = FindStampIcon(paper, "StampSlot_5", "UI_Stamp_Storefront");
        stampIcons[5] = FindStampIcon(paper, "StampSlot_6", "UI_Stamp_Basket");
    }

    private static GameObject FindStampSlot(Transform paper, string slotName)
    {
        Transform slot = paper.Find(slotName);
        if (slot == null)
        {
            Debug.LogWarning($"[StampPaper] 找不到 slot：{slotName}");
            return null;
        }

        return slot.gameObject;
    }

    private static GameObject FindStampIcon(Transform paper, string slotName, string iconName)
    {
        Transform slot = paper.Find(slotName);
        if (slot == null)
        {
            Debug.LogWarning($"[StampPaper] 找不到 slot：{slotName}");
            return null;
        }

        Transform icon = slot.Find(iconName);
        if (icon == null)
        {
            Debug.LogWarning($"[StampPaper] 找不到圖示：{slotName}/{iconName}");
            return null;
        }

        return icon.gameObject;
    }
}
