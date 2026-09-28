using QueComemos.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    //MainMenuController.ShoppingList
    public partial class MainMenuController
    {
        #region Shopping References

        private const int ShoppingItemMaxLength = 80;

        private VisualElement shoppingPanel;
        private TextField shoppingInput;
        private Button shoppingAddBtn;
        private VisualElement shoppingItemsContainer;
        private Label shoppingEmptyLabel;
        private Button shoppingClearDoneBtn;

        private List<ShoppingItemData> shoppingItems = new List<ShoppingItemData>();
        private bool shoppingPanelOpen;
        private bool shoppingSubscribed;

        #endregion

        #region Shopping Initialize

        private void CacheShoppingReferences()
        {
            shoppingPanel = panelRoot.Q<VisualElement>("shopping-panel");

            if (shoppingPanel == null)
            {
                Debug.LogWarning("[MainMenuController] No se encontró 'shopping-panel'.");
                return;
            }

            shoppingInput = shoppingPanel.Q<TextField>("shopping-input");
            shoppingAddBtn = shoppingPanel.Q<Button>("shopping-add-btn");
            shoppingItemsContainer = shoppingPanel.Q<VisualElement>("shopping-items");
            shoppingEmptyLabel = shoppingPanel.Q<Label>("shopping-empty");
            shoppingClearDoneBtn = shoppingPanel.Q<Button>("shopping-clear-done-btn");
        }

        private void RegisterShoppingEvents()
        {
            if (shoppingAddBtn != null)
                shoppingAddBtn.clicked += OnShoppingAddClicked;

            if (shoppingClearDoneBtn != null)
                shoppingClearDoneBtn.clicked += OnShoppingClearDoneClicked;

            if (shoppingInput != null)
            {
                // Primera letra en mayúscula mientras escribes
                shoppingInput.RegisterValueChangedCallback(evt =>
                {
                    string value = evt.newValue;

                    if (string.IsNullOrEmpty(value))
                        return;

                    string capitalized =
                        char.ToUpper(value[0]) + value.Substring(1);

                    if (value != capitalized)
                    {
                        shoppingInput.SetValueWithoutNotify(capitalized);

                        // Coloca el cursor al final
                        shoppingInput.SelectRange(capitalized.Length, capitalized.Length);
                    }
                });

                shoppingInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return ||
                        evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnShoppingAddClicked();
                        evt.StopPropagation();
                    }
                }, TrickleDown.TrickleDown);
            }
        }

        #endregion

        #region Shopping Panel Control

        private void OpenShoppingPanel()
        {
            if (shoppingPanel == null)
                return;

            // Cierra Ajustes si estaba abierto
            if (navPanel != null)
            {
                navPanelOpen = false;
                navPanel.style.display = DisplayStyle.None;
            }

            shoppingPanelOpen = true;
            shoppingPanel.style.display = DisplayStyle.Flex;

            UpdateFooterActiveButton("lista");
            SubscribeShoppingList();
        }

        private void HideShoppingPanel()
        {
            if (shoppingPanel == null || !shoppingPanelOpen)
                return;

            shoppingPanelOpen = false;
            shoppingPanel.style.display = DisplayStyle.None;

            shoppingInput?.Blur();
            UnsubscribeShoppingList();
        }

        // Botón "Calendario" del footer: cierra cualquier panel superpuesto
        private void ShowCalendarTab()
        {
            CloseNavPanel();
            HideShoppingPanel();
        }

        #endregion

        #region Shopping Firebase

        private void SubscribeShoppingList()
        {
            if (shoppingSubscribed)
                return;

            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            firebase.SubscribeToShoppingList(HandleShoppingListChanged);
            shoppingSubscribed = true;
        }

        // Llamar también desde OnDisable()
        private void UnsubscribeShoppingList()
        {
            if (!shoppingSubscribed)
                return;

            FirebaseManager.Instance?.UnsubscribeFromShoppingList(HandleShoppingListChanged);
            shoppingSubscribed = false;
        }

        private void HandleShoppingListChanged(List<ShoppingItemData> items)
        {
            shoppingItems = items ?? new List<ShoppingItemData>();
            RenderShoppingList();
        }

        #endregion

        #region Shopping Actions

        private void OnShoppingAddClicked()
        {
            string text = shoppingInput?.value?.Trim();

            if (string.IsNullOrEmpty(text))
                return;

            if (text.Length > ShoppingItemMaxLength)
                text = text.Substring(0, ShoppingItemMaxLength);

            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            _ = firebase.AddShoppingItemAsync(text);

            shoppingInput.value = "";
            shoppingInput.Focus();
        }

        private void OnShoppingClearDoneClicked()
        {
            var firebase = FirebaseManager.Instance;

            if (firebase == null)
                return;

            _ = firebase.ClearDoneShoppingItemsAsync();
        }

        #endregion

        #region Shopping Render

        private void RenderShoppingList()
        {
            if (shoppingItemsContainer == null)
                return;

            shoppingItemsContainer.Clear();

            bool anyDone = false;

            foreach (var item in shoppingItems)
            {
                if (item.done)
                    anyDone = true;

                shoppingItemsContainer.Add(CreateShoppingRow(item));
            }

            if (shoppingEmptyLabel != null)
            {
                shoppingEmptyLabel.style.display =
                    shoppingItems.Count == 0
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            if (shoppingClearDoneBtn != null)
            {
                shoppingClearDoneBtn.style.display =
                    anyDone
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
        }

        private VisualElement CreateShoppingRow(ShoppingItemData item)
        {
            string id = item.id;
            bool done = item.done;

            var row = new VisualElement();
            row.AddToClassList("shopping-row");

            if (done)
                row.AddToClassList("shopping-row--done");

            var check = new Button(() => ToggleShoppingItem(id, !done));
            check.AddToClassList("shopping-check");

            if (done)
                check.AddToClassList("shopping-check--done");

            var label = new Label(item.text);
            label.AddToClassList("shopping-item-text");
            label.RegisterCallback<ClickEvent>(evt => ToggleShoppingItem(id, !done));

            var delete = new Button(() => RemoveShoppingItem(id)) { text = "×" };
            delete.AddToClassList("shopping-delete-btn");

            row.Add(check);
            row.Add(label);
            row.Add(delete);

            return row;
        }

        private void ToggleShoppingItem(string id, bool done)
        {
            _ = FirebaseManager.Instance?.SetShoppingItemDoneAsync(id, done);
        }

        private void RemoveShoppingItem(string id)
        {
            _ = FirebaseManager.Instance?.RemoveShoppingItemAsync(id);
        }

        #endregion
    }
}
