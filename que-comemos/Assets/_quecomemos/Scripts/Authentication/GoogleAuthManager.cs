using Firebase.Auth;
using Firebase.Extensions;
using System;
using System.Collections;
using UnityEngine;

namespace QueComemos.Data
{
    /// <summary>
    /// Login con Google usando el flujo OAuth "federado" que trae el propio
    /// Firebase Auth Unity SDK (SignInWithProviderAsync)
    /// </summary>
    public class GoogleAuthManager : MonoBehaviour
    {
        public static GoogleAuthManager Instance { get; private set; }

        /// <summary>True en cuanto Firebase Auth está listo para usarse.</summary>
        public bool IsReady { get; private set; }

        /// <summary>True si hay una sesión de Firebase Auth activa ahora mismo.</summary>
        public bool IsSignedIn => auth != null && auth.CurrentUser != null;

        /// <summary>El usuario de Firebase actualmente logueado (o null si no hay sesión).</summary>
        public FirebaseUser CurrentUser => auth?.CurrentUser;

        /// <summary>Se dispara al terminar de iniciar sesión correctamente (incluida una sesión ya guardada de antes).</summary>
        public event Action<FirebaseUser> OnSignedIn;

        /// <summary>Se dispara si el login falla o el usuario lo cancela, con un mensaje listo para mostrar.</summary>
        public event Action<string> OnSignInFailed;

        /// <summary>Se dispara al cerrar sesión.</summary>
        public event Action OnSignedOut;

        private FirebaseAuth auth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenFirebaseReady());
        }

        /// <summary>
        /// FirebaseAuth.DefaultInstance necesita que FirebaseApp ya haya
        /// terminado de comprobar/arreglar sus dependencias (lo que hace
        /// FirebaseManager de forma asíncrona)
        /// </summary>
        private IEnumerator InitializeWhenFirebaseReady()
        {
            float timeout = 10f;
            float elapsed = 0f;
            while ((FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady) && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady)
            {
                Debug.LogError($"[GoogleAuthManager] Firebase no se inicializó en {timeout}s; " +
                    "el login no estará disponible.");
                yield break;
            }

            auth = FirebaseAuth.DefaultInstance;
            IsReady = true;
            Debug.Log("[GoogleAuthManager] Listo.");

            // Firebase recuerda la sesión entre ejecuciones; si ya había
            // una, avisamos igual que si el usuario acabara de loguearse.
            if (auth.CurrentUser != null)
            {
                Debug.Log($"[GoogleAuthManager] Sesión ya activa: {auth.CurrentUser.UserId}");
                NotifySignedIn(auth.CurrentUser);
            }
        }

        /// <summary>Abre el flujo de login de Google. Llamar desde el botón "Iniciar sesión".</summary>
        public void SignIn()
        {
            if (!IsReady || auth == null)
            {
                Debug.LogWarning("[GoogleAuthManager] SignIn() llamado antes de que Firebase Auth esté listo.");
                OnSignInFailed?.Invoke("Todavía no está listo, prueba en un momento.");
                return;
            }

            var providerData = new FederatedOAuthProviderData
            {
                ProviderId = GoogleAuthProvider.ProviderId,
            };
            var provider = new FederatedOAuthProvider();
            provider.SetProviderData(providerData);

            Debug.Log("[GoogleAuthManager] Abriendo login de Google...");

            auth.SignInWithProviderAsync(provider).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("[GoogleAuthManager] Login cancelado por el usuario.");
                    OnSignInFailed?.Invoke("Inicio de sesión cancelado.");
                    return;
                }

                if (task.IsFaulted)
                {
                    string message = task.Exception?.GetBaseException()?.Message ?? "No se pudo iniciar sesión.";
                    Debug.LogError($"[GoogleAuthManager] Falló el login: {message}");
                    OnSignInFailed?.Invoke(message);
                    return;
                }

                var user = task.Result.User;
                Debug.Log($"[GoogleAuthManager] Sesión iniciada: {user.UserId} ({user.Email})");
                NotifySignedIn(user);
            });
        }

        /// <summary>Cierra la sesión actual. Llamar desde "Cerrar sesión" del menú.</summary>
        public void SignOut()
        {
            auth?.SignOut();
            OnSignedOut?.Invoke();
            Debug.Log("[GoogleAuthManager] Sesión cerrada.");
        }

        private void NotifySignedIn(FirebaseUser user)
        {
            FirebaseManager.Instance?.SetCalendarId(
                user.UserId,
                isOwnCalendar: true
            );

            OnSignedIn?.Invoke(user);
        }
    }
}