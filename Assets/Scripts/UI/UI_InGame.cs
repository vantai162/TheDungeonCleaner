using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    public static UI_InGame instance;
    public UI_FadeEffect fadeEffect { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI boxText;
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private TextMeshProUGUI grabText;
    [SerializeField] private TextMeshProUGUI checkPointButtonText;
    [SerializeField] private GameObject lastResortEffect;
    private Image lastResortImage;
    private bool lastResortAvailable = false;
    private float lastResortThreshold = 60f;
    [SerializeField] private GameObject checkPointMarkerPrefab; // THÊM DÒNG NÀY: Prefab của ngôi sao
    private List<GameObject> activeCheckPointMarkers = new();

    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject grabButton;
    [SerializeField] private Button freezeTimeButton;
    [SerializeField] private Button speedUpButton;
    [SerializeField] private Button checkPointButton;
    [SerializeField] private Button lastResortButton;

    [Header("Dio The World Effect")]
    [SerializeField] private GameObject theWorldOverlay;
    private Coroutine theWorldCoroutine; // THÊM BIẾN NÀY LƯU GIỮ COROUTINE

    private bool freezeTimeOnCooldown = false;
    private bool speedUpOnCooldown = false;
    private float freezeTimeCooldownRemaining = 0f;
    private float speedUpCooldownRemaining = 0f;
    private bool checkpointCreated = false;
    private Dictionary<Transform, Vector3> savePositions = new();
    private Dictionary<Player, float> saveHealths = new(); // Thêm dòng này
    private Vector2 savedPlayerDirection;

    private Image grabButtonImage;
    private Image freezeTimeImage;
    private Image speedUpImage;

    private bool isPaused;
    private PlayerBoxInteraction playerBoxInteraction;

    private void Awake()
    {
        instance = this;
        playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
        if (playerBoxInteraction == null)
        {
            Debug.LogWarning("PlayerBoxInteraction not found. Attempting fallback.");
            playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
        }

        // Ensure fadeEffect is assigned
        fadeEffect = GetComponentInChildren<UI_FadeEffect>(true);
        if (fadeEffect == null)
        {
            Debug.LogError("UI_FadeEffect is missing. Ensure it is added to the scene.");
        }

        // Ensure grabButtonImage is assigned
        if (grabButton != null)
        {
            grabButtonImage = grabButton.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("GrabButton is missing. Ensure it is assigned in the inspector.");
        }
        
        // Ensure lastResortEffect is assigned
        if (lastResortButton != null)
        {
            lastResortImage = lastResortButton.GetComponent<Image>();
            SetupCooldownOverlay(lastResortImage);
            lastResortButton.interactable = false;
        }
    }

    private void Start()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        fadeEffect.ScreenFade(0, 1);
    }

    private void Update()
    {
        bool pausePressed = false;
        if (InputManager.instance != null)
        {
            pausePressed = InputManager.instance.IsPausePressed();
        }
        else
        {
            pausePressed = Input.GetKeyDown(KeyCode.Escape);
        }

        if (pausePressed)
        {
            PauseButton();
        }
        UpdateGrabButtonState();
        UpdateCooldowns();
    }
    
    private void SetupCooldownOverlay(Image overlay)
    {
        if (overlay != null)
        {
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = 2;
            overlay.fillClockwise = false;
            overlay.fillAmount = 1f;
            overlay.color = new Color(1f, 1f, 1f, 1f);
        }
    }
    
    private void UpdateCooldowns()
    {
        if (freezeTimeOnCooldown && freezeTimeImage != null)
        {
            freezeTimeCooldownRemaining -= Time.deltaTime;
            freezeTimeImage.fillAmount = 1 - (freezeTimeCooldownRemaining / 60f);

            if (freezeTimeCooldownRemaining <= 0)
            {
                freezeTimeOnCooldown = false;
                freezeTimeImage.fillAmount = 1f;
                freezeTimeImage.color = new Color(1f, 1f, 1f, 1f);
                if (freezeTimeButton != null)
                    freezeTimeButton.interactable = true;
            }
        }
        
        if (speedUpOnCooldown && speedUpImage != null)
        {
            speedUpCooldownRemaining -= Time.deltaTime;
            speedUpImage.fillAmount = 1 - (speedUpCooldownRemaining / 60f);

            if (speedUpCooldownRemaining <= 0)
            {
                speedUpOnCooldown = false;
                speedUpImage.fillAmount = 1f;
                speedUpImage.color = new Color(1f, 1f, 1f, 1f);
                if (speedUpButton != null)
                    speedUpButton.interactable = true;
            }
        }
    }
    
    private void UpdateGrabButtonState()
    {
        // Safety guard: if mobile buttons/texts are unassigned in this layout, return safely
        if (grabText == null || grabButtonImage == null) return;

        if (playerBoxInteraction == null)
        {
            playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
            if (playerBoxInteraction == null) return;
        }

        if (playerBoxInteraction.isDragging)
        {
            grabText.text = "Release";
            grabButtonImage.color = new Color(1f, 1f, 1f, 1f);
        }
        else if (playerBoxInteraction.isBoxHighlighted)
        {
            grabText.text = "Grab";
            grabButtonImage.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            grabText.text = "Grab";
            grabButtonImage.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    public void GrabReleaseButton()
    {
        if (playerBoxInteraction == null)
        {
            playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
            if (playerBoxInteraction == null) return;
        }

        if (playerBoxInteraction.isDragging)
        {
            if (!playerBoxInteraction.isMoving)
                playerBoxInteraction.ReleaseBox();
            else
            {
                playerBoxInteraction.BufferReleaseAction();
            }
        }
        else
        {
            if (!playerBoxInteraction.isMoving)
                playerBoxInteraction.TryGrabBox();
            else
            {
                playerBoxInteraction.BufferGrabAction();
            }
        }
    }
    
    public void UndoButton()
    {
        if (playerBoxInteraction == null)
        {
            playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
        }
        if (playerBoxInteraction != null)
        {
            playerBoxInteraction.UndoMove();
        }
    }
    
    public void ResetLevelButton()
    {
        if (playerBoxInteraction == null)
        {
            playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();
        }
        if (playerBoxInteraction != null)
        {
            playerBoxInteraction.ResetLevel();
        }
    }

    public void PauseButton()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1;
            pauseUI.SetActive(false);
        }
        else
        {
            isPaused = true;
            Time.timeScale = 0;
            pauseUI.SetActive(true);
        }
    }

    public void GoToMainMenuButton()
    {
        SceneManager.LoadScene(0);
    }

    public void UpdateBoxUI(int occupiedBoxes, int totalBoxes)
    {
        boxText.text = occupiedBoxes + "/" + totalBoxes;
        if (occupiedBoxes >= totalBoxes && totalBoxes > 0)
            boxText.color = Color.green;
        else
            boxText.color = Color.white;
    }
    
    public void UpdatePlayerUI(int occupiedPoints, int totalPoints)
    {
        playerText.text = occupiedPoints + "/" + totalPoints;
        if (occupiedPoints >= totalPoints && totalPoints > 0)
            playerText.color = Color.green;
        else
            playerText.color = Color.white;
    }

    public void UpdateTimerUI(float timer)
    {
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        
    }

    // Thêm hàm public mới này bên trong UI_InGame
    public void CheckLastResortAvailability(bool isCritical, float currentHealth, float maxHealth)
    {
        // Kích hoạt khi rơi vào trạng thái Critical lần đầu
        if (isCritical && !lastResortAvailable && lastResortButton != null)
        {
            lastResortAvailable = true;
            lastResortButton.interactable = true;
            AudioManager.instance.PlaySFX(5); // Alert sound when available

            // Visual indication
            StartCoroutine(PulseLastResortButton());
        }

        // Cập nhật vòng quay visual (cooldown) dựa trên lượng máu đã mất cho tới khi tụt xuống Critical
        if (!lastResortAvailable && lastResortButton && lastResortImage)
        {
            // Calculate progress towards critical threshold
            float criticalHealthThreshold = maxHealth * 0.3f; // Dựa theo biến criticalThreshold trong Player

            // Lượng máu cần mất thêm để đến critical
            float healthAboveCritical = currentHealth - criticalHealthThreshold;
            float totalHealthRangeToCritical = maxHealth - criticalHealthThreshold;

            if (totalHealthRangeToCritical > 0)
            {
                float fillAmount = Mathf.Clamp01(healthAboveCritical / totalHealthRangeToCritical);
                lastResortImage.fillAmount = fillAmount;
            }
        }
    }

    public void OnFreezeTimeButtonPressed()
    {
        GameManager.instance.FreezeTime(40f);
        freezeTimeButton.gameObject.SetActive(false);

        // 1. Phát âm thanh ngưng đọng thời gian hệt như Dio
        AudioManager.instance.PlaySFX(12);

        // Đảm bảo không có coroutine nào cũ đang chạy lặp đè
        if (theWorldCoroutine != null)
            StopCoroutine(theWorldCoroutine);
            
        // 2. Kích hoạt hiệu ứng hình ảnh
        theWorldCoroutine = StartCoroutine(TheWorldEffectRoutine(40f));
    }

    private IEnumerator TheWorldEffectRoutine(float duration)
    {
        Image overlayImg = theWorldOverlay?.GetComponent<Image>();
        if (overlayImg == null) yield break;

        theWorldOverlay.SetActive(true);

        // Phase 1: Flash trắng (chạy bằng realtime vì timeScale sắp = 0)
        overlayImg.color = new Color(1f, 1f, 1f, 0.9f);
        yield return new WaitForSecondsRealtime(0.08f);

        // Phase 2: Chuyển sang màu "frozen world" - tím xanh đặc trưng Jojo
        overlayImg.color = new Color(0.15f, 0.05f, 0.3f, 0.35f);
        // Thêm: chuyển dần từ trắng sang tím trong 0.4s
        float transitionTime = 0.4f;
        float elapsed = 0f;
        Color flashColor = new Color(1f, 1f, 1f, 0.9f);
        Color frozenColor = new Color(0.15f, 0.05f, 0.3f, 0.35f);
        while (elapsed < transitionTime)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayImg.color = Color.Lerp(flashColor, frozenColor, elapsed / transitionTime);
            yield return null;
        }

        // Phase 2 giờ có màu đúng rồi, giữ thêm 1s để người chơi "cảm nhận" moment
        yield return new WaitForSecondsRealtime(1.0f);
        // Đổi nhạc sang BGM frozen
        AudioManager.instance.PlayBGM(2);

        // Phase 3: Giữ frozen, fade nhẹ overlay về alpha thấp hơn cho dễ chơi
        float holdTime = duration - 0.5f;
        elapsed = 0f;
        while (elapsed < holdTime)
        {
            elapsed += Time.unscaledDeltaTime; // QUAN TRỌNG: unscaledDeltaTime vì timeScale = 0
                                               // Nhấp nháy nhẹ để tạo cảm giác "thời gian đứng yên"
            float pulse = 0.55f + Mathf.Sin(elapsed * 1.5f) * 0.05f;
            overlayImg.color = new Color(0.15f, 0.05f, 0.3f, pulse);
            yield return null;
        }

        // Phase 4: Fade out overlay trước khi resume
        AudioManager.instance.PlaySFX(13); // SFX time resume
        AudioManager.instance.PlayBGM(1); // Quay lại nhạc nền bình thường
        float fadeTime = 0.5f;
        elapsed = 0f;
        Color fromColor = overlayImg.color;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;
            overlayImg.color = Color.Lerp(fromColor, new Color(1f, 1f, 1f, 0f), t);
            yield return null;
        }

        // Phase 5: Cleanup
        theWorldOverlay.SetActive(false);
    }

    private void ResetTimerTextColor()
    {
        timerText.color = Color.white;
    }

    public void OnSpeedUpButtonPressed()
    {
        StartCoroutine(HandleSpeedUpBooster(60f));
        speedUpButton.gameObject.SetActive(false);
    }
    
    public void OnCheckPointButtonPressed()
    {
        if (!checkpointCreated)
            CreateCheckpoint();
        else
            GoToCheckpoint();
    }

    public void OnLastResortButtonPressed()
    {
        if (!lastResortAvailable) return;
    
        // Try to find the effect if not directly assigned
        if (lastResortEffect == null)
        {
            lastResortEffect = GameObject.FindGameObjectWithTag("LastResortEffect");
        }
    
        // Activate the effect if found
        if (lastResortEffect != null)
        {
            lastResortEffect.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Last Resort effect not found. Make sure to assign it or tag it properly.");
        }
    
        // Disable the button after use
        lastResortButton.gameObject.SetActive(false);
    }

    private IEnumerator HandleSpeedUpBooster(float duration)
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.SetMovementSpeed(0.1f);
        }

        PlayerBoxInteraction[] interactions = FindObjectsByType<PlayerBoxInteraction>(FindObjectsSortMode.None);
        foreach (var interaction in interactions)
        {
            interaction.SetMoveDuration(0.1f);
        }

        yield return new WaitForSeconds(duration);
        
        foreach (var p in players)
        {
            p.SetMovementSpeed(0.135f);
        }

        foreach (var interaction in interactions)
        {
            interaction.SetMoveDuration(0.135f);
        }
    }
    
    public void OnNextLevelButtonClicked()
    {
        if (GameManager.instance != null)
            GameManager.instance.GoToNextLevel();
    }

    public void OnRetryLevelButtonClicked()
    {
        if (GameManager.instance != null)
            GameManager.instance.RestartLevel();
    }
    
    private void CreateCheckpoint()
    {
        savePositions.Clear();
        saveHealths.Clear(); // Xóa dữ liệu cũ

        // Dọn dẹp các ngôi sao của lần Save trước (nếu có lỗi sót)
        foreach (var marker in activeCheckPointMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        activeCheckPointMarkers.Clear();

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            savePositions[player.transform] = player.transform.position;
            saveHealths[player] = player.CurrentHealth; // Lưu lượng máu hiện tại của người chơi

            // THÊM ĐOẠN NÀY: Sinh ra hình ảnh tại đúng vị trí của Player
            if (checkPointMarkerPrefab != null)
            {
                GameObject marker = Instantiate(checkPointMarkerPrefab, player.transform.position, Quaternion.identity);
                activeCheckPointMarkers.Add(marker);
            }
        }

        Box[] boxes = FindObjectsByType<Box>(FindObjectsSortMode.None);
        foreach (Box box in boxes)
        {
            savePositions[box.transform] = box.transform.position;
        }
        checkpointCreated = true;
        checkPointButtonText.text = "Go to Checkpoint";
    }

    private void GoToCheckpoint()
    {
        // Reset any active dragging state
        playerBoxInteraction = FindFirstObjectByType<PlayerBoxInteraction>();

        if (playerBoxInteraction != null && playerBoxInteraction.isDragging)
        {
            playerBoxInteraction.ReleaseBox();
        }

        // Khôi phục lại máu đã lưu (Thêm khối này)
        foreach (var kvp in saveHealths)
        {
            if (kvp.Key != null)
            {
                kvp.Key.RestoreHealth(kvp.Value);
            }
        }

        // Restore saved positions
        foreach (var kvp in savePositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value;

                // Play dust effect for visual feedback
                if (kvp.Key.GetComponent<Player>() != null)
                {
                    kvp.Key.GetComponent<Player>().PlayDustEffect();

                    // Reset player direction
                    kvp.Key.GetComponent<Player>().lastMovementDirection = savedPlayerDirection;

                    // Apply player facing direction
                    if (savedPlayerDirection.x < 0)
                        kvp.Key.localScale = new Vector3(-1, 1, 1);
                    else if (savedPlayerDirection.x > 0)
                        kvp.Key.localScale = new Vector3(1, 1, 1);
                }
                else if (kvp.Key.GetComponent<Box>() != null)
                {
                    kvp.Key.GetComponent<Box>().PlayDustEffect();
                    // First reset state so we can properly update it
                    kvp.Key.GetComponent<Box>().SetOnPointState(false);
                }
            }
        }

        // Force check all box points to update box visual states
        BoxPoint[] boxPoints = FindObjectsByType<BoxPoint>(FindObjectsSortMode.None);
        foreach (var boxPoint in boxPoints)
        {
            // This will trigger the check for any boxes that were placed at their points
            boxPoint.ForceRecalculateOccupation();
        }

        // Force update box and player counts to ensure UI is correct
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateBoxCount();
            GameManager.instance.UpdatePlayerCount();
        }

        checkpointCreated = false;
        checkPointButtonText.text = "Create Checkpoint";

        // THÊM ĐOẠN NÀY: Dọn dẹp và xóa các ngôi sao trên bản đồ khỏi game
        foreach (var marker in activeCheckPointMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        activeCheckPointMarkers.Clear();
    }
    
    private IEnumerator PulseLastResortButton()
    {
        Color originalColor = lastResortImage.color;
        float pulseTime = 0.5f;
    
        for (int i = 0; i < 3; i++)
        {
            lastResortImage.color = Color.yellow;
            yield return new WaitForSeconds(pulseTime);
            lastResortImage.color = originalColor;
            yield return new WaitForSeconds(pulseTime);
        }
    }

    private void OnDestroy()
    {
        // Khi Scene bị reset cứng hoặc thoát về Main Menu, dừng cái bóng đêm The World lại.
        if (theWorldCoroutine != null)
        {
            StopCoroutine(theWorldCoroutine);
            
            // Xóa sổ màu màn hình âm bản (đề phòng lưu bộ nhớ đệm)
            if (theWorldOverlay != null)
            {
                theWorldOverlay.SetActive(false);
            }
            
            // Ép đổi BGM trở lại bài mặc định và hủy hết các hiệu ứng dị thường
            if (AudioManager.instance != null)
            {
                // PlayRandomBGM() hoặc PlayBGM(1) tùy cài đặt mặc định của game bạn
                AudioManager.instance.PlayBGM(1); 
            }
        }
    }
}
