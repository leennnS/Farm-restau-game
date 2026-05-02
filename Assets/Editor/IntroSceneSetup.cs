#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Setup utility for the Intro scene atmospheric polish.
/// Adds UI styling and lighting components automatically.
/// Run from menu: Window > Intro Scene Setup
/// </summary>
public class IntroSceneSetup
{
    [MenuItem("Window/Intro Scene Setup/Apply Atmospheric Polish")]
    public static void ApplyPolish()
    {
        // Find the scene
        var introScene = EditorSceneManager.GetSceneByName("Intro");
        if (!introScene.IsValid())
        {
            introScene = EditorSceneManager.OpenScene("Assets/Scenes/Main/Intro.unity", OpenSceneMode.Single);
        }

        // Find the NarrativeCanvas
        Canvas canvas = FindGameObjectWithName<Canvas>("NarrativeCanvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find NarrativeCanvas in scene!", "OK");
            return;
        }

        // Add IntroUIPolish to the canvas
        if (canvas.GetComponent<IntroUIPolish>() == null)
        {
            IntroUIPolish polish = canvas.gameObject.AddComponent<IntroUIPolish>();

            // Auto-assign references - search for TextMeshProUGUI components in canvas
            TextMeshProUGUI[] textComponents = canvas.GetComponentsInChildren<TextMeshProUGUI>();
            TextMeshProUGUI narrativeText = null;
            TextMeshProUGUI hintText = null;

            // Find narrative and hint text
            foreach (var textComp in textComponents)
            {
                if (textComp.name.Contains("Hint") || textComp.name.Contains("Continue"))
                    hintText = textComp;
                else if (narrativeText == null)
                    narrativeText = textComp;
            }

            if (narrativeText != null)
            {
                SerializedObject so = new SerializedObject(polish);
                so.FindProperty("narrativeText").objectReferenceValue = narrativeText;
                if (hintText != null)
                    so.FindProperty("hintText").objectReferenceValue = hintText;
                so.FindProperty("canvasTransform").objectReferenceValue = canvas.transform;
                so.ApplyModifiedProperties();

                EditorUtility.DisplayDialog("Success", $"UI Polish applied!\nFound: {narrativeText.gameObject.name}, {(hintText ? hintText.gameObject.name : "no hint text")}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "Could not find narrative TextMeshProUGUI in canvas children. Please manually assign.", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "IntroUIPolish already exists on NarrativeCanvas.", "OK");
        }

        // Create IntroLighting GameObject
        GameObject lightingGO = FindGameObjectWithName("IntroLighting");
        if (lightingGO == null)
        {
            lightingGO = new GameObject("IntroLighting");
            lightingGO.transform.SetParent(introScene.GetRootGameObjects()[0].transform, false);
        }

        IntroLighting lighting = lightingGO.GetComponent<IntroLighting>();
        if (lighting == null)
        {
            lighting = lightingGO.AddComponent<IntroLighting>();

            // Auto-assign player character
            var characterVariant = FindGameObjectWithName("Main Character Variant");
            if (characterVariant != null)
            {
                SerializedObject so = new SerializedObject(lighting);
                so.FindProperty("playerCharacter").objectReferenceValue = characterVariant.transform;
                so.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.SaveScene(introScene);
        EditorUtility.DisplayDialog("Setup Complete",
            "Intro scene atmospheric polish applied!\n\n" +
            "Components added:\n" +
            "- IntroUIPolish on NarrativeCanvas\n" +
            "- IntroLighting GameObject with lights\n\n" +
            "The intro scene now has:\n" +
            "✓ Styled dialogue box\n" +
            "✓ Styled continue prompt\n" +
            "✓ Window light streaks\n" +
            "✓ Character overhead light",
            "OK");
    }

    [MenuItem("Window/Intro Scene Setup/Remove Atmospheric Polish")]
    public static void RemovePolish()
    {
        var introScene = EditorSceneManager.GetSceneByName("Intro");
        if (!introScene.IsValid())
        {
            introScene = EditorSceneManager.OpenScene("Assets/Scenes/Main/Intro.unity", OpenSceneMode.Single);
        }

        // Remove IntroUIPolish
        Canvas canvas = FindGameObjectWithName<Canvas>("NarrativeCanvas");
        if (canvas != null && canvas.GetComponent<IntroUIPolish>() != null)
        {
            Object.DestroyImmediate(canvas.GetComponent<IntroUIPolish>());
        }

        // Remove IntroLighting
        GameObject lightingGO = FindGameObjectWithName("IntroLighting");
        if (lightingGO != null)
        {
            Object.DestroyImmediate(lightingGO);
        }

        EditorSceneManager.SaveScene(introScene);
        EditorUtility.DisplayDialog("Removed", "Atmospheric polish removed from Intro scene.", "OK");
    }

    private static T FindGameObjectWithName<T>(string name) where T : Component
    {
        var objs = Resources.FindObjectsOfTypeAll(typeof(T));
        foreach (var obj in objs)
        {
            if (obj is T component && component.name == name)
            {
                return component;
            }
        }
        return null;
    }

    private static GameObject FindGameObjectWithName(string name)
    {
        var objs = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        foreach (var obj in objs)
        {
            if (obj is GameObject go && go.name == name && go.scene.name == "Intro")
            {
                return go;
            }
        }
        return null;
    }
}
#endif
