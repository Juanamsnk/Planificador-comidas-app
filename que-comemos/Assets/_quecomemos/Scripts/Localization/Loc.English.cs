using System.Collections.Generic;

namespace QueComemos.Localization
{
    public static partial class Loc
    {
        private static Dictionary<string, string> BuildEnglish()
        {
            return new Dictionary<string, string>
            {
                // ── Sign-in screen ──────────────────────────────────
                ["auth.subtitle"] = "Plan your meals together",
                ["auth.googleLogin"] = "Sign in with Google",
                ["auth.guest"] = "Continue as guest",
                ["auth.err.notReady"] = "Not ready yet, please try again in a moment.",
                ["auth.err.cancelled"] = "Sign-in cancelled.",
                ["auth.err.generic"] = "Couldn't sign in.",

                // ── Header / calendars ──────────────────────────────
                ["badge.own"] = "Your calendar",
                ["badge.demo"] = "Demo mode: changes are not saved.",
                ["badge.guestNote"] = "You're viewing this calendar because someone shared their link with you.",
                ["calendar.mine"] = "My calendar",
                ["calendar.shared"] = "Shared calendar",
                ["calendar.pin"] = "Pin calendar",
                ["calendar.leave"] = "Remove calendar",
                ["calendar.pinned"] = "Calendar pinned",
                ["common.calendars"] = "Calendars",

                // ── Actions / menu ──────────────────────────────────
                ["action.share"] = "Share link",
                ["action.logout"] = "Log out",
                ["menu.support"] = "Support \"QueComemos\"",
                ["settings.support"] = "Support Qué Comemos",

                // ── Week ────────────────────────────────────────────
                ["week.today"] = "This week",
                ["week.fill"] = "Fill week",
                ["week.range.sameMonth"] = "{2} {0} – {1}",
                ["week.range.diffMonth"] = "{1} {0} – {3} {2}",

                // ── Dates ───────────────────────────────────────────
                ["date.dayNum"] = "{1} {0}",
                ["date.long"] = "{0}, {2} {1}",
                ["day.0"] = "Monday",
                ["day.1"] = "Tuesday",
                ["day.2"] = "Wednesday",
                ["day.3"] = "Thursday",
                ["day.4"] = "Friday",
                ["day.5"] = "Saturday",
                ["day.6"] = "Sunday",
                ["month.1"] = "January",
                ["month.2"] = "February",
                ["month.3"] = "March",
                ["month.4"] = "April",
                ["month.5"] = "May",
                ["month.6"] = "June",
                ["month.7"] = "July",
                ["month.8"] = "August",
                ["month.9"] = "September",
                ["month.10"] = "October",
                ["month.11"] = "November",
                ["month.12"] = "December",
                ["month.short.1"] = "Jan",
                ["month.short.2"] = "Feb",
                ["month.short.3"] = "Mar",
                ["month.short.4"] = "Apr",
                ["month.short.5"] = "May",
                ["month.short.6"] = "Jun",
                ["month.short.7"] = "Jul",
                ["month.short.8"] = "Aug",
                ["month.short.9"] = "Sep",
                ["month.short.10"] = "Oct",
                ["month.short.11"] = "Nov",
                ["month.short.12"] = "Dec",

                // ── Meals ───────────────────────────────────────────
                ["meal.lunch"] = "Lunch",
                ["meal.dinner"] = "Dinner",
                ["cell.add"] = "+ add",
                ["cell.reminder"] = "reminder {0}",

                // ── Edit panel ──────────────────────────────────────
                ["edit.dish"] = "Dish",
                ["edit.dishPlaceholder"] = "E.g. Roast chicken",
                ["edit.random"] = "Random dish",
                ["edit.forgotten"] = "Forgotten dish",
                ["edit.reminderName"] = "Reminder name",
                ["edit.reminderPlaceholder"] = "E.g. Take the chicken out of the freezer",
                ["edit.save"] = "Save",
                ["edit.delete"] = "Delete",
                ["edit.close"] = "Close",
                ["reminder.label"] = "Reminder",
                ["reminder.at"] = "at",

                // ── Toasts ──────────────────────────────────────────
                ["toast.dishRequired"] = "Enter the dish name",
                ["toast.reminderError"] = "Couldn't schedule the reminder",
                ["toast.savedReminder"] = "Saved and reminder scheduled",
                ["toast.saved"] = "Saved",
                ["toast.savedNoReminder"] = "Saved, but the reminder couldn't be scheduled.",
                ["toast.reminderInvalid"] = "The reminder date/time isn't valid.",
                ["toast.reminderPast"] = "The reminder date has already passed.",
                ["toast.deleted"] = "Deleted",
                ["toast.suggestNeedsDishes"] = "Add dishes on other dates to get a suggestion",
                ["toast.analyzeNeedsDishes"] = "Add dishes on several dates to analyze",
                ["toast.fillNeedsDishes"] = "Add dishes on other dates to fill the week",
                ["toast.weekFilled.one"] = "Week filled: {0} dish",
                ["toast.weekFilled.other"] = "Week filled: {0} dishes",
                ["toast.shareNeedsLogin"] = "Can't share yet: sign in first.",
                ["toast.linkCopied"] = "Link copied to clipboard",

                // ── Settings and bottom bar ─────────────────────────
                ["settings.appearance"] = "Appearance",
                ["settings.themeDark"] = "Dark theme",
                ["settings.themeLight"] = "Light theme",
                ["settings.actions"] = "Actions",
                ["settings.account"] = "Account",
                ["settings.language"] = "Language",

                // ── Privacy policy ──────────────────────────────────
                ["privacy.title"] = "Privacy Policy",
                ["privacy.body"] = "Before you continue, please read and accept our Privacy Policy. It explains what data is stored and how it is used. By accepting, you confirm that you have read it.",
                ["privacy.read"] = "Read the Privacy Policy",
                ["privacy.accept"] = "Accept and continue",
                ["privacy.decline"] = "Decline",
                ["footer.calendar"] = "Calendar",
                ["footer.list"] = "List",
                ["footer.settings"] = "Settings",
                ["footer.recipes"] = "Recipes",

                // ── Recipes ─────────────────────────────────────────
                ["recipes.title"] = "Recipes",
                ["recipes.namePlaceholder"] = "Recipe name",
                ["recipes.linkPlaceholder"] = "Link (YouTube, website...)",
                ["recipes.save"] = "Save recipe",
                ["recipes.empty"] = "You have no recipes yet. Enter a name and the link to a video or website, then tap Save recipe.",
                ["toast.recipeNameRequired"] = "Enter the recipe name",
                ["toast.recipeLinkInvalid"] = "Enter a valid link (http or https)",
                ["toast.recipeSaved"] = "Recipe saved",
                ["err.recipeLoad"] = "Couldn't load the recipes.",
                ["err.recipeAdd"] = "Couldn't save the recipe.",
                ["err.recipeDelete"] = "Couldn't delete the recipe.",

                // ── Shopping list ───────────────────────────────────
                ["shopping.title"] = "Shopping list",
                ["shopping.empty"] = "Your list is empty. Type what you need above and tap +",
                ["shopping.clearDone"] = "Clear purchased",

                // ── Errors (Firebase) ───────────────────────────────
                ["err.firebaseInit"] = "Couldn't initialize Firebase.",
                ["err.firebaseUnavailable"] = "Firebase isn't available on this device ({0}).",
                ["err.dbConnect"] = "Couldn't connect to the database.",
                ["err.saveFailed"] = "Couldn't save. Please try again.",
                ["err.leaveCalendar"] = "Couldn't remove that calendar.",
                ["err.shopLoad"] = "Couldn't load the shopping list.",
                ["err.shopAdd"] = "Couldn't add the item.",
                ["err.shopUpdate"] = "Couldn't update the list.",
                ["err.shopDelete"] = "Couldn't delete the item.",
                ["err.shopClear"] = "Couldn't delete the items.",

                // ── Notifications ───────────────────────────────────
                ["notif.channelName"] = "Meal reminders",
                ["notif.channelDesc"] = "QueComemos meal reminders",
                ["notif.defaultTitle"] = "Meal reminder",
                ["notif.bodyScheduled"] = "You have a meal scheduled.",
                ["notif.bodyPending"] = "You have a meal pending."
            };
        }
    }
}
