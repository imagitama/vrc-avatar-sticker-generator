using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;
using PeanutTools_VRC_Avatar_Sticker_Generator_Editor;

[CustomEditor(typeof(VRCAvatarStickerGenerator))]
public class VRCAvatarStickerGeneratorEditor : Editor 
{
    SerializedProperty parameterSettings;
    UnityEditorInternal.ReorderableList parameterSettingsList;

    SerializedProperty animators;
    UnityEditorInternal.ReorderableList animatorsList;

    private void OnEnable() {
        PrepareLists();
    }

    private void PrepareLists()
    {
        parameterSettings = serializedObject.FindProperty("parameterSettings");
        parameterSettingsList = new UnityEditorInternal.ReorderableList(serializedObject, parameterSettings, true, true, true, true);
        parameterSettingsList.drawElementCallback = DrawParameterSettingsListListItems;
        parameterSettingsList.drawHeaderCallback = DrawParameterSettingsListHeader;

        animators = serializedObject.FindProperty("animatorControllers");

        animatorsList = new UnityEditorInternal.ReorderableList(serializedObject, animators, true, true, true, true);
        animatorsList.drawElementCallback = DrawAnimatorsListListItems;
        animatorsList.drawHeaderCallback = DrawAnimatorsListHeader;
    }

    AnimatorController MergeAnimatorControllers(AnimatorController[] animatorControllersToMerge) {
        var newAnimatorController = new AnimatorController();
        List<AnimatorControllerLayer> newLayers = new List<AnimatorControllerLayer>();
        List<AnimatorControllerParameter> newParameters = new List<AnimatorControllerParameter>();

        for (var i = 0; i < animatorControllersToMerge.Length; i++) {
            // TODO: Cleanup after us?
            AnimatorController tempAnimatorController = Animators.CopyAnimatorController(animatorControllersToMerge[i], "Assets/stickers_temp/animator_" + i.ToString() + ".controller");

            var layers = tempAnimatorController.layers;

            for (var l = 0; l < tempAnimatorController.layers.Length; l++) {
                var layer = tempAnimatorController.layers[l];
                newLayers.Add(layer);
            }

            var parameters = tempAnimatorController.parameters;

            for (var p = 0; p < parameters.Length; p++) {
                var parameter = parameters[p];
                newParameters.Add(parameter);
            }
        }

        newAnimatorController.parameters = newParameters.ToArray();
        newAnimatorController.layers = newLayers.ToArray();

        return newAnimatorController;
    }

    void ApplyAnimators() {
        var animatorControllersProperty = serializedObject.FindProperty("animatorControllers");
        var size = animatorControllersProperty.arraySize;
        var animatorControllers = new List<AnimatorController>();

        for (var i = 0; i < size; i++) {
            var arrayItemProperty = animatorControllersProperty.GetArrayElementAtIndex(i);
            var animatorController = arrayItemProperty.objectReferenceValue as AnimatorController;
            animatorControllers.Add(animatorController);
        }

        var newAnimatorController = MergeAnimatorControllers(animatorControllers.ToArray());

        var disableTransitions = serializedObject.FindProperty("disableTransitions").boolValue;

        if (disableTransitions) {
            newAnimatorController = RemoveAnimatorTransitions(newAnimatorController);
        }

        ApplyAnimatorController(newAnimatorController);
    }

    void ApplyAnimatorController(AnimatorController newAnimatorController) {
        var animator = serializedObject.FindProperty("animator").objectReferenceValue as Animator;

        if (animator == null) {
            throw new System.Exception("Cannot apply animator controller without a reference to an animator");
        }

        animator.runtimeAnimatorController = newAnimatorController;
    }

