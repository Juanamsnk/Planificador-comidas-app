using UnityEngine.SceneManagement;

/// <summary>
/// Enumerado de todas las escenas del proyecto.
/// Actualiza este enum cada vez que añadas nuevas escenas.
/// </summary>
public enum SceneNames
{
    Authentication,
    Menu
}

/// <summary>
/// Helper class para manejar la carga de escenas con enumerados.
/// Previene errores por typos en nombres de escenas.
/// </summary>
public static class SceneHelper
{
    /// <summary>
    /// Convierte el enumerado a el nombre de la escena en string.
    /// </summary>
    public static string GetSceneName(SceneNames scene)
    {
        return scene.ToString();
    }

    /// <summary>
    /// Carga una escena usando el enumerado.
    /// </summary>
    public static void LoadScene(SceneNames scene)
    {
        SceneManager.LoadScene(GetSceneName(scene));
    }

    /// <summary>
    /// Carga una escena de forma asíncrona usando el enumerado.
    /// </summary>
    public static void LoadSceneAsync(SceneNames scene)
    {
        SceneManager.LoadSceneAsync(GetSceneName(scene));
    }

    /// <summary>
    /// Recarga la escena actual.
    /// </summary>
    public static void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}