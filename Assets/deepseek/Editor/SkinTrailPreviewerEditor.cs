using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkinTrailPreviewer))]
public class SkinTrailPreviewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SkinTrailPreviewer previewer = (SkinTrailPreviewer)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Previous Skin"))
                previewer.PreviousSkin();

            if (GUILayout.Button("Next Skin"))
                previewer.NextSkin();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rebuild Preview"))
                previewer.RebuildPreview();

            if (GUILayout.Button("Clear Trail"))
                previewer.ClearTrail();
        }

        if (GUI.changed)
            EditorUtility.SetDirty(previewer);
    }
}