    AnimatorController RemoveAnimatorTransitions(AnimatorController animatorController) {
        List<AnimatorControllerLayer> newLayers = new List<AnimatorControllerLayer>();

        for (var i = 0; i < animatorController.layers.Length; i++) {
            var layer = animatorController.layers[i];

            var stateMachine = layer.stateMachine;

            for (var t = 0; t < stateMachine.anyStateTransitions.Length; t++) {
                var transition = stateMachine.anyStateTransitions[t];
                transition.duration = 0;
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                stateMachine.anyStateTransitions[t] = transition;
            }

            for (var s = 0; s < stateMachine.states.Length; s++) {
                var state = stateMachine.states[s].state;

                for (var t = 0; t < state.transitions.Length; t++) {
                    var transition = state.transitions[t];
                    transition.duration = 0;
                    transition.hasExitTime = false;
                    transition.hasFixedDuration = true;
                    state.transitions[t] = transition;
                }
            }

            layer.stateMachine = stateMachine;

            newLayers.Add(layer);
        }
        
        animatorController.layers = newLayers.ToArray();

        return animatorController;
    }

    VRCAvatarStickerGenerator.ParameterTypes MapAnimatorControllerType(AnimatorControllerParameterType type) {
            switch (type) {
                case AnimatorControllerParameterType.Float:
                    return VRCAvatarStickerGenerator.ParameterTypes.Float;
                case AnimatorControllerParameterType.Int:
                    return VRCAvatarStickerGenerator.ParameterTypes.Int;
                case AnimatorControllerParameterType.Bool:
                    return VRCAvatarStickerGenerator.ParameterTypes.Bool;
                default:
                    throw new System.Exception("Unknown type " + type.ToString());
            }
    }

    public void PopulateParameters() {
        var animator = serializedObject.FindProperty("animator").objectReferenceValue as Animator;

        if (animator == null || animator.runtimeAnimatorController == null) {
            throw new System.Exception("Cannot populate parameters without an animator and a controller");
        }

        var parameters = (animator.runtimeAnimatorController as AnimatorController).parameters;

        Debug.Log("Found " + parameters.Length + " parameters");

        var newParameterSettings = new List<VRCAvatarStickerGenerator.ParameterSetting>();

        for (var i = 0; i < parameters.Length; i++) {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == "GestureLeft" || parameter.name == "GestureRight" || newParameterSettings.Exists(p => p.name == parameter.name)) {
                continue;
            }

            newParameterSettings.Add(new VRCAvatarStickerGenerator.ParameterSetting() {
                name = parameter.name,
                type = MapAnimatorControllerType(parameter.type),
                boolValue = parameter.defaultBool,
                intValue = parameter.defaultInt,
                floatValue = parameter.defaultFloat,
            });
        }

