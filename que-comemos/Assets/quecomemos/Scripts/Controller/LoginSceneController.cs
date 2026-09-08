using System.Collections;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace QueComemos.UI
{
    /// <summary>
    /// Escena de Login (UI Canvas normal, no UI Toolkit). Conecta el botón
    /// de Google con GoogleAuthManager y pasa a la escena del menú
    /// principal en cuanto el login termina bien.
    /// </summary>
    public class LoginSceneController : MonoBehaviour
    {
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private TMP_Text statusText;

        [Tooltip("Nombre exacto de la escena del menú principal (debe estar añadida en Build Settings).")]
        [SerializeField] private string mainMenuSceneName = "Menu";

        private Coroutine waitForAuthManagerRoutine;

        private void OnEnable()
        {
            googleSignInButton.onClick.AddListener(HandleSignInButtonClicked);
            googleSignInButton.interactable = false;
            SetStatus("Cargando...");

            waitForAuthManagerRoutine = StartCoroutine(WaitForAuthManagerAndConnect());
        }

        /// <summary>
        /// GoogleAuthManager puede vivir en otro GameObject cuyo Awake()
        /// todavía no haya corrido cuando esta escena arranca (Unity no
        /// garantiza el orden entre objetos distintos), así que esperamos
        /// a que exista antes de suscribirnos — igual que hicimos con
        /// FirebaseManager en MainMenuController.
        /// </summary>
        private IEnumerator WaitForAuthManagerAndConnect()
        {
            float timeout = 5f;
            float elapsed = 0f;
            while ((GoogleAuthManager.Instance == null || !GoogleAuthManager.Instance.IsReady) && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (GoogleAuthManager.Instance == null || !GoogleAuthManager.Instance.IsReady)
            {
                Debug.LogError("[LoginSceneController] No se encontró GoogleAuthManager tras esperar " +
                    $"{timeout}s.");
                SetStatus("Error de configuración: falta GoogleAuthManager.");
                yield break;
            }

            GoogleAuthManager.Instance.OnSignedIn += HandleSignedIn;
            GoogleAuthManager.Instance.OnSignInFailed += HandleSignInFailed;

            if (GoogleAuthManager.Instance.IsSignedIn)
            {
                HandleSignedIn(GoogleAuthManager.Instance.CurrentUser);
                yield break;
            }

            googleSignInButton.interactable = true;
            SetStatus("");
        }

        private void OnDisable()
        {
            if (waitForAuthManagerRoutine != null) StopCoroutine(waitForAuthManagerRoutine);
            googleSignInButton.onClick.RemoveListener(HandleSignInButtonClicked);

            if (GoogleAuthManager.Instance != null)
            {
                GoogleAuthManager.Instance.OnSignedIn -= HandleSignedIn;
                GoogleAuthManager.Instance.OnSignInFailed -= HandleSignInFailed;
            }
        }

        private void HandleSignInButtonClicked()
        {
            googleSignInButton.interactable = false;
            SetStatus("Conectando con Google...");
            GoogleAuthManager.Instance.SignIn();
        }

        private void HandleSignedIn(Firebase.Auth.FirebaseUser user)
        {
            Debug.Log($"[LoginSceneController] Login OK: {user.UserId}. Cargando \"{mainMenuSceneName}\"...");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HandleSignInFailed(string message)
        {
            googleSignInButton.interactable = true;
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}