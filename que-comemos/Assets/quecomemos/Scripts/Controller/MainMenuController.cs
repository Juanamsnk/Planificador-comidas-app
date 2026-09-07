using System;
using System.Collections.Generic;
using System.Linq;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    /// <summary>
    /// Controlador de la pantalla principal "Qué comemos".
    /// Replica la lógica de render()/renderEditPanel() del HTML original.
    /// Los platos ahora viven en Firebase Realtime Database (a través de
    /// FirebaseManager, mismo esquema que la web) — "data" es una copia
    /// local que se actualiza cada vez que llega un cambio de Firebase, y
    /// se reenvía entera a Firebase cada vez que se guarda o borra algo
    /// (igual que hace saveData() en la web).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        // Nombres de los días en el orden "lunes primero" (índice 0 = lunes),
        // para no depender del orden que usa .NET internamente (domingo = 0).
        private static readonly string[] DayNames =
            { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        // Nombres de los meses en español, para no depender de la configuración
        // regional (CultureInfo) del dispositivo donde corra la app.
        private static readonly string[] Months =
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };

        // ------------------------------------------------------------
        // Estas dos clases NO son datos de Firebase: son solo "carpetas"
        // para agrupar referencias a elementos visuales de una tarjeta de
        // día / de un hueco de comida-cena, y así no tener 6 variables
        // sueltas por cada tarjeta.
        // ------------------------------------------------------------
        private class DayCardRefs
        {
            public VisualElement card;       // el contenedor con la clase "day-card"
            public VisualElement head;       // la cabecera con el nombre/número del día
            public Label dayName;            // "Lunes"
            public Label dayNum;             // "8 sep"
            public MealSlotRefs comida;      // referencias del hueco de comida
            public MealSlotRefs cena;        // referencias del hueco de cena
        }

        private class MealSlotRefs
        {
            public Button cell;              // el botón clicable de la celda entera
            public Label emptyLabel;         // texto "+ añadir" (se ve si no hay plato)
            public VisualElement filled;     // contenedor que se ve si SÍ hay plato
            public Label dishLabel;          // nombre del plato guardado
            public Label metaLabel;          // texto pequeño tipo "aviso 12:00"
        }

        private UIDocument document;
        private VisualElement panelRoot; // rootVisualElement del UIDocument (toda la pantalla)
        private VisualElement pageRoot;  // elemento "#root" (el que lleva theme-dark/theme-light)

        // ---------- Referencias de la cabecera ----------
        private Button themeToggleBtn;   // botón sol/luna
        private VisualElement menuWrap;  // contenedor del botón hamburguesa + su menú
        private Button menuToggleBtn;    // botón ☰
        private VisualElement dropdownMenu; // el panel desplegable en sí

        // ---------- Referencias de la navegación de semana ----------
        private Button prevWeekBtn;
        private Button nextWeekBtn;
        private Button todayBtn;         // solo visible si no estás en la semana actual
        private Label weekRangeLabel;    // texto "1 – 7 de septiembre 2026"

        // Referencias de las 7 tarjetas de día (índice 0 = lunes .. 6 = domingo)
        private readonly List<DayCardRefs> dayCards = new List<DayCardRefs>();

        // ---------- Referencias del panel de edición de un plato ----------
        private VisualElement editPanel;
        private Label editPanelTitle;        // "Comida · Lunes 8 de septiembre"
        private TextField dishField;         // campo de texto del nombre del plato
        private Button randomDishBtn;        // "🎲 Plato random"
        private Button oldDishBtn;           // "⏳ Hace tiempo que no..."
        private Toggle reminderToggle;       // checkbox de "Recordatorio"
        private TextField reminderDateField; // fecha del recordatorio (texto libre "AAAA-MM-DD")
        private TextField reminderTimeField; // hora del recordatorio (texto libre "HH:MM")
        private TextField reminderTitleField;// nombre personalizado del recordatorio
        private Button saveBtn;
        private Button deleteBtn;
        private Button closeBtn;

        // ---------- Referencias del aviso flotante ----------
        private Label toast;
        // Guarda la tarea programada que oculta el toast, para poder
        // cancelarla si se muestra un segundo toast antes de que
        // desaparezca el primero (evita que "parpadee" o se corte a medias).
        private IVisualElementScheduledItem toastHideTask;

        // ==========================================================
        //  ESTADO — equivalente a las variables globales del HTML/JS
        // ==========================================================
        // Diccionario en memoria: clave "2026-09-08|comida" -> datos del plato.
        // Es la copia local de lo que hay en Firebase; se sobrescribe entera
        // cada vez que llega un cambio remoto (ver HandleMealPlanChanged).
        private readonly Dictionary<string, MealEntryData> data = new Dictionary<string, MealEntryData>();
        private int weekOffset;                 // 0 = semana actual, 1 = siguiente, -1 = anterior...
        private (string date, string type)? selected; // qué celda está abierta en el panel de edición (o null si ninguna)
        private bool menuOpen;                  // si el menú hamburguesa está desplegado
        private bool isLightTheme;              // si el tema actual es el claro

        /// <summary>
        /// Punto de entrada al activarse el componente: localiza el
        /// UIDocument y el elemento raíz, cachea todas las referencias de
        /// la UI, valida que no falte ninguna, engancha los eventos de
        /// clic y pinta la pantalla por primera vez. Si algo esencial no
        /// se encuentra (UIDocument sin Panel Settings, UXML equivocado),
        /// corta la ejecución con un error claro en vez de seguir y
        /// petar más adelante con un NullReferenceException confuso.
        /// </summary>
        private void OnEnable()
        {
            Debug.Log("[MainMenuController] OnEnable() arrancando...");

            document = GetComponent<UIDocument>();
            panelRoot = document.rootVisualElement;
            if (panelRoot == null)
            {
                // Esto pasa si el UIDocument no tiene un "Panel Settings"
                // asignado en el Inspector: sin eso, Unity no genera el
                // árbol visual y rootVisualElement se queda en null.
                Debug.LogError("[MainMenuController] rootVisualElement es null: " +
                    "revisa que el UIDocument tenga un Panel Settings asignado.");
                return;
            }

            pageRoot = panelRoot.Q<VisualElement>("root");
            if (pageRoot == null)
            {
                // Esto pasa si el "Source Asset" del UIDocument apunta a
                // otro UXML distinto de MainMenu.uxml (o si el nombre
                // "root" se cambió en el UXML sin actualizar este script).
                Debug.LogError("[MainMenuController] No se encuentra el elemento \"root\": " +
                    "revisa que el Source Asset del UIDocument sea MainMenu.uxml.");
                return;
            }

            CacheReferences();

            if (!ValidateReferences())
            {
                // Preferible parar aquí a que, por ejemplo, saveBtn sea
                // null y la app pete al primer clic sin explicación.
                Debug.LogError("[MainMenuController] Faltan referencias (ver errores arriba). " +
                    "No se registran eventos ni se renderiza para evitar más excepciones.");
                return;
            }

            RegisterEvents();
            Render();

            Debug.Log("[MainMenuController] OnEnable() terminado correctamente.");
        }

        /// <summary>
        /// Se conecta a Firebase DESPUÉS de OnEnable (en Start), para
        /// garantizar que toda la UI ya está montada y lista antes de que
        /// empiecen a llegar datos remotos que intenten pintarse sobre ella.
        /// </summary>
        public void Start()
        {
            ConnectToFirebase();
        }

        /// <summary>
        /// Se suscribe a los cambios del calendario en Firebase (carga
        /// inicial + tiempo real, igual que mealsRef.on('value', ...) en la
        /// web). Si FirebaseManager todavía no está en la escena o no ha
        /// terminado de inicializarse, no falla: sencillamente la app sigue
        /// funcionando solo en memoria hasta que esté disponible.
        /// </summary>
        private void ConnectToFirebase()
        {
            if (FirebaseManager.Instance == null)
            {
                Debug.LogWarning("[MainMenuController] No hay FirebaseManager en la escena: " +
                    "los platos no se guardan, solo viven en memoria.");
                return;
            }

            // Nos apuntamos a dos eventos: uno para cuando cambian los
            // datos (llegada inicial o cambio remoto) y otro para errores
            // de red/permisos, que se muestran como un toast al usuario.
            FirebaseManager.Instance.OnMealPlanChanged += HandleMealPlanChanged;
            FirebaseManager.Instance.OnError += HandleFirebaseError;
        }

        /// <summary>
        /// Se ejecuta cada vez que Firebase manda una versión nueva del
        /// calendario completo. Sustituye el diccionario local entero por
        /// el que ha llegado (no hace un "merge" campo a campo) y vuelve
        /// a pintar toda la pantalla.
        /// </summary>
        private void HandleMealPlanChanged(Dictionary<string, MealEntryData> firebaseData)
        {
            data.Clear();
            foreach (var kv in firebaseData) data[kv.Key] = kv.Value;
            Render();
        }

        /// <summary>Muestra cualquier error de Firebase como un aviso flotante.</summary>
        private void HandleFirebaseError(string message)
        {
            ShowToast(message);
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
            // El "&=" (y no "=") es a propósito: así se revisan TODAS las
            // referencias y se ve en consola la lista completa de lo que
            // falta, en vez de cortar en el primer error encontrado.
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

            // Además, recorre las 7 tarjetas de día para comprobar que
            // cada una se resolvió bien (por si el UXML tiene algún
            // "day-card-N" mal escrito o le falta algún hijo).
            for (int i = 0; i < dayCards.Count; i++)
            {
                var d = dayCards[i];
                ok &= LogIfNull(d.card, $"day-card (instancia {i})");
                ok &= LogIfNull(d.comida?.cell, $"cell-button comida (día {i})");
                ok &= LogIfNull(d.cena?.cell, $"cell-button cena (día {i})");
            }

            return ok;
        }

        /// <summary>Si "obj" es null, escribe un error con el nombre indicado y devuelve false; si no, devuelve true.</summary>
        private static bool LogIfNull(object obj, string label)
        {
            if (obj != null) return true;
            Debug.LogError($"[MainMenuController] No se encontró el elemento \"{label}\". " +
                "Revisa que el nombre en el UXML coincida exactamente.");
            return false;
        }

        /// <summary>
        /// Limpieza al desactivarse el componente: quita el listener de
        /// clic global (el que detecta "clic fuera del menú") y se
        /// desuscribe de Firebase, para no dejar referencias colgando ni
        /// que se sigan disparando eventos sobre un objeto ya desactivado.
        /// </summary>
        private void OnDisable()
        {
            panelRoot?.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnMealPlanChanged -= HandleMealPlanChanged;
                FirebaseManager.Instance.OnError -= HandleFirebaseError;
            }
        }

        // ==========================================================
        //  CACHEAR REFERENCIAS
        // ==========================================================

        /// <summary>
        /// Busca UNA sola vez (al arrancar) todos los elementos de la UI
        /// por su nombre en el UXML y los guarda en las variables de la
        /// clase. Así, el resto del script no vuelve a llamar a Q&lt;T&gt;
        /// (que recorre el árbol visual) en cada frame o en cada clic.
        /// </summary>
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
                    // true/false aquí decide si ese hueco se pinta como
                    // "comida" (punto dorado) o "cena" (punto lila).
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

            // La línea del recordatorio (checkbox + fecha + hora) se busca
            // dentro de su propio contenedor "reminder-line", no del
            // editPanel entero, para evitar ambigüedades si hubiera otro
            // TextField con nombre parecido en el resto del panel.
            var reminderLine = editPanel.Q<VisualElement>("reminder-line");
            reminderToggle = reminderLine.Q<Toggle>("reminder-toggle");
            reminderDateField = reminderLine.Q<TextField>("reminder-date-field");
            reminderTimeField = reminderLine.Q<TextField>("reminder-time-field");
            reminderTitleField = editPanel.Q<TextField>("reminder-title-field");

            toast = panelRoot.Q<Label>("toast");
        }

        /// <summary>
        /// A partir del contenedor de un hueco de comida/cena, busca sus
        /// piezas internas (punto de color, etiqueta "Comida"/"Cena",
        /// botón de la celda, y los sub-elementos de dentro del botón).
        /// También deja fijado aquí, de una vez, si el punto es dorado
        /// (comida) o lila (cena) y qué texto lleva la etiqueta — eso no
        /// cambia nunca en tiempo de ejecución, así que no hace falta
        /// tocarlo en cada Render().
        /// </summary>
        private MealSlotRefs BuildMealSlotRefs(VisualElement slotContainer, bool isComida)
        {
            var dot = slotContainer.Q<VisualElement>("meal-dot");
            var tagLabel = slotContainer.Q<Label>("meal-tag-label");

            // Se quitan las dos clases posibles y se añade solo la que
            // corresponde, por si el UXML ya traía una puesta por defecto.
            dot.RemoveFromClassList("meal-dot--comida");
            dot.RemoveFromClassList("meal-dot--cena");
            dot.AddToClassList(isComida ? "meal-dot--comida" : "meal-dot--cena");
            tagLabel.text = isComida ? "Comida" : "Cena";

            var cellButton = slotContainer.Q<Button>("cell-button");
            return new MealSlotRefs
            {
                cell = cellButton,
                // Estos tres se buscan DENTRO del botón: son sub-elementos
                // que se muestran/ocultan según si el plato tiene contenido o no.
                emptyLabel = cellButton.Q<Label>("cell-empty-label"),
                filled = cellButton.Q<VisualElement>("cell-filled"),
                dishLabel = cellButton.Q<Label>("cell-dish-label"),
                metaLabel = cellButton.Q<Label>("cell-meta-label"),
            };
        }

        // ==========================================================
        //  EVENTOS
        // ==========================================================

        /// <summary>
        /// Engancha cada botón/celda a su acción correspondiente. Se llama
        /// una sola vez, después de CacheReferences(), para no volver a
        /// suscribir el mismo evento varias veces (lo cual dispararía la
        /// acción repetida por cada clic).
        /// </summary>
        private void RegisterEvents()
        {
            themeToggleBtn.clicked += ToggleTheme;
            menuToggleBtn.clicked += ToggleMenu;
            // TrickleDown.TrickleDown: se engancha en la fase de "bajada"
            // del evento (antes de que llegue al elemento pulsado), para
            // poder detectar CUALQUIER clic en la pantalla, incluso fuera
            // del menú, y así cerrarlo si corresponde.
            panelRoot.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

            prevWeekBtn.clicked += () => { weekOffset--; Render(); };
            nextWeekBtn.clicked += () => { weekOffset++; Render(); };
            todayBtn.clicked += () => { weekOffset = 0; Render(); };

            // Las 7 tarjetas de día comparten la misma lógica de clic,
            // tanto para su hueco de comida como el de cena.
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

        /// <summary>
        /// Conecta el clic de una celda (comida o cena) para que abra el
        /// panel de edición con la fecha/tipo correctos. La fecha y el
        /// tipo no se vuelven a calcular aquí: se leen de "userData", que
        /// RenderCell() actualiza cada vez que se repinta la semana.
        /// </summary>
        private void RegisterCellClick(MealSlotRefs slot)
        {
            slot.cell.clicked += () =>
            {
                var (dateStr, type) = ((string, string))slot.cell.userData;
                selected = (dateStr, type);
                RenderWeekGrid();   // para resaltar la celda seleccionada
                RenderEditPanel();  // para mostrar/rellenar el panel
            };
        }

        /// <summary>
        /// Detecta cualquier toque en la pantalla; si el menú está abierto
        /// y el toque NO fue ni sobre el botón hamburguesa ni sobre el
        /// propio desplegable, lo cierra. Es el equivalente al
        /// "document.addEventListener('click', ...)" que cierra el menú
        /// al hacer clic fuera, en la versión web.
        /// </summary>
        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!menuOpen) return;
            if (menuWrap.worldBound.Contains(evt.position)) return;
            if (dropdownMenu.worldBound.Contains(evt.position)) return;
            menuOpen = false;
            dropdownMenu.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Cambia entre tema claro/oscuro: quita la clase del tema
        /// contrario y pone la nueva en el elemento raíz "#root" (de ahí
        /// cuelgan todas las variables de color, ver Theme.uss), y
        /// actualiza el icono del botón (sol/luna).
        /// </summary>
        private void ToggleTheme()
        {
            isLightTheme = !isLightTheme;
            pageRoot.RemoveFromClassList(isLightTheme ? "theme-dark" : "theme-light");
            pageRoot.AddToClassList(isLightTheme ? "theme-light" : "theme-dark");
            themeToggleBtn.text = isLightTheme ? "☀️" : "🌙";
        }

        /// <summary>
        /// Abre o cierra el menú hamburguesa. Al abrirlo, primero calcula
        /// dónde debe colocarse (PositionDropdownMenu) y después lo hace
        /// visible; los logs de después sirven para depurar si el menú
        /// aparece en un sitio raro (se comprueba su posición ya calculada
        /// por Unity, 50 ms después, cuando el layout ya se ha aplicado).
        /// </summary>
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
                // "schedule.Execute(...).ExecuteLater(50)" espera 50 ms
                // para dar tiempo a que Unity recalcule el layout antes
                // de leer worldBound (si se lee inmediatamente después de
                // cambiar "display", el valor podría no estar actualizado).
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
            // Top = borde inferior del botón, menos el borde superior de
            // la pantalla (para pasar de coordenadas "de pantalla" a
            // coordenadas "relativas al panel"), más 8px de margen.
            dropdownMenu.style.top = (btnBound.yMax - rootBound.yMin) + 8f;
            // Right = distancia desde el borde derecho de la pantalla
            // hasta el borde derecho del botón (Mathf.Max evita valores
            // negativos si por lo que sea el botón sobresaliera).
            dropdownMenu.style.right = Mathf.Max(0f, rootBound.xMax - btnBound.xMax);
            // Se anula cualquier "left" que pudiera venir puesto en el
            // USS, para que "right" sea el único que mande.
            dropdownMenu.style.left = StyleKeyword.Auto;
        }

        // ==========================================================
        //  RENDER
        // ==========================================================

        /// <summary>Repinta la pantalla entera: barra de semana, las 7 tarjetas, y el panel de edición.</summary>
        private void Render()
        {
            RenderWeekNav();
            RenderWeekGrid();
            RenderEditPanel();
        }

        /// <summary>
        /// Actualiza el texto del rango de fechas ("1 – 7 de septiembre
        /// 2026") y decide si se muestra el botón "Esta semana" (solo si
        /// no estás viendo la semana actual).
        /// </summary>
        private void RenderWeekNav()
        {
            var dates = WeekDates(weekOffset);
            var first = dates[0];
            var last = dates[6];
            // Si la semana no cruza de un mes a otro, se muestra un solo
            // nombre de mes; si lo cruza, se muestran los dos.
            string rangeLabel = first.Month == last.Month
                ? $"{first.Day} – {last.Day} de {Months[first.Month - 1]}"
                : $"{first.Day} {Months[first.Month - 1]} – {last.Day} {Months[last.Month - 1]}";

            weekRangeLabel.text = $"{rangeLabel} {first.Year}";
            todayBtn.style.display = weekOffset != 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Recalcula las 7 fechas de la semana actual (según weekOffset) y
        /// actualiza cada una de las 7 tarjetas: nombre/número de día,
        /// si es "hoy" (para el borde verde), y el contenido de sus dos
        /// celdas (comida/cena).
        /// </summary>
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
                // Substring(0,3): solo las 3 primeras letras del mes ("sep").
                refs.dayNum.text = $"{d.Day} {Months[d.Month - 1].Substring(0, 3)}";

                // Se quitan las clases de "hoy" antes de decidir si se
                // vuelven a poner, para que al cambiar de semana no se
                // quede marcado como "hoy" un día que ya no lo es.
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

        /// <summary>
        /// Pinta una celda concreta (comida o cena de un día): si hay
        /// plato guardado, muestra su nombre y el texto de aviso; si no,
        /// muestra el placeholder "+ añadir". También marca la celda como
        /// "seleccionada" si es la que está abierta en el panel de edición.
        /// </summary>
        private void RenderCell(MealSlotRefs slot, string dateStr, string type)
        {
            data.TryGetValue(Key(dateStr, type), out var entry);
            bool hasDish = entry != null && !string.IsNullOrEmpty(entry.dish);
            bool isSelected = selected.HasValue && selected.Value.date == dateStr && selected.Value.type == type;

            slot.cell.RemoveFromClassList("cell-btn--selected");
            if (isSelected) slot.cell.AddToClassList("cell-btn--selected");

            // Se alterna la visibilidad entre el estado "vacío" y el
            // "relleno" en vez de cambiar el texto de un único Label,
            // porque cada estado tiene su propio layout (el relleno
            // incluye el texto pequeño de "aviso").
            slot.emptyLabel.style.display = hasDish ? DisplayStyle.None : DisplayStyle.Flex;
            slot.filled.style.display = hasDish ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasDish)
            {
                slot.dishLabel.text = entry.dish;
                var metaBits = new List<string>();
                if (!string.IsNullOrEmpty(entry.reminderDate) && !string.IsNullOrEmpty(entry.reminderTime))
                    metaBits.Add("aviso " + entry.reminderTime);
                // Con una lista + Join, es fácil añadir más "bits" de
                // información en el futuro sin tocar el formato entero.
                slot.metaLabel.text = string.Join(" · ", metaBits);
            }

            // El handler de clic (registrado una sola vez en RegisterCellClick)
            // lee esta userData en el momento del clic. Se actualiza aquí
            // porque la fecha/tipo de cada celda puede cambiar al navegar
            // de semana (la misma celda física pasa a representar otro día).
            slot.cell.userData = (dateStr, type);
        }

        /// <summary>
        /// Muestra u oculta el panel de edición según si hay algo
        /// seleccionado, y si lo hay, rellena todos sus campos con los
        /// datos existentes (o con los valores por defecto si es un plato
        /// nuevo: fecha del recordatorio = el día anterior, hora = 12:00).
        /// </summary>
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
            // Si no existía ninguna entrada (plato nuevo), se usa un
            // objeto vacío en su lugar, para no tener que comprobar null
            // en cada línea de abajo.
            entry ??= new MealEntryData();

            var d = DateTime.Parse(dateStr);
            string dayLabel = $"{DayNames[IsoWeekdayIndex(d)]} {d.Day} de {Months[d.Month - 1]}";
            editPanelTitle.text = $"{(type == "comida" ? "Comida" : "Cena")} · {dayLabel}";

            // SetValueWithoutNotify: se usa para no disparar el evento
            // "onValueChanged" del campo, que aquí no hace falta (no hay
            // ningún listener enganchado a esos eventos) y así se evita
            // cualquier efecto secundario inesperado al rellenar valores.
            dishField.SetValueWithoutNotify(entry.dish ?? "");

            var (defDate, defTime) = DefaultReminder(dateStr);
            bool hasReminder = !string.IsNullOrEmpty(entry.reminderDate);
            reminderToggle.SetValueWithoutNotify(hasReminder);
            reminderDateField.SetValueWithoutNotify(entry.reminderDate ?? defDate);
            reminderTimeField.SetValueWithoutNotify(entry.reminderTime ?? defTime);
            reminderTitleField.SetValueWithoutNotify(entry.reminderTitle ?? "");

            // El botón "Eliminar" no tiene sentido si el plato todavía no
            // existe (no hay nada que borrar), así que se oculta.
            deleteBtn.style.display = string.IsNullOrEmpty(entry.dish) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ==========================================================
        //  ACCIONES DEL PANEL DE EDICIÓN
        // ==========================================================

        /// <summary>
        /// Guarda el plato actual: valida que tenga nombre, arma el
        /// objeto con los datos del recordatorio (solo si el checkbox
        /// está activo), lo mete en el diccionario local, lo envía a
        /// Firebase, y vuelve a pintar.
        /// </summary>
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
            var entry = new MealEntryData
            {
                dish = dish,
                // Si el recordatorio está desactivado, se guarda como null
                // en los tres campos (no se guarda basura "por si acaso").
                reminderDate = remOn ? reminderDateField.value : null,
                reminderTime = remOn ? reminderTimeField.value : null,
                reminderTitle = remOn ? reminderTitleField.value?.Trim() : null,
            };

            data[Key(dateStr, type)] = entry;
            PersistData();
            // TODO: siguiente paso -> generar/descargar (o programar) el
            // recordatorio de verdad, igual que downloadIcs() en el HTML
            // original. El guardado del plato en sí ya va a Firebase.

            ShowToast("Guardado");
            Render();
        }

        /// <summary>Borra el plato seleccionado del diccionario local, lo sincroniza con Firebase, cierra el panel y repinta.</summary>
        private void OnDeleteClicked()
        {
            if (!selected.HasValue) return;
            data.Remove(Key(selected.Value.date, selected.Value.type));
            PersistData();
            selected = null;
            Render();
        }

        /// <summary>
        /// Reenvía el mapa ENTERO de platos a Firebase, igual que hace
        /// saveData() en la web (mealsRef.set(data) completo, no
        /// escrituras parciales). "async void" porque se llama desde un
        /// evento de clic (Button.clicked no soporta async Task
        /// directamente); los errores ya se gestionan dentro de
        /// FirebaseManager y se muestran vía OnError -&gt; ShowToast.
        /// </summary>
        private async void PersistData()
        {
            if (FirebaseManager.Instance == null) return;
            await FirebaseManager.Instance.SaveMealPlanAsync(data);
        }

        /// <summary>Cierra el panel de edición sin guardar ni borrar nada.</summary>
        private void OnCloseClicked()
        {
            selected = null;
            Render();
        }

        /// <summary>Pide un plato aleatorio de otras semanas y lo escribe en el campo de texto (el usuario decide si lo guarda o no).</summary>
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

        /// <summary>Pide el plato que hace más tiempo que no se come y lo escribe en el campo de texto.</summary>
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

        /// <summary>
        /// Reúne los nombres de plato de TODO el historial (data) que no
        /// pertenezcan a la semana que se está viendo ahora mismo, y
        /// devuelve uno al azar. Si no hay ninguno (por ejemplo, recién
        /// empezado el calendario), devuelve null.
        /// </summary>
        private string RandomOtherWeekDish()
        {
            // Conjunto de fechas (en formato "yyyy-MM-dd") de la semana
            // actual, para poder excluirlas con Contains() de forma rápida.
            var excluded = new HashSet<string>(WeekDates(weekOffset).Select(FormatDate));
            var candidates = data
                .Where(kv => !string.IsNullOrEmpty(kv.Value.dish) && !excluded.Contains(kv.Key.Split('|')[0]))
                .Select(kv => kv.Value.dish)
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Para cada plato distinto que aparece en el historial, calcula
        /// la fecha MÁS RECIENTE en la que se comió, y devuelve el plato
        /// cuya fecha más reciente sea, aun así, la más antigua de todas
        /// (es decir: el que hace más tiempo que no se repite).
        /// </summary>
        private string LeastRecentDish()
        {
            var lastSeen = new Dictionary<string, string>();
            foreach (var kv in data)
            {
                if (string.IsNullOrEmpty(kv.Value.dish)) continue;
                string dateStr = kv.Key.Split('|')[0];
                // CompareOrdinal funciona bien aquí porque las fechas están
                // en formato "yyyy-MM-dd": comparar como texto equivale a
                // compararlas cronológicamente.
                if (!lastSeen.TryGetValue(kv.Value.dish, out var prev) || string.CompareOrdinal(dateStr, prev) > 0)
                    lastSeen[kv.Value.dish] = dateStr;
            }
            if (lastSeen.Count == 0) return null;
            // Se ordena por fecha ascendente y se coge el primero: el
            // plato cuya última aparición es la más antigua de todas.
            return lastSeen.OrderBy(kv => kv.Value, StringComparer.Ordinal).First().Key;
        }

        // ==========================================================
        //  TOAST
        // ==========================================================

        /// <summary>
        /// Muestra el aviso flotante con el mensaje indicado durante 2.2
        /// segundos. Si ya había un aviso en curso, se cancela su tarea de
        /// ocultado pendiente (Pause) antes de programar una nueva, para
        /// que dos avisos seguidos no se pisen a medio camino.
        /// </summary>
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

        /// <summary>Convierte una fecha a texto "yyyy-MM-dd", el mismo formato que se usa como clave.</summary>
        private static string FormatDate(DateTime d) => d.ToString("yyyy-MM-dd");

        /// <summary>Construye la clave del diccionario a partir de fecha + tipo, ej. "2026-09-08|comida".</summary>
        private static string Key(string dateStr, string type) => dateStr + "|" + type;

        /// <summary>
        /// Traduce el DayOfWeek de .NET (donde domingo = 0, lunes = 1...)
        /// a un índice donde lunes = 0 y domingo = 6, para que encaje con
        /// el array DayNames de arriba.
        /// </summary>
        private static int IsoWeekdayIndex(DateTime d)
        {
            int day = (int)d.DayOfWeek; // domingo = 0
            return day == 0 ? 6 : day - 1;
        }

        /// <summary>Calcula la fecha del lunes de la semana en la que estamos hoy.</summary>
        private static DateTime GetMondayOfCurrentWeek()
        {
            var d = DateTime.Today;
            int day = (int)d.DayOfWeek;
            int diff = day == 0 ? -6 : 1 - day;
            return d.AddDays(diff);
        }

        /// <summary>
        /// Devuelve las 7 fechas (lunes a domingo) de la semana indicada
        /// por "offset" (0 = actual, 1 = la siguiente, -1 = la anterior...).
        /// </summary>
        private static List<DateTime> WeekDates(int offset)
        {
            var monday = GetMondayOfCurrentWeek().AddDays(offset * 7);
            var list = new List<DateTime>();
            for (int i = 0; i < 7; i++) list.Add(monday.AddDays(i));
            return list;
        }

        /// <summary>Calcula el recordatorio por defecto para una fecha: el día antes, a las 12:00.</summary>
        private static (string date, string time) DefaultReminder(string dateStr)
        {
            var d = DateTime.Parse(dateStr).AddDays(-1);
            return (FormatDate(d), "12:00");
        }
    }
}