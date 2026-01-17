using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI instance;

    public UI_InGame inGameUI { get; private set; }
    public UI_WeaponSelection weaponSelection { get; private set; }
    public UI_GameOver gameOverUI { get; private set; }
    public UI_Settings settingsUI { get; private set; }
    public GameObject victoryScreenUI;
    public GameObject pauseUI;


    [SerializeField] private GameObject[] UIElements;

    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;
    private void Awake()
    {
        instance = this;
        inGameUI = GetComponentInChildren<UI_InGame>(true);
        weaponSelection = GetComponentInChildren<UI_WeaponSelection>(true);
        gameOverUI = GetComponentInChildren<UI_GameOver>(true);
        settingsUI = GetComponentInChildren<UI_Settings>(true);
    }
    private void Start()
    {
        AssignInputsUI();

        StartCoroutine(ChangeImageAlpha(0, 1.5f, null));


        // Remove this if statement before build, it's for easier testing
        if (GameManager.instance.quickStart)
        {
            LevelGenerator.instance.InitializeGeneration();
            StartTheGame();
        }
    }
    public void SwitchTo(GameObject uiToSwitchOn)
    {
        foreach (GameObject go in UIElements)
        {
            go.SetActive(false);
        }
         
        uiToSwitchOn.SetActive(true);

        if (uiToSwitchOn == settingsUI.gameObject)
            settingsUI.LoadSettings();
    }

    public void StartTheGame() => StartCoroutine(StartGameSequence());

    public void QuitTheGame() => Application.Quit();
    public void StartLevelGeneration() => LevelGenerator.instance.InitializeGeneration();

    public void RestartTheGame()
    {
        StartCoroutine(ChangeImageAlpha(1, 1f, GameManager.instance.RestartScene));
    }

    public void PauseSwitch()
    {
        bool gamePaused = pauseUI.activeSelf;

        if (gamePaused)
        {
            SwitchTo(inGameUI.gameObject);
            ControlsManager.instance.SwitchToCharacterControls();
            TimeManager.instance.ResumeTime();
        }
        else
        {
            SwitchTo(pauseUI);
            ControlsManager.instance.SwitchToUIControls();
            TimeManager.instance.PauseTime();
        }
    }

    public void ShowGameOverUI(string message = "GAME OVER!")
    {
        SwitchTo(gameOverUI.gameObject);
        gameOverUI.ShowGameOverMessage(message);
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

    }

    private void AssignInputsUI()
    {
        PlayerControls controls = GameManager.instance.player.controls;

        controls.UI.UIPause.performed += ctx => PauseSwitch();
    }

    private IEnumerator StartGameSequence()
    {
        bool quickStart = GameManager.instance.quickStart;

        //THIS SHOULD BE UNCOMMENTED BEFORE MAKING A BUILD
        if (quickStart == false)
        {
            fadeImage.color = Color.black;
            StartCoroutine(ChangeImageAlpha(1, 1, null));
            yield return new WaitForSeconds(1);

        }

        yield return null;
        SwitchTo(inGameUI.gameObject);
        GameManager.instance.GameStart();

        if(quickStart)
            StartCoroutine(ChangeImageAlpha(0,.1f, null));
        else
            StartCoroutine(ChangeImageAlpha(0,1f, null));
    }

    private IEnumerator ChangeImageAlpha(float targetAlpha, float duration,System.Action onComplete)
    {
        float time = 0;
        Color currentColor = fadeImage.color;
        float startAlpha = currentColor.a;

        while(time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            fadeImage.color = new Color(currentColor.r,currentColor.g, currentColor.b,alpha);
            yield return null;
        }

        fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);


        // Call the cimpletion method if it exists
        onComplete?.Invoke();
    }

    [ContextMenu("Assign Audio To Buttons")]
    public void AssignAudioListenesrsToButtons()
    {
        UI_Button[] buttons = FindObjectsOfType<UI_Button>(true);

        foreach (var button in buttons)
        {
            button.AssignAudioSource();
        }
    }
}
