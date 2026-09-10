using System;
using UnityEngine;
using UnityEngine.Android;
using Unity.Notifications.Android;

namespace QueComemos.Notifications
{
    /// <summary>
    /// Gestiona los recordatorios locales de comidas.
    ///
    /// Las notificaciones se programan en el sistema operativo,
    /// por lo que pueden aparecer aunque la aplicación esté cerrada.
    /// </summary>
    public class MealNotificationManager : MonoBehaviour
    {
        public static MealNotificationManager Instance { get; private set; }

        private const string AndroidChannelId = "meal_reminders";

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

            InitializeNotifications();
        }

        private void InitializeNotifications()
        {
            InitializeAndroid();
        }

        private void InitializeAndroid()
        {
            var channel = new AndroidNotificationChannel
            {
                Id = AndroidChannelId,
                Name = "Recordatorios de comidas",
                Importance = Importance.High,
                Description = "Recordatorios de comidas"
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        /// <summary>
        /// Programa un recordatorio.
        /// </summary>
        public void ScheduleMealReminder(
            string notificationId,
            string title,
            string message,
            DateTime dateTime)
        {
            if (string.IsNullOrEmpty(notificationId))
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
                    $"un recordatorio en el pasado: {dateTime}"
                );

                return;
            }

            // Primero cancelamos uno anterior con el mismo identificador.
            CancelMealReminder(notificationId);

            ScheduleAndroid(
                notificationId,
                title,
                message,
                dateTime
            );
        }

        /// <summary>
        /// Cancela un recordatorio previamente programado.
        /// </summary>
        public void CancelMealReminder(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId))
                return;

            // Los IDs de Android son enteros. Recuperamos el ID que
            // guardamos para este recordatorio.
            string key = GetAndroidNotificationKey(notificationId);

            if (PlayerPrefs.HasKey(key))
            {
                int androidId = PlayerPrefs.GetInt(key);

                AndroidNotificationCenter.CancelNotification(
                    androidId
                );

                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();

                Debug.Log(
                    $"[MealNotificationManager] " +
                    $"Notificación Android cancelada: {notificationId}"
                );
            }
        }

        /// <summary>
        /// Cancela todas las notificaciones de comidas programadas
        /// por esta aplicación.
        /// </summary>
        public void CancelAllMealReminders()
        {

            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();

            ClearAndroidNotificationIds();

            Debug.Log(
                "[MealNotificationManager] Todos los recordatorios cancelados."
            );
        }

        private void ScheduleAndroid(
            string notificationId,
            string title,
            string message,
            DateTime dateTime)
        {
            var notification = new AndroidNotification
            {
                Title = string.IsNullOrEmpty(title)
                    ? "??? Recordatorio"
                    : title,

                Text = string.IsNullOrEmpty(message)
                    ? "Tienes una comida programada."
                    : message,

                FireTime = dateTime,

                ShouldAutoCancel = true,

                // Sonido predeterminado del sistema.
                //ShouldVibrate = true
            };

            int androidNotificationId =
                AndroidNotificationCenter.SendNotification(
                    notification,
                    AndroidChannelId
                );

            PlayerPrefs.SetInt(
                GetAndroidNotificationKey(notificationId),
                androidNotificationId
            );

            PlayerPrefs.Save();

            Debug.Log(
                $"[MealNotificationManager] " +
                $"Android programado: " +
                $"ID={notificationId}, " +
                $"AndroidID={androidNotificationId}, " +
                $"Fecha={dateTime:yyyy-MM-dd HH:mm}"
            );
        }

        private string GetAndroidNotificationKey(
            string notificationId)
        {
            return "MealNotification_Android_" + notificationId;
        }

        private void ClearAndroidNotificationIds()
        {
            // PlayerPrefs no permite enumerar claves.
            // Las claves se eliminan cuando cancelamos individualmente.
            //
            // CancelAll se utiliza como limpieza general del sistema.
        }
    }
}