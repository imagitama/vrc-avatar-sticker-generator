using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using PeanutTools_VRC_Avatar_Sticker_Generator_Editor;

[CustomEditor(typeof(VRCAvatarStickerGenerator))]
public class VRCAvatarStickerGeneratorEditor : Editor 
{
    SerializedProperty parameterSettings;
    ReorderableList parameterSettingsList;

    SerializedProperty animators;
    ReorderableList animatorsList;

    private void OnEnable()
    {
        parameterSettings = serializedObject.FindProperty("parameterSettings");
        parameterSettingsList = new ReorderableList(serializedObject, parameterSettings, true, true, true, true);
        parameterSettingsList.drawElementCallback = DrawParameterSettingsListListItems;
        parameterSettingsList.drawHeaderCallback = DrawParameterSettingsListHeader;

        animators = serializedObject.FindProperty("animatorControllers");
        animatorsList = new ReorderableList(serializedObject, animators, true, true, true, true);
        animatorsList.drawElementCallback = DrawAnimatorsListListItems;
        animatorsList.drawHeaderCallback = DrawAnimatorsListHeader;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        animatorsList.DoLayoutList();
        parameterSettingsList.DoLayoutList();

        if (CustomGUI.StandardButton("Reposition Camera")) {
            (target as VRCAvatarStickerGenerator).PositionCamera();
        }

        if (CustomGUI.StandardButton("Detect VRC Animators")) {
            (target as VRCAvatarStickerGenerator).AutoDetectAnimators();
            // Repaint();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    void DrawParameterSettingsListHeader(Rect rect)
    {
        string name = "Animator Parameters";
        EditorGUI.LabelField(rect, name);
    }

    void DrawParameterSettingsListListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = parameterSettingsList.serializedProperty.GetArrayElementAtIndex(index);

        EditorGUI.LabelField(new Rect(rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight), "Name");

        EditorGUI.PropertyField(
            new Rect(rect.x + 50, rect.y, 100, EditorGUIUtility.singleLineHeight), 
            element.FindPropertyRelative("name"),
            GUIContent.none
        ); 

        EditorGUI.LabelField(new Rect(rect.x + 175, rect.y, 50, EditorGUIUtility.singleLineHeight), "Type");

        EditorGUI.PropertyField(
            new Rect(rect.x + 225, rect.y, 50, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("type"),
            GUIContent.none
        ); 

        EditorGUI.LabelField(new Rect(rect.x + 300, rect.y, 50, EditorGUIUtility.singleLineHeight), "Value");

        var enumValueIndex = element.FindPropertyRelative("type").enumValueIndex;

        switch (enumValueIndex) {
            case 0:
                EditorGUI.PropertyField(
                    new Rect(rect.x + 350, rect.y, 100, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("floatValue"),
                    GUIContent.none
                );
                break;
            case 1:
                EditorGUI.PropertyField(
                    new Rect(rect.x + 350, rect.y, 100, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("intValue"),
                    GUIContent.none
                );
                break;
            case 2:
                EditorGUI.PropertyField(
                    new Rect(rect.x + 350, rect.y, 100, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("boolValue"),
                    GUIContent.none
                );
                break;
        }
    }

    void DrawAnimatorsListHeader(Rect rect)
    {
        string name = "Animators";
        EditorGUI.LabelField(rect, name);
    }

    void DrawAnimatorsListListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = animatorsList.serializedProperty.GetArrayElementAtIndex(index);

        EditorGUI.PropertyField(
            new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            element,
            GUIContent.none
        );
    }
}