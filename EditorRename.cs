using UnityEngine;
using UnityEditor;
using System.IO;
using System;


public class RenameAssetsScript : EditorWindow
{
    private string searchTerm = ""; //To Search 
    private string replacementTerm = ""; //Renaming 

    [MenuItem("Tools/Rename Assets")]
    static void Init()
    {
        RenameAssetsScript window = (RenameAssetsScript)EditorWindow.GetWindow(typeof(RenameAssetsScript));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Asset Renamer", EditorStyles.boldLabel);

        searchTerm = EditorGUILayout.TextField("Suchbegriff:", searchTerm);
        replacementTerm = EditorGUILayout.TextField("Ersatzbegriff:", replacementTerm);

        if (GUILayout.Button("Assets umbenennen"))
        {
            if (EditorUtility.DisplayDialog("Bestätigung", "Möchten Sie die Assets wirklich umbenennen?", "Ja", "Nein"))
            {
                RenameAssets();
                EditorUtility.DisplayDialog("Abgeschlossen", "Die Umbenennung der Assets wurde abgeschlossen.", "OK");
            }
        }
    }

    void RenameAssets()
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:Object", null);

        foreach (string assetGuid in assetGuids)
        {
            try
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);

                if (assetName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    string newName = assetName.Replace(searchTerm, replacementTerm, StringComparison.OrdinalIgnoreCase);


                    AssetDatabase.RenameAsset(assetPath, newName);
                    Debug.Log("Asset umbenannt (Editor): " + newName);
                    string oldFilePath = Application.dataPath + assetPath.Substring("Assets".Length);
                    string newFilePath = Application.dataPath + assetPath.Replace(assetName, newName).Substring("Assets".Length);
                    File.Move(oldFilePath, newFilePath);
                    Debug.Log("Asset umbenannt (Dateisystem): " + newFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Fehler beim Umbenennen des Assets: " + e.Message);
            }
        }

        AssetDatabase.Refresh();
    }


    //Do not think about the errors its fine.


}








//18.1.2024
//Von Noah Kirsch