using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using QueComemos.Data;
using QueComemos.Localization;

namespace QueComemos.UI
{
    //MainMenuController.NavPanel
    public partial class MainMenuController
    {
        #region NavPanel References

        private VisualElement navPanel;

        // Footer buttons
        private Button footerCalendarBtn;
        private Button footerShoppingBtn;
        private Button footerSettingsBtn;

        // Settings buttons (Fullscreen Ajustes)
        private Button settingsThemeBtn;
        private Button settingsShareBtn;
        private Button settingsSupportBtn;
        private Button settingsLogoutBtn;
        private Button settingsPrivacyBtn;

        // Settings Calendarios
        private Button settingsOwnCalendarBtn;
        private Button settingsOwnPinBtn;
        private VisualElement settingsSharedCalendarsContainer;

        // Selector de idioma (Ajustes)
        private DropdownField languageDropdown;

        // Estado
        private bool navPanelOpen;

        #endregion

        #region NavPanel Initialize

        private void CacheNavPanelReferences()
        {
            // NavPanel
            navPanel = panelRoot.Q<VisualElement>("nav-panel");

            // Footer buttons
            footerCalendarBtn = panelRoot.Q<Button>("footer-calendar-btn");
            footerShoppingBtn = panelRoot.Q<Button>("footer-shopping-btn");
            footerSettingsBtn = panelRoot.Q<Button>("footer-settings-btn");

            LoadFooterIcons();

            // Settings buttons (dentro del navPanel)
            if (navPanel != null)
            {
                settingsThemeBtn = navPanel.Q<Button>("settings-theme-btn");
                settingsShareBtn = navPanel.Q<Button>("settings-share-btn");
                settingsSupportBtn = navPanel.Q<Button>("settings-support-btn");
                settingsLogoutBtn = navPanel.Q<Button>("settings-logout-btn");
                settingsPrivacyBtn = navPanel.Q<Button>("settings-privacy-btn");

                // Calendarios en Ajustes
                var settingsOwnCalendarContainer = navPanel.Q<VisualElement>("settings-own-calendar-row");
                var settingsOwnCalendarItem = settingsOwnCalendarContainer?.Q<Button>("menu-item");

                if (settingsOwnCalendarItem != null)
                    settingsOwnCalendarBtn = settingsOwnCalendarItem;

                settingsOwnPinBtn = navPanel.Q<Button>("settings-own-pin-btn");
                settingsSharedCalendarsContainer = navPanel.Q<VisualElement>("settings-shared-calendars-container");

                // Idioma
                languageDropdown = navPanel.Q<DropdownField>("settings-language-dropdown");

                if (languageDropdown != null)
                {
                    var names = new List<string>();

                    foreach (var language in Loc.Supported)
                        names.Add(Loc.NativeName(language));

                    languageDropdown.choices = names;
                }
            }

            CacheShoppingReferences();
        }

        private void LoadFooterIcons()
        {
            Texture2D calendarIcon = Resources.Load<Texture2D>("Icons/calendar");
            Texture2D shoppingIcon = Resources.Load<Texture2D>("Icons/shopping-list");
            Texture2D settingsIcon = Resources.Load<Texture2D>("Icons/settings");

            if (footerCalendarBtn != null && calendarIcon != null)
            {
                var iconElement = footerCalendarBtn.Q<VisualElement>("footer-btn-icon-calendar");
                if (iconElement != null)
                    iconElement.style.backgroundImage = new StyleBackground(calendarIcon);
            }

            if (footerShoppingBtn != null && shoppingIcon != null)
            {
                var iconElement = footerShoppingBtn.Q<VisualElement>("footer-btn-icon-shopping");
                if (iconElement != null)
                    iconElement.style.backgroundImage = new StyleBackground(shoppingIcon);
            }

            if (footerSettingsBtn != null && settingsIcon != null)
            {
                var iconElement = footerSettingsBtn.Q<VisualElement>("footer-btn-icon-settings");
                if (iconElement != null)
                    iconElement.style.backgroundImage = new StyleBackground(settingsIcon);
            }
        }

