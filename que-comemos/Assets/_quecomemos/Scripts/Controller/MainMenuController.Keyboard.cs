using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    //MainMenuController.KeyBoard
    public partial class MainMenuController
    {
        #region Keyboard Spacer

        private void CreateKeyboardSpacer()
        {
            if (editPanel == null)
                return;

            keyboardSpacer =
                new VisualElement
                {
                    name = "keyboard-spacer"
                };

            keyboardSpacer.style.height =
                280; //320

            keyboardSpacer.style.width =
                Length.Percent(100);

            keyboardSpacer.style.flexShrink =
                0;

            keyboardSpacer.style.flexGrow =
                0;

            keyboardSpacer.style.display =
                DisplayStyle.None;

            editPanel.Add(
                keyboardSpacer
            );

            keyboardCheckTask =
                editPanel.schedule
                    .Execute(
                        UpdateKeyboardSpacer
                    )
                    .Every(100);

            Debug.Log(
                "[MainMenuController] " +
                "Spacer para teclado creado."
            );
        }

        private void UpdateKeyboardSpacer()
        {
            if (keyboardSpacer == null)
                return;

            bool keyboardVisible =
                TouchScreenKeyboard.visible;

            keyboardSpacer.style.display =
                keyboardVisible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        #endregion

        #region Keyboard Scrolling

        private void RegisterKeyboardScrolling(
            TextField field)
        {
            if (field == null)
                return;

            field.RegisterCallback<FocusInEvent>(
                evt =>
                {
                    field.schedule
                        .Execute(
                            () =>
                            {
                                UpdateKeyboardSpacer();
                                ScrollToEditPanel();
                            }
                        )
                        .ExecuteLater(250);
                }
            );
        }

        private void ScrollToEditPanel()
        {
            if (weekScroll == null || editPanel == null)
                return;

            ScrollToBottom();

            // El layout (display:flex del panel, spacer del teclado) tarda
            // uno o varios frames en actualizarse, así que repetimos.
            editPanel.schedule
                .Execute(ScrollToBottom)
                .ExecuteLater(50);

            editPanel.schedule
                .Execute(ScrollToBottom)
                .ExecuteLater(300);
        }

        private void ScrollToEditPanelAdjusted()
        {
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (weekScroll == null)
                return;

            float maxY = Mathf.Max(
                0f,
                weekScroll.contentContainer.layout.height -
                weekScroll.contentViewport.layout.height
            );

            weekScroll.scrollOffset = new Vector2(
                weekScroll.scrollOffset.x,
                maxY
            );
        }

        private void HideKeyboard()
        {
            dishField.Blur();
            reminderDateField.Blur();
            reminderTimeField.Blur();
            reminderTitleField.Blur();
            GUI.FocusControl(null);
        }

        #endregion

        private void ConfigureAndRegisterTextField(TextField field, bool capitalizeFirstLetter = false)
        {

            field.hideMobileInput = true;

            field.keyboardType = TouchScreenKeyboardType.Default;

            //if (field == null)
            //    return;

            //// CONFIGURACIÓN BASE
            //field.autoCorrection = false;
            //field.selectAllOnFocus = false;
            //field.selectAllOnMouseUp = false;
            //field.keyboardType = TouchScreenKeyboardType.NamePhonePad;

            // Solo la PRIMERA letra del texto en mayúscula
            if (capitalizeFirstLetter)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    string value = evt.newValue;

                    if (string.IsNullOrEmpty(value) || !char.IsLower(value[0]))
                        return;

                    int cursor = field.cursorIndex;

                    field.SetValueWithoutNotify(
                        char.ToUpper(value[0]) + value.Substring(1)
                    );

                    field.cursorIndex = cursor;
                    field.selectIndex = cursor;
                });
            }

            // KEYBOARD SCROLLING
            field.RegisterCallback<FocusInEvent>(evt =>
            {
                UpdateKeyboardSpacer();
                ScrollToEditPanel();
            });
        }
    }
}