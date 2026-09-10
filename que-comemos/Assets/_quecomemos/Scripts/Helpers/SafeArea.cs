using UnityEngine;
using UnityEngine.UIElements;

public class SafeArea : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement safeArea;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null)
            return;

        safeArea = uiDocument.rootVisualElement.Q<VisualElement>("SafeArea");

        if (safeArea == null)
        {
            Debug.LogError("[SafeArea] No se encontró el elemento 'SafeArea'.");
            return;
        }

        ApplySafeArea();
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

        safeArea.style.paddingLeft = left;
        safeArea.style.paddingRight = right;
        safeArea.style.paddingTop = top;
        safeArea.style.paddingBottom = bottom;

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastSafeArea = safe;
    }
}