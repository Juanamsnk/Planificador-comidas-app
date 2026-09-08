using System.Collections;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public class LoginSceneController : MonoBehaviour
    {
        [Tooltip("Arrastra aquí el GameObject '---UIDOCUMENT---' (el que tiene el componente UIDocument real, con Authentication como Source Asset). NO uses GetComponent/RequireComponent porque este script no vive en ese mismo GameObject.")]
        [SerializeField] private UIDocument uiDocument;

        [Tooltip("Nombre exacto de la escena del menú principal (debe estar añadida en Build Settings).")]
        [SerializeField] private string mainMenuSceneName = "Menu";

        private Button googleSignInButton;
        private Coroutine waitForAuthManagerRoutine;

        private void Start()
        {
            var root = uiDocument.rootVisualElement;
            googleSignInButton = root.Q<Button>("google-login-btn");

            googleSignInButton.clicked += HandleSignInButtonClicked;
            googleSignInButton.SetEnabled(false);
            waitForAuthManagerRoutine = StartCoroutine(WaitForAuthManagerAndConnect());
        }

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
                    $"{timeout}s. El botón se queda deshabilitado.");
                yield break;
            }

            Debug.Log("[LoginSceneController] GoogleAuthManager listo. Suscribiendo OnSignedIn/OnSignInFailed.");

            GoogleAuthManager.Instance.OnSignedIn += HandleSignedIn;
            GoogleAuthManager.Instance.OnSignInFailed += HandleSignInFailed;

            if (GoogleAuthManager.Instance.IsSignedIn)
            {
                Debug.Log("[LoginSceneController] Ya había sesión iniciada. Saltando directo a HandleSignedIn.");
                HandleSignedIn(GoogleAuthManager.Instance.CurrentUser);
                yield break;
            }

            googleSignInButton.SetEnabled(true);
            Debug.Log("[LoginSceneController] Botón habilitado. Listo para pulsar.");
        }

        private void OnDisable()
        {
            if (waitForAuthManagerRoutine != null) StopCoroutine(waitForAuthManagerRoutine);
            if (googleSignInButton != null) googleSignInButton.clicked -= HandleSignInButtonClicked;

            if (GoogleAuthManager.Instance != null)
            {
                GoogleAuthManager.Instance.OnSignedIn -= HandleSignedIn;
                GoogleAuthManager.Instance.OnSignInFailed -= HandleSignInFailed;
            }
        }

        private void HandleSignInButtonClicked()
        {
            Debug.Log("[LoginSceneController] Click recibido en 'google-login-btn'. Llamando a GoogleAuthManager.SignIn().");

            if (GoogleAuthManager.Instance == null)
            {
                Debug.LogError("[LoginSceneController] GoogleAuthManager.Instance es null al hacer click.");
                return;
            }

            googleSignInButton.SetEnabled(false);
            GoogleAuthManager.Instance.SignIn();
        }

        private void HandleSignedIn(Firebase.Auth.FirebaseUser user)
        {
            Debug.Log($"[LoginSceneController] Login OK: {user.UserId}. Cargando \"{mainMenuSceneName}\"...");
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HandleSignInFailed(string message)
        {
            Debug.LogWarning($"[LoginSceneController] Login fallido: {message}");
            googleSignInButton.SetEnabled(true);
        }
    }
}