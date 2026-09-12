using QueComemos.Data;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public partial class MainMenuController
    {
        #region Cache References

        private void CacheReferences()
        {
            themeToggleBtn =
                panelRoot.Q<Button>("theme-toggle-btn");

            menuWrap =
                panelRoot.Q<VisualElement>("menu-wrap");

            menuToggleBtn =
                panelRoot.Q<Button>("menu-toggle-btn");

            dropdownMenu =
                panelRoot.Q<VisualElement>("dropdown-menu");

            shareRealBtn =
                panelRoot.Q<Button>("share-real-btn");

            supportBtn =
                panelRoot.Q<Button>("share-demo-btn");

            logoutBtn =
                panelRoot.Q<Button>("logout-btn");

            ownerBadge =
                panelRoot.Q<Label>("owner-badge");

            var ownCalendarContainer =
                panelRoot.Q<VisualElement>("own-calendar-item");

            ownCalendarBtn =
                ownCalendarContainer?.Q<Button>("menu-item");

            sharedCalendarsContainer =
                panelRoot.Q<VisualElement>(
                    "shared-calendars-container"
                );

            weekScroll =
                panelRoot.Q<ScrollView>("page-scroll");

            prevWeekBtn =
                panelRoot.Q<Button>("prev-week-btn");

            nextWeekBtn =
                panelRoot.Q<Button>("next-week-btn");

            todayBtn =
                panelRoot.Q<Button>("today-btn");

            weekRangeLabel =
                panelRoot.Q<Label>("week-range-label");

            CacheDayCards();

            editPanel =
                panelRoot.Q<VisualElement>("edit-panel");

            if (editPanel != null)
            {
                editPanelTitle =
                    editPanel.Q<Label>("edit-panel-title");

                dishField =
                    editPanel.Q<TextField>("dish-field");

                randomDishBtn =
                    editPanel.Q<Button>("random-dish-btn");

                oldDishBtn =
                    editPanel.Q<Button>("old-dish-btn");

                saveBtn =
                    editPanel.Q<Button>("save-btn");

                deleteBtn =
                    editPanel.Q<Button>("delete-btn");

                closeBtn =
                    editPanel.Q<Button>("close-btn");

                var reminderLine =
                    editPanel.Q<VisualElement>("reminder-line");

                if (reminderLine != null)
                {
                    reminderToggle =
                        reminderLine.Q<Toggle>(
                            "reminder-toggle"
                        );

                    reminderDateField =
                        reminderLine.Q<TextField>(
                            "reminder-date-field"
                        );

                    reminderTimeField =
                        reminderLine.Q<TextField>(
                            "reminder-time-field"
                        );
                }

                reminderTitleField =
                    editPanel.Q<TextField>(
                        "reminder-title-field"
                    );
            }

            toast =
                panelRoot.Q<Label>("toast");
        }

        private void CacheDayCards()
        {
            dayCards.Clear();

            for (int i = 0; i < 7; i++)
            {
                var instance =
                    panelRoot.Q<VisualElement>(
                        $"day-card-{i}"
                    );

                if (instance == null)
                {
                    Debug.LogError(
                        $"[MainMenuController] No se encontró " +
                        $"\"day-card-{i}\"."
                    );

                    dayCards.Add(new DayCardRefs());
                    continue;
                }

                var card =
                    instance.Q<VisualElement>("day-card");

                if (card == null)
                {
                    Debug.LogError(
                        $"[MainMenuController] No se encontró " +
                        $"\"day-card\" dentro de day-card-{i}."
                    );
                }

                dayCards.Add(
                    new DayCardRefs
                    {
                        card = card,

                        head =
                            card?.Q<VisualElement>(
                                "day-card-head"
                            ),

                        dayName =
                            card?.Q<Label>(
                                "day-name-label"
                            ),

                        dayNum =
                            card?.Q<Label>(
                                "day-num-label"
                            ),

                        comida =
                            BuildMealSlotRefs(
                                card?.Q<VisualElement>(
                                    "meal-slot-comida"
                                ),
                                true
                            ),

                        cena =
                            BuildMealSlotRefs(
                                card?.Q<VisualElement>(
                                    "meal-slot-cena"
                                ),
                                false
                            )
                    }
                );
            }
        }

        private MealSlotRefs BuildMealSlotRefs(
            VisualElement slotContainer,
            bool isComida)
        {
            if (slotContainer == null)
            {
                Debug.LogError(
                    "[MainMenuController] No se encontró " +
                    "el contenedor del meal slot."
                );

                return new MealSlotRefs();
            }

            var dot =
                slotContainer.Q<VisualElement>("meal-dot");

            var tagLabel =
                slotContainer.Q<Label>("meal-tag-label");

            if (dot != null)
            {
                dot.RemoveFromClassList("meal-dot--comida");
                dot.RemoveFromClassList("meal-dot--cena");

                dot.AddToClassList(
                    isComida
                        ? "meal-dot--comida"
                        : "meal-dot--cena"
                );
            }

            if (tagLabel != null)
            {
                tagLabel.text =
                    isComida ? "Comida" : "Cena";
            }

            var cellButton =
                slotContainer.Q<Button>("cell-button");

            return new MealSlotRefs
            {
                cell = cellButton,

                emptyLabel =
                    cellButton?.Q<Label>(
                        "cell-empty-label"
                    ),

                filled =
                    cellButton?.Q<VisualElement>(
                        "cell-filled"
                    ),

                dishLabel =
                    cellButton?.Q<Label>(
                        "cell-dish-label"
                    ),

                metaLabel =
                    cellButton?.Q<Label>(
                        "cell-meta-label"
                    )
            };
        }

        #endregion

        #region Validate

        private bool ValidateReferences()
        {
            bool ok = true;

            ok &= LogIfNull(
                themeToggleBtn,
                "theme-toggle-btn"
            );

            ok &= LogIfNull(
                menuWrap,
                "menu-wrap"
            );

            ok &= LogIfNull(
                menuToggleBtn,
                "menu-toggle-btn"
            );

            ok &= LogIfNull(
                dropdownMenu,
                "dropdown-menu"
            );

            ok &= LogIfNull(
                shareRealBtn,
                "share-real-btn"
            );

            ok &= LogIfNull(
                supportBtn,
                "share-demo-btn"
            );

            ok &= LogIfNull(
                logoutBtn,
                "logout-btn"
            );

            ok &= LogIfNull(
                ownerBadge,
                "owner-badge"
            );

            ok &= LogIfNull(
                ownCalendarBtn,
                "own-calendar-item > menu-item"
            );

            ok &= LogIfNull(
                sharedCalendarsContainer,
                "shared-calendars-container"
            );

            ok &= LogIfNull(
                prevWeekBtn,
                "prev-week-btn"
            );

            ok &= LogIfNull(
                nextWeekBtn,
                "next-week-btn"
            );

            ok &= LogIfNull(
                todayBtn,
                "today-btn"
            );

            ok &= LogIfNull(
                weekRangeLabel,
                "week-range-label"
            );

            ok &= LogIfNull(
                editPanel,
                "edit-panel"
            );

            ok &= LogIfNull(
                dishField,
                "dish-field"
            );

            ok &= LogIfNull(
                saveBtn,
                "save-btn"
            );

            ok &= LogIfNull(
                toast,
                "toast"
            );

            for (int i = 0; i < dayCards.Count; i++)
            {
                var day = dayCards[i];

                ok &= LogIfNull(
                    day?.card,
                    $"day-card instancia {i}"
                );

                ok &= LogIfNull(
                    day?.comida?.cell,
                    $"cell-button comida día {i}"
                );

                ok &= LogIfNull(
                    day?.cena?.cell,
                    $"cell-button cena día {i}"
                );
            }

            return ok;
        }

        private static bool LogIfNull(
            object obj,
            string label)
        {
            if (obj != null)
                return true;

            Debug.LogError(
                $"[MainMenuController] No se encontró " +
                $"el elemento \"{label}\". " +
                $"Revisa el nombre en el UXML."
            );

            return false;
        }

        #endregion

        #region Events

        private void RegisterEvents()
        {
            themeToggleBtn.clicked += ToggleTheme;

            menuToggleBtn.clicked += ToggleMenu;

            shareRealBtn.clicked += OnShareRealClicked;

            supportBtn.clicked += OnOpenSupport;

            logoutBtn.clicked += OnLogoutClicked;

            ownCalendarBtn.clicked +=
                () => SwitchToCalendar(
                    FirebaseManager.Instance?.OwnCalendarId
                );

            panelRoot.RegisterCallback<PointerDownEvent>(
                OnRootPointerDown,
                TrickleDown.TrickleDown
            );

            prevWeekBtn.clicked +=
                () =>
                {
                    weekOffset--;
                    Render();
                };

            nextWeekBtn.clicked +=
                () =>
                {
                    weekOffset++;
                    Render();
                };

            todayBtn.clicked +=
                () =>
                {
                    weekOffset = 0;
                    Render();
                };

            foreach (var refs in dayCards)
            {
                if (refs == null)
                    continue;

                RegisterCellClick(refs.comida);
                RegisterCellClick(refs.cena);
            }

            randomDishBtn.clicked +=
                OnRandomDishClicked;

            oldDishBtn.clicked +=
                OnOldDishClicked;

            saveBtn.clicked +=
                OnSaveClicked;

            deleteBtn.clicked +=
                OnDeleteClicked;

            closeBtn.clicked +=
                OnCloseClicked;

            RegisterKeyboardScrolling(dishField);
            RegisterKeyboardScrolling(reminderDateField);
            RegisterKeyboardScrolling(reminderTimeField);
            RegisterKeyboardScrolling(reminderTitleField);
        }

        private void RegisterCellClick(
            MealSlotRefs slot)
        {
            if (slot?.cell == null)
                return;

            slot.cell.clicked +=
                () =>
                {
                    if (slot.cell.userData
                        is ValueTuple<string, string> value)
                    {
                        selected =
                            (value.Item1, value.Item2);

                        lastVisibleDate =
                            value.Item1;

                        RenderWeekGrid();
                        RenderEditPanel();
                    }
                };
        }

        #endregion

        #region Theme

        private void ToggleTheme()
        {
            isLightTheme = !isLightTheme;


            if (pageRoot == null)
                return;

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

            Texture2D themeIcon =
                Resources.Load<Texture2D>(
                    isLightTheme
                        ? "Icons/sun"
                        : "Icons/moon"
                );

            if (themeIcon != null)
            {
                themeToggleBtn.style.backgroundImage =
                    new StyleBackground(themeIcon);

                themeToggleBtn.text = "";
            }
            else
            {
                Debug.LogError(
                    "[Theme] No se encontró el icono del tema."
                );
            }

            Texture2D menuIcon =
                Resources.Load<Texture2D>(
                    isLightTheme
                        ? "Icons/menu-light"
                        : "Icons/menu-dark"
                );

            if (menuIcon != null)
            {
                menuToggleBtn.style.backgroundImage =
                    new StyleBackground(menuIcon);

                menuToggleBtn.text = "";
            }
            else
            {
                Debug.LogError(
                    "[Menu] No se encontró el icono del menú."
                );
            }
        }

        #endregion

        #region Menu

        private void ToggleMenu()
        {
            menuOpen = !menuOpen;

            if (menuOpen)
            {
                PositionDropdownMenu();
                RefreshCalendarsMenu();
            }

            dropdownMenu.style.display =
                menuOpen
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (menuOpen)
            {
                dropdownMenu.schedule
                    .Execute(
                        () =>
                        {
                            Debug.Log(
                                "[MainMenuController] " +
                                $"dropdownMenu worldBound=" +
                                $"{dropdownMenu.worldBound}"
                            );
                        }
                    )
                    .ExecuteLater(50);
            }
        }

        private void CloseMenu()
        {
            menuOpen = false;

            if (dropdownMenu != null)
            {
                dropdownMenu.style.display =
                    DisplayStyle.None;
            }
        }

        private void PositionDropdownMenu()
        {
            if (menuToggleBtn == null ||
                dropdownMenu == null ||
                panelRoot == null)
            {
                return;
            }

            var btnBound =
                menuToggleBtn.worldBound;

            var rootBound =
                panelRoot.worldBound;

            dropdownMenu.style.position =
                Position.Absolute;

            dropdownMenu.style.top =
                btnBound.yMax -
                rootBound.yMin +
                8f;

            dropdownMenu.style.right =
                Mathf.Max(
                    0f,
                    rootBound.xMax -
                    btnBound.xMax
                );

            dropdownMenu.style.left =
                StyleKeyword.Auto;
        }

        private void OnRootPointerDown(
            PointerDownEvent evt)
        {
            if (!menuOpen)
                return;

            if (menuWrap != null &&
                menuWrap.worldBound.Contains(
                    evt.position))
            {
                return;
            }

            if (dropdownMenu != null &&
                dropdownMenu.worldBound.Contains(
                    evt.position))
            {
                return;
            }

            CloseMenu();
        }

        #endregion
    }
}