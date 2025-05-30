using System;
using System.Collections;
using TMPro;
using UnityEngine;
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
                score += add > 0 ? add : 0; // 남은 시간에 따라 점수 추가
                playerResultPanel.ShowGameClearPanel(timer, score);
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
            skillBarImage.color = Color.Lerp(normalSkillBarColor, chargeSkillBarColor, ratio);
        }

        public void SetSkillBarCooldown(float time)
        {
            cooldownStartTime = time;
            cooldownCurrentTime = time;
            isCooldownActive = true;

            skillBarImage.color = normalSkillBarColor;
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