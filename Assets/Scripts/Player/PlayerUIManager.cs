using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Player
{
    public class PlayerUIManager : MonoBehaviour
    {
        public Image lowHpWarningImage;
        public Image skillBarImage;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;

        const string scoreFormat = "{0:D6}"; // 점수 표시 형식 (6자리 숫자)

        // 초 : 밀리초 형식으로 타이머 표시
        const string timerFormat = "{0:D2}:{1:D3}";

        Color normalSkillBarColor = Color.white;
        Color chargeSkillBarColor = Color.blue;
        Color fullChargeSkillBarColor = Color.yellow;

        float cooldownStartTime;
        float cooldownCurrentTime;
        public bool isCooldownActive = false;

        public int score = 0;

        public int timer = 0;

        public int enemyDeadScore = 100; // 적 처치 시 점수
        public int maxScoreTime = 300000; // 최대 점수

        [SerializeField]
        PlayerResultPanel playerResultPanel;

        void Start()
        {
            playerResultPanel = GetComponent<PlayerResultPanel>();
        }

        void Update()
        {
            if (isCooldownActive)
            {
                cooldownCurrentTime -= Time.deltaTime;
                skillBarImage.fillAmount = 1 - cooldownCurrentTime / cooldownStartTime;
                if (cooldownCurrentTime <= 0f)
                {
                    isCooldownActive = false;
                    skillBarImage.fillAmount = 1f;
                }
            }

            timer += (int)(Time.deltaTime * 1000); // 밀리초 단위로 타이머 증가
            timerText.text = string.Format(timerFormat, timer / 1000, timer % 1000);
        }

        public void EndGame(bool isClear)
        {
            PlayerController.Instance.gameObject.SetActive(false);

            if (isClear)
            {
                int add = maxScoreTime / (timer / 1000);
                playerResultPanel.ShowGameClearPanel(timer, score, add);

                string sceneName = Mainmenu.sceneNames[SceneManager.GetActiveScene().buildIndex - 1]; // 현재 씬 이름 가져오기
                string scoreKey = "Score_" + sceneName;
                string timerKey = "Timer_" + sceneName;
                if (PlayerPrefs.HasKey(scoreKey))
                {
                    int bestScore = PlayerPrefs.GetInt(scoreKey);
                    if (score > bestScore)
                    {
                        PlayerPrefs.SetInt(scoreKey, score + add);
                    }
                }
                else
                {
                    PlayerPrefs.SetInt(scoreKey, score + add);
                }
                if (PlayerPrefs.HasKey(timerKey))
                {
                    int bestTimer = PlayerPrefs.GetInt(timerKey);
                    if (timer < bestTimer)
                    {
                        PlayerPrefs.SetInt(timerKey, timer);
                    }
                }
                else
                {
                    PlayerPrefs.SetInt(timerKey, timer);
                }
            }
            else
            {
                playerResultPanel.ShowGameOverPanel(timer, score);
            }
            
        }

        public void AddScore(int amount)
        {
            score += amount;
            scoreText.text = string.Format(scoreFormat, score);
        }

        public void AddEnemyDeadScore()
        {
            AddScore(enemyDeadScore);
        }

        public void SetSkillBarCharge(float ratio)
        {

            if (ratio >= 1f)
            {
                skillBarImage.color = fullChargeSkillBarColor; // 완전 충전 상태
            }
            else if (ratio <= 0f)
            {
                skillBarImage.color = normalSkillBarColor; // 초기 상태
            }
            else
            {
                skillBarImage.color = Color.Lerp(normalSkillBarColor, chargeSkillBarColor, ratio);
            }
        }

        public void SetSkillBarCooldown(float time)
        {
            if (PlayerController.Instance.attackOvercharge)
            {
                // 공격 과충전 상태일 때는 쿨타임을 적용하지 않음
                // PlayerController.Instance.attackOvercharge = false;
                skillBarImage.color = fullChargeSkillBarColor;
                return;
            }

            cooldownStartTime = time;
            cooldownCurrentTime = time;
            isCooldownActive = true;

            skillBarImage.color = normalSkillBarColor;
            SetSkillBarCharge(0f); // 초기화
        }

        public void PlayerHpChanged(int hp, int beforeHp)
        {
            var player = PlayerController.Instance;
            if (hp <= player.warningHp && beforeHp > player.warningHp)
            {
                StopAllCoroutines(); // 이전 코루틴 중지
                StartCoroutine(ImageFadeInCoroutine(lowHpWarningImage, 0.5f));
            }
            else if (hp > player.warningHp && beforeHp <= player.warningHp)
            {
                // HP가 경고 이하에서 정상으로 회복되면 이미지 페이드 아웃
                StopAllCoroutines(); // 이전 코루틴 중지
                StartCoroutine(ImageFadeOutCoroutine(lowHpWarningImage, 0.5f));
            }
            else if (hp >= player.warningHp && beforeHp > hp)
            {
                // 체력이 경고 이상에서 감소한 경우
                StopAllCoroutines(); // 이전 코루틴 중지
                StartCoroutine(TemporaryImageFadeInHalfCoroutine(lowHpWarningImage, 0.5f));
            }
        }

        public void OnPauseButtonClicked()
        {
            // PauseMenu 호출하기
            MenuManager.SetMenuActive(true);
        }

        // She is better than you
        // tall taller tallest
        // She is taller than you
        // She is the tallest

        #region AnimCoroutines

        IEnumerator ImageFadeOutCoroutine(Image image, float duration)
        {
            Color startColor = image.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                image.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.color = endColor;
            image.gameObject.SetActive(false);
        }

        IEnumerator ImageFadeInCoroutine(Image image, float duration)
        {
            image.gameObject.SetActive(true);
            Color startColor = image.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                image.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.color = endColor;
        }

        IEnumerator TemporaryImageFadeInHalfCoroutine(Image image, float duration)
        {
            image.gameObject.SetActive(true);
            Color startColor = image.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0.4f);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                image.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.color = endColor;

            yield return new WaitForSeconds(0.5f);
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                image.color = Color.Lerp(endColor, startColor, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            image.color = startColor;
            image.gameObject.SetActive(false); // 이미지 비활성화
        }
        #endregion
    }
}