        private void RegisterNavPanelEvents()
        {
            // Footer buttons
            if (footerCalendarBtn != null)
                footerCalendarBtn.clicked += ShowCalendarTab;   // antes: CloseNavPanel

            if (footerShoppingBtn != null)
                footerShoppingBtn.clicked += OpenShoppingPanel;

            if (footerSettingsBtn != null)
                footerSettingsBtn.clicked += OpenNavPanel;

            // Settings buttons
            if (settingsThemeBtn != null)
                settingsThemeBtn.clicked += OnSettingsThemeClicked;

            if (settingsShareBtn != null)
                settingsShareBtn.clicked += OnSettingsShareClicked;

            if (settingsSupportBtn != null)
                settingsSupportBtn.clicked += OnSettingsSupportClicked;

            if (settingsLogoutBtn != null)
                settingsLogoutBtn.clicked += OnSettingsLogoutClicked;

            if (settingsPrivacyBtn != null)
                settingsPrivacyBtn.clicked += PrivacyConsent.OpenPolicy;

            // Idioma
            if (languageDropdown != null)
            {
                languageDropdown.RegisterValueChangedCallback(evt =>
                {
                    int index = languageDropdown.index;

                    if (index >= 0 && index < Loc.Supported.Length)
                        Loc.SetLanguage(Loc.Supported[index]);
                });

                // La lista desplegable la crea Unity fuera de nuestro arbol UXML,
                // asi que hay que darle el estilo de la app cuando aparece.
                languageDropdown.RegisterCallback<PointerDownEvent>(
                    evt => StyleLanguagePopup(),
                    TrickleDown.TrickleDown
                );

                languageDropdown.RegisterCallback<ClickEvent>(
                    evt => StyleLanguagePopup()
                );
            }

            // Calendarios en Ajustes
            if (settingsOwnCalendarBtn != null)
                settingsOwnCalendarBtn.clicked += () => SwitchToCalendar(FirebaseManager.Instance?.OwnCalendarId);

            if (settingsOwnPinBtn != null)
                settingsOwnPinBtn.clicked += () => PinCalendar(FirebaseManager.Instance?.OwnCalendarId);

            RegisterShoppingEvents();
        }

        #endregion

        #region NavPanel Control

        private void OpenNavPanel()
        {
            HideShoppingPanel();

            if (navPanel == null)
                return;

            navPanelOpen = true;
            navPanel.style.display = DisplayStyle.Flex;

            UpdateFooterActiveButton("ajustes");
            RefreshCalendarsMenuInSettings();
            UpdateShareButtonInSettings();
        }

        private void CloseNavPanel()
        {
            if (navPanel == null)
                return;

            navPanelOpen = false;
            navPanel.style.display = DisplayStyle.None;

            UpdateFooterActiveButton("calendario");
        }

        private void UpdateFooterActiveButton(string tab)
        {
            // Remover clase activa de todos
            if (footerCalendarBtn != null)
                footerCalendarBtn.RemoveFromClassList("footer-btn--active");

            if (footerShoppingBtn != null)
                footerShoppingBtn.RemoveFromClassList("footer-btn--active");

            if (footerSettingsBtn != null)
                footerSettingsBtn.RemoveFromClassList("footer-btn--active");

            // Agregar clase activa al seleccionado
            switch (tab)
            {
                case "calendario":
                    if (footerCalendarBtn != null)
                        footerCalendarBtn.AddToClassList("footer-btn--active");
                    break;

                case "ajustes":
                    if (footerSettingsBtn != null)
                        footerSettingsBtn.AddToClassList("footer-btn--active");
                    break;
                case "lista":
                    if (footerShoppingBtn != null)
                        footerShoppingBtn.AddToClassList("footer-btn--active");
                    break;
            }
        }

        #endregion

        #region Settings Actions

        private void OnSettingsThemeClicked()
        {
            ToggleTheme();
            UpdateThemeButtonText();
        }

        private void UpdateThemeButtonText()
        {
            if (settingsThemeBtn == null)
                return;

            settingsThemeBtn.text =
                Loc.T(isLightTheme ? "settings.themeLight" : "settings.themeDark");
        }

        /// <summary>
        /// Aplica el estilo de la app (colores del tema) a la lista del dropdown.
        /// Busca el popup durante ~0,5 s porque Unity lo crea justo despues del clic.
        /// </summary>
        private void StyleLanguagePopup()
        {
            if (panelRoot == null)
                return;

            int attempts = 0;
            bool styled = false;

            panelRoot.schedule
                .Execute(() =>
                {
                    attempts++;

                    var popup =
                        panelRoot.panel?.visualTree
                            .Q(className: "unity-base-dropdown");

                    if (popup == null)
                        return;

                    styled = true;

                    if (popup.ClassListContains("language-popup"))
                        return;

                    // Solo las hojas necesarias: variables de tema y estilos del popup.
                    for (int i = 0; i < panelRoot.styleSheets.count; i++)
                    {
                        var sheet = panelRoot.styleSheets[i];

                        if ((sheet.name == "Theme" || sheet.name == "Molecules") &&
                            !popup.styleSheets.Contains(sheet))
                        {
                            popup.styleSheets.Add(sheet);
                        }
                    }

                    popup.AddToClassList("language-popup");
                    popup.AddToClassList(isLightTheme ? "theme-light" : "theme-dark");
                })
                .Every(16)
                .Until(() => styled || attempts > 30);
        }

