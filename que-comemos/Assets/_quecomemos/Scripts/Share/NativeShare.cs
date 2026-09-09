using UnityEngine;

namespace QueComemos.UI
{
    /// <summary>
    /// Comparte texto usando el cuadro de diálogo nativo "Compartir con..."
    /// de Android (Intent.ACTION_SEND) — deja elegir WhatsApp, Mensajes,
    /// Gmail, etc. En cualquier otra plataforma (Editor, iOS por ahora, o
    /// si algo falla) copia el texto al portapapeles como alternativa.
    /// Devuelve true si se abrió el diálogo nativo, false si solo se copió
    /// al portapapeles (para que el llamador pueda avisar con un toast).
    /// </summary>
    public static class NativeShare
    {
        public static bool ShareText(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var intentObject = new AndroidJavaObject("android.content.Intent");
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var chooser = intentClass.CallStatic<AndroidJavaObject>(
                    "createChooser", intentObject, "Compartir enlace");
                currentActivity.Call("startActivity", chooser);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NativeShare] No se pudo abrir el diálogo nativo: {e.Message}");
            }
#endif
            GUIUtility.systemCopyBuffer = text;
            return false;
        }
    }
}
