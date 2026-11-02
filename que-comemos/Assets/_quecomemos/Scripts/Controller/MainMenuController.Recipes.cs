using QueComemos.Data;
using QueComemos.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    //MainMenuController.Recipes
    public partial class MainMenuController
    {
        #region Recipes References

        private const int RecipeNameMaxLength = 80;
        private const int RecipeUrlMaxLength = 500;

        private Button footerRecipesBtn;

        private VisualElement recipesPanel;
        private TextField recipeNameInput;
        private TextField recipeLinkInput;
        private Button recipeSaveBtn;
        private VisualElement recipesItemsContainer;
        private Label recipesEmptyLabel;

        private List<RecipeData> recipes = new List<RecipeData>();
        private bool recipesPanelOpen;
        private bool recipesSubscribed;

        #endregion

        #region Recipes Initialize

        private void CacheRecipesReferences()
        {
            footerRecipesBtn = panelRoot.Q<Button>("footer-recipes-btn");

            // Icono de la barra inferior (Resources/Icons/recipes.png)
            if (footerRecipesBtn != null)
            {
                Texture2D icon = Resources.Load<Texture2D>("Icons/recipes");
                var iconElement = footerRecipesBtn.Q<VisualElement>("footer-btn-icon-recipes");

                if (iconElement != null && icon != null)
                    iconElement.style.backgroundImage = new StyleBackground(icon);
            }

            recipesPanel = panelRoot.Q<VisualElement>("recipes-panel");

            if (recipesPanel == null)
            {
                Debug.LogWarning("[MainMenuController] No se encontró 'recipes-panel'.");
                return;
            }

            recipeNameInput = recipesPanel.Q<TextField>("recipe-name-input");
            recipeLinkInput = recipesPanel.Q<TextField>("recipe-link-input");
            recipeSaveBtn = recipesPanel.Q<Button>("recipe-save-btn");
            recipesItemsContainer = recipesPanel.Q<VisualElement>("recipes-items");
            recipesEmptyLabel = recipesPanel.Q<Label>("recipes-empty");
        }

        private void RegisterRecipesEvents()
        {
            if (footerRecipesBtn != null)
                footerRecipesBtn.clicked += OpenRecipesPanel;

            if (recipeSaveBtn != null)
                recipeSaveBtn.clicked += OnRecipeSaveClicked;

            if (recipeNameInput != null)
                ConfigureAndRegisterTextField(recipeNameInput, true);

            if (recipeLinkInput != null)
            {
                ConfigureAndRegisterTextField(recipeLinkInput, false);
                recipeLinkInput.keyboardType = TouchScreenKeyboardType.URL;

                recipeLinkInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return ||
                        evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnRecipeSaveClicked();
                        evt.StopPropagation();
                    }
                }, TrickleDown.TrickleDown);
            }
        }

        #endregion

        #region Recipes Panel Control

        private void OpenRecipesPanel()
        {
            if (recipesPanel == null)
                return;

            // Cierra los otros paneles superpuestos
            HideShoppingPanel();

            if (navPanel != null)
            {
                navPanelOpen = false;
                navPanel.style.display = DisplayStyle.None;
            }

            recipesPanelOpen = true;
            recipesPanel.style.display = DisplayStyle.Flex;

            UpdateFooterActiveButton("recetas");
            SubscribeRecipes();
        }

        private void HideRecipesPanel()
        {
            if (recipesPanel == null || !recipesPanelOpen)
                return;

            recipesPanelOpen = false;
            recipesPanel.style.display = DisplayStyle.None;

            recipeNameInput?.Blur();
            recipeLinkInput?.Blur();
            UnsubscribeRecipes();
        }

        #endregion

        #region Recipes Firebase

        private void SubscribeRecipes()
        {
            if (recipesSubscribed)
                return;

            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            firebase.SubscribeToRecipes(HandleRecipesChanged);
            recipesSubscribed = true;
        }

        // Llamar también desde OnDisable()
        private void UnsubscribeRecipes()
        {
            if (!recipesSubscribed)
                return;

            FirebaseManager.Instance?.UnsubscribeFromRecipes(HandleRecipesChanged);
            recipesSubscribed = false;
        }

        private void HandleRecipesChanged(List<RecipeData> items)
        {
            recipes = items ?? new List<RecipeData>();
            RenderRecipes();
        }

        #endregion

        #region Recipes Actions

        private void OnRecipeSaveClicked()
        {
            string name = recipeNameInput?.value?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                ShowToast(Loc.T("toast.recipeNameRequired"));
                return;
            }

            if (!TryNormalizeRecipeUrl(recipeLinkInput?.value, out string url))
            {
                ShowToast(Loc.T("toast.recipeLinkInvalid"));
                return;
            }

            if (name.Length > RecipeNameMaxLength)
                name = name.Substring(0, RecipeNameMaxLength);

            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            _ = firebase.AddRecipeAsync(name, url);

            recipeNameInput.value = "";
            recipeLinkInput.value = "";
            recipeNameInput.Blur();
            recipeLinkInput.Blur();

            ShowToast(Loc.T("toast.recipeSaved"));
        }

        private void OpenRecipe(string rawUrl)
        {
            // Se vuelve a validar al abrir: solo se abren enlaces http/https.
            if (TryNormalizeRecipeUrl(rawUrl, out string url))
            {
                // En Android, los enlaces de YouTube se abren en la app de YouTube
                // y el resto en el navegador.
                Application.OpenURL(url);
            }
            else
            {
                ShowToast(Loc.T("toast.recipeLinkInvalid"));
            }
        }

        private void RemoveRecipe(string id)
        {
            _ = FirebaseManager.Instance?.RemoveRecipeAsync(id);
        }

        /// <summary>
        /// Acepta "youtube.com/..." sin esquema (añade https://) y solo permite http/https.
        /// </summary>
        private static bool TryNormalizeRecipeUrl(string raw, out string url)
        {
            url = null;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            raw = raw.Trim();

            if (raw.Length > RecipeUrlMaxLength)
                return false;

            if (raw.IndexOf("://", StringComparison.Ordinal) < 0)
                raw = "https://" + raw;

            if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            // Descarta cosas como "https://hola" (sin dominio).
            if (string.IsNullOrEmpty(uri.Host) || uri.Host.IndexOf('.') < 0)
                return false;

            url = raw;
            return true;
        }

        private static string RecipeHost(string rawUrl)
        {
            if (!TryNormalizeRecipeUrl(rawUrl, out string url))
                return rawUrl;

            string host = new Uri(url).Host;

            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? host.Substring(4)
                : host;
        }

        #endregion

        #region Recipes Render

        private void RenderRecipes()
        {
            if (recipesItemsContainer == null)
                return;

            recipesItemsContainer.Clear();

            foreach (var recipe in recipes)
                recipesItemsContainer.Add(CreateRecipeRow(recipe));

            if (recipesEmptyLabel != null)
            {
                recipesEmptyLabel.style.display =
                    recipes.Count == 0
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
        }

        private VisualElement CreateRecipeRow(RecipeData recipe)
        {
            string id = recipe.id;
            string url = recipe.url;

            var row = new VisualElement();
            row.AddToClassList("shopping-row");
            row.AddToClassList("recipe-row");

            // Zona pulsable: nombre + dominio + flecha. Se usa ClickEvent (no Button)
            // para que arrastrar la lista haga scroll sin abrir la receta.
            var openArea = new VisualElement();
            openArea.AddToClassList("recipe-open-area");
            openArea.RegisterCallback<ClickEvent>(evt => OpenRecipe(url));

            var textColumn = new VisualElement();
            textColumn.AddToClassList("recipe-text-col");
            textColumn.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(recipe.name);
            nameLabel.AddToClassList("recipe-name");
            nameLabel.pickingMode = PickingMode.Ignore;

            var hostLabel = new Label(RecipeHost(url));
            hostLabel.AddToClassList("recipe-host");
            hostLabel.pickingMode = PickingMode.Ignore;

            textColumn.Add(nameLabel);
            textColumn.Add(hostLabel);

            var chevron = new Label("›");
            chevron.AddToClassList("recipe-chevron");
            chevron.pickingMode = PickingMode.Ignore;

            openArea.Add(textColumn);
            openArea.Add(chevron);

            var delete = new Button(() => RemoveRecipe(id)) { text = "×" };
            delete.AddToClassList("shopping-delete-btn");

            row.Add(openArea);
            row.Add(delete);

            return row;
        }

        #endregion
    }
}
