using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class PlayerUIManager : MonoBehaviour
    {
        public Image lowHpWarningImage;

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