using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

using QueComemos.Data;

namespace QueComemos.Notifications
{
    public class MealNotificationManager : MonoBehaviour
    {
        public static MealNotificationManager Instance { get; private set; }

        private const string AndroidChannelId = "meal_reminders";
        private const string AndroidChannelName = "Recordatorios de comidas";
        private const string AndroidChannelDescription = "Recordatorios de comidas de QueComemos";

        private const string PlayerPrefsPrefix = "MealNotification_Android_";

        // Recordatorios que este dispositivo tiene actualmente programados.
        private readonly HashSet<string> scheduledReminderIds =
            new HashSet<string>();

        private bool firebaseSubscribed = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            InitializeNotifications();
        }

        private void Start()
        {
            StartCoroutine(RequestPermissionAndStart());
        }

        private void OnDestroy()
        {
            if (FirebaseManager.Instance != null && firebaseSubscribed)
            {
                FirebaseManager.Instance.UnsubscribeFromMealPlan(OnMealPlanChanged);
                firebaseSubscribed = false;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // =========================================================
        // INICIALIZACIÓN
        // =========================================================

        private void InitializeNotifications()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            InitializeAndroid();
#else
            Debug.Log("[MealNotificationManager] Notificaciones Android no inicializadas en este dispositivo.");
#endif
        }

        private IEnumerator RequestPermissionAndStart()
        {
#if UNITY_ANDROID && !UNITY_EDITOR

            // Esperamos un poco para que Android/Unity termine de arrancar.
            yield return new WaitForSeconds(1f);

            RequestNotificationPermission();

#endif

            // Después continuamos normalmente con Firebase.
            yield return StartCoroutine(WaitForFirebaseAndSubscribe());
        }

#if UNITY_ANDROID && !UNITY_EDITOR

        private void InitializeAndroid()
        {
            var channel = new AndroidNotificationChannel
            {
                Id = AndroidChannelId,
                Name = AndroidChannelName,
                Importance = Importance.High,
                Description = AndroidChannelDescription
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            Debug.Log("[MealNotificationManager] Canal Android registrado.");
        }

        private void RequestNotificationPermission()
        {
            int sdkInt = GetAndroidSdkInt();

            // Android 13+
            if (sdkInt >= 33)
            {
                const string permission =
                    "android.permission.POST_NOTIFICATIONS";

                if (!Permission.HasUserAuthorizedPermission(permission))
                {
                    Debug.Log(
                        "[MealNotificationManager] Solicitando permiso POST_NOTIFICATIONS."
                    );

                    Permission.RequestUserPermission(permission);
                }
                else
                {
                    Debug.Log(
                        "[MealNotificationManager] Permiso de notificaciones ya concedido."
                    );
                }
            }
        }

        private int GetAndroidSdkInt()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }

#endif

        // =========================================================
        // ESPERAR A FIREBASE
        // =========================================================

        private IEnumerator WaitForFirebaseAndSubscribe()
        {
            Debug.Log("[MealNotificationManager] Esperando a Firebase...");

            while (FirebaseManager.Instance == null ||
                   !FirebaseManager.Instance.IsReady)
            {
                yield return null;
            }

            Debug.Log("[MealNotificationManager] Firebase listo.");

            FirebaseManager.Instance.SubscribeToMealPlan(OnMealPlanChanged);

            firebaseSubscribed = true;

            Debug.Log(
                "[MealNotificationManager] Suscrito a los cambios del MealPlan."
            );
        }

        // =========================================================
        // SINCRONIZACIÓN DEL MEALPLAN
        // =========================================================

        private void OnMealPlanChanged(
            Dictionary<string, MealEntryData> mealPlan)
        {
            if (mealPlan == null)
            {
                Debug.LogWarning(
                    "[MealNotificationManager] MealPlan recibido es NULL."
                );

                return;
            }

            Debug.Log(
                $"[MealNotificationManager] Sincronizando {mealPlan.Count} comidas."
            );

            var currentReminderIds = new HashSet<string>();

            foreach (var pair in mealPlan)
            {
                string mealId = pair.Key;
                MealEntryData meal = pair.Value;

                if (meal == null)
                    continue;

                if (string.IsNullOrWhiteSpace(meal.reminderDate))
                    continue;

                if (string.IsNullOrWhiteSpace(meal.reminderTime))
                    continue;

                string notificationId = BuildNotificationId(
                    FirebaseManager.Instance.CurrentCalendarId,
                    mealId
                );

                if (!TryParseReminderDateTime(
                        meal.reminderDate,
                        meal.reminderTime,
                        out DateTime fireTime))
                {
                    Debug.LogWarning(
                        $"[MealNotificationManager] No puedo interpretar " +
                        $"el recordatorio de '{mealId}': " +
                        $"{meal.reminderDate} {meal.reminderTime}"
                    );

                    continue;
                }

                if (fireTime <= DateTime.Now)
                {
                    Debug.Log(
                        $"[MealNotificationManager] Recordatorio pasado: " +
                        $"{notificationId}"
                    );

                    continue;
                }

                string title = !string.IsNullOrWhiteSpace(meal.reminderTitle)
                    ? meal.reminderTitle
                    : "Recordatorio de comida";

                string message = !string.IsNullOrWhiteSpace(meal.dish)
                    ? meal.dish
                    : "Tienes una comida pendiente.";

                ScheduleMealReminder(
                    notificationId,
                    title,
                    message,
                    fireTime
                );

                currentReminderIds.Add(notificationId);
            }

            CancelRemovedReminders(currentReminderIds);

            scheduledReminderIds.Clear();

            foreach (string id in currentReminderIds)
            {
                scheduledReminderIds.Add(id);
            }

            Debug.Log(
                $"[MealNotificationManager] Sincronización terminada. " +
                $"Recordatorios activos: {scheduledReminderIds.Count}"
            );
        }

        private void CancelRemovedReminders(
            HashSet<string> currentReminderIds)
        {
            var oldIds = new List<string>(scheduledReminderIds);

            foreach (string oldId in oldIds)
            {
                if (!currentReminderIds.Contains(oldId))
                {
                    Debug.Log(
                        $"[MealNotificationManager] Cancelando " +
                        $"recordatorio eliminado: {oldId}"
                    );

                    CancelMealReminder(oldId);
                }
            }
        }

        // =========================================================
        // CREAR ID ESTABLE
        // =========================================================

        private string BuildNotificationId(
            string calendarId,
            string mealId)
        {
            if (string.IsNullOrWhiteSpace(calendarId))
                calendarId = "unknown_calendar";

            if (string.IsNullOrWhiteSpace(mealId))
                mealId = "unknown_meal";

            return $"meal_{calendarId}_{mealId}";
        }

        // =========================================================
        // PROGRAMAR RECORDATORIO
        // =========================================================

        public void ScheduleMealReminder(
            string notificationId,
            string title,
            string message,
            DateTime dateTime)
        {
            if (string.IsNullOrWhiteSpace(notificationId))
            {
                Debug.LogWarning(
                    "[MealNotificationManager] notificationId vacío."
                );

                return;
            }

            if (dateTime <= DateTime.Now)
            {
                Debug.LogWarning(
                    $"[MealNotificationManager] No se puede programar " +
                    $"'{notificationId}' porque la fecha ya ha pasado."
                );

                return;
            }

            CancelMealReminder(notificationId);

#if UNITY_ANDROID && !UNITY_EDITOR

            ScheduleAndroid(
                notificationId,
                title,
                message,
                dateTime
            );

#else

            Debug.Log(
                $"[MealNotificationManager] " +
                $"SIMULACIÓN: '{title}' - '{message}' " +
                $"a las {dateTime}"
            );

#endif
        }

        // =========================================================
        // ANDROID
        // =========================================================

#if UNITY_ANDROID && !UNITY_EDITOR

        private void ScheduleAndroid(
            string notificationId,
            string title,
            string message,
            DateTime dateTime)
        {
            var notification = new AndroidNotification
            {
                Title = title,
                Text = message,
                FireTime = dateTime,
                ShouldAutoCancel = true
            };

            int androidNotificationId =
                AndroidNotificationCenter.SendNotification(
                    notification,
                    AndroidChannelId
                );

            string prefsKey =
                PlayerPrefsPrefix + notificationId;

            PlayerPrefs.SetInt(
                prefsKey,
                androidNotificationId
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"[MealNotificationManager] NOTIFICACIÓN PROGRAMADA\n" +
                $"ID: {notificationId}\n" +
                $"Android ID: {androidNotificationId}\n" +
                $"Título: {title}\n" +
                $"Mensaje: {message}\n" +
                $"Hora: {dateTime:yyyy-MM-dd HH:mm:ss}"
            );
        }

#endif

        // =========================================================
        // CANCELAR UN RECORDATORIO
        // =========================================================

        public void CancelMealReminder(string notificationId)
        {
            if (string.IsNullOrWhiteSpace(notificationId))
                return;

#if UNITY_ANDROID && !UNITY_EDITOR

            string prefsKey =
                PlayerPrefsPrefix + notificationId;

            if (PlayerPrefs.HasKey(prefsKey))
            {
                int androidNotificationId =
                    PlayerPrefs.GetInt(prefsKey);

                AndroidNotificationCenter.CancelNotification(
                    androidNotificationId
                );

                PlayerPrefs.DeleteKey(prefsKey);
                PlayerPrefs.Save();

                Debug.Log(
                    $"[MealNotificationManager] " +
                    $"Notificación cancelada: {notificationId}"
                );
            }

#endif

            scheduledReminderIds.Remove(notificationId);
        }

        // =========================================================
        // CANCELAR TODAS
        // =========================================================

        public void CancelAllMealReminders()
        {
#if UNITY_ANDROID && !UNITY_EDITOR

            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();

#endif

            scheduledReminderIds.Clear();

            Debug.Log(
                "[MealNotificationManager] Todas las notificaciones canceladas."
            );
        }

        // =========================================================
        // PARSEAR FECHA + HORA
        // =========================================================

        private bool TryParseReminderDateTime(
            string date,
            string time,
            out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(date) ||
                string.IsNullOrWhiteSpace(time))
            {
                return false;
            }

            string combined =
                $"{date.Trim()} {time.Trim()}";

            string[] formats =
            {
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd H:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd H:mm:ss",

                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy H:mm",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy H:mm:ss",

                "dd-MM-yyyy HH:mm",
                "dd-MM-yyyy H:mm",
                "dd-MM-yyyy HH:mm:ss",
                "dd-MM-yyyy H:mm:ss",

                "yyyy-MM-ddTHH:mm",
                "yyyy-MM-ddTHH:mm:ss"
            };

            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(
                        combined,
                        format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out result))
                {
                    result = DateTime.SpecifyKind(
                        result,
                        DateTimeKind.Local
                    );

                    return true;
                }
            }

            if (DateTime.TryParse(
                    combined,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out result))
            {
                result = DateTime.SpecifyKind(
                    result,
                    DateTimeKind.Local
                );

                return true;
            }

            if (DateTime.TryParse(
                    combined,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out result))
            {
                result = DateTime.SpecifyKind(
                    result,
                    DateTimeKind.Local
                );

                return true;
            }

            return false;
        }
    }
}