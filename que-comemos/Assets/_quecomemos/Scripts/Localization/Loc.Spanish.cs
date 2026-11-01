using System.Collections.Generic;

namespace QueComemos.Localization
{
    public static partial class Loc
    {
        // Idioma fuente. Cualquier clave nueva se añade primero aquí.
        private static Dictionary<string, string> BuildSpanish()
        {
            return new Dictionary<string, string>
            {
                // ── Pantalla de acceso ──────────────────────────────
                ["auth.subtitle"] = "Planifica tus comidas en equipo",
                ["auth.googleLogin"] = "Iniciar sesión con Google",
                ["auth.guest"] = "Entrar como invitado",
                ["auth.err.notReady"] = "Todavía no está listo, prueba en un momento.",
                ["auth.err.cancelled"] = "Inicio de sesión cancelado.",
                ["auth.err.generic"] = "No se pudo iniciar sesión.",

                // ── Cabecera / calendarios ──────────────────────────
                ["badge.own"] = "Tu calendario",
                ["badge.demo"] = "Modo demostración: los cambios no se guardan.",
                ["badge.guestNote"] = "Estás viendo este calendario porque alguien te ha compartido su enlace.",
                ["calendar.mine"] = "Mi calendario",
                ["calendar.shared"] = "Calendario compartido",
                ["calendar.pin"] = "Fijar calendario",
                ["calendar.leave"] = "Quitar calendario",
                ["calendar.pinned"] = "Calendario fijado",
                ["common.calendars"] = "Calendarios",

                // ── Acciones / menú ─────────────────────────────────
                ["action.share"] = "Compartir enlace",
                ["action.logout"] = "Cerrar sesión",
                ["menu.support"] = "Apoyar a \"QueComemos\"",
                ["settings.support"] = "Apoyar a Qué Comemos",

                // ── Semana ──────────────────────────────────────────
                ["week.today"] = "Esta semana",
                ["week.fill"] = "Rellenar semana",
                // Mismo mes: {0}=día inicio {1}=día fin {2}=mes
                ["week.range.sameMonth"] = "{0} – {1} de {2}",
                // Meses distintos: {0}=día inicio {1}=mes inicio {2}=día fin {3}=mes fin
                ["week.range.diffMonth"] = "{0} {1} – {2} {3}",

                // ── Fechas ──────────────────────────────────────────
                // Cabecera de tarjeta: {0}=día {1}=mes corto
                ["date.dayNum"] = "{0} {1}",
                // Título del panel: {0}=día de la semana {1}=día {2}=mes
                ["date.long"] = "{0} {1} de {2}",
                ["day.0"] = "Lunes",
                ["day.1"] = "Martes",
                ["day.2"] = "Miércoles",
                ["day.3"] = "Jueves",
                ["day.4"] = "Viernes",
                ["day.5"] = "Sábado",
                ["day.6"] = "Domingo",
                ["month.1"] = "enero",
                ["month.2"] = "febrero",
                ["month.3"] = "marzo",
                ["month.4"] = "abril",
                ["month.5"] = "mayo",
                ["month.6"] = "junio",
                ["month.7"] = "julio",
                ["month.8"] = "agosto",
                ["month.9"] = "septiembre",
                ["month.10"] = "octubre",
                ["month.11"] = "noviembre",
                ["month.12"] = "diciembre",
                ["month.short.1"] = "ene",
                ["month.short.2"] = "feb",
                ["month.short.3"] = "mar",
                ["month.short.4"] = "abr",
                ["month.short.5"] = "may",
                ["month.short.6"] = "jun",
                ["month.short.7"] = "jul",
                ["month.short.8"] = "ago",
                ["month.short.9"] = "sep",
                ["month.short.10"] = "oct",
                ["month.short.11"] = "nov",
                ["month.short.12"] = "dic",

                // ── Comidas ─────────────────────────────────────────
                ["meal.lunch"] = "Comida",
                ["meal.dinner"] = "Cena",
                ["cell.add"] = "+ añadir",
                ["cell.reminder"] = "aviso {0}",

                // ── Panel de edición ────────────────────────────────
                ["edit.dish"] = "Plato",
                ["edit.dishPlaceholder"] = "Ej. Pollo al horno",
                ["edit.random"] = "Plato Aleatorio",
                ["edit.forgotten"] = "Plato olvidado",
                ["edit.reminderName"] = "Nombre del recordatorio",
                ["edit.reminderPlaceholder"] = "Ej. Sacar el pollo del congelador",
                ["edit.save"] = "Guardar",
                ["edit.delete"] = "Eliminar",
                ["edit.close"] = "Cerrar",
                ["reminder.label"] = "Recordatorio",
                ["reminder.at"] = "a las",

                // ── Avisos (toasts) ─────────────────────────────────
                ["toast.dishRequired"] = "Escribe el nombre del plato",
                ["toast.reminderError"] = "Error al programar recordatorio",
                ["toast.savedReminder"] = "Guardado y recordatorio programado",
                ["toast.saved"] = "Guardado",
                ["toast.savedNoReminder"] = "Guardado, pero no se pudo programar el aviso.",
                ["toast.reminderInvalid"] = "La fecha/hora del recordatorio no es válida.",
                ["toast.reminderPast"] = "La fecha del recordatorio ya ha pasado.",
                ["toast.deleted"] = "Eliminado",
                ["toast.suggestNeedsDishes"] = "Añade platos en otras fechas para sugerir",
                ["toast.analyzeNeedsDishes"] = "Agrega platos en varias fechas para analizar",
                ["toast.fillNeedsDishes"] = "Agrega platos en otras fechas para rellenar la semana",
                ["toast.weekFilled.one"] = "Semana rellenada: {0} plato",
                ["toast.weekFilled.other"] = "Semana rellenada: {0} platos",
                ["toast.shareNeedsLogin"] = "Todavía no se puede compartir: inicia sesión primero.",
                ["toast.linkCopied"] = "Enlace copiado al portapapeles",

                // ── Ajustes y barra inferior ────────────────────────
                ["settings.appearance"] = "Apariencia",
                ["settings.themeDark"] = "Tema oscuro",
                ["settings.themeLight"] = "Tema claro",
                ["settings.actions"] = "Acciones",
                ["settings.account"] = "Cuenta",
                ["settings.language"] = "Idioma",

                // ── Política de privacidad ──────────────────────────
                ["privacy.title"] = "Política de privacidad",
                ["privacy.body"] = "Antes de continuar, lee y acepta nuestra Política de privacidad. En ella explicamos qué datos se guardan y cómo se usan. Al aceptar, confirmas que la has leído.",
                ["privacy.read"] = "Leer la política de privacidad",
                ["privacy.accept"] = "Aceptar y continuar",
                ["privacy.decline"] = "Rechazar",
                ["footer.calendar"] = "Calendario",
                ["footer.list"] = "Lista",
                ["footer.settings"] = "Ajustes",

                // ── Lista de la compra ──────────────────────────────
                ["shopping.title"] = "Lista de la compra",
                ["shopping.empty"] = "Tu lista está vacía. Escribe arriba lo que necesitas y pulsa +",
                ["shopping.clearDone"] = "Borrar comprados",

                // ── Errores (Firebase) ──────────────────────────────
                ["err.firebaseInit"] = "No se pudo inicializar Firebase.",
                ["err.firebaseUnavailable"] = "Firebase no disponible en este dispositivo ({0}).",
                ["err.dbConnect"] = "No se pudo conectar con la base de datos.",
                ["err.saveFailed"] = "No se pudo guardar. Inténtalo de nuevo.",
                ["err.leaveCalendar"] = "No se pudo quitar ese calendario.",
                ["err.shopLoad"] = "No se pudo cargar la lista de la compra.",
                ["err.shopAdd"] = "No se pudo añadir el elemento.",
                ["err.shopUpdate"] = "No se pudo actualizar la lista.",
                ["err.shopDelete"] = "No se pudo borrar el elemento.",
                ["err.shopClear"] = "No se pudieron borrar los elementos.",

                // ── Notificaciones ──────────────────────────────────
                ["notif.channelName"] = "Recordatorios de comidas",
                ["notif.channelDesc"] = "Recordatorios de comidas de QueComemos",
                ["notif.defaultTitle"] = "Recordatorio de comida",
                ["notif.bodyScheduled"] = "Tienes una comida programada.",
                ["notif.bodyPending"] = "Tienes una comida pendiente."
            };
        }
    }
}
