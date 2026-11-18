using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrepararActividad45
{
    [MenuItem("Helix Jump/Preparar escenas actividad 4.5")]
    public static void Preparar()
    {
        const string original = "Assets/Scenes/SampleScene.unity";
        string[] rutas = { "Assets/Scenes/Menu.unity", "Assets/Scenes/Nivel1.unity", "Assets/Scenes/Nivel2.unity" };
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Helix Jump", "Deten Play antes de preparar las escenas.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(original) == null)
        {
            EditorUtility.DisplayDialog("Helix Jump", "No se encontro Assets/Scenes/SampleScene.unity. Conserva tu escena original.", "OK");
            return;
        }
        // Verificar la escena base antes de duplicarla. No se sobrescribe ninguna escena existente.
        EditorSceneManager.OpenScene(original);
        if (Object.FindFirstObjectByType<HelixGame>() == null)
        {
            EditorUtility.DisplayDialog("Helix Jump", "SampleScene necesita el objeto con HelixGame adjunto.", "OK");
            return;
        }
        foreach (string ruta in rutas)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null && !AssetDatabase.CopyAsset(original, ruta))
            {
                Debug.LogError("No fue posible crear " + ruta);
                return;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(rutas[0], true),
            new EditorBuildSettingsScene(rutas[1], true),
            new EditorBuildSettingsScene(rutas[2], true)
        };
        EditorSceneManager.OpenScene(rutas[0]);
        EditorUtility.DisplayDialog("Actividad 4.5", "Escenas Menu, Nivel1 y Nivel2 preparadas y registradas. Si usas un perfil de compilacion con lista propia, comprueba que incluya las tres escenas. Presiona Play para probar el menu.", "OK");
    }
}
