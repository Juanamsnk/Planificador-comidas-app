using System;
using System.Collections.Generic;
using System.Linq;

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

        private void ScrollToDayCard(
            string dateStr)
        {
            if (weekScroll == null ||
                dayCards == null)
            {
                return;
            }

            var dayCard =
                dayCards.FirstOrDefault(
                    dc =>
                        dc != null &&
                        dc.dateStr == dateStr
                );

            if (dayCard?.card == null)
                return;

            weekScroll.ScrollTo(
                dayCard.card
            );

            dayCard.card.AddToClassList("day-card--highlighted");

            dayCard.card.schedule
                .Execute(() =>
                {
                    dayCard.card.RemoveFromClassList("day-card--highlighted");
                })
                .ExecuteLater(1000);
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