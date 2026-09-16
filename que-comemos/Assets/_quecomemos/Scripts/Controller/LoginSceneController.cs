using System.Collections;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public class LoginSceneController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private Button googleSignInButton;
        private Button guestLoginButton;
        private Coroutine waitForAuthManagerRoutine;

        private Label appVersionLabel;

        private void Start()
        {
            var root = uiDocument.rootVisualElement;
            appVersionLabel = root.Q<Label>("app-version");
            googleSignInButton = root.Q<Button>("google-login-btn");
            guestLoginButton = root.Q<Button>("guest-login-btn");

            if (googleSignInButton == null)
            {
                Debug.LogError("[LoginSceneController] No se encontró el botón \"google-login-btn\"");
                return;
            }

            if (guestLoginButton == null)
            {
                Debug.LogError("[LoginSceneController] No se encontró el botón \"guest-login-btn\"");
                return;
            }

            googleSignInButton.clicked += HandleSignInButtonClicked;
            guestLoginButton.clicked += HandleGuestLoginClicked;
            googleSignInButton.SetEnabled(true);
            guestLoginButton.SetEnabled(true);
            
            waitForAuthManagerRoutine = StartCoroutine(WaitForAuthManagerAndConnect());

            if (appVersionLabel != null)
            {
                appVersionLabel.text = $"v{Application.version}";
            }
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

#if UNITY_EDITOR
            // En el Editor el login real de Google no funciona.
            Debug.Log("[LoginSceneController] Editor: saltando el login real, " +
                "usando el \"Test User Id\" de FirebaseManager.");
            SceneHelper.LoadScene(SceneNames.Menu);
            yield break;
#endif

            googleSignInButton.SetEnabled(true);
            Debug.Log("[LoginSceneController] Botón habilitado. Listo para pulsar.");
        }

        private void OnDisable()
        {
            if (waitForAuthManagerRoutine != null) StopCoroutine(waitForAuthManagerRoutine);
            if (googleSignInButton != null) googleSignInButton.clicked -= HandleSignInButtonClicked;
            if (guestLoginButton != null) guestLoginButton.clicked -= HandleGuestLoginClicked;

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
            guestLoginButton.SetEnabled(false);
            GoogleAuthManager.Instance.SignIn();
        }

        private void HandleGuestLoginClicked()
        {
            Debug.Log("[LoginSceneController] Click recibido en 'guest-login-btn'. Iniciando sesión como invitado.");

            if (FirebaseManager.Instance == null)
            {
                Debug.LogError("[LoginSceneController] FirebaseManager.Instance es null al hacer guest login.");
                return;
            }

            googleSignInButton.SetEnabled(false);
            guestLoginButton.SetEnabled(false);

            // Crear un UID temporal único para el guest
            string guestId = "guest-" + System.Guid.NewGuid().ToString().Substring(0, 8);
            
            Debug.Log($"[LoginSceneController] Usuario invitado creado: {guestId}");
            
            // Establecer el calendario guest en FirebaseManager (sin persistencia)
            FirebaseManager.Instance.SetCalendarId(guestId, isOwnCalendar: true);
            
            // Cargar la escena Menu
            Debug.Log("[LoginSceneController] Cargando Menu...");
            SceneHelper.LoadScene(SceneNames.Menu);
        }

        private void HandleSignedIn(Firebase.Auth.FirebaseUser user)
        {
            Debug.Log($"[LoginSceneController] Login OK: {user.UserId}. Cargando \"{SceneNames.Menu}\"...");
            SceneHelper.LoadScene(SceneNames.Menu);
        }

        private void HandleSignInFailed(string message)
        {
            Debug.LogWarning($"[LoginSceneController] Login fallido: {message}");
            if (googleSignInButton != null) googleSignInButton.SetEnabled(true);
            if (guestLoginButton != null) guestLoginButton.SetEnabled(true);
        }
    }
}