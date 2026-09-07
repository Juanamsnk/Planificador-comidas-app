using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

namespace QueComemos.Data
{
    /// <summary>
    /// Habla con el MISMO Realtime Database que ya usa la web
    /// (calendars/{calendarId}/mealplan), con la misma estructura de clave
    /// plana "YYYY-MM-DD|comida" / "YYYY-MM-DD|cena".
    ///
    /// De momento usa un UID de PRUEBA fijo (campo "testUserId" en el
    /// Inspector) en vez de login real de Google — eso es el siguiente
    /// paso, pendiente de montar el plugin nativo de Google Sign-In.
    /// Cuando lo tengamos, "CurrentCalendarId" pasará a ser el UID real
    /// de Firebase Auth y todo lo demás de esta clase no debería cambiar.
    ///
    /// IMPORTANTE: esto asume que ya tienes el Firebase Unity SDK
    /// (FirebaseApp + FirebaseDatabase) importado y el google-services.json
    /// puesto en Assets/, como en los pasos que ya hicimos.
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        [Tooltip("UID de prueba mientras no tenemos login real de Google. " +
            "Usa cualquier texto fijo para probar; todos los que usen el " +
            "mismo valor comparten el mismo calendario de pruebas.")]
        [SerializeField] private string testUserId = "test-user-uid-001";

        /// <summary>True en cuanto Firebase está listo y ya se puede leer/escribir.</summary>
        public bool IsReady { get; private set; }

        /// <summary>ID del calendario actual (de momento, el UID de prueba).</summary>
        public string CurrentCalendarId => testUserId;

        /// <summary>Se dispara cada vez que llegan datos nuevos de Firebase (carga inicial y cambios en vivo).</summary>
        public event Action<Dictionary<string, MealEntryData>> OnMealPlanChanged;

        /// <summary>Se dispara si algo falla (conexión, permisos, etc.), con un mensaje ya listo para mostrar al usuario.</summary>
        public event Action<string> OnError;

        private DatabaseReference mealsRef;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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

            var database = FirebaseDatabase.DefaultInstance;
            mealsRef = database.RootReference.Child("calendars").Child(testUserId).Child("mealplan");

            IsReady = true;
            Debug.Log($"[FirebaseManager] Listo. Calendario de prueba: \"{testUserId}\".");

            mealsRef.ValueChanged += HandleValueChanged;
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
            Debug.Log($"[FirebaseManager] mealplan actualizado: {data.Count} entradas.");
            OnMealPlanChanged?.Invoke(data);
        }

        /// <summary>
        /// Guarda el mapa ENTERO de platos, igual que saveData() en la web
        /// (que hace mealsRef.set(data) con todo el objeto cada vez, no
        /// escrituras parciales por celda).
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

        private void OnDestroy()
        {
            if (mealsRef != null)
            {
                mealsRef.ValueChanged -= HandleValueChanged;
            }
        }

        // ==========================================================
        //  Conversión hacia/desde el formato plano de Firebase
        //  (Dictionary<string,object> anidado, que es lo que entiende
        //  el SDK de Realtime Database — no serializa clases C# solas).
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
