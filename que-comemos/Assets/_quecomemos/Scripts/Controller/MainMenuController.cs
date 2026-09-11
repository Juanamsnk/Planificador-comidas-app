using System;
using System.Collections.Generic;
using System.Linq;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.UIElements;
using QueComemos.Notifications;

namespace QueComemos.UI
{
    /// <summary>
    /// Controlador de la pantalla principal "Qué comemos".
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("URL base de la web (sin barra final), para construir los enlaces para compartir. Cámbiala por tu dominio real.")]
        [SerializeField] private string webBaseUrl = "https://keen-haupia-6b85c0.netlify.app";

        private static readonly string[] DayNames =
            { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        private static readonly string[] Months =
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };

        private class DayCardRefs
        {
            public VisualElement card;
            public VisualElement head;
            public Label dayName;
            public Label dayNum;
            public MealSlotRefs comida;
            public MealSlotRefs cena;

            // NUEVO: se guarda la fecha asociada a esta tarjeta para poder hacer scroll a ella.
            public string dateStr;
        }

        private class MealSlotRefs
        {
            public Button cell;
            public Label emptyLabel;
            public VisualElement filled;
            public Label dishLabel;
            public Label metaLabel;
        }

        private UIDocument document;
        private VisualElement panelRoot; // rootVisualElement del UIDocument
        private VisualElement pageRoot;  // elemento "#root" (el que lleva theme-dark/theme-light)

        // Header
        private Button themeToggleBtn;
        private VisualElement menuWrap;
        private Button menuToggleBtn;
        private VisualElement dropdownMenu;
        private Button shareRealBtn;
        private Button supportBtn;
        private Button logoutBtn;
        private Label ownerBadge;
        private Button ownCalendarBtn;
        private VisualElement sharedCalendarsContainer;

        // NUEVO: ScrollView que contiene la semana y el panel de edición.
        private ScrollView weekScroll;

        [Tooltip("Arrastra aquí el asset Components/CalendarMenuRow.uxml — hace falta para poder crear filas nuevas en tiempo de ejecución.")]
        [SerializeField] private VisualTreeAsset calendarMenuRowTemplate;

        // Week nav
        private Button prevWeekBtn;
        private Button nextWeekBtn;
        private Button todayBtn;
        private Label weekRangeLabel;

        // Tarjetas de día (lunes a domingo)
        private readonly List<DayCardRefs> dayCards = new List<DayCardRefs>();

        // Edit panel
        private VisualElement editPanel;
        private Label editPanelTitle;
        private TextField dishField;
        private Button randomDishBtn;
        private Button oldDishBtn;
        private Toggle reminderToggle;
        private TextField reminderDateField;
        private TextField reminderTimeField;
        private TextField reminderTitleField;
        private Button saveBtn;
        private Button deleteBtn;
        private Button closeBtn;
        private VisualElement keyboardSpacer; // Espacio vacío para dejar lugar al teclado

        // Toast
        private Label toast;
        private IVisualElementScheduledItem toastHideTask;

        // ---------- Estado ----------
        private readonly Dictionary<string, MealEntryData> data = new Dictionary<string, MealEntryData>();
        private int weekOffset;
        private (string date, string type)? selected;
        private bool menuOpen;
        private bool isLightTheme;
        private string lastVisibleDate; // Guardar qué día estaba visible antes de abrir edición
        private IVisualElementScheduledItem keyboardCheckTask;

        private void OnEnable()
        {
            Debug.Log("[MainMenuController] OnEnable() arrancando...");

            document = GetComponent<UIDocument>();
            panelRoot = document.rootVisualElement;
            if (panelRoot == null)
            {
                Debug.LogError("[MainMenuController] rootVisualElement es null: " +
                    "revisa que el UIDocument tenga un Panel Settings asignado.");
                return;
            }

            pageRoot = panelRoot.Q<VisualElement>("root");
            if (pageRoot == null)
            {
                Debug.LogError("[MainMenuController] No se encuentra el elemento \"root\": " +
                    "revisa que el Source Asset del UIDocument sea MainMenu.uxml.");
                return;
            }

            CacheReferences();

            if (!ValidateReferences())
            {
                Debug.LogError("[MainMenuController] Faltan referencias (ver errores arriba). " +
                    "No se registran eventos ni se renderiza para evitar más excepciones.");
                return;
            }

            RegisterEvents();
            CreateKeyboardSpacer();
            Render();

            ConnectToFirebase();
            UpdateOwnerBadge();

            isLightTheme = true;

            ToggleTheme();

            Debug.Log("[MainMenuController] OnEnable() terminado correctamente.");
        }

        private void OnDisable()
        {
            panelRoot?.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            keyboardCheckTask?.Pause();

            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.UnsubscribeFromMealPlan(HandleMealPlanChanged);
                FirebaseManager.Instance.OnError -= HandleFirebaseError;
            }
        }

        #region Firebase
        /// <summary>
        /// Se suscribe a los cambios del calendario en Firebase
        /// </summary>
        private void ConnectToFirebase()
        {
            if (FirebaseManager.Instance == null)
            {
                Debug.LogWarning("[MainMenuController] No hay FirebaseManager en la escena: " +
                    "los platos no se guardan, solo viven en memoria.");
                return;
            }

            FirebaseManager.Instance.SubscribeToMealPlan(HandleMealPlanChanged);
            FirebaseManager.Instance.OnError += HandleFirebaseError;
        }

        private void HandleMealPlanChanged(Dictionary<string, MealEntryData> firebaseData)
        {
            data.Clear();
            foreach (var kv in firebaseData) data[kv.Key] = kv.Value;
            Render();

            ScrollToToday();

            SyncAllRemindersAsync();
        }

        /// <summary>Obtiene mealplans de todos los calendarios (actual + compartidos) y sincroniza sus recordatorios.</summary>
        private async void SyncAllRemindersAsync()
        {
            if (FirebaseManager.Instance == null) return;
            if (MealNotificationManager.Instance == null) return;

            try
            {
                var allMealPlans = await FirebaseManager.Instance.GetAllAccessibleMealPlansAsync();
                MealNotificationManager.Instance.SyncAllCalendarReminders(allMealPlans);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenuController] Error sincronizando recordatorios: {e}");
            }
        }

        private void HandleFirebaseError(string message)
        {
            ShowToast(message);
        }

        /// <summary>Muestra el nombre del calendario que se está viendo ahora mismo</summary>
        private async void UpdateOwnerBadge()
        {
            var firebase = FirebaseManager.Instance;
            if (firebase == null || ownerBadge == null || string.IsNullOrEmpty(firebase.CurrentCalendarId))
            {
                if (ownerBadge != null) ownerBadge.style.display = DisplayStyle.None;
                return;
            }

            ownerBadge.RemoveFromClassList("badge--owner");
            ownerBadge.RemoveFromClassList("badge--shared");
            ownerBadge.style.display = DisplayStyle.Flex;

            bool isOwn = firebase.CurrentCalendarId == firebase.OwnCalendarId;
            if (isOwn)
            {
                ownerBadge.text = "Tu calendario";
                ownerBadge.AddToClassList("badge--owner");
                return;
            }

            string label = await firebase.GetOwnerLabelAsync(firebase.CurrentCalendarId);
            ownerBadge.text = string.IsNullOrEmpty(label) ? "Calendario compartido" : label;
            ownerBadge.AddToClassList("badge--shared");
        }
        #endregion

        #region UITOOLKIT
        /// Comprueba una a una las referencias más importantes y va
        /// diciendo por consola cuál falta
        /// </summary>
        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= LogIfNull(themeToggleBtn, "theme-toggle-btn");
            ok &= LogIfNull(menuWrap, "menu-wrap");
            ok &= LogIfNull(menuToggleBtn, "menu-toggle-btn");
            ok &= LogIfNull(dropdownMenu, "dropdown-menu");
            ok &= LogIfNull(shareRealBtn, "share-real-btn");
            ok &= LogIfNull(supportBtn, "share-demo-btn");
            ok &= LogIfNull(logoutBtn, "logout-btn");
            ok &= LogIfNull(ownerBadge, "owner-badge");
            ok &= LogIfNull(ownCalendarBtn, "own-calendar-item > menu-item");
            ok &= LogIfNull(sharedCalendarsContainer, "shared-calendars-container");
            ok &= LogIfNull(prevWeekBtn, "prev-week-btn");
            ok &= LogIfNull(nextWeekBtn, "next-week-btn");
            ok &= LogIfNull(todayBtn, "today-btn");
            ok &= LogIfNull(weekRangeLabel, "week-range-label");
            ok &= LogIfNull(editPanel, "edit-panel");
            ok &= LogIfNull(dishField, "dish-field");
            ok &= LogIfNull(saveBtn, "save-btn");
            ok &= LogIfNull(toast, "toast");

            for (int i = 0; i < dayCards.Count; i++)
            {
                var d = dayCards[i];
                ok &= LogIfNull(d.card, $"day-card (instancia {i})");
                ok &= LogIfNull(d.comida?.cell, $"cell-button comida (día {i})");
                ok &= LogIfNull(d.cena?.cell, $"cell-button cena (día {i})");
            }

            return ok;
        }

        private static bool LogIfNull(object obj, string label)
        {
            if (obj != null) return true;
            Debug.LogError($"[MainMenuController] No se encontró el elemento \"{label}\". " +
                "Revisa que el nombre en el UXML coincida exactamente.");
            return false;
        }

        /// <summary>
        /// El menú ya no está anidado dentro del botón hamburguesa así que hay que calcular
        /// a mano dónde colocarlo para que quede pegado debajo del botón.
        /// </summary>
        private void PositionDropdownMenu()
        {
            var btnBound = menuToggleBtn.worldBound;
            var rootBound = panelRoot.worldBound;

            dropdownMenu.style.position = Position.Absolute;
            dropdownMenu.style.top = (btnBound.yMax - rootBound.yMin) + 8f;
            dropdownMenu.style.right = Mathf.Max(0f, rootBound.xMax - btnBound.xMax);
            dropdownMenu.style.left = StyleKeyword.Auto;
        }

        #endregion

        #region Cachear Referencias
        private void CacheReferences()
        {
            themeToggleBtn = panelRoot.Q<Button>("theme-toggle-btn");
            menuWrap = panelRoot.Q<VisualElement>("menu-wrap");
            menuToggleBtn = panelRoot.Q<Button>("menu-toggle-btn");
            dropdownMenu = panelRoot.Q<VisualElement>("dropdown-menu");
            shareRealBtn = panelRoot.Q<Button>("share-real-btn");
            supportBtn = panelRoot.Q<Button>("share-demo-btn");
            logoutBtn = panelRoot.Q<Button>("logout-btn");
            ownerBadge = panelRoot.Q<Label>("owner-badge");
            ownCalendarBtn = panelRoot.Q<VisualElement>("own-calendar-item").Q<Button>("menu-item");
            sharedCalendarsContainer = panelRoot.Q<VisualElement>("shared-calendars-container");

            weekScroll = panelRoot.Q<ScrollView>("page-scroll");

            prevWeekBtn = panelRoot.Q<Button>("prev-week-btn");
            nextWeekBtn = panelRoot.Q<Button>("next-week-btn");
            todayBtn = panelRoot.Q<Button>("today-btn");
            weekRangeLabel = panelRoot.Q<Label>("week-range-label");

            for (int i = 0; i < 7; i++)
            {
                // "day-card-0".."day-card-6" es el nombre de la INSTANCIA (ver
                // MainMenu.uxml); dentro de cada una, el elemento con la clase
                // real ".day-card" se llama simplemente "day-card".
                var instance = panelRoot.Q<VisualElement>($"day-card-{i}");
                var card = instance.Q<VisualElement>("day-card");

                dayCards.Add(new DayCardRefs
                {
                    card = card,
                    head = card.Q<VisualElement>("day-card-head"),
                    dayName = card.Q<Label>("day-name-label"),
                    dayNum = card.Q<Label>("day-num-label"),
                    comida = BuildMealSlotRefs(card.Q<VisualElement>("meal-slot-comida"), isComida: true),
                    cena = BuildMealSlotRefs(card.Q<VisualElement>("meal-slot-cena"), isComida: false),
                });
            }

            editPanel = panelRoot.Q<VisualElement>("edit-panel");
            editPanelTitle = editPanel.Q<Label>("edit-panel-title");
            dishField = editPanel.Q<TextField>("dish-field");
            randomDishBtn = editPanel.Q<Button>("random-dish-btn");
            oldDishBtn = editPanel.Q<Button>("old-dish-btn");
            saveBtn = editPanel.Q<Button>("save-btn");
            deleteBtn = editPanel.Q<Button>("delete-btn");
            closeBtn = editPanel.Q<Button>("close-btn");

            var reminderLine = editPanel.Q<VisualElement>("reminder-line");
            reminderToggle = reminderLine.Q<Toggle>("reminder-toggle");
            reminderDateField = reminderLine.Q<TextField>("reminder-date-field");
            reminderTimeField = reminderLine.Q<TextField>("reminder-time-field");
            reminderTitleField = editPanel.Q<TextField>("reminder-title-field");

            toast = panelRoot.Q<Label>("toast");
        }

        private MealSlotRefs BuildMealSlotRefs(VisualElement slotContainer, bool isComida)
        {
            var dot = slotContainer.Q<VisualElement>("meal-dot");
            var tagLabel = slotContainer.Q<Label>("meal-tag-label");
            dot.RemoveFromClassList("meal-dot--comida");
            dot.RemoveFromClassList("meal-dot--cena");
            dot.AddToClassList(isComida ? "meal-dot--comida" : "meal-dot--cena");
            tagLabel.text = isComida ? "Comida" : "Cena";

            var cellButton = slotContainer.Q<Button>("cell-button");
            return new MealSlotRefs
            {
                cell = cellButton,
                emptyLabel = cellButton.Q<Label>("cell-empty-label"),
                filled = cellButton.Q<VisualElement>("cell-filled"),
                dishLabel = cellButton.Q<Label>("cell-dish-label"),
                metaLabel = cellButton.Q<Label>("cell-meta-label"),
            };
        }

        #endregion

        #region Eventos
        private void RegisterEvents()
        {
            themeToggleBtn.clicked += ToggleTheme;
            menuToggleBtn.clicked += ToggleMenu;
            shareRealBtn.clicked += OnShareRealClicked;
            supportBtn.clicked += OnOpenSupport;
            logoutBtn.clicked += OnLogoutClicked;
            ownCalendarBtn.clicked += () => SwitchToCalendar(FirebaseManager.Instance?.OwnCalendarId);
            panelRoot.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

            prevWeekBtn.clicked += () => { weekOffset--; Render(); };
            nextWeekBtn.clicked += () => { weekOffset++; Render(); };
            todayBtn.clicked += () => { weekOffset = 0; Render(); };

            foreach (var refs in dayCards)
            {
                RegisterCellClick(refs.comida);
                RegisterCellClick(refs.cena);
            }

            randomDishBtn.clicked += OnRandomDishClicked;
            oldDishBtn.clicked += OnOldDishClicked;
            saveBtn.clicked += OnSaveClicked;
            deleteBtn.clicked += OnDeleteClicked;
            closeBtn.clicked += OnCloseClicked;

            RegisterKeyboardScrolling(dishField);
            RegisterKeyboardScrolling(reminderDateField);
            RegisterKeyboardScrolling(reminderTimeField);
            RegisterKeyboardScrolling(reminderTitleField);
        }

        private void RegisterCellClick(MealSlotRefs slot)
        {
            slot.cell.clicked += () =>
            {
                var (dateStr, type) = ((string, string))slot.cell.userData;
                selected = (dateStr, type);

                lastVisibleDate = dateStr;

                RenderWeekGrid();
                RenderEditPanel();
            };
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!menuOpen) return;
            if (menuWrap.worldBound.Contains(evt.position)) return;
            if (dropdownMenu.worldBound.Contains(evt.position)) return;
            menuOpen = false;
            dropdownMenu.style.display = DisplayStyle.None;
        }

        private void ToggleTheme()
        {
            isLightTheme = !isLightTheme;

            pageRoot.RemoveFromClassList(isLightTheme ? "theme-dark" : "theme-light");
            pageRoot.AddToClassList(isLightTheme ? "theme-light" : "theme-dark");

            Texture2D icon = Resources.Load<Texture2D>(
                isLightTheme ? "Icons/sun" : "Icons/moon"
            );

            if (icon != null)
            {
                themeToggleBtn.style.backgroundImage = new StyleBackground(icon);
                themeToggleBtn.text = "";
            }
            else
            {
                Debug.LogError("[Theme] No se encontró el icono del tema.");
            }

            Texture2D iconMenu = Resources.Load<Texture2D>(
                isLightTheme ? "Icons/menu-light" : "Icons/menu-dark"
            );

            if (iconMenu != null)
            {
                menuToggleBtn.style.backgroundImage = new StyleBackground(iconMenu);
                menuToggleBtn.text = "";
            }
            else
            {
                Debug.LogError("[Menu] No se encontró el icono del menú.");
            }
        }

        private void ToggleMenu()
        {
            menuOpen = !menuOpen;
            Debug.Log($"[MainMenuController] ToggleMenu() pulsado -> menuOpen={menuOpen}");

            if (menuOpen)
            {
                PositionDropdownMenu();
                RefreshCalendarsMenu();
            }

            dropdownMenu.style.display = menuOpen ? DisplayStyle.Flex : DisplayStyle.None;

            if (menuOpen)
            {
                // Log de verificación tras layout: si vuelve a fallar,
                // aquí veríamos enseguida un worldBound raro otra vez.
                dropdownMenu.schedule.Execute(() =>
                {
                    Debug.Log($"[MainMenuController] dropdownMenu (tras layout) -> " +
                        $"worldBound={dropdownMenu.worldBound}");
                }).ExecuteLater(50);
            }
        }

        private void CloseMenu()
        {
            menuOpen = false;
            dropdownMenu.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Cambia de calendario activo (el tuyo o uno compartido)
        /// </summary>
        private void SwitchToCalendar(string calendarId)
        {
            CloseMenu();
            if (string.IsNullOrEmpty(calendarId)) return;
            selected = null; // por si había un panel de edición abierto del calendario anterior
            FirebaseManager.Instance?.SetCalendarId(calendarId);
            UpdateOwnerBadge();
        }

        private async void LeaveCalendar(string calendarId)
        {
            if (FirebaseManager.Instance == null) return;

            await FirebaseManager.Instance.LeaveSharedCalendarAsync(calendarId);

            // Si estabas viendo justo el que acabas de quitar, vuelve al tuyo.
            if (FirebaseManager.Instance.CurrentCalendarId == calendarId)
            {
                FirebaseManager.Instance.SetCalendarId(FirebaseManager.Instance.OwnCalendarId);
                UpdateOwnerBadge();
            }

            RefreshCalendarsMenu();
        }

        /// <summary>
        /// Repuebla la lista de calendarios del menú
        /// </summary>
        private async void RefreshCalendarsMenu()
        {
            var firebase = FirebaseManager.Instance;
            if (firebase == null) return;
            if (calendarMenuRowTemplate == null)
            {
                Debug.LogError("[MainMenuController] \"Calendar Menu Row Template\" está vacío en el " +
                    "Inspector — arrástrale Components/CalendarMenuRow.uxml. Sin eso no se puede " +
                    "mostrar la lista de calendarios compartidos.");
                return;
            }

            string currentId = firebase.CurrentCalendarId;
            string ownId = firebase.OwnCalendarId;

            ownCalendarBtn.text = "Mi calendario";
            ownCalendarBtn.RemoveFromClassList("menu-item--active");
            if (currentId == ownId) ownCalendarBtn.AddToClassList("menu-item--active");

            sharedCalendarsContainer.Clear();

            var sharedIds = await firebase.GetSharedCalendarIdsAsync();
            foreach (var id in sharedIds)
            {
                string ownerLabel = await firebase.GetOwnerLabelAsync(id);

                var row = calendarMenuRowTemplate.Instantiate();
                var nameBtn = row.Q<Button>("calendar-name-btn");
                var leaveBtn = row.Q<Button>("leave-btn");

                nameBtn.text = string.IsNullOrEmpty(ownerLabel) ? "Calendario compartido" : ownerLabel;
                nameBtn.RemoveFromClassList("menu-item--active");
                if (currentId == id) nameBtn.AddToClassList("menu-item--active");

                string capturedId = id; // evitar capturar la variable de bucle
                nameBtn.clicked += () => SwitchToCalendar(capturedId);
                leaveBtn.clicked += () => LeaveCalendar(capturedId);

                sharedCalendarsContainer.Add(row);
            }
        }

        private void OnShareRealClicked()
        {
            CloseMenu();

            var calendarId = FirebaseManager.Instance?.CurrentCalendarId;
            if (string.IsNullOrEmpty(calendarId))
            {
                ShowToast("Todavía no se puede compartir: inicia sesión primero.");
                return;
            }

            string url = $"{webBaseUrl}/?cal={calendarId}";
            bool sharedNatively = NativeShare.ShareText(url);
            if (!sharedNatively)
            {
                ShowToast("Enlace copiado al portapapeles");
            }
        }

        private void OnOpenSupport()
        {
            CloseMenu();

            Application.OpenURL("https://ko-fi.com/juanmasnk");
        }

        private void OnLogoutClicked()
        {
            CloseMenu();
            GoogleAuthManager.Instance?.SignOut();
            SceneHelper.LoadScene(SceneNames.Authentication);
        }

        #endregion

        #region Render
        private void Render()
        {
            RenderWeekNav();
            RenderWeekGrid();
            RenderEditPanel();
        }

        private void RenderWeekNav()
        {
            var dates = WeekDates(weekOffset);
            var first = dates[0];
            var last = dates[6];
            string rangeLabel = first.Month == last.Month
                ? $"{first.Day} – {last.Day} de {Months[first.Month - 1]}"
                : $"{first.Day} {Months[first.Month - 1]} – {last.Day} {Months[last.Month - 1]}";

            weekRangeLabel.text = $"{rangeLabel} {first.Year}";
            todayBtn.style.display = weekOffset != 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenderWeekGrid()
        {
            var dates = WeekDates(weekOffset);
            var todayStr = FormatDate(DateTime.Today);

            for (int i = 0; i < 7; i++)
            {
                var d = dates[i];
                var dateStr = FormatDate(d);
                bool isToday = dateStr == todayStr;
                var refs = dayCards[i];

                refs.dateStr = dateStr;

                refs.dayName.text = DayNames[IsoWeekdayIndex(d)];
                refs.dayNum.text = $"{d.Day} {Months[d.Month - 1].Substring(0, 3)}";

                refs.card.RemoveFromClassList("day-card--today");
                refs.head.RemoveFromClassList("day-card-head--today");
                if (isToday)
                {
                    refs.card.AddToClassList("day-card--today");
                    refs.head.AddToClassList("day-card-head--today");
                }

                RenderCell(refs.comida, dateStr, "comida");
                RenderCell(refs.cena, dateStr, "cena");
            }
        }

        private void RenderCell(MealSlotRefs slot, string dateStr, string type)
        {
            data.TryGetValue(Key(dateStr, type), out var entry);
            bool hasDish = entry != null && !string.IsNullOrEmpty(entry.dish);
            bool isSelected = selected.HasValue && selected.Value.date == dateStr && selected.Value.type == type;

            slot.cell.RemoveFromClassList("cell-btn--selected");
            if (isSelected) slot.cell.AddToClassList("cell-btn--selected");

            slot.emptyLabel.style.display = hasDish ? DisplayStyle.None : DisplayStyle.Flex;
            slot.filled.style.display = hasDish ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasDish)
            {
                slot.dishLabel.text = entry.dish;
                var metaBits = new List<string>();
                if (!string.IsNullOrEmpty(entry.reminderDate) && !string.IsNullOrEmpty(entry.reminderTime))
                    metaBits.Add("aviso " + entry.reminderTime);
                slot.metaLabel.text = string.Join(" · ", metaBits);
            }

            slot.cell.userData = (dateStr, type);
        }

        private void RenderEditPanel()
        {
            if (!selected.HasValue)
            {
                editPanel.style.display = DisplayStyle.None;
                keyboardSpacer.style.display = DisplayStyle.None;
                return;
            }

            editPanel.style.display = DisplayStyle.Flex;
            UpdateKeyboardSpacer();

            var (dateStr, type) = selected.Value;
            data.TryGetValue(Key(dateStr, type), out var entry);
            entry ??= new MealEntryData();

            var d = DateTime.Parse(dateStr);
            string dayLabel = $"{DayNames[IsoWeekdayIndex(d)]} {d.Day} de {Months[d.Month - 1]}";
            editPanelTitle.text = $"{(type == "comida" ? "Comida" : "Cena")} · {dayLabel}";

            dishField.SetValueWithoutNotify(entry.dish ?? "");

            var (defDate, defTime) = DefaultReminder(dateStr);
            bool hasReminder = !string.IsNullOrEmpty(entry.reminderDate);
            reminderToggle.SetValueWithoutNotify(hasReminder);
            reminderDateField.SetValueWithoutNotify(entry.reminderDate ?? defDate);
            reminderTimeField.SetValueWithoutNotify(entry.reminderTime ?? defTime);
            reminderTitleField.SetValueWithoutNotify(entry.reminderTitle ?? "");

            deleteBtn.style.display = string.IsNullOrEmpty(entry.dish) ? DisplayStyle.None : DisplayStyle.Flex;

            ScrollToEditPanel();
        }

        #endregion

        #region Acciones del Panel de Edición
        private void OnSaveClicked()
        {
            if (!selected.HasValue)
                return;

            var (dateStr, type) = selected.Value;

            string dish = dishField.value?.Trim();

            if (string.IsNullOrEmpty(dish))
            {
                ShowToast("Escribe el nombre del plato");
                return;
            }

            bool remOn = reminderToggle.value;

            var entry = new MealEntryData
            {
                dish = dish,
                reminderDate = remOn ? reminderDateField.value : null,
                reminderTime = remOn ? reminderTimeField.value : null,
                reminderTitle = remOn ? reminderTitleField.value?.Trim() : null
            };

            string notificationId = BuildNotificationId(dateStr, type);

            if (MealNotificationManager.Instance != null)
            {
                MealNotificationManager.Instance.CancelMealReminder(notificationId);
            }

            data[Key(dateStr, type)] = entry;
            PersistData();

            try
            {
                if (remOn)
                {
                    Debug.Log("[OnSaveClicked] Intentando programar recordatorio...");
                    ScheduleReminder(notificationId, entry);
                    Debug.Log("[OnSaveClicked] Recordatorio programado OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnSaveClicked] Error en ScheduleReminder: {ex.Message}\n{ex.StackTrace}");
                ShowToast("Error al programar el recordatorio");
            }

            ShowToast(remOn ? "Guardado y recordatorio programado" : "Guardado");

            Debug.Log("[OnSaveClicked] Haciendo selected = null");
            selected = null;

            Debug.Log("[OnSaveClicked] Llamando a Render()");
            Render();

            Debug.Log("[OnSaveClicked] Scrolleando");
            if (!string.IsNullOrEmpty(lastVisibleDate))
            {
                ScrollToDayCard(lastVisibleDate);
            }
            else
            {
                ScrollToToday();
            }

            Debug.Log("[OnSaveClicked] Terminado");
        }

        private static string BuildNotificationId(
    string dateStr,
    string type)
        {
            return $"{dateStr}_{type}";
        }

        private void ScheduleReminder(
    string notificationId,
    MealEntryData entry)
        {
            if (MealNotificationManager.Instance == null)
            {
                Debug.LogWarning(
                    "[MainMenuController] " +
                    "No existe MealNotificationManager en la escena."
                );

                ShowToast(
                    "Guardado, pero no se pudo programar el aviso."
                );

                return;
            }

            if (string.IsNullOrEmpty(entry.reminderDate) ||
                string.IsNullOrEmpty(entry.reminderTime))
            {
                Debug.LogWarning(
                    "[MainMenuController] " +
                    "El recordatorio no tiene fecha u hora."
                );

                return;
            }

            DateTime reminderDateTime;

            bool parsed = DateTime.TryParse(
                $"{entry.reminderDate} {entry.reminderTime}",
                out reminderDateTime
            );

            if (!parsed)
            {
                Debug.LogError(
                    $"[MainMenuController] " +
                    $"No se pudo interpretar la fecha del recordatorio: " +
                    $"{entry.reminderDate} {entry.reminderTime}"
                );

                ShowToast(
                    "La fecha/hora del recordatorio no es válida."
                );

                return;
            }

            if (reminderDateTime <= DateTime.Now)
            {
                Debug.LogWarning(
                    $"[MainMenuController] " +
                    $"El recordatorio está en el pasado: " +
                    $"{reminderDateTime}"
                );

                ShowToast(
                    "La fecha del recordatorio ya ha pasado."
                );

                return;
            }

            string title = string.IsNullOrEmpty(entry.reminderTitle)
                ? "🍽️ Recordatorio de comida"
                : entry.reminderTitle;

            string message = string.IsNullOrEmpty(entry.dish)
                ? "Tienes una comida programada."
                : entry.dish;

            MealNotificationManager.Instance.ScheduleMealReminder(
                notificationId,
                title,
                message,
                reminderDateTime
            );
        }

        private void OnDeleteClicked()
        {
            if (!selected.HasValue)
                return;

            var (dateStr, type) = selected.Value;

            string notificationId = BuildNotificationId(
                dateStr,
                type
            );

            // Cancelar el recordatorio local.
            if (MealNotificationManager.Instance != null)
            {
                MealNotificationManager.Instance.CancelMealReminder(
                    notificationId
                );
            }

            // Eliminar la comida.
            data.Remove(
                Key(dateStr, type)
            );

            // Guardar cambios en Firebase.
            PersistData();

            ShowToast("Eliminado");

            selected = null;

            Render();

            if (!string.IsNullOrEmpty(lastVisibleDate))
            {
                ScrollToDayCard(lastVisibleDate);
            }
            else
            {
                ScrollToToday();
            }
        }

        /// <summary>
        /// Reenvía el mapa ENTERO de platos a Firebase
        /// </summary>
        private async void PersistData()
        {
            if (FirebaseManager.Instance == null) return;
            await FirebaseManager.Instance.SaveMealPlanAsync(data);
        }

        private void OnCloseClicked()
        {
            selected = null;
            Render();

            if (!string.IsNullOrEmpty(lastVisibleDate))
            {
                ScrollToDayCard(lastVisibleDate);
            }
            else
            {
                ScrollToToday();
            }
        }

        private void OnRandomDishClicked()
        {
            var suggestion = RandomOtherWeekDish();
            if (suggestion == null)
            {
                ShowToast("Todavía no hay suficientes platos guardados en otras semanas");
                return;
            }
            dishField.value = suggestion;
        }

        private void OnOldDishClicked()
        {
            var suggestion = LeastRecentDish();
            if (suggestion == null)
            {
                ShowToast("Todavía no hay suficiente histórico guardado");
                return;
            }
            dishField.value = suggestion;
        }

        private string RandomOtherWeekDish()
        {
            var excluded = new HashSet<string>(WeekDates(weekOffset).Select(FormatDate));
            var candidates = data
                .Where(kv => !string.IsNullOrEmpty(kv.Value.dish) && !excluded.Contains(kv.Key.Split('|')[0]))
                .Select(kv => kv.Value.dish)
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private string LeastRecentDish()
        {
            var lastSeen = new Dictionary<string, string>();
            foreach (var kv in data)
            {
                if (string.IsNullOrEmpty(kv.Value.dish)) continue;
                string dateStr = kv.Key.Split('|')[0];
                if (!lastSeen.TryGetValue(kv.Value.dish, out var prev) || string.CompareOrdinal(dateStr, prev) > 0)
                    lastSeen[kv.Value.dish] = dateStr;
            }
            if (lastSeen.Count == 0) return null;
            return lastSeen.OrderBy(kv => kv.Value, StringComparer.Ordinal).First().Key;
        }

        #endregion

        #region Toast
        private void ShowToast(string message)
        {
            toast.text = message;
            toast.AddToClassList("toast--show");
            toastHideTask?.Pause();
            toastHideTask = toast.schedule.Execute(() => toast.RemoveFromClassList("toast--show")).StartingIn(2200);
        }
        #endregion

        #region Utilidades de Fecha
        private static string FormatDate(DateTime d) => d.ToString("yyyy-MM-dd");

        private static string Key(string dateStr, string type) => dateStr + "|" + type;

        private static int IsoWeekdayIndex(DateTime d)
        {
            int day = (int)d.DayOfWeek; // domingo = 0
            return day == 0 ? 6 : day - 1;
        }

        private static DateTime GetMondayOfCurrentWeek()
        {
            var d = DateTime.Today;
            int day = (int)d.DayOfWeek;
            int diff = day == 0 ? -6 : 1 - day;
            return d.AddDays(diff);
        }

        private static List<DateTime> WeekDates(int offset)
        {
            var monday = GetMondayOfCurrentWeek().AddDays(offset * 7);
            var list = new List<DateTime>();
            for (int i = 0; i < 7; i++) list.Add(monday.AddDays(i));
            return list;
        }

        private static (string date, string time) DefaultReminder(string dateStr)
        {
            var d = DateTime.Parse(dateStr).AddDays(-1);
            return (FormatDate(d), "12:00");
        }

        #endregion

        #region Keyboard & Scroll Management

        /// <summary>
        /// Crea un espaciador invisible al final del panel de edición
        /// para dejar lugar cuando aparece el teclado virtual en mobile
        /// </summary>
        private void CreateKeyboardSpacer()
        {
            keyboardSpacer = new VisualElement
            {
                name = "keyboard-spacer"
            };

            // Altura aproximada del teclado virtual en mobile (iOS/Android)
            keyboardSpacer.style.height = 320;
            keyboardSpacer.style.width = Length.Percent(100);
            keyboardSpacer.style.flexShrink = 0;
            keyboardSpacer.style.flexGrow = 0;
            keyboardSpacer.style.display = DisplayStyle.None;

            // Agregar al final del edit-panel
            editPanel.Add(keyboardSpacer);

            // Comprobar periódicamente si el teclado está realmente visible.
            keyboardCheckTask = editPanel.schedule
                .Execute(UpdateKeyboardSpacer)
                .Every(100);

            Debug.Log("[MainMenuController] Spacer para teclado creado");
        }

        private void UpdateKeyboardSpacer()
        {
            if (keyboardSpacer == null) return;

            bool keyboardVisible = TouchScreenKeyboard.visible;
            keyboardSpacer.style.display = keyboardVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Registra listeners para cada TextField que hacen scroll
        /// cuando aparece el teclado (mediante FocusIn)
        /// </summary>
        private void RegisterKeyboardScrolling(TextField field)
        {
            field.RegisterCallback<FocusInEvent>(evt =>
            {
                // Esperar a que el teclado se haya mostrado realmente.
                field.schedule.Execute(() =>
                {
                    UpdateKeyboardSpacer();
                    ScrollToEditPanel();
                }).ExecuteLater(250);
            });
        }

        /// <summary>
        /// Scrollea al panel de edición y vuelve a hacerlo después para evitar
        /// que el teclado tape el campo de entrada.
        /// </summary>
        private void ScrollToEditPanel()
        {
            if (weekScroll == null || editPanel == null) return;

            // Scroll más agresivo para mobile: intenta centrar el panel
            weekScroll.ScrollTo(editPanel);

            // Re-scroll tras layout para asegurar que el teclado no tapa nada
            editPanel.schedule.Execute(ScrollToEditPanelAdjusted).ExecuteLater(250);
        }

        private void ScrollToEditPanelAdjusted()
        {
            if (weekScroll == null || editPanel == null) return;

            // Scroll de nuevo, pero esta vez después de que el teclado haya aparecido completamente
            weekScroll.ScrollTo(editPanel);

            // Log para debugging
            Debug.Log($"[MainMenuController] EditPanel scrolleado. ScrollOffset={weekScroll.scrollOffset}");
        }

        /// <summary>
        /// Scrollea al día especificado (por fecha en formato "YYYY-MM-DD"),
        /// buscando la tarjeta con ese nombre.
        /// </summary>
        private void ScrollToDayCard(string dateStr)
        {
            if (weekScroll == null || dayCards == null) return;

            var dayCard = dayCards.FirstOrDefault(dc => dc.dateStr == dateStr);
            if (dayCard == null) return;

            weekScroll.ScrollTo(dayCard.card);
        }

        /// <summary>
        /// Scrollea al día de hoy, pero solo si hoy pertenece a la semana visible.
        /// </summary>
        private void ScrollToToday()
        {
            string todayStr = FormatDate(DateTime.Today);
            var weekDates = WeekDates(weekOffset);

            if (!weekDates.Any(d => FormatDate(d) == todayStr)) return;

            ScrollToDayCard(todayStr);
        }

        #endregion
    }
}