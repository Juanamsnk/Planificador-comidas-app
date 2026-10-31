using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueComemos.Localization
{
    /// <summary>
    /// Idiomas disponibles en la app.
    /// Para añadir uno nuevo: añade el valor aquí, crea su tabla en
    /// Loc.&lt;Idioma&gt;.cs, regístrala en BuildTables() y añádelo a
    /// Loc.Supported / NativeName / Code.
    /// </summary>
    public enum Language
    {
        Spanish = 0,
        English = 1,
        French = 2
    }

    /// <summary>
    /// Textos de la interfaz en varios idiomas.
    ///
    /// Uso desde código:      Loc.T("toast.saved")
    ///                        Loc.T("toast.weekFilled.other", 5)   // string.Format
    /// Uso desde UXML:        text="@toast.saved"  (lo resuelve LocBinder)
    ///
    /// Si una clave falta en el idioma actual se usa el texto en español;
    /// si falta también ahí, se devuelve la propia clave.
    /// </summary>
    public static partial class Loc
    {
        private const string LanguagePrefKey = "quecomemos_language";

        /// <summary>Idioma "fuente": se usa cuando falta una traducción.</summary>
        public const Language SourceLanguage = Language.Spanish;

        /// <summary>Idioma por defecto si el del móvil no está soportado.</summary>
        private const Language UnsupportedSystemFallback = Language.English;

        public static readonly Language[] Supported =
        {
            Language.Spanish,
            Language.English,
            Language.French
        };

        private static Dictionary<Language, Dictionary<string, string>> tables;
        private static Language current;
        private static bool initialized;

        /// <summary>Se lanza después de cambiar el idioma con SetLanguage.</summary>
        public static event Action LanguageChanged;

        public static Language Current
        {
            get
            {
                EnsureInitialized();
                return current;
            }
        }

        // Evita estado viejo si el Editor tiene desactivado el "Domain Reload".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            initialized = false;
            tables = null;
            LanguageChanged = null;
        }

        #region API

        public static void SetLanguage(Language language)
        {
            EnsureInitialized();

            if (language == current)
                return;

            current = language;

            PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
            PlayerPrefs.Save();

            LanguageChanged?.Invoke();
        }

        public static string T(string key)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
                return "";

            if (tables[current].TryGetValue(key, out var value))
                return value;

            if (current != SourceLanguage &&
                tables[SourceLanguage].TryGetValue(key, out value))
            {
                return value;
            }

            Debug.LogWarning($"[Loc] Falta la clave de texto \"{key}\".");
            return key;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[Loc] Formato inválido en la clave \"{key}\": {format}");
                return format;
            }
        }

        /// <summary>Nombre del día. 0 = lunes ... 6 = domingo.</summary>
        public static string DayName(int isoIndex) => T("day." + isoIndex);

        /// <summary>Nombre del mes. 1 = enero ... 12 = diciembre.</summary>
        public static string MonthName(int month) => T("month." + month);

        /// <summary>Nombre corto del mes. 1 = ene ... 12 = dic.</summary>
        public static string MonthShort(int month) => T("month.short." + month);

        /// <summary>Nombre del idioma en su propio idioma (para el selector).</summary>
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.Spanish: return "Español";
                case Language.English: return "English";
                case Language.French: return "Français";
                default: return language.ToString();
            }
        }

        /// <summary>Código ISO 639-1 del idioma.</summary>
        public static string Code(Language language)
        {
            switch (language)
            {
                case Language.Spanish: return "es";
                case Language.English: return "en";
                case Language.French: return "fr";
                default: return "en";
            }
        }

        #endregion

        #region Init

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            tables = BuildTables();
            current = LoadInitialLanguage();

#if UNITY_EDITOR
            ValidateTables();
#endif
        }

        private static Dictionary<Language, Dictionary<string, string>> BuildTables()
        {
            return new Dictionary<Language, Dictionary<string, string>>
            {
                { Language.Spanish, BuildSpanish() },
                { Language.English, BuildEnglish() },
                { Language.French, BuildFrench() }
            };
        }

        private static Language LoadInitialLanguage()
        {
            if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                int saved = PlayerPrefs.GetInt(LanguagePrefKey);

                if (Enum.IsDefined(typeof(Language), saved))
                    return (Language)saved;
            }

            return FromSystemLanguage(Application.systemLanguage);
        }

        private static Language FromSystemLanguage(SystemLanguage system)
        {
            switch (system)
            {
                case SystemLanguage.Spanish:
                case SystemLanguage.Catalan:
                case SystemLanguage.Basque:
                    return Language.Spanish;

                case SystemLanguage.French:
                    return Language.French;

                case SystemLanguage.English:
                    return Language.English;

                default:
                    return UnsupportedSystemFallback;
            }
        }

#if UNITY_EDITOR
        /// <summary>Avisa en consola si algún idioma no tiene las mismas claves que el español.</summary>
        private static void ValidateTables()
        {
            var source = tables[SourceLanguage];

            foreach (var pair in tables)
            {
                if (pair.Key == SourceLanguage)
                    continue;

                foreach (var key in source.Keys)
                {
                    if (!pair.Value.ContainsKey(key))
                        Debug.LogWarning($"[Loc] {pair.Key}: falta la clave \"{key}\".");
                }

                foreach (var key in pair.Value.Keys)
                {
                    if (!source.ContainsKey(key))
                        Debug.LogWarning($"[Loc] {pair.Key}: la clave \"{key}\" no existe en español.");
                }
            }
        }
#endif

        #endregion
    }
}
