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
                320;

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
            if (weekScroll == null ||
                editPanel == null)
            {
                return;
            }

            weekScroll.ScrollTo(
                editPanel
            );

            editPanel.schedule
                .Execute(
                    ScrollToEditPanelAdjusted
                )
                .ExecuteLater(250);
        }

        private void ScrollToEditPanelAdjusted()
        {
            if (weekScroll == null ||
                editPanel == null)
            {
                return;
            }

            weekScroll.ScrollTo(
                editPanel
            );

            Debug.Log(
                "[MainMenuController] " +
                $"EditPanel scrolleado. " +
                $"ScrollOffset={weekScroll.scrollOffset}"
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
            if (field == null)
                return;

            // CONFIGURACIÓN BASE
            field.autoCorrection = false;
            field.selectAllOnFocus = false;
            field.selectAllOnMouseUp = false;
            field.keyboardType = TouchScreenKeyboardType.NamePhonePad;

            // CAPITALIZACIÓN (si aplica)
            if (capitalizeFirstLetter)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    string value = evt.newValue;

                    if (string.IsNullOrEmpty(value))
                        return;

                    string capitalized =
                        char.ToUpper(value[0]) + value.Substring(1);

                    if (value != capitalized)
                        field.SetValueWithoutNotify(capitalized);
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