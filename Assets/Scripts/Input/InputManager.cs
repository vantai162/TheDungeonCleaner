using UnityEngine;
using UnityEngine.SceneManagement;

public enum UILayoutMode
{
    Auto = 0,
    ForceMobile = 1,
    ForceDesktop = 2
}

public enum InputMode
{
    Keyboard = 0,
    MobileTouch = 1,
    Gamepad = 2
}

/// <summary>
/// Central Input System Manager.
/// Implements a persistent Singleton that manages the active input adapter,
/// handles automatic device detection, and controls UI layout panel visibility (mobile vs desktop).
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager _instance;

    public static InputManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager (Dynamic)");
                    _instance = go.AddComponent<InputManager>();
                }
            }
            return _instance;
        }
    }

    [Header("UI Canvas Layout Panels")]
    [Tooltip("Drag the parent container of your Mobile Joystick/touch controls here.")]
    [SerializeField] private GameObject mobileUIPanel;
    [Tooltip("Drag the parent container of your Desktop Control layout/instructions here.")]
    [SerializeField] private GameObject desktopUIPanel;

    [Header("Desktop Prompt Sub-Panels (Optional)")]
    [Tooltip("Sub-panel showing Keyboard button prompts (e.g. WASD, U, R, Escape).")]
    [SerializeField] private GameObject keyboardPromptPanel;
    [Tooltip("Sub-panel showing Gamepad button prompts (e.g. D-Pad, A, B, Y, Start).")]
    [SerializeField] private GameObject gamepadPromptPanel;

    [Header("Configuration Settings")]
    [SerializeField] private UILayoutMode layoutMode = UILayoutMode.Auto;
    [SerializeField] private InputMode currentInputMode = InputMode.Keyboard;

    private IInputAdapter activeAdapter;
    private Player cachedPlayer;

    private void Awake()
    {
        // Singleton Pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPreferences();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdatePlayerReference();
        InitializeAdapter();
        ApplyLayoutAndInputMode();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Auto-rebind player reference when a new level or scene is loaded
        UpdatePlayerReference();
        
        // Auto-find panels if they went missing during scene transition
        FindUIReferencesInScene();
        
        ApplyLayoutAndInputMode();
    }

    private void Update()
    {
        // Guard check for Unity pseudo-null on destroyed player objects
        if (cachedPlayer == null)
        {
            UpdatePlayerReference();
        }

        DetectInputDevice();
    }

    /// <summary>
    /// Re-binds to the active Player in the scene.
    /// </summary>
    public void UpdatePlayerReference()
    {
        cachedPlayer = FindFirstObjectByType<Player>();
        InitializeAdapter();
    }

    /// <summary>
    /// Attempts to dynamically find UI references in the scene if they aren't assigned in the inspector.
    /// Useful for sandbox testing or loading direct scenes in the editor.
    /// </summary>
    private void FindUIReferencesInScene()
    {
        // 1. Locate the active HUD Canvas (UI_InGame) in the scene
        UI_InGame inGameCanvas = FindFirstObjectByType<UI_InGame>();
        if (inGameCanvas == null) return;

        Transform canvasTransform = inGameCanvas.transform;

        // 2. Recursively find Mobile panel (even if disabled/inactive in the editor)
        if (mobileUIPanel == null)
        {
            mobileUIPanel = FindChildRecursive(canvasTransform, "Mobile_Movement");
            if (mobileUIPanel == null) mobileUIPanel = FindChildRecursive(canvasTransform, "MobileUIPanel");
            if (mobileUIPanel == null) mobileUIPanel = FindChildRecursive(canvasTransform, "TouchControls");
            if (mobileUIPanel == null) mobileUIPanel = FindChildRecursive(canvasTransform, "Mobile_Action");
        }

        // 3. Recursively find Desktop panel (even if disabled/inactive in the editor)
        if (desktopUIPanel == null)
        {
            desktopUIPanel = FindChildRecursive(canvasTransform, "Desktop_Movement");
            if (desktopUIPanel == null) desktopUIPanel = FindChildRecursive(canvasTransform, "DesktopUIPanel");
            if (desktopUIPanel == null) desktopUIPanel = FindChildRecursive(canvasTransform, "KeyboardInstructions");
        }

        // 4. Recursively find prompt panels (either inside desktop wrapper or directly under canvas)
        Transform searchRoot = desktopUIPanel != null ? desktopUIPanel.transform : canvasTransform;

        if (keyboardPromptPanel == null)
        {
            keyboardPromptPanel = FindChildRecursive(searchRoot, "Keyboard_Prompt");
            if (keyboardPromptPanel == null) keyboardPromptPanel = FindChildRecursive(searchRoot, "KeyboardPromptPanel");
            if (keyboardPromptPanel == null) keyboardPromptPanel = FindChildRecursive(searchRoot, "KeyboardPrompts");
            if (keyboardPromptPanel == null) keyboardPromptPanel = FindChildRecursive(searchRoot, "KeyboardInstructions");
        }

        if (gamepadPromptPanel == null)
        {
            gamepadPromptPanel = FindChildRecursive(searchRoot, "Controller_Prompt");
            if (gamepadPromptPanel == null) gamepadPromptPanel = FindChildRecursive(searchRoot, "GamepadPromptPanel");
            if (gamepadPromptPanel == null) gamepadPromptPanel = FindChildRecursive(searchRoot, "GamepadPrompts");
            if (gamepadPromptPanel == null) gamepadPromptPanel = FindChildRecursive(searchRoot, "GamepadControls");
        }
    }

    private GameObject FindChildRecursive(Transform parent, string childName)
    {
        // Recursively inspects both active and inactive children in the hierarchy
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private void InitializeAdapter()
    {
        switch (currentInputMode)
        {
            case InputMode.Keyboard:
                activeAdapter = new KeyboardInputAdapter();
                break;
            case InputMode.Gamepad:
                activeAdapter = new GamepadInputAdapter();
                break;
            case InputMode.MobileTouch:
                activeAdapter = new MobileInputAdapter(cachedPlayer);
                break;
            default:
                activeAdapter = new KeyboardInputAdapter();
                break;
        }
    }

    /// <summary>
    /// Monitors hardware inputs to dynamically swap input types and button prompts in real-time.
    /// </summary>
    private void DetectInputDevice()
    {
        bool isMobile = IsMobilePlatform() || IsPortraitRatio();

        // If layout is hardlocked to Mobile Touch, or auto-detect on a Mobile platform/emulator
        if (layoutMode == UILayoutMode.ForceMobile || (layoutMode == UILayoutMode.Auto && isMobile))
        {
            // Stay in MobileTouch unless a controller button is physically pressed
            if (IsGamepadButtonPressed())
            {
                if (currentInputMode != InputMode.Gamepad)
                {
                    SetInputMode(InputMode.Gamepad);
                }
            }
            else
            {
                if (currentInputMode != InputMode.MobileTouch)
                {
                    SetInputMode(InputMode.MobileTouch);
                }
            }
            return;
        }

        // 1. Detect Controller/Gamepad activity on Desktop
        if (IsGamepadButtonPressed())
        {
            if (currentInputMode != InputMode.Gamepad)
            {
                SetInputMode(InputMode.Gamepad);
            }
            return;
        }

        // 2. Detect Keyboard activity on Desktop
        if (Input.anyKey && !Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            if (IsKeyboardKeyDetected())
            {
                if (currentInputMode != InputMode.Keyboard)
                {
                    SetInputMode(InputMode.Keyboard);
                }
            }
        }
    }

    private bool IsGamepadButtonPressed()
    {
        // Check standard Unity joystick button KeyCodes (JoystickButton0 to JoystickButton19)
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsKeyboardKeyDetected()
    {
        // Ignore gamepad triggers/buttons when checking for keyboard
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Sets the current active input mode and instantiates the correct Adapter.
    /// </summary>
    public void SetInputMode(InputMode newMode)
    {
        if (currentInputMode == newMode && activeAdapter != null) return;

        currentInputMode = newMode;
        InitializeAdapter();
        
        Debug.Log($"<b>[InputManager]</b> Active input adapter swapped to: <color=yellow><b>{currentInputMode}</b></color>");

        // Always update prompt layouts on device changes so prompt visual matching works in any mode
        bool showMobile = false;
        switch (layoutMode)
        {
            case UILayoutMode.Auto:
                showMobile = (currentInputMode == InputMode.MobileTouch) || IsMobilePlatform();
                break;
            case UILayoutMode.ForceMobile:
                showMobile = true;
                break;
            case UILayoutMode.ForceDesktop:
                showMobile = false;
                break;
        }
        UpdateUILayoutVisibility(showMobile);
    }

    /// <summary>
    /// Manually sets the UI Layout preference (Auto, Force Mobile, Force Desktop)
    /// and persists it in PlayerPrefs.
    /// </summary>
    public void SetUILayoutMode(UILayoutMode newMode)
    {
        layoutMode = newMode;
        PlayerPrefs.SetInt("UILayoutMode", (int)layoutMode);
        PlayerPrefs.Save();
        
        Debug.Log($"<b>[InputManager]</b> UI Layout constraint manually set to: <color=cyan><b>{layoutMode}</b></color>");
        
        ApplyLayoutAndInputMode();
    }

    public UILayoutMode GetUILayoutMode() => layoutMode;
    public InputMode GetInputMode() => currentInputMode;

    /// <summary>
    /// Calculates the correct UI and Input systems based on layout preference constraints.
    /// </summary>
    private void ApplyLayoutAndInputMode()
    {
        bool showMobile = false;

        switch (layoutMode)
        {
            case UILayoutMode.Auto:
                showMobile = IsMobilePlatform() || IsPortraitRatio();
                break;
            case UILayoutMode.ForceMobile:
                showMobile = true;
                break;
            case UILayoutMode.ForceDesktop:
                showMobile = false;
                break;
        }

        UpdateUILayoutVisibility(showMobile);

        if (showMobile)
        {
            SetInputMode(InputMode.MobileTouch);
        }
        else
        {
            // Default to Keyboard on Desktop/Editor startup.
            // We do NOT auto-force Gamepad just because a virtual controller/tablet is plugged in.
            // DetectInputDevice() will dynamically swap to Gamepad if you physically press a controller button!
            SetInputMode(InputMode.Keyboard);
        }
    }

    private void UpdateUILayoutVisibility(bool showMobile)
    {
        Debug.Log($"<b>[InputManager]</b> Updating UI Layout: <color=orange><b>{(showMobile ? "Mobile UI" : "Desktop UI")}</b></color> (MobilePanel: {(mobileUIPanel != null ? "Bound" : "Unbound")}, DesktopPanel: {(desktopUIPanel != null ? "Bound" : "Unbound")}, KeyboardPrompts: {(keyboardPromptPanel != null ? "Bound" : "Unbound")}, GamepadPrompts: {(gamepadPromptPanel != null ? "Bound" : "Unbound")})");

        if (mobileUIPanel != null)
        {
            Debug.Log($"<b>[InputManager]</b> Setting MobilePanel (<b>{mobileUIPanel.name}</b>) active status to: <b>{showMobile}</b>");
            mobileUIPanel.SetActive(showMobile);
        }
        
        if (desktopUIPanel != null)
        {
            Debug.Log($"<b>[InputManager]</b> Setting DesktopPanel (<b>{desktopUIPanel.name}</b>) active status to: <b>{!showMobile}</b>");
            desktopUIPanel.SetActive(!showMobile);
        }

        // Toggle Keyboard vs Gamepad button prompt sub-panels dynamically
        if (!showMobile)
        {
            if (keyboardPromptPanel != null)
            {
                bool active = currentInputMode == InputMode.Keyboard;
                Debug.Log($"<b>[InputManager]</b> Setting KeyboardPrompt (<b>{keyboardPromptPanel.name}</b>) active status to: <b>{active}</b>");
                keyboardPromptPanel.SetActive(active);
            }
            if (gamepadPromptPanel != null)
            {
                bool active = currentInputMode == InputMode.Gamepad;
                Debug.Log($"<b>[InputManager]</b> Setting GamepadPrompt (<b>{gamepadPromptPanel.name}</b>) active status to: <b>{active}</b>");
                gamepadPromptPanel.SetActive(active);
            }
        }
        else
        {
            // Deactivate both prompts if mobile touch controls are shown on screen
            if (keyboardPromptPanel != null) 
            {
                Debug.Log($"<b>[InputManager]</b> Mobile active: Hiding KeyboardPrompt (<b>{keyboardPromptPanel.name}</b>)");
                keyboardPromptPanel.SetActive(false);
            }
            if (gamepadPromptPanel != null) 
            {
                Debug.Log($"<b>[InputManager]</b> Mobile active: Hiding GamepadPrompt (<b>{gamepadPromptPanel.name}</b>)");
                gamepadPromptPanel.SetActive(false);
            }
        }
    }

    private bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
        // In the Unity Editor, if the active build target is Android/iOS (Device Simulator), this returns true!
        return true;
#else
        // Using UnityEngine.Device namespace makes these return the simulated values inside the Editor Device Simulator!
        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif
    }

    private bool IsPortraitRatio()
    {
        // Height > Width implies portrait ratio, commonly found in mobile builds/aspect ratios
        return Screen.width < Screen.height;
    }

    public IInputAdapter GetInputAdapter()
    {
        if (activeAdapter == null)
        {
            InitializeAdapter();
        }
        return activeAdapter;
    }

    public Vector2 GetMovementDirection() => GetInputAdapter().GetMovementDirection();
    public bool IsGrabPressed() => GetInputAdapter().IsGrabPressed();
    public bool IsUndoPressed() => GetInputAdapter().IsUndoPressed();
    public bool IsResetPressed() => GetInputAdapter().IsResetPressed();
    public bool IsPausePressed() => GetInputAdapter().IsPausePressed();

    private void LoadPreferences()
    {
        layoutMode = (UILayoutMode)PlayerPrefs.GetInt("UILayoutMode", (int)UILayoutMode.Auto);
        Debug.Log($"<b>[InputManager]</b> Loaded UI Layout preference from PlayerPrefs: <color=yellow><b>{layoutMode}</b></color>");
    }
}
