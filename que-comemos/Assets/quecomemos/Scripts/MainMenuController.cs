using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    /// <summary>
    /// Controlador de la pantalla principal "Qué comemos".
    /// Replica la lógica de render()/renderEditPanel() del HTML original,
    /// pero SIN backend todavía: los platos se guardan en un Dictionary en
    /// memoria (se pierden al cerrar). Cuando conectemos Firebase (o lo que
    /// decidas), solo hay que sustituir el acceso a "data" por las llamadas
    /// reales, sin tocar el resto de esta clase.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private static readonly string[] DayNames =
            { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        private static readonly string[] Months =
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };

        [Serializable]
        private class MealEntry
        {
            public string dish;
            public string reminderDate;
            public string reminderTime;
            public string reminderTitle;
        }

        private class DayCardRefs
        {
            public VisualElement card;
            public VisualElement head;
            public Label dayName;
            public Label dayNum;
            public MealSlotRefs comida;
            public MealSlotRefs cena;
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

        // Toast
        private Label toast;
        private IVisualElementScheduledItem toastHideTask;

        // ---------- Estado (equivalente a las variables globales del HTML) ----------
        private readonly Dictionary<string, MealEntry> data = new Dictionary<string, MealEntry>();
        private int weekOffset;
        private (string date, string type)? selected;
        private bool menuOpen;
        private bool isLightTheme;

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
            Render();

            Debug.Log("[MainMenuController] OnEnable() terminado correctamente.");
        }

        /// <summary>
        /// Comprueba una a una las referencias más importantes y va
        /// diciendo por consola cuál falta, en vez de fallar en silencio
        /// (Q&lt;T&gt; en UI Toolkit no lanza excepción si no encuentra el
        /// elemento: simplemente devuelve null).
        /// </summary>
        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= LogIfNull(themeToggleBtn, "theme-toggle-btn");
            ok &= LogIfNull(menuWrap, "menu-wrap");
            ok &= LogIfNull(menuToggleBtn, "menu-toggle-btn");
            ok &= LogIfNull(dropdownMenu, "dropdown-menu");
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

        private void OnDisable()
        {
            panelRoot?.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        // ==========================================================
        //  CACHEAR REFERENCIAS
        // ==========================================================
        private void CacheReferences()
        {
            themeToggleBtn = panelRoot.Q<Button>("theme-toggle-btn");
            menuWrap = panelRoot.Q<VisualElement>("menu-wrap");
            menuToggleBtn = panelRoot.Q<Button>("menu-toggle-btn");
            dropdownMenu = panelRoot.Q<VisualElement>("dropdown-menu");

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

        // ==========================================================
        //  EVENTOS
        // ==========================================================
        private void RegisterEvents()
        {
            themeToggleBtn.clicked += ToggleTheme;
            menuToggleBtn.clicked += ToggleMenu;
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
        }

        private void RegisterCellClick(MealSlotRefs slot)
        {
            slot.cell.clicked += () =>
            {
                var (dateStr, type) = ((string, string))slot.cell.userData;
                selected = (dateStr, type);
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
            themeToggleBtn.text = isLightTheme ? "☀️" : "🌙";
        }

        private void ToggleMenu()
        {
            menuOpen = !menuOpen;
            Debug.Log($"[MainMenuController] ToggleMenu() pulsado -> menuOpen={menuOpen}");

            if (menuOpen)
            {
                PositionDropdownMenu();
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

        /// <summary>
        /// El menú ya no está anidado dentro del botón hamburguesa (se movió
        /// a ser el último hijo de #root para que se pinte por encima de
        /// todo, ver comentarios en MainMenu.uxml), así que hay que calcular
        /// a mano dónde colocarlo para que quede pegado debajo del botón.
        /// </summary>
        private void PositionDropdownMenu()
        {
            var btnBound = menuToggleBtn.worldBound;
            // OJO: usamos panelRoot (el contenedor real de Unity, que
            // SIEMPRE ocupa toda la pantalla), no pageRoot/"#root" — #root
            // puede acabar con una altura basada en su propio contenido
            // (todas las tarjetas) en vez de la altura real de pantalla,
            // lo que mandaba el menú muy abajo.
            var rootBound = panelRoot.worldBound;

            dropdownMenu.style.position = Position.Absolute;
            dropdownMenu.style.top = (btnBound.yMax - rootBound.yMin) + 8f;
            dropdownMenu.style.right = Mathf.Max(0f, rootBound.xMax - btnBound.xMax);
            dropdownMenu.style.left = StyleKeyword.Auto;
        }

        // ==========================================================
        //  RENDER
        // ==========================================================
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

            // El handler de clic (registrado una sola vez en RegisterCellClick)
            // lee esta userData en el momento del clic.
            slot.cell.userData = (dateStr, type);
        }

        private void RenderEditPanel()
        {
            if (!selected.HasValue)
            {
                editPanel.style.display = DisplayStyle.None;
                return;
            }

            editPanel.style.display = DisplayStyle.Flex;
            var (dateStr, type) = selected.Value;
            data.TryGetValue(Key(dateStr, type), out var entry);
            entry ??= new MealEntry();

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
        }

        // ==========================================================
        //  ACCIONES DEL PANEL DE EDICIÓN
        // ==========================================================
        private void OnSaveClicked()
        {
            if (!selected.HasValue) return;
            var (dateStr, type) = selected.Value;

            string dish = dishField.value?.Trim();
            if (string.IsNullOrEmpty(dish))
            {
                ShowToast("Escribe el nombre del plato");
                return;
            }

            bool remOn = reminderToggle.value;
            var entry = new MealEntry
            {
                dish = dish,
                reminderDate = remOn ? reminderDateField.value : null,
                reminderTime = remOn ? reminderTimeField.value : null,
                reminderTitle = remOn ? reminderTitleField.value?.Trim() : null,
            };

            data[Key(dateStr, type)] = entry;
            // TODO: siguiente paso -> persistir "data" de verdad (Firebase u
            // otro backend) y generar/descargar el .ics del recordatorio,
            // igual que downloadIcs() en el HTML original.

            ShowToast("Guardado");
            Render();
        }

        private void OnDeleteClicked()
        {
            if (!selected.HasValue) return;
            data.Remove(Key(selected.Value.date, selected.Value.type));
            selected = null;
            Render();
        }

        private void OnCloseClicked()
        {
            selected = null;
            Render();
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

        // ==========================================================
        //  TOAST
        // ==========================================================
        private void ShowToast(string message)
        {
            toast.text = message;
            toast.AddToClassList("toast--show");
            toastHideTask?.Pause();
            toastHideTask = toast.schedule.Execute(() => toast.RemoveFromClassList("toast--show")).StartingIn(2200);
        }

        // ==========================================================
        //  UTILIDADES DE FECHA (equivalentes a las del HTML)
        // ==========================================================
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
    }
}