using System;
using System.Collections.Generic;
using QueComemos.Data;
using QueComemos.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public partial class MainMenuController
    {
        #region Firebase

        private void ConnectToFirebase()
        {
            if (FirebaseManager.Instance == null)
            {
                Debug.LogWarning(
                    "[MainMenuController] No hay FirebaseManager " +
                    "en la escena. Los datos solo vivirán en memoria."
                );

                return;
            }

            FirebaseManager.Instance.SubscribeToMealPlan(
                HandleMealPlanChanged
            );

            FirebaseManager.Instance.OnError +=
                HandleFirebaseError;
        }

        private void HandleMealPlanChanged(
            Dictionary<string, MealEntryData> firebaseData)
        {
            data.Clear();

            if (firebaseData != null)
            {
                foreach (var kv in firebaseData)
                {
                    data[kv.Key] = kv.Value;
                }
            }

            Render();

            ScrollToToday();

            SyncAllRemindersAsync();
        }

        private async void SyncAllRemindersAsync()
        {
            if (FirebaseManager.Instance == null)
                return;

            if (MealNotificationManager.Instance == null)
                return;

            try
            {
                var allMealPlans =
                    await FirebaseManager.Instance
                        .GetAllAccessibleMealPlansAsync();

                MealNotificationManager.Instance
                    .SyncAllCalendarReminders(
                        allMealPlans
                    );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[MainMenuController] Error sincronizando " +
                    $"recordatorios: {e}"
                );
            }
        }

        private void HandleFirebaseError(
            string message)
        {
            ShowToast(message);
        }

        private async void UpdateOwnerBadge()
        {
            var firebase =
                FirebaseManager.Instance;

            if (firebase == null ||
                ownerBadge == null ||
                string.IsNullOrEmpty(
                    firebase.CurrentCalendarId))
            {
                if (ownerBadge != null)
                {
                    ownerBadge.style.display =
                        DisplayStyle.None;
                }

                return;
            }

            ownerBadge.RemoveFromClassList(
                "badge--owner"
            );

            ownerBadge.RemoveFromClassList(
                "badge--shared"
            );

            ownerBadge.style.display =
                DisplayStyle.Flex;

            bool isOwn =
                firebase.CurrentCalendarId ==
                firebase.OwnCalendarId;

            if (isOwn)
            {
                ownerBadge.text =
                    "Tu calendario";

                ownerBadge.AddToClassList(
                    "badge--owner"
                );

                return;
            }

            string label =
                await firebase.GetOwnerLabelAsync(
                    firebase.CurrentCalendarId
                );

            ownerBadge.text =
                string.IsNullOrEmpty(label)
                    ? "Calendario compartido"
                    : label;

            ownerBadge.AddToClassList(
                "badge--shared"
            );
        }

        #endregion

        #region Calendarios

        private void SwitchToCalendar(
            string calendarId)
        {
            CloseMenu();

            if (string.IsNullOrEmpty(calendarId))
                return;

            selected = null;

            FirebaseManager.Instance?
                .SetCalendarId(calendarId);

            UpdateOwnerBadge();
        }

        private async void LeaveCalendar(
            string calendarId)
        {
            if (FirebaseManager.Instance == null)
                return;

            await FirebaseManager.Instance
                .LeaveSharedCalendarAsync(calendarId);

            if (FirebaseManager.Instance
                    .CurrentCalendarId ==
                calendarId)
            {
                FirebaseManager.Instance
                    .SetCalendarId(
                        FirebaseManager.Instance
                            .OwnCalendarId
                    );

                UpdateOwnerBadge();
            }

            RefreshCalendarsMenu();
        }

        private async void RefreshCalendarsMenu()
        {
            var firebase =
                FirebaseManager.Instance;

            if (firebase == null)
                return;

            if (calendarMenuRowTemplate == null)
            {
                Debug.LogError(
                    "[MainMenuController] " +
                    "\"Calendar Menu Row Template\" " +
                    "está vacío en el Inspector. " +
                    "Arrastra Components/CalendarMenuRow.uxml."
                );

                return;
            }

            string currentId =
                firebase.CurrentCalendarId;

            string ownId =
                firebase.OwnCalendarId;

            ownCalendarBtn.text =
                "Mi calendario";

            ownCalendarBtn.RemoveFromClassList(
                "menu-item--active"
            );

            if (currentId == ownId)
            {
                ownCalendarBtn.AddToClassList(
                    "menu-item--active"
                );
            }

            sharedCalendarsContainer.Clear();

            var sharedIds =
                await firebase.GetSharedCalendarIdsAsync();

            if (sharedIds == null)
                return;

            foreach (var id in sharedIds)
            {
                string ownerLabel =
                    await firebase.GetOwnerLabelAsync(id);

                TemplateContainer row =
                    calendarMenuRowTemplate.Instantiate();

                Button nameBtn =
                    row.Q<Button>(
                        "calendar-name-btn"
                    );

                Button leaveBtn =
                    row.Q<Button>(
                        "leave-btn"
                    );

                if (nameBtn == null ||
                    leaveBtn == null)
                {
                    Debug.LogError(
                        "[MainMenuController] " +
                        "CalendarMenuRow.uxml no contiene " +
                        "calendar-name-btn o leave-btn."
                    );

                    continue;
                }

                nameBtn.text =
                    string.IsNullOrEmpty(ownerLabel)
                        ? "Calendario compartido"
                        : ownerLabel;

                nameBtn.RemoveFromClassList(
                    "menu-item--active"
                );

                if (currentId == id)
                {
                    nameBtn.AddToClassList(
                        "menu-item--active"
                    );
                }

                string capturedId = id;

                nameBtn.clicked +=
                    () =>
                    {
                        SwitchToCalendar(
                            capturedId
                        );
                    };

                leaveBtn.clicked +=
                    () =>
                    {
                        LeaveCalendar(
                            capturedId
                        );
                    };

                sharedCalendarsContainer.Add(row);
            }
        }

        #endregion

        #region Compartir / Soporte / Logout

        private void OnShareRealClicked()
        {
            CloseMenu();

            var calendarId =
                FirebaseManager.Instance?
                    .CurrentCalendarId;

            if (string.IsNullOrEmpty(calendarId))
            {
                ShowToast(
                    "Todavía no se puede compartir: " +
                    "inicia sesión primero."
                );

                return;
            }

            string url =
                $"{webBaseUrl}/?cal={calendarId}";

            bool sharedNatively =
                NativeShare.ShareText(url);

            if (!sharedNatively)
            {
                ShowToast(
                    "Enlace copiado al portapapeles"
                );
            }
        }

        private void OnOpenSupport()
        {
            CloseMenu();

            Application.OpenURL(
                "https://ko-fi.com/juanmasnk"
            );
        }

        private void OnLogoutClicked()
        {
            CloseMenu();

            GoogleAuthManager.Instance?.SignOut();

            SceneHelper.LoadScene(
                SceneNames.Authentication
            );
        }

        #endregion
    }
}