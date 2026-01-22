using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI instance;

    public UI_InGame inGameUI { get; private set; }
    public UI_WeaponSelection weaponSelection { get; private set; }
    public UI_GameOver gameOverUI { get; private set; }
    public UI_Settings settingsUI { get; private set; }

    [Header("UI Screens")]
    [SerializeField] private GameObject mainMenuUI;
    public GameObject victoryScreenUI;
    public GameObject pauseUI;

    [SerializeField] private GameObject[] UIElements;

    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;

    private bool gameHasStarted;
    private bool isPaused;

    private PlayerControls controls;

    private void Awake()
    {
        instance = this;

        inGameUI = GetComponentInChildren<UI_InGame>(true);
        weaponSelection = GetComponentInChildren<UI_WeaponSelection>(true);
        gameOverUI = GetComponentInChildren<UI_GameOver>(true);
        settingsUI = GetComponentInChildren<UI_Settings>(true);

        if (mainMenuUI == null)
        {
            Transform t = transform.Find("MainMenu_UI");
            if (t != null) mainMenuUI = t.gameObject;
        }
    }

    private void Start()
    {
        AssignInputsUI();

        if (mainMenuUI != null)
            SwitchTo(mainMenuUI);

        SetCursorMenu();

        StartCoroutine(ChangeImageAlpha(0, 1.5f, null));

        // QuickStart (testing)
        if (GameManager.instance != null && GameManager.instance.quickStart)
        {
            if (LevelGenerator.instance != null)
                LevelGenerator.instance.InitializeGeneration();

            StartTheGame();
        }
    }

    private void OnDestroy()
    {
        if (controls != null)
            controls.UI.UIPause.performed -= OnPausePerformed;
    }

    public void SwitchTo(GameObject uiToSwitchOn)
    {
        foreach (GameObject go in UIElements)
            go.SetActive(false);

        if (uiToSwitchOn != null)
            uiToSwitchOn.SetActive(true);

        if (uiToSwitchOn == settingsUI.gameObject)
            settingsUI.LoadSettings();
    }

    public void StartTheGame() => StartCoroutine(StartGameSequence());
    public void QuitTheGame() => Application.Quit();

    public void StartLevelGeneration()
    {
        if (LevelGenerator.instance != null)
            LevelGenerator.instance.InitializeGeneration();
    }

    public void RestartTheGame()
    {
        if (pauseUI != null && pauseUI.activeSelf)
        {
            isPaused = false;
            TimeManager.instance.ResumeTime();
            ControlsManager.instance.SwitchToCharacterControls();
            SwitchTo(inGameUI.gameObject);
            SetCursorGameplay();
        }

        StartCoroutine(ChangeImageAlpha(1, 1f, GameManager.instance.RestartScene));
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        gameHasStarted = false;

        TimeManager.instance.ResumeTime();
        ControlsManager.instance.SwitchToUIControls();

        if (mainMenuUI != null)
            SwitchTo(mainMenuUI);

        SetCursorMenu();
    }

    public void PauseSwitch()
    {
        if (!gameHasStarted)
        {
            if (mainMenuUI != null && !mainMenuUI.activeSelf)
                SwitchTo(mainMenuUI);

            SetCursorMenu();
            return;
        }

        isPaused = !isPaused;

        if (isPaused)
        {
            SwitchTo(pauseUI);
            ControlsManager.instance.SwitchToUIControls();
            TimeManager.instance.PauseTime();
            SetCursorMenu();
        }
        else
        {
            SwitchTo(inGameUI.gameObject);
            ControlsManager.instance.SwitchToCharacterControls();
            TimeManager.instance.ResumeTime();
            SetCursorGameplay();
        }
    }

    public void ShowGameOverUI(string message = "GAME OVER!")
    {
        gameHasStarted = false;
        isPaused = false;

        SwitchTo(gameOverUI.gameObject);
        gameOverUI.ShowGameOverMessage(message);
        SetCursorMenu();
    }

    public void ShowVictoryScreenUI()
    {
        gameHasStarted = false;
        isPaused = false;

        StartCoroutine(ChangeImageAlpha(1, 1.5f, SwitchToVictoryScreenUI));
    }

    private void SwitchToVictoryScreenUI()
    {
        SwitchTo(victoryScreenUI);

        Color color = fadeImage.color;
        color.a = 0;
        fadeImage.color = color;

        SetCursorMenu();
    }

    private void AssignInputsUI()
    {
        if (GameManager.instance == null || GameManager.instance.player == null)
            return;

        controls = GameManager.instance.player.controls;
        controls.UI.UIPause.performed += OnPausePerformed;
    }

    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        PauseSwitch();
    }

    private IEnumerator StartGameSequence()
    {
        bool quickStart = GameManager.instance != null && GameManager.instance.quickStart;

        if (!quickStart)
        {
            fadeImage.color = Color.black;
            StartCoroutine(ChangeImageAlpha(1, 1f, null));
            yield return new WaitForSeconds(1f);
        }

        SwitchTo(inGameUI.gameObject);

        GameManager.instance.GameStart();

        gameHasStarted = true;
        isPaused = false;

        ControlsManager.instance.SwitchToCharacterControls();
        TimeManager.instance.ResumeTime();
        SetCursorGameplay();

        if (quickStart)
            StartCoroutine(QuickStartWeaponFix());

        if (quickStart)
            StartCoroutine(ChangeImageAlpha(0, .1f, null));
        else
            StartCoroutine(ChangeImageAlpha(0, 1f, null));
    }

    private IEnumerator QuickStartWeaponFix()
    {
        yield return null;

        if (GameManager.instance == null || GameManager.instance.player == null)
            yield break;

        var player = GameManager.instance.player;

        if (player.weaponVisuals != null)
            player.weaponVisuals.PlayWeaponEquipAnimation();

        yield return new WaitForSeconds(0.05f);

        if (player.weapon != null)
            player.weapon.SetWeaponReady(true);
    }

    private void SetCursorMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SetCursorGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private IEnumerator ChangeImageAlpha(float targetAlpha, float duration, System.Action onComplete)
    {
        float time = 0;
        Color currentColor = fadeImage.color;
        float startAlpha = currentColor.a;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
        onComplete?.Invoke();
    }

    [ContextMenu("Assign Audio To Buttons")]
    public void AssignAudioListenesrsToButtons()
    {
        UI_Button[] buttons = FindObjectsOfType<UI_Button>(true);
        foreach (var button in buttons)
            button.AssignAudioSource();
    }
}
