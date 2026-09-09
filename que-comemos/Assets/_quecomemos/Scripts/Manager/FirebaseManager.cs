using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

namespace QueComemos.Data
{
    /// <summary>
    /// Habla con Realtime Database que ya usa la web
    /// Arranca apuntando a un UID de PRUEBA fijo (campo "testUserId")
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        [Tooltip("UID de prueba mientras no hay sesión real de Google.")]
        [SerializeField] private string testUserId = "test-user-uid-001";

        /// <summary>True en cuanto Firebase está listo y ya se puede leer/escribir.</summary>
        public bool IsReady { get; private set; }

        /// <summary>ID del calendario actualmente activo (UID de prueba o UID real tras login).</summary>
        public string CurrentCalendarId { get; private set; }

        /// <summary>ID de TU PROPIO calendario (el UID con el que iniciaste sesión), aparte del que estés viendo ahora mismo.</summary>
        public string OwnCalendarId { get; private set; }

        /// <summary>Se dispara cada vez que llegan datos nuevos de Firebase (carga inicial y cambios en vivo). Úsalo indirectamente a través de SubscribeToMealPlan/UnsubscribeFromMealPlan, no directamente con += / -=.</summary>
        private event Action<Dictionary<string, MealEntryData>> OnMealPlanChanged;

        /// <summary>Se dispara si algo falla (conexión, permisos, etc.), con un mensaje ya listo para mostrar al usuario.</summary>
        public event Action<string> OnError;

        private FirebaseDatabase database;
        private DatabaseReference mealsRef;

        // El último dato recibido para el calendario ACTUAL, para poder
        // "reenviarlo" inmediatamente a quien se suscriba después de que ya
        // haya llegado (por ejemplo, un MainMenuController recién cargado
        // tras cambiar de escena, si Firebase respondió antes de que
        // terminara de suscribirse). Sin esto, esa primera entrega se
        // pierde y la pantalla se queda vacía aunque Firebase sí tenga los
        // datos.
        private Dictionary<string, MealEntryData> lastKnownData;
        private bool hasReceivedData;

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

        private async void Start()
        {
            DependencyStatus status;
            try
            {
                status = await FirebaseApp.CheckAndFixDependenciesAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Excepción comprobando dependencias: {e}");
                OnError?.Invoke("No se pudo inicializar Firebase.");
                return;
            }

            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[FirebaseManager] Dependencias de Firebase no disponibles: {status}");
                OnError?.Invoke($"Firebase no disponible en este dispositivo ({status}).");
                return;
            }

            database = FirebaseDatabase.DefaultInstance;
            IsReady = true;