        private void UpdateLanguageDropdown()
        {
            languageDropdown?.SetValueWithoutNotify(
                Loc.NativeName(Loc.Current)
            );
        }

        /// <summary>
        /// Se ejecuta al cambiar de idioma. Los textos "@clave" del UXML ya los
        /// ha refrescado LocBinder; aquí se actualiza lo que pone el código.
        /// </summary>
        private void HandleLanguageChanged()
        {
            RenderWeekNav();
            RenderWeekGrid();
            UpdateEditPanelTitle();

            UpdateThemeButtonText();
            UpdateLanguageDropdown();

            UpdateOwnerBadge();

            if (ownCalendarBtn != null)
                ownCalendarBtn.text = Loc.T("calendar.mine");

            if (settingsOwnCalendarBtn != null)
                settingsOwnCalendarBtn.text = Loc.T("calendar.mine");

            // Reconstruyen las filas de calendarios compartidos con el texto nuevo.
            if (navPanelOpen)
                RefreshCalendarsMenuInSettings();

            if (menuOpen)
                RefreshCalendarsMenu();
        }

        private void OnSettingsShareClicked()
        {
            OnShareRealClicked();
            //CloseNavPanel();
        }

        private void OnSettingsSupportClicked()
        {
            OnOpenSupport();
            //CloseNavPanel();
        }

        private void OnSettingsLogoutClicked()
        {
            OnLogoutClicked();
            CloseNavPanel();
        }

        private void UpdateShareButtonInSettings()
        {
            if (settingsShareBtn == null)
                return;

            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            bool isOwnCalendar =
                firebase.CurrentCalendarId == firebase.OwnCalendarId;

            bool isGuest = firebase.IsGuestSession();

            bool shouldShow = (isOwnCalendar && !isGuest);

            settingsShareBtn.style.display =
                shouldShow
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        #endregion

        #region Settings Calendarios

        private async void RefreshCalendarsMenuInSettings()
        {
            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            if (calendarMenuRowTemplate == null)
                return;

            if (settingsSharedCalendarsContainer == null)
                return;

            string currentId = firebase.CurrentCalendarId;
            string ownId = firebase.OwnCalendarId;
            string pinnedId = PinnedCalendarId;

            if (settingsOwnCalendarBtn != null)
            {
                settingsOwnCalendarBtn.text = Loc.T("calendar.mine");
                settingsOwnCalendarBtn.RemoveFromClassList("menu-item--active");

                if (currentId == ownId)
                    settingsOwnCalendarBtn.AddToClassList("menu-item--active");
            }

            SetPinIcon(settingsOwnPinBtn, string.IsNullOrEmpty(pinnedId) || pinnedId == ownId);

            settingsSharedCalendarsContainer.Clear();

            var sharedIds = await firebase.GetSharedCalendarIdsAsync();

            if (sharedIds == null)
                return;

            foreach (var id in sharedIds)
            {
                string ownerLabel = await firebase.GetOwnerLabelAsync(id);

                TemplateContainer row = calendarMenuRowTemplate.Instantiate();

                Button nameBtn = row.Q<Button>("calendar-name-btn");
                Button pinBtn = row.Q<Button>("pin-btn");
                Button leaveBtn = row.Q<Button>("leave-btn");

                if (nameBtn == null || leaveBtn == null)
                    continue;

                nameBtn.text =
                    string.IsNullOrEmpty(ownerLabel)
                        ? Loc.T("calendar.shared")
                        : FirebaseManager.GetDisplayNameFromEmail(ownerLabel);

                nameBtn.RemoveFromClassList("menu-item--active");

                if (currentId == id)
                    nameBtn.AddToClassList("menu-item--active");

                string capturedId = id;

                nameBtn.clicked += () => SwitchToCalendar(capturedId);

                SetPinIcon(pinBtn, pinnedId == id);

                if (pinBtn != null)
                    pinBtn.clicked += () => PinCalendar(capturedId);

                leaveBtn.clicked += () => LeaveCalendar(capturedId);

                settingsSharedCalendarsContainer.Add(row);
            }
        }

        #endregion
    }
}