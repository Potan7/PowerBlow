using System;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public GameObject mainMenu;

    public Button[] menuButtons;
    public Slider[] soundSliders;

    public bool IsMenuActive => mainMenu.activeSelf;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        mainMenu.SetActive(false);

        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i; // Capture the current index
            // Debug.Log($"Menu button {i} initialized.");
            menuButtons[i].onClick.AddListener(() => OnMenuButtonClicked(index));
        }

        for (int i = 0; i < soundSliders.Length; i++)
        {
            int index = i; // Capture the current index
            soundSliders[i].onValueChanged.AddListener(value => 
            {
                AudioManager.SetAudioVolume((AudioManager.AudioType)index, value);
            });
            soundSliders[i].value = AudioManager.GetAudioVolume((AudioManager.AudioType)i);
        }
    }

    private void OnMenuButtonClicked(int index)
    {
        Debug.Log($"Menu button clicked: {index}");
        SetMenuActive(false);
        if (index == 0)
        {
            return;
        }

        string moveSceneName = index switch
        {
            1 => SceneManager.GetActiveScene().name, // Restart
            2 => "MainMenu", // Back to Title
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(moveSceneName)) return;

        var sceneLoad = SceneManager.LoadSceneAsync(moveSceneName);
        sceneLoad.allowSceneActivation = false;

        FadeSystem.StartFadeIn(1f, () =>
        {
            sceneLoad.allowSceneActivation = true;
        });
    }

    public static void SetMenuActive(bool isActive)
    {
        if (isActive)
        {
            Instance.mainMenu.SetActive(true);
            Time.timeScale = 0f;
            for (int i = 0; i < Instance.soundSliders.Length; i++)
            {
                Instance.soundSliders[i].value = AudioManager.GetAudioVolume((AudioManager.AudioType)i);
            }

            PlayerController.Instance?.gameObject.SetActive(false);
        }
        else
        {
            Instance.mainMenu.SetActive(false);
            Time.timeScale = 1f;

            PlayerController.Instance?.gameObject.SetActive(true);
        }
    }


}
