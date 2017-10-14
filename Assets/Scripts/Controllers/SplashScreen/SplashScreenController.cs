using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts.Helpers;

namespace Assets.Scripts.Controllers.SplashScreen
{
	public class SplashScreenController
		: MonoBehaviour
	{
		[SerializeField]
		private Image splashLogoImage;
		[SerializeField]
		private Image splashUnityImage;
		[SerializeField]
		private string loadlevel;

		private const float DELAYTIME = 1.5f;

		private void Start()
		{
			splashLogoImage.canvasRenderer.SetAlpha(0.0f);
			splashUnityImage.canvasRenderer.SetAlpha(0.0f);

			StartCoroutine(CoroutineHelper.ActionQueue(
				DELAYTIME,
				() => FadeIn(splashLogoImage),
				() => FadeIn(splashUnityImage),
				() => SceneManager.LoadSceneAsync(loadlevel)));
		}

		public void FadeIn(Image img)
		{
			img.CrossFadeAlpha(1.0f, DELAYTIME, false);
		}
	}
}