            // Arranca con el calendario de prueba; si GoogleAuthManager ya
            // tenía sesión guardada de antes, sobreescribirá esto enseguida
            // llamando a SetCalendarId con el UID real.
            SetCalendarId(testUserId, isOwnCalendar: true);
        }

        /// <summary>
        /// Cambia el calendario activo: se desuscribe del anterior (si
        /// había) y se suscribe al nuevo.
        /// </summary>
        public void SetCalendarId(string calendarId, bool isOwnCalendar = false)
        {
            if (string.IsNullOrEmpty(calendarId) || !IsReady) return;

            if (isOwnCalendar) OwnCalendarId = calendarId;

            if (calendarId == CurrentCalendarId && mealsRef != null) return;

            if (mealsRef != null)
            {
                mealsRef.ValueChanged -= HandleValueChanged;
            }

            // El dato en caché pertenece al calendario ANTERIOR; hay que
            // olvidarlo para no reenviárselo por error a quien se suscriba
            // mientras esperamos la respuesta del calendario nuevo.
            lastKnownData = null;
            hasReceivedData = false;

            CurrentCalendarId = calendarId;
            mealsRef = database.RootReference.Child("calendars").Child(calendarId).Child("mealplan");
            mealsRef.ValueChanged += HandleValueChanged;

            Debug.Log($"[FirebaseManager] Calendario activo: \"{calendarId}\".");
        }

        /// <summary>
        /// Suscribe un callback a los cambios del calendario activo. Si ya
        /// había llegado un dato antes de suscribirse (por ejemplo, durante
        /// un cambio de escena), se lo entrega inmediatamente en vez de
        /// obligar a esperar al próximo cambio.
        /// </summary>
        public void SubscribeToMealPlan(Action<Dictionary<string, MealEntryData>> callback)
        {
            OnMealPlanChanged += callback;
            if (hasReceivedData)
            {
                callback(lastKnownData);
            }
        }

        public void UnsubscribeFromMealPlan(Action<Dictionary<string, MealEntryData>> callback)
        {
            OnMealPlanChanged -= callback;
        }

        private void HandleValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[FirebaseManager] Error leyendo mealplan: {args.DatabaseError.Message}");
                OnError?.Invoke("No se pudo conectar con la base de datos.");
                return;
            }

            var data = FromFirebaseValue(args.Snapshot.Value);
            lastKnownData = data;
            hasReceivedData = true;
            Debug.Log($"[FirebaseManager] mealplan actualizado: {data.Count} entradas.");
            OnMealPlanChanged?.Invoke(data);
        }

        /// <summary>
        /// Guarda el mapa ENTERO de platos
        /// </summary>
        public async Task SaveMealPlanAsync(Dictionary<string, MealEntryData> data)
        {
            if (!IsReady || mealsRef == null)
            {
                Debug.LogWarning("[FirebaseManager] SaveMealPlanAsync llamado antes de estar listo; ignorado.");
                return;
            }

            try
            {
                await mealsRef.SetValueAsync(ToFirebaseDict(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error guardando mealplan: {e}");
                OnError?.Invoke("No se pudo guardar. Inténtalo de nuevo.");
            }
        }

        /// <summary>
        /// Lee la lista de calendarios que otros te han compartido
        /// </summary>
        public async Task<List<string>> GetSharedCalendarIdsAsync()
        {
            var result = new List<string>();
            if (!IsReady || string.IsNullOrEmpty(OwnCalendarId)) return result;

            try
            {
                var snapshot = await database.RootReference
                    .Child("users").Child(OwnCalendarId).Child("shared").GetValueAsync();

                if (snapshot.Value is IDictionary<string, object> dict)
                {
                    result.AddRange(dict.Keys);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error leyendo calendarios compartidos: {e}");
            }

            return result;
        }

        /// <summary>Nombre a mostrar del dueño de un calendario (calendars/{id}/ownerLabel), o null si no tiene.</summary>
        public async Task<string> GetOwnerLabelAsync(string calendarId)
        {
            try
            {
                var snapshot = await database.RootReference
                    .Child("calendars").Child(calendarId).Child("ownerLabel").GetValueAsync();
                return snapshot.Value as string;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error leyendo ownerLabel de \"{calendarId}\": {e}");
                return null;
            }
        }

        /// <summary>Quita un calendario compartido de tu lista.</summary>
        public async Task LeaveSharedCalendarAsync(string calendarId)
        {
            if (!IsReady || string.IsNullOrEmpty(OwnCalendarId)) return;

            try
            {
                await database.RootReference
                    .Child("users").Child(OwnCalendarId).Child("shared").Child(calendarId).RemoveValueAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Error al quitar calendario compartido \"{calendarId}\": {e}");
                OnError?.Invoke("No se pudo quitar ese calendario.");
            }
        }

        private void OnDestroy()
        {
            if (mealsRef != null)
            {
                mealsRef.ValueChanged -= HandleValueChanged;
            }
        }

        // ==========================================================
        //  Conversión hacia/desde el formato plano de Firebase
        // ==========================================================
        private static Dictionary<string, object> ToFirebaseDict(Dictionary<string, MealEntryData> data)
        {
            var result = new Dictionary<string, object>();
            foreach (var kv in data)
            {
                var entry = kv.Value;
                var entryDict = new Dictionary<string, object>
                {
                    ["dish"] = entry.dish ?? "",
                    ["notifiedLive"] = entry.notifiedLive,
                };
                if (!string.IsNullOrEmpty(entry.reminderDate)) entryDict["reminderDate"] = entry.reminderDate;
                if (!string.IsNullOrEmpty(entry.reminderTime)) entryDict["reminderTime"] = entry.reminderTime;
                if (!string.IsNullOrEmpty(entry.reminderTitle)) entryDict["reminderTitle"] = entry.reminderTitle;
                result[kv.Key] = entryDict;
            }
            return result;
        }

        private static Dictionary<string, MealEntryData> FromFirebaseValue(object value)
        {
            var result = new Dictionary<string, MealEntryData>();
            if (value is not IDictionary<string, object> dict) return result;

            foreach (var kv in dict)
            {
                if (kv.Value is not IDictionary<string, object> entryDict) continue;

                result[kv.Key] = new MealEntryData
                {
                    dish = entryDict.TryGetValue("dish", out var d) ? d as string : null,
                    reminderDate = entryDict.TryGetValue("reminderDate", out var rd) ? rd as string : null,
                    reminderTime = entryDict.TryGetValue("reminderTime", out var rt) ? rt as string : null,
                    reminderTitle = entryDict.TryGetValue("reminderTitle", out var rti) ? rti as string : null,
                    notifiedLive = entryDict.TryGetValue("notifiedLive", out var nl) && nl is bool b && b,
                };
            }
            return result;
        }
    }
}