        (target as VRCAvatarStickerGenerator).parameterSettings = newParameterSettings.ToArray();
    }

    bool GetIsAnimatorWronglySet() {
        return (serializedObject.FindProperty("animator").objectReferenceValue as Animator) != null && (serializedObject.FindProperty("animator").objectReferenceValue as Animator).runtimeAnimatorController != null && AssetDatabase.Contains((serializedObject.FindProperty("animator").objectReferenceValue as Animator).runtimeAnimatorController);
    }

    bool GetIsAnimatorSet() {
        return (serializedObject.FindProperty("animator").objectReferenceValue as Animator) != null && (serializedObject.FindProperty("animator").objectReferenceValue as Animator).runtimeAnimatorController == null;
    }

    public override void OnInspectorGUI()
    {
         serializedObject.Update();

        CustomGUI.BoldLabel("VRC Avatar Sticker Generator");
        GUILayout.Label("Generates Telegram stickers from a VRChat (or ChilloutVR) avatar.");

        CustomGUI.SmallLineGap();
        CustomGUI.LargeLabel("Step 1: Set up your camera");
        CustomGUI.SmallLineGap();

        GUILayout.Label("Position your camera so that it is facing your avatar then set it below.");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("camera"), new GUIContent("Camera"));
        CustomGUI.ItalicLabel("Note that the render texture will automatically be replaced");
        CustomGUI.SmallLineGap();

        CustomGUI.LargeLabel("Step 2: Configure your avatar");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), new GUIContent("Avatar"));
        CustomGUI.ItalicLabel("Your avatar must have an \"animator\" component on it");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("head"), new GUIContent("Head"));
        CustomGUI.ItalicLabel("The head of your avatar (used to re-position the camera and when hiding the body)");
        CustomGUI.SmallLineGap();
        
        CustomGUI.LargeLabel("Step 3: Merge your animators");
        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("You must merge your animators every time this list changes");
        CustomGUI.SmallLineGap();

        animatorsList.DoLayoutList();

        #if VRC_SDK_VRCSDK3
        if (CustomGUI.StandardButton("Detect VRC Animators")) {
            (target as VRCAvatarStickerGenerator).AutoDetectAnimators();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }
        #endif

        CustomGUI.SmallLineGap();

        if (GetIsAnimatorWronglySet()) {
            CustomGUI.RenderWarningMessage("Your animator already has a controller (you should delete it)");
        } else if (GetIsAnimatorSet()) {
            CustomGUI.RenderErrorMessage("You must click the button below to merge your animators");
        } else {
            CustomGUI.RenderSuccessMessage("Your animator has been set");
        }

        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Merge Animators")) {
            ApplyAnimators();

            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }

        CustomGUI.LineGap();
        CustomGUI.LargeLabel("Step 4: Settings (optional)");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("repositionCamera"), new GUIContent("Re-position Camera"));
        CustomGUI.ItalicLabel("If to move the camera at all");
        CustomGUI.SmallLineGap();

        if (serializedObject.FindProperty("repositionCamera").boolValue) {
            if (CustomGUI.StandardButton("Preview Camera")) {
                (target as VRCAvatarStickerGenerator).PositionCamera();
            }

            CustomGUI.SmallLineGap();
        }

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

        EditorGUILayout.PropertyField(serializedObject.FindProperty("addOilPainting"), new GUIContent("Add Oil Painting Effect"));
        CustomGUI.ItalicLabel("If to add an \"oil painting\" effect to each sticker");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("oilPaintingAmount"), new GUIContent("Effect Amount"));
        CustomGUI.ItalicLabel("The effect amount (1-2 works best)");
        CustomGUI.SmallLineGap();

        CustomGUI.LineGap();
        CustomGUI.LargeLabel("Step 3: Configure parameters (optional)");
        CustomGUI.SmallLineGap();

        parameterSettingsList.DoLayoutList();
        
        if (CustomGUI.StandardButton("Populate")) {
            PopulateParameters();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }
        CustomGUI.ItalicLabel("Warning: This deletes all existing parameters");

        CustomGUI.LineGap();
        CustomGUI.LargeLabel("Step 4: Generate");
        CustomGUI.SmallLineGap();

        GUILayout.Label("Enter play mode to generate stickers!");
        CustomGUI.SmallLineGap();

        if (CustomGUI.StandardButton("Open Output Folder")) {
            (target as VRCAvatarStickerGenerator).OpenOutputFolder();
        }

        CustomGUI.SmallLineGap();
        CustomGUI.LargeLabel("Debugging");
        CustomGUI.SmallLineGap();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugHideBody"), new GUIContent("Hide Body"));

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

        var margin = 50;

        var firstColumnPerc = 0.4f;
        var firstColumnWidth = rect.width * firstColumnPerc;

        EditorGUI.PropertyField(
            new Rect(margin, rect.y, firstColumnWidth, EditorGUIUtility.singleLineHeight), 
            element.FindPropertyRelative("name"),
            GUIContent.none
        );
        
        var secondColumnPerc = 0.2f;
        var secondColumnWidth = rect.width * secondColumnPerc;

        EditorGUI.PropertyField(
            new Rect(margin + firstColumnWidth, rect.y, secondColumnWidth, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("type"),
            GUIContent.none
        );

        var thirdColumnPerc = 0.4f;
        var thirdColumnWidth = rect.width * thirdColumnPerc - margin;

        var enumValueIndex = element.FindPropertyRelative("type").enumValueIndex;

        switch (enumValueIndex) {
            case 0:
                EditorGUI.PropertyField(
                    new Rect(margin + firstColumnWidth + secondColumnWidth, rect.y, thirdColumnWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("floatValue"),
                    GUIContent.none
                );
                break;
            case 1:
                EditorGUI.PropertyField(
                    new Rect(margin + firstColumnWidth + secondColumnWidth, rect.y, thirdColumnWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("intValue"),
                    GUIContent.none
                );
                break;
            case 2:
                EditorGUI.PropertyField(
                    new Rect(margin + firstColumnWidth + secondColumnWidth, rect.y, thirdColumnWidth, EditorGUIUtility.singleLineHeight),
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