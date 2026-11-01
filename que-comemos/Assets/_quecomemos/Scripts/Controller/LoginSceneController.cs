using System.Collections;
using QueComemos.Data;
using QueComemos.Localization;
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
        private VisualElement pageRoot;
        private LocBinder locBinder;
        private VisualElement privacyModal;
        private bool loadingMenu;
        private const string ThemePrefKey = "quecomemos_theme_light";

        private void Start()
        {
            var root = uiDocument.rootVisualElement;

            pageRoot = root.Q<VisualElement>("root");
            ApplyStoredTheme();

            // Traduce los textos "@clave" del UXML.
            locBinder = new LocBinder();
            locBinder.Bind(root);

            SetupPrivacyModal(root);

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
        #region Politica de privacidad

        private void SetupPrivacyModal(VisualElement root)
        {
            privacyModal = root.Q<VisualElement>("privacy-modal");

            if (privacyModal == null)
                return;

            var linkButton = root.Q<Button>("privacy-link-btn");
            var acceptButton = root.Q<Button>("privacy-accept-btn");
            var declineButton = root.Q<Button>("privacy-decline-btn");

            if (linkButton != null)
                linkButton.clicked += PrivacyConsent.OpenPolicy;

            if (acceptButton != null)
                acceptButton.clicked += OnPrivacyAccepted;

            if (declineButton != null)
                declineButton.clicked += OnPrivacyDeclined;

            privacyModal.style.display =
                PrivacyConsent.IsAccepted
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
        }

        private void OnPrivacyAccepted()
        {
            PrivacyConsent.Accept();
            privacyModal.style.display = DisplayStyle.None;
        }

        private void OnPrivacyDeclined()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>No se entra a la app hasta que se acepte la política (aunque ya hubiera sesión).</summary>
        private void LoadMenuWhenConsented()
        {
            if (loadingMenu)
                return;

            loadingMenu = true;
            StartCoroutine(LoadMenuWhenConsentedRoutine());
        }

        private IEnumerator LoadMenuWhenConsentedRoutine()
        {
            yield return new WaitUntil(() => PrivacyConsent.IsAccepted);
            SceneHelper.LoadScene(SceneNames.Menu);
        }

        #endregion

        private void ApplyStoredTheme()
        {
            if (pageRoot == null)
                return;

            // Por defecto, tema oscuro (mismo criterio que MainMenuController).
            bool isLightTheme =
                PlayerPrefs.GetInt(ThemePrefKey, 0) == 1;

            pageRoot.RemoveFromClassList(
                isLightTheme
                    ? "theme-dark"
                    : "theme-light"
            );

            pageRoot.AddToClassList(
                isLightTheme
                    ? "theme-light"
                    : "theme-dark"
            );
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
            yield return new WaitForSeconds(3f);
            LoadMenuWhenConsented();
            yield break;
#endif

            googleSignInButton.SetEnabled(true);
            Debug.Log("[LoginSceneController] Botón habilitado. Listo para pulsar.");
        }

        private void OnDisable()
        {
            locBinder?.Dispose();
            locBinder = null;

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
            LoadMenuWhenConsented();
        }

        private void HandleSignedIn(Firebase.Auth.FirebaseUser user)
        {
            Debug.Log($"[LoginSceneController] Login OK: {user.UserId}. Cargando \"{SceneNames.Menu}\"...");
            LoadMenuWhenConsented();
        }

        private void HandleSignInFailed(string message)
        {
            Debug.LogWarning($"[LoginSceneController] Login fallido: {message}");
            if (googleSignInButton != null) googleSignInButton.SetEnabled(true);
            if (guestLoginButton != null) guestLoginButton.SetEnabled(true);
        }
    }
}