using QueComemos.Data;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public partial class MainMenuController
    {
        #region Render

        private void Render()
        {
            RenderWeekNav();
            RenderWeekGrid();
            RenderEditPanel();
        }

        private void RenderWeekNav()
        {
            var dates =
                WeekDates(weekOffset);

            var first = dates[0];
            var last = dates[6];

            string rangeLabel;

            if (first.Month == last.Month)
            {
                rangeLabel =
                    $"{first.Day} – {last.Day} " +
                    $"de {Months[first.Month - 1]}";
            }
            else
            {
                rangeLabel =
                    $"{first.Day} " +
                    $"{Months[first.Month - 1]} – " +
                    $"{last.Day} " +
                    $"{Months[last.Month - 1]}";
            }

            weekRangeLabel.text =
                $"{rangeLabel} {first.Year}";

            todayBtn.style.display =
                weekOffset != 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void RenderWeekGrid()
        {
            var dates =
                WeekDates(weekOffset);

            var todayStr =
                FormatDate(
                    System.DateTime.Today
                );

            for (int i = 0; i < 7; i++)
            {
                if (i >= dayCards.Count)
                    continue;

                var refs =
                    dayCards[i];

                if (refs == null ||
                    refs.card == null)
                {
                    continue;
                }

                var d =
                    dates[i];

                string dateStr =
                    FormatDate(d);

                bool isToday =
                    dateStr == todayStr;

                refs.dateStr =
                    dateStr;

                if (refs.dayName != null)
                {
                    refs.dayName.text =
                        DayNames[
                            IsoWeekdayIndex(d)
                        ];
                }

                if (refs.dayNum != null)
                {
                    refs.dayNum.text =
                        $"{d.Day} " +
                        $"{Months[d.Month - 1].Substring(0, 3)}";
                }

                refs.card.RemoveFromClassList(
                    "day-card--today"
                );

                refs.head?.RemoveFromClassList(
                    "day-card-head--today"
                );

                if (isToday)
                {
                    refs.card.AddToClassList(
                        "day-card--today"
                    );

                    refs.head?.AddToClassList(
                        "day-card-head--today"
                    );
                }

                RenderCell(
                    refs.comida,
                    dateStr,
                    "comida"
                );

                RenderCell(
                    refs.cena,
                    dateStr,
                    "cena"
                );
            }
        }

        private void RenderCell(
            MealSlotRefs slot,
            string dateStr,
            string type)
        {
            if (slot == null ||
                slot.cell == null)
            {
                return;
            }

            data.TryGetValue(
                Key(dateStr, type),
                out var entry
            );

            bool hasDish =
                entry != null &&
                !string.IsNullOrEmpty(
                    entry.dish
                );

            bool isSelected =
                selected.HasValue &&
                selected.Value.date == dateStr &&
                selected.Value.type == type;

            slot.cell.RemoveFromClassList(
                "cell-btn--selected"
            );

            if (isSelected)
            {
                slot.cell.AddToClassList(
                    "cell-btn--selected"
                );
            }

            if (slot.emptyLabel != null)
            {
                slot.emptyLabel.style.display =
                    hasDish
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }

            if (slot.filled != null)
            {
                slot.filled.style.display =
                    hasDish
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            if (hasDish)
            {
                if (slot.dishLabel != null)
                {
                    slot.dishLabel.text =
                        entry.dish;
                }

                if (slot.metaLabel != null)
                {
                    var metaBits =
                        new List<string>();

                    if (!string.IsNullOrEmpty(
                            entry.reminderDate) &&
                        !string.IsNullOrEmpty(
                            entry.reminderTime))
                    {
                        metaBits.Add(
                            "aviso " +
                            entry.reminderTime
                        );
                    }

                    slot.metaLabel.text =
                        string.Join(
                            " · ",
                            metaBits
                        );
                }
            }

            slot.cell.userData =
                (dateStr, type);
        }

        private void RenderEditPanel()
        {
            if (!selected.HasValue)
            {
                editPanel.style.display =
                    DisplayStyle.None;

                if (keyboardSpacer != null)
                {
                    keyboardSpacer.style.display =
                        DisplayStyle.None;
                }

                return;
            }

            editPanel.style.display =
                DisplayStyle.Flex;

            UpdateKeyboardSpacer();

            var selection =
                selected.Value;

            string dateStr =
                selection.date;

            string type =
                selection.type;

            data.TryGetValue(
                Key(dateStr, type),
                out var entry
            );

            if (entry == null)
            {
                entry =
                    new MealEntryData();
            }

            var d =
                System.DateTime.Parse(dateStr);

            string dayLabel =
                $"{DayNames[IsoWeekdayIndex(d)]} " +
                $"{d.Day} de " +
                $"{Months[d.Month - 1]}";

            editPanelTitle.text =
                $"{(type == "comida" ? "Comida" : "Cena")} · " +
                dayLabel;

            dishField.SetValueWithoutNotify(
                entry.dish ?? ""
            );

            var defaults =
                DefaultReminder(dateStr);

            bool hasReminder =
                !string.IsNullOrEmpty(
                    entry.reminderDate
                );

            reminderToggle.SetValueWithoutNotify(
                hasReminder
            );

            reminderDateField.SetValueWithoutNotify(
                entry.reminderDate ??
                defaults.date
            );

            reminderTimeField.SetValueWithoutNotify(
                entry.reminderTime ??
                defaults.time
            );

            reminderTitleField.SetValueWithoutNotify(
                entry.reminderTitle ?? ""
            );

            deleteBtn.style.display =
                string.IsNullOrEmpty(entry.dish)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

            ScrollToEditPanel();
        }

        #endregion
    }
}