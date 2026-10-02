using QueComemos.Data;
using QueComemos.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{  
    //MainMenuController.Editing
    public partial class MainMenuController
    {
        #region Guardar

        private void OnSaveClicked()
        {
            HideKeyboard();

            if (!selected.HasValue) return;

            var selection = selected.Value;
            string dateStr = selection.date;
            string type = selection.type;
            string dish = dishField.value?.Trim();

            if (string.IsNullOrEmpty(dish))
            {
                ShowToast("Escribe el nombre del plato");
                return;
            }

            bool reminderEnabled = reminderToggle.value;

            var entry = new MealEntryData
            {
                dish = dish,
                reminderDate = reminderEnabled ? reminderDateField.value : null,
                reminderTime = reminderEnabled ? reminderTimeField.value : null,
                reminderTitle = reminderEnabled ? reminderTitleField.value?.Trim() : null
            };

            string notificationId = BuildNotificationId(dateStr, type);

            MealNotificationManager.Instance?.CancelMealReminder(notificationId);
            data[Key(dateStr, type)] = entry;
            PersistData();

            try
            {
                if (reminderEnabled)
                    ScheduleReminder(notificationId, entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
                ShowToast("Error al programar recordatorio");
            }

            ShowToast(reminderEnabled ? "Guardado y recordatorio programado" : "Guardado");

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
            HideKeyboard();

            if (!selected.HasValue) return;

            var selection = selected.Value;
            MealNotificationManager.Instance?.CancelMealReminder(BuildNotificationId(selection.date, selection.type));
            data.Remove(Key(selection.date, selection.type));
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
            HideKeyboard();

            selected = null;
            Render();
            RestoreLastVisibleDay();
        }

        private void RestoreLastVisibleDay()
        {
            if (!string.IsNullOrEmpty(lastVisibleDate))
            {
                ScrollToDayCard(lastVisibleDate);
            }
            else
            {
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
                    "Añade platos en otras fechas para sugerir"
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
                    "Agrega platos en varias fechas para analizar"
                );

                return;
            }

            dishField.value =
                suggestion;
        }

        private string RandomOtherWeekDish()
        {
            if (!selected.HasValue)
                return null;

            string currentType = selected.Value.type; // "comida" o "cena"

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
                            ) &&
                            kv.Key.Split('|')[1] == currentType  // ← NUEVO: filtrar por tipo
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
            if (!selected.HasValue)
                return null;

            string currentType = selected.Value.type; // "comida" o "cena"

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

                string key = kv.Key;
                string type = key.Split('|')[1];

                // Filtrar por tipo actual
                if (type != currentType)
                    continue;

                string dateStr =
                    key.Split('|')[0];

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

        private void OnFillWeekClicked()
        {
            HideKeyboard();
            FillWeekWithDishes();
        }

        private void FillWeekWithDishes()
        {
            var weekDates = WeekDates(weekOffset);
            int filledCount = 0;

            // Mantener track de platos ya usados en esta semana
            var usedComidas = new HashSet<string>();
            var usedCenas = new HashSet<string>();

            foreach (var date in weekDates)
            {
                string dateStr = FormatDate(date);

                // Rellenar Comida
                data.TryGetValue(Key(dateStr, "comida"), out var comidaEntry);
                if (string.IsNullOrEmpty(comidaEntry?.dish))
                {
                    string dish = GetRandomDishForType("comida", dateStr, usedComidas);
                    if (!string.IsNullOrEmpty(dish))
                    {
                        data[Key(dateStr, "comida")] = new MealEntryData { dish = dish };
                        usedComidas.Add(dish);  // Agrega a excluidos
                        filledCount++;
                    }
                }
                else
                {
                    usedComidas.Add(comidaEntry.dish);  // Si ya hay, agrégalo a excluidos
                }

                // Rellenar Cena
                data.TryGetValue(Key(dateStr, "cena"), out var cenaEntry);
                if (string.IsNullOrEmpty(cenaEntry?.dish))
                {
                    string dish = GetRandomDishForType("cena", dateStr, usedCenas);
                    if (!string.IsNullOrEmpty(dish))
                    {
                        data[Key(dateStr, "cena")] = new MealEntryData { dish = dish };
                        usedCenas.Add(dish);  // Agrega a excluidos
                        filledCount++;
                    }
                }
                else
                {
                    usedCenas.Add(cenaEntry.dish);  // Si ya hay, agrégalo a excluidos
                }
            }

            if (filledCount == 0)
            {
                ShowToast("Agrega platos en otras fechas para rellenar la semana");
                return;
            }

            PersistData();
            ShowToast($"Semana rellenada: {filledCount} platos");
            Render();
        }

        private string GetRandomDishForType(string type, string excludeDate, HashSet<string> excludeDishes)
        {
            var excluded = new HashSet<string> { excludeDate };

            var candidates =
                data
                    .Where(
                        kv =>
                            !string.IsNullOrEmpty(kv.Value?.dish) &&
                            !excluded.Contains(kv.Key.Split('|')[0]) &&
                            kv.Key.Split('|')[1] == type &&
                            !excludeDishes.Contains(kv.Value.dish)  // ← NO repetir en la semana
                    )
                    .Select(kv => kv.Value.dish)
                    .Distinct()  // Evitar duplicados en la lista de candidatos
                    .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
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