using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Data")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("UI")]
    public Transform levelGridRoot;
    public Button prevPageButton;
    public Button nextPageButton;
    public Button backButton;

    [Header("Prefab")]
    public GameObject levelButtonPrefab;

    [Header("Paging")]
    public int levelsPerPage = 8;

    [Header("Reference")]
    public MainMenuUI mainMenuUI;

    private int currentPage = 0;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        Debug.Log($"[LEVEL SELECT] Start | levels={levels.Count}");

        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (backButton != null)
            backButton.onClick.AddListener(BackToMain);

        RefreshPage();
    }

    void RefreshPage()
    {
        ClearButtons();

        int totalLevels = levels.Count;
        int startIndex = currentPage * levelsPerPage;
        int endIndex = Mathf.Min(startIndex + levelsPerPage, totalLevels);

        Debug.Log($"[LEVEL SELECT] RefreshPage {currentPage} | show {startIndex}..{endIndex - 1}");

        for (int i = startIndex; i < endIndex; i++)
        {
            if (levels[i] == null) continue;

            GameObject obj = Instantiate(levelButtonPrefab, levelGridRoot);
            spawnedButtons.Add(obj);

            LevelSelectButton btn = obj.GetComponent<LevelSelectButton>();
            if (btn != null)
            {
                string label = string.IsNullOrWhiteSpace(levels[i].levelName)
                    ? $"Level\n{i}"
                    : levels[i].levelName.Replace("_", "\n");

                btn.Setup(i, label, OnLevelSelected);
            }
            else
            {
                Debug.LogError("[LEVEL SELECT] Prefab tidak punya LevelSelectButton!");
            }
        }

        int maxPage = Mathf.CeilToInt((float)Mathf.Max(1, totalLevels) / levelsPerPage) - 1;

        if (prevPageButton != null)
            prevPageButton.interactable = currentPage > 0;

        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < maxPage;
    }

    void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }

        spawnedButtons.Clear();
    }

    void PrevPage()
    {
        if (currentPage <= 0) return;
        currentPage--;
        RefreshPage();
    }

    void NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)Mathf.Max(1, levels.Count) / levelsPerPage) - 1;
        if (currentPage >= maxPage) return;
        currentPage++;
        RefreshPage();
    }

    void BackToMain()
    {
        Debug.Log("[LEVEL SELECT] BackToMain");

        if (mainMenuUI != null)
            mainMenuUI.ShowMain();
        else
            Debug.LogError("[LEVEL SELECT] mainMenuUI belum di-assign!");
    }

    void OnLevelSelected(int levelIndex)
    {
        Debug.Log($"[LEVEL SELECT] Click level {levelIndex}");

        if (mainMenuUI != null)
            mainMenuUI.StartGameFromLevel(levelIndex);
        else
            Debug.LogError("[LEVEL SELECT] mainMenuUI belum di-assign!");
    }
}
