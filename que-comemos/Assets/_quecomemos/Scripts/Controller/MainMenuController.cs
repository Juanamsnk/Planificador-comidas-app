using System;
using System.Collections.Generic;
using QueComemos.Data;
using UnityEngine;
using UnityEngine.UIElements;
using QueComemos.Notifications;

namespace QueComemos.UI
{
    /// <summary>
    /// Controlador de la pantalla principal "Qué comemos".
    /// 
    /// El comportamiento está dividido en varios archivos mediante partial class.
    /// Solo este componente debe estar añadido al GameObject.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public partial class MainMenuController : MonoBehaviour
    {
        [Header("Configuración")]

        [Tooltip("URL base de la web (sin barra final), para construir los enlaces para compartir.")]
        [SerializeField]
        private string webBaseUrl = "https://keen-haupia-6b85c0.netlify.app";

        [Tooltip("Asset Components/CalendarMenuRow.uxml usado para crear filas de calendarios compartidos.")]
        [SerializeField]
        private VisualTreeAsset calendarMenuRowTemplate;

        private static readonly string[] DayNames =
        {
            "Lunes",
            "Martes",
            "Miércoles",
            "Jueves",
            "Viernes",
            "Sábado",
            "Domingo"
        };

        private static readonly string[] Months =
        {
            "enero",
            "febrero",
            "marzo",
            "abril",
            "mayo",
            "junio",
            "julio",
            "agosto",
            "septiembre",
            "octubre",
            "noviembre",
            "diciembre"
        };

        #region Clases auxiliares

        private class DayCardRefs
        {
            public VisualElement card;
            public VisualElement head;
            public Label dayName;
            public Label dayNum;

            public MealSlotRefs comida;
            public MealSlotRefs cena;

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

        #endregion

        #region UIDocument

        private UIDocument document;
        private VisualElement panelRoot;
        private VisualElement pageRoot;

        #endregion

        #region Header

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

        #endregion

        #region Semana

        private ScrollView weekScroll;

        private Button prevWeekBtn;
        private Button nextWeekBtn;
        private Button todayBtn;
        private Label weekRangeLabel;

        private readonly List<DayCardRefs> dayCards =
            new List<DayCardRefs>();

        #endregion

        #region Panel de edición

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

        private VisualElement keyboardSpacer;

        #endregion

        #region Toast

        private Label toast;
        private IVisualElementScheduledItem toastHideTask;

        #endregion

        #region Estado

        private readonly Dictionary<string, MealEntryData> data =
            new Dictionary<string, MealEntryData>();

        private int weekOffset;

        private (string date, string type)? selected;

        private bool menuOpen;
        private bool isLightTheme;

        private string lastVisibleDate;
        private bool initialDayScrollDone;

        private IVisualElementScheduledItem keyboardCheckTask;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Debug.Log("[MainMenuController] OnEnable() arrancando...");

            document = GetComponent<UIDocument>();

            if (document == null)
            {
                Debug.LogError(
                    "[MainMenuController] No se encontró UIDocument."
                );

                return;
            }

            panelRoot = document.rootVisualElement;

            if (panelRoot == null)
            {
                Debug.LogError(
                    "[MainMenuController] rootVisualElement es null. " +
                    "Revisa el Panel Settings del UIDocument."
                );

                return;
            }

            pageRoot = panelRoot.Q<VisualElement>("root");

            if (pageRoot == null)
            {
                Debug.LogError(
                    "[MainMenuController] No se encuentra el elemento \"root\". " +
                    "Revisa que el Source Asset del UIDocument sea MainMenu.uxml."
                );

                return;
            }

            CacheReferences();

            if (!ValidateReferences())
            {
                Debug.LogError(
                    "[MainMenuController] Faltan referencias. " +
                    "No se registrarán eventos."
                );

                return;
            }

            RegisterEvents();

            CreateKeyboardSpacer();

            // El estado inicial es tema claro.
            isLightTheme = true;
            ToggleTheme();

            Render();

            ConnectToFirebase();

            UpdateOwnerBadge();

            Debug.Log(
                "[MainMenuController] OnEnable() terminado correctamente."
            );
        }

        private void OnDisable()
        {
            if (panelRoot != null)
            {
                panelRoot.UnregisterCallback<PointerDownEvent>(
                    OnRootPointerDown,
                    TrickleDown.TrickleDown
                );
            }

            keyboardCheckTask?.Pause();
            toastHideTask?.Pause();

            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.UnsubscribeFromMealPlan(
                    HandleMealPlanChanged
                );

                FirebaseManager.Instance.OnError -= HandleFirebaseError;
            }
        }

        #endregion
    }
}