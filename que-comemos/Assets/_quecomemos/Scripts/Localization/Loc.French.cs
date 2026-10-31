using System.Collections.Generic;

namespace QueComemos.Localization
{
    public static partial class Loc
    {
        private static Dictionary<string, string> BuildFrench()
        {
            return new Dictionary<string, string>
            {
                // ── Écran de connexion ──────────────────────────────
                ["auth.subtitle"] = "Planifiez vos repas en équipe",
                ["auth.googleLogin"] = "Se connecter avec Google",
                ["auth.guest"] = "Continuer en tant qu'invité",
                ["auth.err.notReady"] = "Pas encore prêt, réessayez dans un instant.",
                ["auth.err.cancelled"] = "Connexion annulée.",
                ["auth.err.generic"] = "Impossible de se connecter.",

                // ── En-tête / calendriers ───────────────────────────
                ["badge.own"] = "Votre calendrier",
                ["badge.demo"] = "Mode démo : les modifications ne sont pas enregistrées.",
                ["badge.guestNote"] = "Vous consultez ce calendrier parce que quelqu'un vous a partagé son lien.",
                ["calendar.mine"] = "Mon calendrier",
                ["calendar.shared"] = "Calendrier partagé",
                ["calendar.pin"] = "Épingler le calendrier",
                ["calendar.leave"] = "Retirer le calendrier",
                ["calendar.pinned"] = "Calendrier épinglé",
                ["common.calendars"] = "Calendriers",

                // ── Actions / menu ──────────────────────────────────
                ["action.share"] = "Partager le lien",
                ["action.logout"] = "Se déconnecter",
                ["menu.support"] = "Soutenir « QueComemos »",
                ["settings.support"] = "Soutenir Qué Comemos",

                // ── Semaine ─────────────────────────────────────────
                ["week.today"] = "Cette semaine",
                ["week.fill"] = "Remplir la semaine",
                ["week.range.sameMonth"] = "{0} – {1} {2}",
                ["week.range.diffMonth"] = "{0} {1} – {2} {3}",

                // ── Dates ───────────────────────────────────────────
                ["date.dayNum"] = "{0} {1}",
                ["date.long"] = "{0} {1} {2}",
                ["day.0"] = "Lundi",
                ["day.1"] = "Mardi",
                ["day.2"] = "Mercredi",
                ["day.3"] = "Jeudi",
                ["day.4"] = "Vendredi",
                ["day.5"] = "Samedi",
                ["day.6"] = "Dimanche",
                ["month.1"] = "janvier",
                ["month.2"] = "février",
                ["month.3"] = "mars",
                ["month.4"] = "avril",
                ["month.5"] = "mai",
                ["month.6"] = "juin",
                ["month.7"] = "juillet",
                ["month.8"] = "août",
                ["month.9"] = "septembre",
                ["month.10"] = "octobre",
                ["month.11"] = "novembre",
                ["month.12"] = "décembre",
                ["month.short.1"] = "janv.",
                ["month.short.2"] = "févr.",
                ["month.short.3"] = "mars",
                ["month.short.4"] = "avr.",
                ["month.short.5"] = "mai",
                ["month.short.6"] = "juin",
                ["month.short.7"] = "juil.",
                ["month.short.8"] = "août",
                ["month.short.9"] = "sept.",
                ["month.short.10"] = "oct.",
                ["month.short.11"] = "nov.",
                ["month.short.12"] = "déc.",

                // ── Repas ───────────────────────────────────────────
                ["meal.lunch"] = "Déjeuner",
                ["meal.dinner"] = "Dîner",
                ["cell.add"] = "+ ajouter",
                ["cell.reminder"] = "rappel {0}",

                // ── Panneau d'édition ───────────────────────────────
                ["edit.dish"] = "Plat",
                ["edit.dishPlaceholder"] = "Ex. Poulet rôti",
                ["edit.random"] = "Plat aléatoire",
                ["edit.forgotten"] = "Plat oublié",
                ["edit.reminderName"] = "Nom du rappel",
                ["edit.reminderPlaceholder"] = "Ex. Sortir le poulet du congélateur",
                ["edit.save"] = "Enregistrer",
                ["edit.delete"] = "Supprimer",
                ["edit.close"] = "Fermer",
                ["reminder.label"] = "Rappel",
                ["reminder.at"] = "à",

                // ── Messages (toasts) ───────────────────────────────
                ["toast.dishRequired"] = "Saisissez le nom du plat",
                ["toast.reminderError"] = "Erreur lors de la programmation du rappel",
                ["toast.savedReminder"] = "Enregistré et rappel programmé",
                ["toast.saved"] = "Enregistré",
                ["toast.savedNoReminder"] = "Enregistré, mais le rappel n'a pas pu être programmé.",
                ["toast.reminderInvalid"] = "La date/heure du rappel n'est pas valide.",
                ["toast.reminderPast"] = "La date du rappel est déjà passée.",
                ["toast.deleted"] = "Supprimé",
                ["toast.suggestNeedsDishes"] = "Ajoutez des plats à d'autres dates",
                ["toast.analyzeNeedsDishes"] = "Ajoutez des plats à plusieurs dates",
                ["toast.fillNeedsDishes"] = "Ajoutez des plats à d'autres dates pour remplir la semaine",
                ["toast.weekFilled.one"] = "Semaine remplie : {0} plat",
                ["toast.weekFilled.other"] = "Semaine remplie : {0} plats",
                ["toast.shareNeedsLogin"] = "Partage impossible pour le moment : connectez-vous d'abord.",
                ["toast.linkCopied"] = "Lien copié dans le presse-papiers",

                // ── Réglages et barre du bas ────────────────────────
                ["settings.appearance"] = "Apparence",
                ["settings.themeDark"] = "Thème sombre",
                ["settings.themeLight"] = "Thème clair",
                ["settings.actions"] = "Actions",
                ["settings.account"] = "Compte",
                ["settings.language"] = "Langue",
                ["footer.calendar"] = "Calendrier",
                ["footer.list"] = "Liste",
                ["footer.settings"] = "Réglages",

                // ── Liste de courses ────────────────────────────────
                ["shopping.title"] = "Liste de courses",
                ["shopping.empty"] = "Votre liste est vide. Écrivez ci-dessus ce dont vous avez besoin et appuyez sur +",
                ["shopping.clearDone"] = "Supprimer les articles achetés",

                // ── Erreurs (Firebase) ──────────────────────────────
                ["err.firebaseInit"] = "Impossible d'initialiser Firebase.",
                ["err.firebaseUnavailable"] = "Firebase n'est pas disponible sur cet appareil ({0}).",
                ["err.dbConnect"] = "Impossible de se connecter à la base de données.",
                ["err.saveFailed"] = "Enregistrement impossible. Réessayez.",
                ["err.leaveCalendar"] = "Impossible de retirer ce calendrier.",
                ["err.shopLoad"] = "Impossible de charger la liste de courses.",
                ["err.shopAdd"] = "Impossible d'ajouter l'article.",
                ["err.shopUpdate"] = "Impossible de mettre à jour la liste.",
                ["err.shopDelete"] = "Impossible de supprimer l'article.",
                ["err.shopClear"] = "Impossible de supprimer les articles.",

                // ── Notifications ───────────────────────────────────
                ["notif.channelName"] = "Rappels de repas",
                ["notif.channelDesc"] = "Rappels de repas de QueComemos",
                ["notif.defaultTitle"] = "Rappel de repas",
                ["notif.bodyScheduled"] = "Vous avez un repas prévu.",
                ["notif.bodyPending"] = "Vous avez un repas en attente."
            };
        }
    }
}
