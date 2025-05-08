
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
public class StarterWizardSetup
{
    [MenuItem("Meta/Tools/Apply All Project Setup Tool Suggestions")]
    public static async void AutoApplySetupSteps()
    {
        Debug.Log("Applying all project setup tool suggestions: Android");
        await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Android);
        Debug.Log("Applying all project setup tool suggestions: Standalone");
        await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Standalone);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
        else
        {
            RemoveStarterScript();
        }
    }
    private static void RemoveStarterScript()
    {
        string scriptPath = "Assets/StarterWizardSetup.cs";
        if (File.Exists(scriptPath))
        {
            File.Delete(scriptPath);
        }
    }
}
