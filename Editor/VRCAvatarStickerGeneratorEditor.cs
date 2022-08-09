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
        CustomGUI.BoldLabel("VRC Avatar Sticker Generator");
        GUILayout.Label("Generates Telegram stickers from a VRChat (or ChilloutVR) avatar.");

        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("Step 1: Enter required info");
        
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("camera"), new GUIContent("Camera"));
        CustomGUI.ItalicLabel("The camera to use for each sticker (the render texture will be replaced)");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("head"), new GUIContent("Head"));
        CustomGUI.ItalicLabel("The head of your avatar (used to re-position the camera and when hiding the body)");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), new GUIContent("Avatar Animator"));
        CustomGUI.ItalicLabel("The animator for your avatar");
        CustomGUI.SmallLineGap();

        animatorsList.DoLayoutList();
        CustomGUI.ItalicLabel("Each animator will be merged together and will replace your avatar\'s animator");
        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Detect VRC Animators")) {
            (target as VRCAvatarStickerGenerator).AutoDetectAnimators();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }
        CustomGUI.ItalicLabel("Uses your avatar animator to detect your VRC Avatar Descriptor.");

        CustomGUI.LineGap();
        CustomGUI.BoldLabel("Step 2: Optional settings");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("repositionCamera"), new GUIContent("Re-position Camera"));
        CustomGUI.ItalicLabel("If to move the camera at all");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOffset"), new GUIContent("Camera Offset"));
        CustomGUI.ItalicLabel("Applies a Y axis offset after positioning the camera at the head bone");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("disableTransitions"), new GUIContent("Disable Transitions"));
        CustomGUI.ItalicLabel("Disables all state transitions in your animators");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDelay"), new GUIContent("Transition Delay (ms)"));
        CustomGUI.ItalicLabel("The delay before taking a photo (useful if you do not disable transitions)");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomlyRotateVertically"), new GUIContent("Rotate Vertically"));
        CustomGUI.ItalicLabel("If to rotate the head vertically with a random angle");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomlyRotateHorizontally"), new GUIContent("Rotate Horizontally"));
        CustomGUI.ItalicLabel("If to rotate the head horizontally with a random angle");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("angleLimit"), new GUIContent("Angle Limit"));
        CustomGUI.ItalicLabel("How much to rotate vertically or horizontally (eg. -20 to 20)");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookAtCamera"), new GUIContent("Look At Camera"));
        CustomGUI.ItalicLabel("Make the eyes look at the camera (only useful if you randomly rotate the head)");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("eyeLeft"), new GUIContent("Left Eye"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("eyeRight"), new GUIContent("Right Eye"));

        if (CustomGUI.StandardButton("Detect VRC Eyes")) {
            (target as VRCAvatarStickerGenerator).AutoDetectVRCEyes();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }

        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("stripDynamicBones"), new GUIContent("Strip DynamicBones"));
        CustomGUI.ItalicLabel("DynamicBones (and VRChat PhysBones) take time to apply");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stopPlayingAtEnd"), new GUIContent("Auto-Stop Playing"));
        CustomGUI.ItalicLabel("If to automatically exit Play mode at the end");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("emptyDirectories"), new GUIContent("Empty Output"));
        CustomGUI.ItalicLabel("If to empty the output folder before taking any photos");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hideBody"), new GUIContent("Hide Body"));
        CustomGUI.ItalicLabel("If to hide your avatar's body (by shrinking it to oblivion and making the head huge)");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("armatureToHide"), new GUIContent("Armature"));
        CustomGUI.ItalicLabel("The body to hide (set it to your armature)");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("addBorder"), new GUIContent("Add Border"));
        CustomGUI.ItalicLabel("If to use ImageMagick to add a white border around your stickers");
        CustomGUI.SmallLineGap();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderWidth"), new GUIContent("Border Width"));
        CustomGUI.ItalicLabel("The border width (pixels)");
        CustomGUI.SmallLineGap();

        CustomGUI.LineGap();
        CustomGUI.BoldLabel("Step 3: Configure parameters");
        CustomGUI.SmallLineGap();

        parameterSettingsList.DoLayoutList();
        
        CustomGUI.LineGap();
        CustomGUI.BoldLabel("Step 4: Run");
        CustomGUI.SmallLineGap();

        GUILayout.Label("Enter play mode to generate stickers!");
        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Open Output Folder")) {
            (target as VRCAvatarStickerGenerator).OpenOutputFolder();
        }

        CustomGUI.SmallLineGap();
        CustomGUI.BoldLabel("Debugging");
        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Reposition Camera")) {
            (target as VRCAvatarStickerGenerator).PositionCamera();
        }

        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugHideBody"), new GUIContent("Debug Hide Body"));

        CustomGUI.LineGap();

        CustomGUI.MyLinks("vrc-avatar-sticker-generator");
        
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