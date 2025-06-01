using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;

    int currentStep = 0;

    readonly WaitForSeconds wait = new WaitForSeconds(0.01f);

    [TextArea(3, 10)]
    public string[] tutorialSteps;

    void Start()
    {
        // 튜토리얼 시작 시 첫 번째 단계 표시
        ShowTutorialStep();
    }

    public void ShowTutorialStep()
    {
        if (currentStep < tutorialSteps.Length)
        {
            StopAllCoroutines(); // 이전 코루틴 중지
            StartCoroutine(ShowTextCoroutine(tutorialSteps[currentStep]));
            currentStep++;
        }
    }

    IEnumerator ShowTextCoroutine(string text)
    {
        tutorialText.text = ""; // 이전 텍스트 초기화
        foreach (char c in text)
        {
            tutorialText.text += c; // 한 글자씩 추가
            yield return wait; // 잠시 대기
        }
    
    }
}
