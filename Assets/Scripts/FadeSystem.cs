using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeSystem : MonoBehaviour
{
    public static FadeSystem Instance { get; private set; }
    public Image fadeImage;

    void Awake()
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

    public static void StartFadeIn(float duration, Action onComplete = null)
    {
        Instance.StartCoroutine(Instance.FadeIn(duration, onComplete));
    }

    public static void StartFadeOut(float duration, Action onComplete = null)
    {
        Instance.StartCoroutine(Instance.FadeOut(duration, onComplete));
    }

    IEnumerator FadeOut(float duration, Action onComplete = null)
    {
        fadeImage.gameObject.SetActive(true); // 페이드 이미지 활성화
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        color.a = 1f; // 시작은 완전히 검은색
        fadeImage.color = color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f; // 완전히 투명하게 설정
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false); // 페이드 이미지 비활성화
        onComplete?.Invoke(); // 완료 콜백 호출
    }

    IEnumerator FadeIn(float duration, Action onComplete = null)
    {
        fadeImage.gameObject.SetActive(true); // 페이드 이미지 활성화
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        color.a = 0f; // 시작은 완전히 투명
        fadeImage.color = color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f; // 완전히 검은색으로 설정
        fadeImage.color = color;
        onComplete?.Invoke(); // 완료 콜백 호출
    }
}
