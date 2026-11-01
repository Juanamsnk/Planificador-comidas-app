using UnityEngine;

namespace QueComemos.UI
{
    /// <summary>
    /// Política de privacidad: URL y aceptación del usuario.
    /// La aceptación se guarda con la versión de la política: si cambias
    /// PolicyVersion, la app volverá a pedir la aceptación.
    /// </summary>
    public static class PrivacyConsent
    {
        public const string PolicyUrl =
            "https://juanamsnk.github.io/que-comemos-politicas---privacidad/";

        /// <summary>Súbela cuando cambies la política y quieras que todos vuelvan a aceptarla.</summary>
        private const int PolicyVersion = 1;

        private const string AcceptedVersionPrefKey =
            "quecomemos_privacy_accepted_version";

        public static bool IsAccepted =>
            PlayerPrefs.GetInt(AcceptedVersionPrefKey, 0) >= PolicyVersion;

        public static void Accept()
        {
            PlayerPrefs.SetInt(AcceptedVersionPrefKey, PolicyVersion);
            PlayerPrefs.Save();
        }

        public static void OpenPolicy()
        {
            Application.OpenURL(PolicyUrl);
        }
    }
}
