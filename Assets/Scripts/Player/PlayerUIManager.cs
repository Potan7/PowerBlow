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

        Color nomalSkillBarColor = Color.white;
        Color chargeSkillBarColor = Color.blue;

        float cooldownStartTime;
        float cooldownCurrentTime;
        public bool isCooldownActive = false;

        public int score = 0;

        public int timer = 0;

        public int enemyDeadScore = 100; // 적 처치 시 점수

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

        public void AddScore(int amount)
        {
            score += amount;
            scoreText.text = string.Format(scoreFormat, score);
        }

        public void AddEnemyDeadScore()
        {
            AddScore(enemyDeadScore);
        }

        public void SetLowHpWarningVisibility(bool isVisible)
        {
            if (isVisible)
            {
                StartCoroutine(ImageFadeInCoroutine(lowHpWarningImage, 0.5f));
            }
            else
            {
                StartCoroutine(ImageFadeOutCoroutine(lowHpWarningImage, 0.5f));
            }
        }

        public void SetSkillBarCharge(float ratio)
        {
            skillBarImage.color = Color.Lerp(nomalSkillBarColor, chargeSkillBarColor, ratio);
        }

        public void SetSkillBarCooldown(float time)
        {
            cooldownStartTime = time;
            cooldownCurrentTime = time;
            isCooldownActive = true;

            skillBarImage.color = nomalSkillBarColor;
        }

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
    }
}