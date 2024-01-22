using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class RenameAssetsScript : EditorWindow
{
    private string searchTerm = "";
    private string replacementTerm = "";
    private List<string> selectedAssets = new List<string>();

    private bool RenameContents;
    private string[] assetGuids;

    private bool SearchAllContents;

    private bool ChangeObjectsInScenes;

    private Vector2 scrollPosition;
    private Regex regex;

    [MenuItem("Tools/Asset Renamer")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<RenameAssetsScript>("Asset Renamer");
    }

    void OnGUI()
    {
        GUILayout.Label("Asset Renamer", EditorStyles.boldLabel);
        GUILayout.Label("General", EditorStyles.boldLabel);

        GUIContent searchTermContent = new GUIContent("Search Term:", "Enter the term to search for and replace with the replacement term");
        searchTerm = EditorGUILayout.TextField(searchTermContent, searchTerm);

        GUIContent replacementTermContent = new GUIContent("Replacement Term:", "Enter the term which should replace the search term");
        replacementTerm = EditorGUILayout.TextField(replacementTermContent, replacementTerm);

        GUILayout.Space(10);
        GUILayout.Label("Include Scripts", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUIContent renameContentsContent = new GUIContent("Include Script Content from Scripts that you've checked", "Enable this option if you want to change the contents of script files in addition to renaming assets. Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Let's find out :D");
        RenameContents = EditorGUILayout.ToggleLeft(renameContentsContent, RenameContents);

        GUIContent shouldSearchAllScenes = new GUIContent("Include content of all your Scripts? (Trust me it takes a while)", "Enable this option if you want to change the contents of ALL script files in addition to renaming assets. Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Let's find out :D");
        SearchAllContents = EditorGUILayout.ToggleLeft(shouldSearchAllScenes, SearchAllContents);

        GUILayout.Space(10);
        GUILayout.Label("Include Scenes", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUIContent scenesInBuildSettings = new GUIContent("Would you like to change the Name of your GameObjects in your Scenes?", "Enable this option if you want to change the name of GameObjects and Prefabs that are currently in your Scenes. The Scenes must be referenced in your build settings! Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Let's find out :D");
        ChangeObjectsInScenes = EditorGUILayout.ToggleLeft(scenesInBuildSettings, ChangeObjectsInScenes);

        GUILayout.Space(20);
        GUILayout.Label("Check the Assets you want to rename", EditorStyles.boldLabel);
        GUILayout.Space(5);

        float scrollViewHeight = Mathf.Min(500, position.height - 200);
scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollViewHeight));

assetGuids = AssetDatabase.FindAssets("t:Object", null);
foreach (string assetGuid in assetGuids)
{
    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
    string assetName = Path.GetFileNameWithoutExtension(assetPath);

    if (assetName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1)
    {
        bool isSelected = selectedAssets.Contains(assetPath);
        bool newSelection = EditorGUILayout.ToggleLeft(assetName, isSelected);

        if (newSelection != isSelected)
        {
            if (newSelection)
                selectedAssets.Add(assetPath);
            else
                selectedAssets.Remove(assetPath);
        }
    }
}

EditorGUILayout.EndScrollView();

GUILayout.BeginHorizontal();

GUILayout.FlexibleSpace();

if (GUILayout.Button("Rename Assets", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
{
    if (EditorUtility.DisplayDialog("Confirm", "Do you really want to risk it and rename your assets? It could break your project. Do you have a backup? (Just in case)", "Yes", "No"))
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            if (ChangeObjectsInScenes)
            {
                SearchAndReplaceInAllScenes();
            }

            RenameAssets();
            EditorUtility.DisplayDialog("It's ready", "The renaming was successful! I knew it!", "Ok");
        }
        else
        {
            EditorUtility.DisplayDialog("Whoops, there was a mistake", "You've forgotten the Search Term, please enter one", "Ok");
        }
    }
}

GUILayout.Space(20);

GUILayout.EndHorizontal();}

    void RenameAssets()
    {
        if (SearchAllContents)
        {
            RenameContents = false;
            regex = new Regex(searchTerm, RegexOptions.IgnoreCase);
            foreach (string assetGuid in assetGuids)
            {
                try
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                                        string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string extension = Path.GetExtension(assetPath);

                    if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] scriptLines = File.ReadAllLines(assetPath);
                        for (int i = 0; i < scriptLines.Length; i++)
                        {
                            scriptLines[i] = regex.Replace(scriptLines[i], replacementTerm);
                        }
                        File.WriteAllLines(assetPath, scriptLines);

                        Debug.Log("The content of Script has been changed: " + assetPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Whoops, there was a mistake: " + e.Message);
                }
            }

            foreach (string assetPath in selectedAssets)
            {
                try
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string extension = Path.GetExtension(assetPath);

                    if (!SearchAllContents)
                    {
                        if (RenameContents)
                        {
                            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                            {
                                string[] scriptLines = File.ReadAllLines(assetPath);
                                for (int i = 0; i < scriptLines.Length; i++)
                                {
                                    scriptLines[i] = regex.Replace(scriptLines[i], replacementTerm);
                                }
                                File.WriteAllLines(assetPath, scriptLines);

                                Debug.Log("The content of Script has been changed: " + assetPath);
                            }
                        }
                    }

                    string newName = assetName.Replace(searchTerm, replacementTerm, StringComparison.OrdinalIgnoreCase);

                    if (newName != assetName)
                    {
                        AssetDatabase.RenameAsset(assetPath, newName);
                        Debug.Log("Asset renamed (Editor): " + newName);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Whoops, there was a mistake: " + e.Message);
                }
            }

            AssetDatabase.Refresh();
        }
    }

    void SearchAndReplaceInAllScenes()
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            EditorSceneManager.OpenScene(scene.path);
            SearchAndReplace();
            SaveScene(scene.path);
        }
    }

    void SearchAndReplace()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            Debug.Log(obj.name);
            if (obj.name.Contains(searchTerm))
            {
                Undo.RecordObject(obj, "Name replace");
                obj.name = obj.name.Replace(searchTerm, replacementTerm);
                Debug.Log("New name: " + obj.name);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }

    void SaveScene(string scenePath)
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        Debug.Log("Scene was saved: " + scenePath);
    }
}
