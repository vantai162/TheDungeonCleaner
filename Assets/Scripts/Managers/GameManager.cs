using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    private UI_InGame inGameUI;
    [SerializeField] private GameObject loseUI;
    [SerializeField] private GameObject winUI;

    [Header("Level Management")]
    [SerializeField] private float levelTimer;
    [SerializeField] public float maxLevelTime = 60f;
    [SerializeField] private int currentLevelIndex;
    private int nextLevelIndex;
    
    [Header("Boosters")]
    public bool freezeTimeActive = false;
    
    [Header("Rewards")]
    private Dictionary<int, int> levelRewards = new()
    {
        {1, 5},
        {2, 10},
        {3, 15},
        {4, 20},
        {5, 30},
        {6, 40},
        {7, 50},
    };

    private BoxPoint[] boxPoints;
    private PlayerPoint[] playerPoints;
    private bool levelFinishProcessed = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset everything since there's a bug and this is the only way i can deal with it
        Time.timeScale = 1;
        Input.ResetInputAxes();
        
        ResetPlayerStates();
    }

    private void ResetPlayerStates()
    {
        // Reset Player and also if you were wondering why Player[]. It is for future local co op function so yeah i'm not dumb
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.ResetState();
        }

        // Reset PlayerBoxInteraction
        PlayerBoxInteraction[] interactions = FindObjectsByType<PlayerBoxInteraction>(FindObjectsSortMode.None);
        foreach (PlayerBoxInteraction interaction in interactions)
        {
            interaction.ResetState();
        }
        
        StopAllCoroutines();
    }

    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        currentLevelIndex = currentScene.buildIndex;
        nextLevelIndex = currentLevelIndex + 1;
        inGameUI = UI_InGame.instance;
        CollectGameInfo();
        
        levelTimer = 0f;
    }

    private void CollectGameInfo()
    {
        boxPoints = FindObjectsByType<BoxPoint>(FindObjectsSortMode.None);
        playerPoints = FindObjectsByType<PlayerPoint>(FindObjectsSortMode.None);
        UpdatePlayerCount();
        UpdateBoxCount();
    }
    
    private void Update()
    {
        if (!levelFinishProcessed && !freezeTimeActive)
        {
            levelTimer += Time.deltaTime;
        }
    }
    
    public void UpdateBoxCount()
    {
        if (inGameUI == null || boxPoints == null) return;
    
        int occupiedBoxes = 0;
        foreach (BoxPoint bp in boxPoints)
        {
            if (bp != null && bp.isOccupied)
                occupiedBoxes++;
        }
    
        inGameUI.UpdateBoxUI(occupiedBoxes, boxPoints.Length);
    }

    public void UpdatePlayerCount()
    {
        if (inGameUI == null || playerPoints == null) return;
    
        int occupiedPlayers = 0;
        foreach (PlayerPoint pp in playerPoints)
        {
            if (pp != null && pp.isOccupied)
                occupiedPlayers++;
        }
    
        inGameUI.UpdatePlayerUI(occupiedPlayers, playerPoints.Length);
    }

    public void ShowLoseUI()
    {
        if (loseUI != null)
        {
            loseUI.SetActive(true);
            DisablePlayerControls();
        }
        else if (inGameUI != null)
        {
            inGameUI.fadeEffect.ScreenFade(1, 1.5f, ReturnToMainMenu);
        }
    }

    public void CheckLevelCompletion()
    {
        if (!levelFinishProcessed && AreAllPointsOccupied())
        {
            levelFinishProcessed = true;
            LevelFinished();
        }
    }

    private bool AreAllPointsOccupied()
    {
        if (boxPoints == null || boxPoints.Length == 0 || 
            playerPoints == null || playerPoints.Length == 0)
        {
            return false;
        }

        foreach (BoxPoint bp in boxPoints)
        {
            if (bp == null || !bp.isOccupied) return false;
        }
        
        foreach (PlayerPoint pp in playerPoints)
        {
            if (pp == null || !pp.isOccupied) return false;
        }
        
        return true;
    }
    
    private void LevelFinished()
    {
        AddLevelReward();
        SaveLevelProgression();
        SaveBestTime();
    
        if (winUI != null)
        {
            winUI.SetActive(true);
            // Stop all player movement/input when showing win UI
            DisablePlayerControls();
        }
        else
        {
            // Fallback if UI not assigned
            LoadNextScene();
        }
    }
    
    private void DisablePlayerControls()
    {
        // Disable player controls when showing result screens
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.enabled = false;
        }
    
        PlayerBoxInteraction[] interactions = FindObjectsByType<PlayerBoxInteraction>(FindObjectsSortMode.None);
        foreach (var interaction in interactions)
        {
            interaction.enabled = false;
        }
    }

    private void SaveBestTime()
    {
        float lastTime = PlayerPrefs.GetFloat("Level" + currentLevelIndex + "BestTime", 999);
        if (levelTimer < lastTime)
            PlayerPrefs.SetFloat("Level" + currentLevelIndex + "BestTime", levelTimer);
    }

    private void SaveLevelProgression()
    {
        PlayerPrefs.SetInt("Level" + nextLevelIndex + "Unlocked", 1);
        if (!NoMoreLevels())
            PlayerPrefs.SetInt("ContinueLevelNumber", nextLevelIndex);
    }
    
    private void AddLevelReward()
    {
        int currentMoney = PlayerPrefs.GetInt("MoneyInBank", 0);
        int reward = 5;

        if (levelRewards.ContainsKey(currentLevelIndex))
            reward = levelRewards[currentLevelIndex];
        
        PlayerPrefs.SetInt("MoneyInBank", currentMoney + reward);
        PlayerPrefs.Save();
    }
    
    private void LoadLevelEnd() => SceneManager.LoadScene("TheEnd");

    private void LoadNextLevel()
    {
        SceneManager.LoadScene("Level_" + nextLevelIndex);
    }
    
    private void LoadNextScene()
    {
        CancelInvoke(nameof(CheckLevelCompletion));
        var fadeEffect = inGameUI.fadeEffect;

        if (NoMoreLevels())
            fadeEffect.ScreenFade(1, 1.5f, LoadLevelEnd);
        else
            fadeEffect.ScreenFade(1, 1.5f, LoadNextLevel);        
    }
    
    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private bool NoMoreLevels()
    {
        // Menu and End screen take 2 that's why total scene = all level scene + 2
        return currentLevelIndex + 2 >= SceneManager.sceneCountInBuildSettings;
    }
    
    public void FreezeTime(float duration)
    {
        if (!freezeTimeActive)
            StartCoroutine(HandleFreezeTime(duration));
    }

    private IEnumerator HandleFreezeTime(float duration)
    {
        freezeTimeActive = true;
        yield return new WaitForSeconds(duration);
        freezeTimeActive = false;
    }
    
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        if (NoMoreLevels())
            LoadLevelEnd();
        else
            LoadNextLevel();
    }

    public void GoToMainMenu()
    {
        ReturnToMainMenu();
    }
}