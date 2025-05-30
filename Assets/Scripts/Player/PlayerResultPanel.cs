using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Player
{


    public class PlayerResultPanel : MonoBehaviour
    {
        public GameObject resultPanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI restartButtonText;

        const string ScoreFormat = "Score: {0:D6}"; // 점수 표시 형식 (6자리 숫자)
        const string TimeFormat = "Time: {0:D2}:{1:D3}"; // 초:밀리초 형식으로 타이머 표시

        bool isClear;

        public Button[] buttons;

        private void Start()
        {
            resultPanel.SetActive(false);
            titleText.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(false);
            timeText.gameObject.SetActive(false);

            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i; // Capture the current index
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
                buttons[i].gameObject.SetActive(false); // Initially hide buttons
            }
        }

        public void ShowGameOverPanel(int time, int score)
        {
            titleText.text = "Game Over";
            scoreText.text = string.Format(ScoreFormat, score);
            timeText.text = string.Format(TimeFormat, time / 1000, time % 1000);
            restartButtonText.text = "Restart";
            isClear = false;

            StartCoroutine(ShowAnimCoroutine(1f));
        }

        public void ShowGameClearPanel(int time, int score)
        {
            titleText.text = "Stage Clear";
            scoreText.text = string.Format(ScoreFormat, score);
            timeText.text = string.Format(TimeFormat, time / 1000, time % 1000);
            restartButtonText.text = "Next Stage";
            isClear = true;

            StartCoroutine(ShowAnimCoroutine(1f));
        }

        void OnButtonClicked(int index)
        {
            AsyncOperation sceneLoad = null;
            if (index == 0)
            {
                int currentSceneIndex = SceneManager.GetActiveScene().buildIndex + (isClear ? 1 : 0);
                sceneLoad = SceneManager.LoadSceneAsync(currentSceneIndex);
            }
            else
            {
                sceneLoad = SceneManager.LoadSceneAsync("Mainmenu");
            }

            sceneLoad.allowSceneActivation = false;
            FadeSystem.StartFadeIn(1f, () =>
            {
                sceneLoad.allowSceneActivation = true;
            });
        }

        IEnumerator ShowAnimCoroutine(float waitTime)
        {
            var wait = new WaitForSeconds(waitTime);

            resultPanel.SetActive(true);
            yield return wait;
            titleText.gameObject.SetActive(true);
            yield return wait;
            scoreText.gameObject.SetActive(true);
            yield return wait;
            timeText.gameObject.SetActive(true);
            yield return wait;
            foreach (var button in buttons)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}
