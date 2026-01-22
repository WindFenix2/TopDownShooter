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
    [SerializeField] private GameObject mainMenuUI; // assign MainMenu_UI here
    public GameObject victoryScreenUI;
    public GameObject pauseUI;

    [SerializeField] private GameObject[] UIElements;

    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;

    private bool gameHasStarted;

    private void Awake()
    {
        instance = this;

        inGameUI = GetComponentInChildren<UI_InGame>(true);
        weaponSelection = GetComponentInChildren<UI_WeaponSelection>(true);
        gameOverUI = GetComponentInChildren<UI_GameOver>(true);
        settingsUI = GetComponentInChildren<UI_Settings>(true);

        // Fallback: auto-find MainMenu_UI if not assigned
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

    public void SwitchTo(GameObject uiToSwitchOn)
    {
        foreach (GameObject go in UIElements)
            go.SetActive(false);

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
        if (pauseUI.activeSelf)
        {
            TimeManager.instance.ResumeTime();
            ControlsManager.instance.SwitchToCharacterControls();
            SwitchTo(inGameUI.gameObject);
            Cursor.visible = false;
        }

        StartCoroutine(ChangeImageAlpha(1, 1f, GameManager.instance.RestartScene));
    }


    public void PauseSwitch()
    {
        if (!gameHasStarted)
        {
            if (mainMenuUI != null && mainMenuUI.activeSelf == false)
                SwitchTo(mainMenuUI);

            SetCursorMenu();
            return;
        }

        bool isPausingNow = !pauseUI.activeSelf;

        if (isPausingNow)
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
        SwitchTo(gameOverUI.gameObject);
        gameOverUI.ShowGameOverMessage(message);
        SetCursorMenu();
    }

    public void ShowVictoryScreenUI()
    {
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
        PlayerControls controls = GameManager.instance.player.controls;
        controls.UI.UIPause.performed += ctx => PauseSwitch();
    }

    private IEnumerator StartGameSequence()
    {
        bool quickStart = GameManager.instance.quickStart;

        if (quickStart == false)
        {
            fadeImage.color = Color.black;
            StartCoroutine(ChangeImageAlpha(1, 1, null));
            yield return new WaitForSeconds(1);
        }

        SwitchTo(inGameUI.gameObject);
        GameManager.instance.GameStart();

        gameHasStarted = true;

        ControlsManager.instance.SwitchToCharacterControls();
        TimeManager.instance.ResumeTime();

        SetCursorGameplay();

        if (quickStart)
            StartCoroutine(ChangeImageAlpha(0, .1f, null));
        else
            StartCoroutine(ChangeImageAlpha(0, 1f, null));
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
