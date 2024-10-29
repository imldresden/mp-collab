using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MixedPresenceToolkit))]
public class InstallerEditor : Editor
{
    [MenuItem("MixedPresence/Add to Scene...")]
    public static void AddAndConfigure()
    {
        ConfigureDialog.ShowWindow();
    }

    [MenuItem("MixedPresence/Add to Scene...", true)]
    static bool ValidateAddAndConfigure()
    {
        return true;
    }

    [MenuItem("MixedPresence/Version: " + MixedPresenceToolkit.VERSION)]
    public static void Version()
    {
        ConfigureDialog.ShowWindow();
    }

    [MenuItem("MixedPresence/Version: " + MixedPresenceToolkit.VERSION, true)]
    static bool ValidateVersion()
    {
        return false;
    }

}
