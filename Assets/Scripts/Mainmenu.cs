using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Mainmenu : MonoBehaviour
{

    public Slider[] soundSliders;
    public Slider mouseSensitivitySlider;

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
