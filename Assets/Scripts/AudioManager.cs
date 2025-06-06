using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioMixer audioMixer; // 오디오 믹서

    public enum AudioType
    {
        Master,
        SFX,
        BGM
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성 방지
        }
    }

    public static void SetAudioVolume(AudioType type, float volume)
    {
        // Debug.Log($"Setting volume for {type}: {volume}");
        Instance.audioMixer.SetFloat(type.ToString(), Mathf.Log10(volume) * 20); // 볼륨을 dB로 변환
    }

    public static float GetAudioVolume(AudioType type)
    {
        Instance.audioMixer.GetFloat(type.ToString(), out float volume);
        return Mathf.Pow(10, volume / 20); // dB를 일반 볼륨으로 변환
    }
}
