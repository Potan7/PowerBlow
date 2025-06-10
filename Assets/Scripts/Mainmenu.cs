using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Mainmenu : MonoBehaviour
{

    public Slider[] soundSliders;
    public Slider mouseSensitivitySlider;

    // public GameObject scoreboard;
    public TextMeshProUGUI[] scoreTexts;

    public static string[] sceneNames = new string[3] { "Tutorial", "Stage1", "Stage2" };



    void Start()
    {
        FadeSystem.StartFadeOut(1f);

        for (int i = 0; i < soundSliders.Length; i++)
        {
            // 슬라이더의 값이 변경될 때마다 오디오 믹서의 볼륨을 업데이트
            int index = i;
            soundSliders[i].onValueChanged.AddListener(value =>
            {
                AudioManager.SetAudioVolume((AudioManager.AudioType)index, value);
            });
            soundSliders[i].value = AudioManager.GetAudioVolume((AudioManager.AudioType)i);
        }

        mouseSensitivitySlider.minValue = MenuManager.mousePivotMin;
        mouseSensitivitySlider.maxValue = MenuManager.mousePivotMax;
        mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        mouseSensitivitySlider.value = MenuManager.mouseSensitivity;

        for (int i = 0; i < 3; i++)
        {
            string sceneName = sceneNames[i];
            Debug.Log($"Scene {i + 1}: {sceneName}");
            string scoreKey = "Score_" + sceneName;
            string timerKey = "Timer_" + sceneName;
            
            int scoreIdx = i * 2;
            int timerIdx = i * 2 + 1;

            if (PlayerPrefs.HasKey(scoreKey) && PlayerPrefs.HasKey(timerKey))
            {
                int score = PlayerPrefs.GetInt(scoreKey);
                int timer = PlayerPrefs.GetInt(timerKey);

                scoreTexts[scoreIdx].text = $"{sceneName} - Score: {score}";
                scoreTexts[timerIdx].text = $"Time: {timer / 1000}s {timer % 1000}ms";
            }
            else
            {
                scoreTexts[scoreIdx].text = $"{sceneName} - Score: N/A";
                scoreTexts[timerIdx].text = "Time: N/A";
            }
        }
    }

    public void StartGame(int stageIndex)
    {
        var sceneLoad = SceneManager.LoadSceneAsync(stageIndex);
        sceneLoad.allowSceneActivation = false;
        FadeSystem.StartFadeIn(1f, () =>
        {
            sceneLoad.allowSceneActivation = true;
        });
    }

    public void OnMouseSensitivityChanged(float value)
    {
        MenuManager.mouseSensitivity = Mathf.Clamp(value, MenuManager.mousePivotMin, MenuManager.mousePivotMax);
    }

}
