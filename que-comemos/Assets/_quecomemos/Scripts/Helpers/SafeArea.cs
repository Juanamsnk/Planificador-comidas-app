using UnityEngine;
using UnityEngine.UIElements;

public class SafeArea : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float footerBaseHeight = 64f;   // Debe coincidir con .footer en USS
    [SerializeField] private float bannerExtraMargin = 4f;   // "Un poquito más" que el banner (unidades del panel)

    private VisualElement safeArea;
    private VisualElement navPanel;
    private VisualElement shoppingPanel;
    private VisualElement footer;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;
    private float bannerHeightPx;


    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        bannerHeightPx = AdsManager.BannerHeightPx;
        AdsManager.OnBannerHeightChanged += HandleBannerHeightChanged;

        if (uiDocument == null)
            return;

        var root = uiDocument.rootVisualElement;
        safeArea = root.Q<VisualElement>("SafeArea");
        navPanel = root.Q<VisualElement>("nav-panel");
        shoppingPanel = root.Q<VisualElement>("shopping-panel");
        footer = root.Q<VisualElement>(className: "footer");

        if (safeArea == null)
        {
            Debug.LogError("[SafeArea] No se encontró el elemento 'SafeArea'.");
            return;
        }

        lastScreenSize = Vector2.zero;
        lastSafeArea = Rect.zero;
    }

    private void OnDisable()
    {
        AdsManager.OnBannerHeightChanged -= HandleBannerHeightChanged;
    }

    private void HandleBannerHeightChanged(float px)
    {
        bannerHeightPx = px;
        lastSafeArea = Rect.zero; // fuerza reaplicar en el siguiente Update
    }

    private void Update()
    {
        if (safeArea == null)
            return;

        if (lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (safeArea.panel == null)
            return;

        Rect safe = Screen.safeArea;

        float panelWidth = safeArea.panel.visualTree.resolvedStyle.width;
        float panelHeight = safeArea.panel.visualTree.resolvedStyle.height;

        if (panelWidth <= 0 || panelHeight <= 0)
            return;

        float scaleX = panelWidth / Screen.width;
        float scaleY = panelHeight / Screen.height;

        float left = safe.x * scaleX;
        float right = (Screen.width - safe.xMax) * scaleX;
        float bottom = safe.y * scaleY;
        float top = (Screen.height - safe.yMax) * scaleY;

        // Banner: píxeles de pantalla -> unidades del panel (+ un pequeño margen)
        float banner = bannerHeightPx > 0f ? bannerHeightPx * scaleY + bannerExtraMargin : 0f;
        float topTotal = top + banner;

        safeArea.style.paddingLeft = left;
        safeArea.style.paddingRight = right;
        safeArea.style.paddingTop = topTotal;
        safeArea.style.paddingBottom = bottom;

        if (footer != null)
        {
            footer.style.height = footerBaseHeight + bottom;
            footer.style.paddingBottom = bottom;
        }

        if (navPanel != null)
        {
            ApplyOverlayInsets(navPanel, left, right, topTotal, bottom);
            ApplyOverlayInsets(shoppingPanel, left, right, topTotal, bottom);
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = safe;
    }

    private void ApplyOverlayInsets(VisualElement panel, float left, float right, float top, float bottom)
    {
        if (panel == null)
            return;

        panel.style.paddingLeft = left;
        panel.style.paddingRight = right;
        panel.style.paddingTop = top;
        panel.style.bottom = footerBaseHeight + bottom;
    }
}