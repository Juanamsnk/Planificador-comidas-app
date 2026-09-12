using System;
using System.Collections.Generic;
using System.Linq;
using QueComemos.Data;
using QueComemos.Notifications;
using UnityEngine;

namespace QueComemos.UI
{
    public partial class MainMenuController
    {
        #region Guardar

        private void OnSaveClicked()
        {
            if (!selected.HasValue)
                return;

            var selection =
                selected.Value;

            string dateStr =
                selection.date;

            string type =
                selection.type;

            string dish =
                dishField.value?.Trim();

            if (string.IsNullOrEmpty(dish))
            {
                ShowToast(
                    "Escribe el nombre del plato"
                );

                return;
            }

            bool reminderEnabled =
                reminderToggle.value;

            var entry =
                new MealEntryData
                {
                    dish = dish,

                    reminderDate =
                        reminderEnabled
                            ? reminderDateField.value
                            : null,

                    reminderTime =
                        reminderEnabled
                            ? reminderTimeField.value
                            : null,

                    reminderTitle =
                        reminderEnabled
                            ? reminderTitleField.value?.Trim()
                            : null
                };

            string notificationId =
                BuildNotificationId(
                    dateStr,
                    type
                );

            MealNotificationManager.Instance?
                .CancelMealReminder(
                    notificationId
                );

            data[
                Key(dateStr, type)
            ] = entry;

            PersistData();

            try
            {
                if (reminderEnabled)
                {
                    ScheduleReminder(
                        notificationId,
                        entry
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[MainMenuController] Error en ScheduleReminder: " +
                    $"{ex.Message}\n{ex.StackTrace}"
                );

                ShowToast(
                    "Error al programar el recordatorio"
                );
            }

            ShowToast(
                reminderEnabled
                    ? "Guardado y recordatorio programado"
                    : "Guardado"
            );

            selected = null;

            Render();

            RestoreLastVisibleDay();
        }

        private static string BuildNotificationId(
            string dateStr,
            string type)
        {
            return $"{dateStr}_{type}";
        }

        #endregion

        #region Recordatorios

        private void ScheduleReminder(
            string notificationId,
            MealEntryData entry)
        {
            if (MealNotificationManager.Instance == null)
            {
                Debug.LogWarning(
                    "[MainMenuController] " +
                    "No existe MealNotificationManager."
                );

                ShowToast(
                    "Guardado, pero no se pudo programar el aviso."
                );

                return;
            }

            if (string.IsNullOrEmpty(
                    entry.reminderDate) ||
                string.IsNullOrEmpty(
                    entry.reminderTime))
            {
                Debug.LogWarning(
                    "[MainMenuController] " +
                    "El recordatorio no tiene fecha u hora."
                );

                return;
            }

            DateTime reminderDateTime;

            bool parsed =
                DateTime.TryParse(
                    $"{entry.reminderDate} " +
                    $"{entry.reminderTime}",
                    out reminderDateTime
                );

            if (!parsed)
            {
                Debug.LogError(
                    "[MainMenuController] No se pudo interpretar " +
                    $"la fecha: {entry.reminderDate} " +
                    $"{entry.reminderTime}"
                );

                ShowToast(
                    "La fecha/hora del recordatorio no es válida."
                );

                return;
            }

            if (reminderDateTime <= DateTime.Now)
            {
                Debug.LogWarning(
                    "[MainMenuController] " +
                    $"El recordatorio está en el pasado: " +
                    $"{reminderDateTime}"
                );

                ShowToast(
                    "La fecha del recordatorio ya ha pasado."
                );

                return;
            }

            string title =
                string.IsNullOrEmpty(
                    entry.reminderTitle
                )
                    ? "🍽️ Recordatorio de comida"
                    : entry.reminderTitle;

            string message =
                string.IsNullOrEmpty(entry.dish)
                    ? "Tienes una comida programada."
                    : entry.dish;

            MealNotificationManager.Instance
                .ScheduleMealReminder(
                    notificationId,
                    title,
                    message,
                    reminderDateTime
                );
        }

        #endregion

        #region Eliminar

        private void OnDeleteClicked()
        {
            if (!selected.HasValue)
                return;

            var selection =
                selected.Value;

            string dateStr =
                selection.date;

            string type =
                selection.type;

            string notificationId =
                BuildNotificationId(
                    dateStr,
                    type
                );

            MealNotificationManager.Instance?
                .CancelMealReminder(
                    notificationId
                );

            data.Remove(
                Key(dateStr, type)
            );

            PersistData();

            ShowToast("Eliminado");

            selected = null;

            Render();

            RestoreLastVisibleDay();
        }

        #endregion

        #region Firebase Save

        private async void PersistData()
        {
            if (FirebaseManager.Instance == null)
                return;

            await FirebaseManager.Instance
                .SaveMealPlanAsync(data);
        }

        #endregion

        #region Cerrar

        private void OnCloseClicked()
        {
            Debug.Log(
                $"[DAY DEBUG] CLOSE antes de Render | " +
                $"lastVisibleDate={lastVisibleDate} | " +
                $"selected={selected}"
            );

            selected = null;

            Render();

            Debug.Log(
                $"[DAY DEBUG] CLOSE después de Render | " +
                $"lastVisibleDate={lastVisibleDate}"
            );

            RestoreLastVisibleDay();
        }

        private void RestoreLastVisibleDay()
        {
            Debug.Log(
                $"[DAY DEBUG] RestoreLastVisibleDay | " +
                $"lastVisibleDate={lastVisibleDate}"
            );

            if (!string.IsNullOrEmpty(lastVisibleDate))
            {
                Debug.Log(
                    $"[DAY DEBUG] Restaurando día: {lastVisibleDate}"
                );

                ScrollToDayCard(lastVisibleDate);
            }
            else
            {
                Debug.Log(
                    "[DAY DEBUG] lastVisibleDate vacío -> ScrollToToday"
                );

                ScrollToToday();
            }
        }

        #endregion

        #region Platos aleatorios

        private void OnRandomDishClicked()
        {
            var suggestion =
                RandomOtherWeekDish();

            if (suggestion == null)
            {
                ShowToast(
                    "Todavía no hay suficientes platos " +
                    "guardados en otras semanas"
                );

                return;
            }

            dishField.value =
                suggestion;
        }

        private void OnOldDishClicked()
        {
            var suggestion =
                LeastRecentDish();

            if (suggestion == null)
            {
                ShowToast(
                    "Todavía no hay suficiente histórico guardado"
                );

                return;
            }

            dishField.value =
                suggestion;
        }

        private string RandomOtherWeekDish()
        {
            var excluded =
                new HashSet<string>(
                    WeekDates(weekOffset)
                        .Select(FormatDate)
                );

            var candidates =
                data
                    .Where(
                        kv =>
                            !string.IsNullOrEmpty(
                                kv.Value?.dish
                            ) &&
                            !excluded.Contains(
                                kv.Key.Split('|')[0]
                            )
                    )
                    .Select(
                        kv => kv.Value.dish
                    )
                    .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates[
                UnityEngine.Random.Range(
                    0,
                    candidates.Count
                )
            ];
        }

        private string LeastRecentDish()
        {
            var lastSeen =
                new Dictionary<string, string>();

            foreach (var kv in data)
            {
                if (kv.Value == null ||
                    string.IsNullOrEmpty(
                        kv.Value.dish))
                {
                    continue;
                }

                string dateStr =
                    kv.Key.Split('|')[0];

                if (
                    !lastSeen.TryGetValue(
                        kv.Value.dish,
                        out var previous
                    ) ||
                    string.CompareOrdinal(
                        dateStr,
                        previous
                    ) > 0
                )
                {
                    lastSeen[
                        kv.Value.dish
                    ] = dateStr;
                }
            }

            if (lastSeen.Count == 0)
                return null;

            return lastSeen
                .OrderBy(
                    kv => kv.Value,
                    StringComparer.Ordinal
                )
                .First()
                .Key;
        }

        #endregion

        #region Toast

        private void ShowToast(
            string message)
        {
            if (toast == null)
                return;

            toast.text =
                message;

            toast.AddToClassList(
                "toast--show"
            );

            toastHideTask?.Pause();

            toastHideTask =
                toast.schedule
                    .Execute(
                        () =>
                        {
                            toast.RemoveFromClassList(
                                "toast--show"
                            );
                        }
                    )
                    .StartingIn(2200);
        }

        #endregion
    }
}