using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central stamp progress state.
/// Keeps track of completed levels and drives the stamp paper and top-right progress bar.
/// </summary>
public class StampManager : MonoBehaviour
{
    public static StampManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private int completedLevelCount;
    [SerializeField] private int completedStampIndex = 6;

    private readonly bool[] completedStampSlots = new bool[6];
    private GameObject[] progressEmptyIcons;
    private GameObject[] progressFilledIcons;

    public int CompletedCount => completedLevelCount;
    public int CompletedStampIndex => completedStampIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheProgressBar();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshProgressBar();
        SyncStampPaper();
    }

    public void SetCompletedCount(int count)
    {
        completedLevelCount = Mathf.Clamp(count, 0, 6);
        System.Array.Clear(completedStampSlots, 0, completedStampSlots.Length);

        for (int i = 0; i < completedLevelCount; i++)
        {
            completedStampSlots[i] = true;
        }

        if (completedLevelCount == 0)
        {
            completedStampIndex = 0;
        }
        else
        {
            completedStampIndex = completedLevelCount;
        }

        RefreshProgressBar();
        SyncStampPaper();
    }

    public void RegisterLevelCompleted()
    {
        RegisterLevelCompleted(6);
    }

    public void RegisterLevelCompleted(int stampIndex)
    {
        TryRegisterCompletedStamp(stampIndex);
    }

    public bool HasStamp(int stampIndex)
    {
        int zeroBasedIndex = stampIndex - 1;
        if (zeroBasedIndex < 0 || zeroBasedIndex >= completedStampSlots.Length)
        {
            return false;
        }

        return completedStampSlots[zeroBasedIndex];
    }

    public bool TryRegisterCompletedStamp(int stampIndex)
    {
        int zeroBasedIndex = Mathf.Clamp(stampIndex, 1, 6) - 1;

        if (completedStampSlots[zeroBasedIndex])
        {
            SyncStampPaper();
            return false;
        }

        completedStampSlots[zeroBasedIndex] = true;
        completedLevelCount = Mathf.Clamp(completedLevelCount + 1, 0, 6);
        completedStampIndex = zeroBasedIndex + 1;
        RefreshProgressBar();
        SyncStampPaper();
        return true;
    }

    private void CacheProgressBar()
    {
        Transform bar = GameObject.Find("StampProgressBar")?.transform;
        Transform slotsRoot = bar != null ? bar.Find("ProgressSlots") : null;

        progressEmptyIcons = new GameObject[6];
        progressFilledIcons = new GameObject[6];

        if (bar == null || slotsRoot == null)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            Transform slot = slotsRoot != null ? slotsRoot.Find($"ProgressSlot_{i + 1}") : null;
            if (slot == null)
            {
                Debug.LogWarning($"[StampManager] 找不到進度槽：ProgressSlot_{i + 1}");
                continue;
            }

            Transform emptyIcon = slot.Find("EmptyIcon");
            Transform filledIcon = slot.Find("FilledIcon");

            if (emptyIcon != null)
            {
                progressEmptyIcons[i] = emptyIcon.gameObject;
            }

            if (filledIcon != null)
            {
                progressFilledIcons[i] = filledIcon.gameObject;
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheProgressBar();
        RefreshProgressBar();
        SyncStampPaper();
    }

    private void RefreshProgressBar()
    {
        if (progressEmptyIcons == null || progressFilledIcons == null)
        {
            CacheProgressBar();
        }

        for (int i = 0; i < 6; i++)
        {
            bool isCompleted = i < completedLevelCount;

            if (progressEmptyIcons != null && progressEmptyIcons[i] != null)
            {
                progressEmptyIcons[i].SetActive(true);
            }

            if (progressFilledIcons != null && progressFilledIcons[i] != null)
            {
                progressFilledIcons[i].SetActive(isCompleted);
            }
        }
    }

    private void SyncStampPaper()
    {
        if (StampPaperProgressController.Instance != null)
        {
            StampPaperProgressController.Instance.ResetStampPaperState();

            for (int i = 0; i < completedStampSlots.Length; i++)
            {
                if (completedStampSlots[i])
                {
                    StampPaperProgressController.Instance.RevealStamp(i + 1);
                }
            }
        }
    }
}
