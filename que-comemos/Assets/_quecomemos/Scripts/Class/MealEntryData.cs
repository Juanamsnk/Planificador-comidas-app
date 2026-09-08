using System;

namespace QueComemos.Data
{
    /// <summary>
    /// Un plato guardado para un día/tipo (comida o cena). Los nombres de
    /// campo son EXACTAMENTE los que usa la web en
    /// calendars/{calendarId}/mealplan/{fecha}|{tipo} — no los cambies sin
    /// cambiarlos también ahí, o dejarán de ser compatibles.
    /// </summary>
    [Serializable]
    public class MealEntryData
    {
        public string dish;
        public string reminderDate;
        public string reminderTime;
        public string reminderTitle;
        public bool notifiedLive;
    }
}
