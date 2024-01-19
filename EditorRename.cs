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

    [MenuItem("Tools/Asset Renamer")] // Hier wird der Menüpfad festgelegt
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<RenameAssetsScript>("Asset Renamer");
    }
    void OnGUI()
    {
        GUILayout.Label("Asset Renamer", EditorStyles.boldLabel);

        GUIContent searchTermContent = new GUIContent("Search Term:", "Enter the term to search for and you want to replace with replacement term");
        searchTerm = EditorGUILayout.TextField(searchTermContent, searchTerm);

        GUIContent replacementTermContent = new GUIContent("Replacement Term:", "Enter the term witch should replace the search term");
        replacementTerm = EditorGUILayout.TextField(replacementTermContent, replacementTerm);

        GUIContent renameContentsContent = new GUIContent("Would you like to change contents of Scripts with have the search term in their name?", "Enable this option if you want to change the contents of script files in addition to renaming assets. Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Lets find out :D");
        RenameContents = EditorGUILayout.ToggleLeft(renameContentsContent, RenameContents);

        GUIContent ShouldSearchallScenes = new GUIContent("Would you like to change all contents of your Scripts? (Trust me it takes a while)", "Enable this option if you want to change the contents of ALL script files in addition to renaming assets. Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Lets find out :D");
        SearchAllContents = EditorGUILayout.ToggleLeft(ShouldSearchallScenes, SearchAllContents);

        GUIContent ScenesInBuildSettings = new GUIContent("Would you like to change the Name of your GameObjects in Scene?", "Enable this option if you want to change the name from GameObjects an Prefabs that are currently in your Scenes. The Scenes must be referenced in your buildsettings! Maybe it breaks something, who knows ¯\\_(ツ)_/¯. Lets find out :D");
        ChangeObjectsInScenes = EditorGUILayout.ToggleLeft(ScenesInBuildSettings, SearchAllContents);

        GUILayout.Space(20);

        float scrollViewHeight = Mathf.Min(800, position.height - 200);
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

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Rename Assets", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Do you really want to risk it and rename your assets? It could break your project, do you have an bakeup? (Just in case)", "Yes", "No"))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    if (ChangeObjectsInScenes)
                    {
                        SearchAndReplaceInAllScenes();
                    }

                    RenameAssets();
                    EditorUtility.DisplayDialog("It's ready", "The renaming was succesfully! I knew it!", "Ok");
                }
                else
                {
                    EditorUtility.DisplayDialog("Whoops, there was a mistake", "You've forgotten the Search Term, please enter one", "Ok");
                }
            }
        }
    }


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
                    Debug.LogError("Whoops, there was a mistake made: " + e.Message);
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
                    Debug.LogError("Whoops, there was a mistake made: " + e.Message);
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
                Debug.Log("New name" + obj.name);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            }
        }
    }


    void SaveScene(string scenePath)
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        Debug.Log("Szene was saved: " + scenePath);
    }

}





//18.1.2024
//Von Noah Kirsch

