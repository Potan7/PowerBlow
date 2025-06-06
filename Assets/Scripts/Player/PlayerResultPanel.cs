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
        public TextMeshProUGUI downButtonText;

        const string ScoreFormat = "Score: {0:D6}"; // 점수 표시 형식 (6자리 숫자)
        const string ScoreWithAdditionalFormat = "Score: {0:D6} (+{1:D6})"; // 추가 점수 표시 형식
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
            // 게임 오버 패널 설정
            titleText.text = "Game Over";
            scoreText.text = string.Format(ScoreFormat, score);
            timeText.text = string.Format(TimeFormat, time / 1000, time % 1000);
            downButtonText.text = "Back To Main Menu";
            isClear = false;

            // 패널 띄우기
            resultPanel.SetActive(true);
            titleText.gameObject.SetActive(true);
            scoreText.gameObject.SetActive(true);
            timeText.gameObject.SetActive(true);
            foreach (var button in buttons)
            {
                button.gameObject.SetActive(true);
            }
        }

        public void ShowGameClearPanel(int time, int score, int additionalScore = 0)
        {
            // 게임 클리어 패널 설정
            titleText.text = "Stage Clear";
            // scoreText.text = string.Format(ScoreFormat, score);
            timeText.text = string.Format(TimeFormat, time / 1000, time % 1000);
            downButtonText.text = "Next Stage";
            isClear = true;

            // 만약 마지막 스테이지라면 버튼 텍스트 변경
            int buildCount = SceneManager.sceneCountInBuildSettings;
            if (SceneManager.GetActiveScene().buildIndex + 1 >= buildCount)
            {
                downButtonText.text = "End\n(Back To Title)";
            }

            // 패널 띄우기
            StartCoroutine(ShowClearPanelCoroutine(1f, score, additionalScore));
        }

        void OnButtonClicked(int index)
        {
            AsyncOperation sceneLoad = null;
            if (index == 0)
            {
                sceneLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                if (isClear && SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings)
                {
                    sceneLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
                }
                else
                {
                    // 마지막 스테이지이거나 사망시 메인 메뉴로 이동 버튼
                    sceneLoad = SceneManager.LoadSceneAsync("Mainmenu");
                }
            }

            sceneLoad.allowSceneActivation = false;
            FadeSystem.StartFadeIn(1f, () =>
            {
                sceneLoad.allowSceneActivation = true;
            });
        }

        IEnumerator ShowClearPanelCoroutine(float waitTime, int score, int additionalScore)
        {
            var wait = new WaitForSeconds(waitTime);

            resultPanel.SetActive(true);
            yield return wait;
            titleText.gameObject.SetActive(true);
            yield return wait;
            scoreText.text = string.Format(ScoreFormat, score);
            scoreText.gameObject.SetActive(true);
            yield return wait;
            timeText.gameObject.SetActive(true);
            
            if (additionalScore > 0)
            {
                scoreText.text = string.Format(ScoreWithAdditionalFormat, score, additionalScore);
                yield return wait;
                yield return wait;

                float addDuration = 1f;
                float elapsedTime = 0f;

                int addedScore = score;
                int initAdditionalScore = additionalScore;

                while (elapsedTime < addDuration)
                {
                    scoreText.text = string.Format(ScoreWithAdditionalFormat, addedScore, additionalScore);
                    int move = Mathf.RoundToInt(Mathf.Lerp(0, initAdditionalScore, elapsedTime / addDuration));

                    addedScore = score + move;
                    additionalScore = initAdditionalScore - move;

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                scoreText.text = string.Format(ScoreFormat, addedScore);
            }
            yield return wait;

            foreach (var button in buttons)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}
