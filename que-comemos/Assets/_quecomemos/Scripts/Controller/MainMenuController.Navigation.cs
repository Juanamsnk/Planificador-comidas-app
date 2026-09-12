using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace QueComemos.UI
{
    public partial class MainMenuController
    {
        #region Fechas

        private static string FormatDate(
            DateTime date)
        {
            return date.ToString(
                "yyyy-MM-dd"
            );
        }

        private static string Key(
            string dateStr,
            string type)
        {
            return dateStr +
                   "|" +
                   type;
        }

        private static int IsoWeekdayIndex(
            DateTime date)
        {
            int day =
                (int)date.DayOfWeek;

            return day == 0
                ? 6
                : day - 1;
        }

        private static DateTime GetMondayOfCurrentWeek()
        {
            var date =
                DateTime.Today;

            int day =
                (int)date.DayOfWeek;

            int diff =
                day == 0
                    ? -6
                    : 1 - day;

            return date.AddDays(diff);
        }

        private static List<DateTime> WeekDates(
            int offset)
        {
            var monday =
                GetMondayOfCurrentWeek()
                    .AddDays(offset * 7);

            var dates =
                new List<DateTime>();

            for (int i = 0; i < 7; i++)
            {
                dates.Add(
                    monday.AddDays(i)
                );
            }

            return dates;
        }

        private static (string date, string time)
            DefaultReminder(
                string dateStr)
        {
            var date =
                DateTime.Parse(dateStr)
                    .AddDays(-1);

            return (
                FormatDate(date),
                "12:00"
            );
        }

        #endregion

        #region Scroll

        private IVisualElementScheduledItem highlightTask;

        private void ScrollToDayCard(string dateStr)
        {
            if (weekScroll == null || dayCards == null)
                return;

            var dayCard = dayCards.FirstOrDefault(
                dc =>
                    dc != null &&
                    dc.dateStr == dateStr
            );

            if (dayCard?.card == null)
                return;

            weekScroll.schedule.Execute(() =>
            {
                float viewportCenter =
                    weekScroll.contentViewport
                        .worldBound
                        .center
                        .y;

                float cardCenter =
                    dayCard.card
                        .worldBound
                        .center
                        .y;

                float delta =
                    cardCenter - viewportCenter;

                Debug.Log(
    $"[DAY DEBUG] SCROLL | " +
    $"date={dateStr} | " +
    $"cardCenter={cardCenter} | " +
    $"viewportCenter={viewportCenter} | " +
    $"delta={delta}"
);

                Vector2 offset =
                    weekScroll.scrollOffset;

                Debug.Log(
    $"[DAY DEBUG] SCROLL OFFSET | " +
    $"date={dateStr} | " +
    $"offsetY={offset.y}"
);

                offset.y += delta;

                weekScroll.scrollOffset =
                    offset;

                // Aseguramos que solo haya un card iluminado.
                foreach (var card in dayCards)
                {
                    if (card?.card == null)
                        continue;

                    card.card.RemoveFromClassList(
                        "day-card--highlighted"
                    );
                }

                // Iluminamos el card actual.
                dayCard.card.AddToClassList(
                    "day-card--highlighted"
                );

                // Lo quitamos después de 1 segundo.
                dayCard.card.schedule
                    .Execute(() =>
                    {
                        dayCard.card.RemoveFromClassList(
                            "day-card--highlighted"
                        );
                    })
                    .ExecuteLater(1000);

            }).ExecuteLater(1);
        }

        private void ScrollToToday()
        {
            string todayStr =
                FormatDate(
                    DateTime.Today
                );

            var dates =
                WeekDates(weekOffset);

            bool todayIsVisible =
                dates.Any(
                    d =>
                        FormatDate(d) ==
                        todayStr
                );

            if (!todayIsVisible)
                return;

            ScrollToDayCard(
                todayStr
            );
        }

        #endregion
    }
}