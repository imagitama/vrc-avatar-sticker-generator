using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Avatars.Components;
using cakeslice;

public class VRCAvatarStickerGenerator : EditorWindow
{
    [Serializable]
    class ParameterOverride {
        public string name;
        public string type;
        public string value;
    }

    int pixelWidth = 512;
    float distanceFromHead = 0.5f;
    float headOffset = -0.02f;
    int borderThickness = 10;
    bool hasStarted = false;
    string pathToStickersFolder;
    AnimatorController finalAnimatorController;
    GameObject cameraObject;
    Camera camera;
    bool isPreviewing = false;

    // confirmation
    [SerializeField]
    bool needsConfirmation;
    bool hasConfirmed = false;

    // single
    [SerializeField]
    bool isCreatingSingleSticker = false;
    [SerializeField]
    Gestures selectedGestureLeft;
    [SerializeField]
    Gestures selectedGestureRight;

    // override params
    bool isOverridingParams = false;
    string newParameterOverrideName;
    string newParameterOverrideType;
    string newParameterOverrideValue;
    List<ParameterOverride> defaultParameterOverrides = new List<ParameterOverride>() {
        new ParameterOverride() {
            name = "Grounded",
            type = "bool",
            value = "true"
        },
        new ParameterOverride() {
            name = "GestureLeftWeight",
            type = "float",
            value = "1.0"
        },
        new ParameterOverride() {
            name = "GestureRightWeight",
            type = "float",
            value = "1.0"
        }
    };
    
    [SerializeField]
    List<ParameterOverride> parameterOverrides = new List<ParameterOverride>() {
        new ParameterOverride() {
            name = "Grounded",
            type = "bool",
            value = "true"
        },
        new ParameterOverride() {
            name = "GestureLeftWeight",
            type = "float",
            value = "1.0"
        },
        new ParameterOverride() {
            name = "GestureRightWeight",
            type = "float",
            value = "1.0"
        }
    };

    // for resetting
    Vector3 originalArmatureScale;
    Vector3 originalHeadScale;
    Camera[] allCameras;
    float originalReflectionIntensity;

    [SerializeField]
    Transform head;

    [SerializeField]
    GameObject vrcAvatar;

    enum Gestures {
        none = 0,
        fist = 1,
        open = 2,
        point = 3,
        peace = 4,
        rocknroll = 5,
        gun = 6,
        thumbsup = 7
    }

    public VRCAvatarStickerGenerator() {
        if (parameterOverrides == null) {
            // ResetDefaultParameterOverrides();
        }
    }

    [MenuItem("PeanutTools/VRC Avatar Sticker Generator")]
    static void Init()
    {
        EditorWindow.GetWindow(typeof (VRCAvatarStickerGenerator));
    }

    void GuiLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color (0.5f, 0.5f, 0.5f, 1) );
    }

    private void OnGUI()
    {
        GUILayout.Label("VRC Avatar Sticker Generator", EditorStyles.boldLabel);
        GUILayout.Label("By @HiPeanutBuddha");
        GUILayout.Label("https://github.com/imagitama/vrc-avatar-sticker-generator");
        
        GUILayout.Label("");

        GuiLine();
        
        GUILayout.Label("");

        if (Application.isPlaying)
        {
            if (needsConfirmation == true && !hasConfirmed) {
                if (GUILayout.Button("Proceed")) {
                    ConfirmStickerCreation();
                }
            } else {
                GUILayout.Label("Creating stickers...");
            }
            return;
        }

        if (isPreviewing) {
            if (GUILayout.Button("Stop Preview"))
            {
                StopPreview();
            }
            return;
        }

        GUILayout.Label("Select an avatar with a VRChat avatar descriptor:");

        VRCAvatarDescriptor vrcAvatarDescriptor =
            EditorGUILayout
                .ObjectField(vrcAvatar, typeof (VRCAvatarDescriptor), true) as
            VRCAvatarDescriptor;

        if (vrcAvatarDescriptor)
        {
            vrcAvatar = vrcAvatarDescriptor.gameObject;
        }
        
        GUILayout.Label("");

        GUILayout.Label("Head bone the camera will look at (rest of body is hidden):");
        head = EditorGUILayout.ObjectField(head, typeof (Transform), true) as Transform;

        if (vrcAvatar != null && GUILayout.Button("Find Head")) {
            FindHead();
        }

        GUILayout.Label("");

        GUILayout.Label("Vertical offset head (default -0.02 for Canis Woof):");
        headOffset = EditorGUILayout.FloatField(headOffset);

        GUILayout.Label("");

        GUILayout.Label("Distance from head (default 0.5 for Canis Woof):");
        distanceFromHead = EditorGUILayout.FloatField(distanceFromHead);
        
        GUILayout.Label("");

        GUILayout.Label("Border thickness as pixels (default 10):");
        borderThickness = EditorGUILayout.IntField(borderThickness);
        
        GUILayout.Label("");

        GUILayout.Label("Wait for you to click a confirm button before actually creating the stickers (default false):");
        needsConfirmation = EditorGUILayout.Toggle("Require confirm:", needsConfirmation == null ? false : needsConfirmation);
        
        GUILayout.Label("");
        
        GuiLine();

        if (vrcAvatar == null || head == null)
        {
            GUILayout.Label("Waiting for a VRC avatar and head!");
            return;
        }
        
        GUILayout.Label("");

        if (GUILayout.Button("Preview"))
        {
            StartPreview();
        }
        
        if (isCreatingSingleSticker == false) {
            GUILayout.Label("");

            if (GUILayout.Button("Create All Stickers"))
            {
                EnterCreationMode();
            }
            GUILayout.Label("Warning: This overrides any existing stickers!");
        }

        GUILayout.Label("");

        GuiLine();
        
        GUILayout.Label("");

        if (GUILayout.Button("Toggle Create Single Sticker"))
        {
            ToggleCreateSingleSticker();
        }

        if (isCreatingSingleSticker) {
            GUILayout.Label("");

            selectedGestureLeft = (Gestures)EditorGUILayout.EnumPopup("Gesture Left:", selectedGestureLeft);
            selectedGestureRight = (Gestures)EditorGUILayout.EnumPopup("Gesture Right:", selectedGestureRight);

            if (selectedGestureLeft != null && selectedGestureRight != null) {
                if (GUILayout.Button("Create Single Sticker")) {
                    EnterCreationMode();
                }
                GUILayout.Label("Warning: This overrides any existing stickers!");
            }
        }

        GUILayout.Label("");

        GuiLine();
        
        GUILayout.Label("");

        if (GUILayout.Button("Toggle Override Params")) {
            ToggleOverrideParams();
        }

        if (isOverridingParams) {
            GUILayout.Label("");

            if (parameterOverrides.Count == 0) {
                GUILayout.Label("No parameters found");
            }

            foreach (ParameterOverride parameterOverride in parameterOverrides.ToList()) {
                GUILayout.Label(parameterOverride.name + " => " + parameterOverride.value);
                
                if (GUILayout.Button("Delete", GUILayout.Width(100))) {
                    DeleteParameterOverride(parameterOverride);
                }
            }

            GUILayout.Label("");

            GUILayout.Label("Parameter name:");
            newParameterOverrideName = EditorGUILayout.TextField(newParameterOverrideName);

            GUILayout.Label("Parameter type (bool, string, float, int):");
            newParameterOverrideType = EditorGUILayout.TextField(newParameterOverrideType);
            
            GUILayout.Label("Parameter value:");
            newParameterOverrideValue = EditorGUILayout.TextField(newParameterOverrideValue);

            if (GUILayout.Button("Add")) {
                AddParameterOverride(newParameterOverrideName, newParameterOverrideType, newParameterOverrideValue);
            }

            GUILayout.Label("");

            if (GUILayout.Button("Reset To Defaults")) {
                ResetDefaultParameterOverrides();
            }
        }
    }

    void ConfirmStickerCreation() {
        hasConfirmed = true;

        if (isCreatingSingleSticker) {
            CreateSingleSticker();
        } else {
            CreateStickers();
        }
    }
    
    void ResetDefaultParameterOverrides() {
        parameterOverrides = defaultParameterOverrides.ToList();
    }

    void DeleteParameterOverride(ParameterOverride parameterOverride) {
        parameterOverrides.Remove(parameterOverride);
    }

    void AddParameterOverride(string name, string type, string value) {
        ParameterOverride newParameterOverride = new ParameterOverride() {
            name = name,
            type = type,
            value = value
        };
        parameterOverrides.Add(newParameterOverride);
    }
    
    void ToggleOverrideParams() {
        isOverridingParams = !isOverridingParams; 
    }

    void ToggleCreateSingleSticker() {
        isCreatingSingleSticker = !isCreatingSingleSticker; 
    }

    void EnterCreationMode() {
        Debug.Log("Starting to create single sticker...");

        hasStarted = false;
        hasConfirmed = false;

        EditorApplication.EnterPlaymode();
    }

    void FindHead() {
        Transform match = vrcAvatar.GetComponentsInChildren<Transform>().Where(k => k.gameObject.name == "Head").FirstOrDefault();

        if (match != null) {
            head = match;
        }
    }

    async void Update()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        // wait for deserialization
        if (vrcAvatar == null || head == null || needsConfirmation == null)
        {
            return;
        }

        if (!hasStarted)
        {
            hasStarted = true;

            PrepareForCreation();

            Debug.Log(needsConfirmation);
            Debug.Log(hasConfirmed);

            if (needsConfirmation == true && !hasConfirmed) {
                return;
            } else {
                if (isCreatingSingleSticker) {
                    CreateSingleSticker();
                } else {
                    CreateStickers();
                }
            }
        }
    }

    void OnDrawGizmos() {
        DrawDebug();
    }

    void DrawDebug() {
        SkinnedMeshRenderer [] skinnedMeshRenderers = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();

        // Debug.Log("Found " + skinnedMeshRenderers + " skinned mesh renderers");

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers) {
            Mesh mesh = new Mesh(); 

            skinnedMeshRenderer.BakeMesh(mesh);

            Vector3[] vertices = mesh.vertices;

            // Debug.Log(vertices.Length);

            for (int i = 0; i < vertices.Length; i++) {
                // Gizmos.color = Color.yellow;
                // Gizmos.DrawSphere(vertices[i], 0.1f);

                if (i > 0) {
                    Handles.DrawLine(vertices[i], vertices[i - 1]);
                }
            }
        }
    }

    void StartPreview() {
        isPreviewing = true;
        SetLighting();
        ScaleAvatar();
        AddOutline();
        CreateCamera();
        AddDebugging();
        FocusOnCamera();
        FocusCameraOnAvatar();
    }

    void AddDebugging() {
        camera.gameObject.AddComponent<DebugVRCAvatarStickerGenerator>();
    }

    void StopPreview() {
        isPreviewing = false;
        RevertLighting();
        RevertAvatarScale();
        RemoveOutline();
        StopFocusingOnCamera();
        RemoveCamera();
    }

    void FocusOnCamera() {
        // allCameras returns all ENABLED cameras so when we disable them all we will lose them
        allCameras = Camera.allCameras;

        foreach (Camera cameraItem in allCameras) {
            cameraItem.enabled = false;
        }

        camera.enabled = true;

        Selection.activeGameObject = camera.gameObject;
    }

    void StopFocusingOnCamera() {
        foreach (Camera cameraItem in allCameras) {
            cameraItem.enabled = true;
        }
    }

    async void PrepareForCreation() {
        CreateFolders();

        SetLighting();

        ScaleAvatar();

        AddOutline();

        CreateFinalAnimatorController();

        AddAnimators();

        AddFinalAnimatorControllerToAvatar();

        await WaitForAnimatorToApply();

        CreateCamera();
        
        AddDebugging();
    }

    async void CreateSingleSticker()
    {
        if (selectedGestureLeft == null || selectedGestureRight == null) {
            Debug.Log("Cannot create single sticker without left and right!");
            return;
        }

        CreateStickers(selectedGestureLeft, selectedGestureRight);
    }

    async void CreateStickers(Gestures? gestureLeft = null, Gestures? gestureRight = null)
    {
        Debug.Log("Creating stickers...");
    
        if (gestureLeft != null && gestureRight != null) {
            await ActuallyCreateSingleSticker((Gestures)gestureLeft, (Gestures)gestureRight);
        } else {
            await ActuallyCreateAllStickers();
        }

        Debug.Log("Stickers have been created");

        StopPlaying();
    }

    void StopPlaying() {
        EditorApplication.isPlaying = false;
    }

    void SetLighting() {
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
        RenderSettings.reflectionIntensity = 0;
    }
    
    void RevertLighting() {
        if (originalReflectionIntensity != null) {
            return;
        }
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
    }

    void ScaleAvatar() {
        Transform armature = vrcAvatar.transform.Find("Armature");

        originalArmatureScale = armature.transform.localScale;

        armature.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

        originalHeadScale = head.transform.localScale;

        head.transform.localScale = new Vector3(10000, 10000, 10000);
    }

    void RevertAvatarScale() {
        if (originalArmatureScale == null) {
            return;
        }
        vrcAvatar.transform.Find("Armature").transform.localScale = originalArmatureScale;
        head.transform.localScale = originalHeadScale;
    }

    void CreateFolders() {
        pathToStickersFolder = Path.GetDirectoryName(Application.dataPath) + "/Stickers";
        
        if (!Directory.Exists(pathToStickersFolder)) {
            Debug.Log("Creating directory...");
            Directory.CreateDirectory(pathToStickersFolder);
            Debug.Log("Done");
        }
    }

    void ReplaceAnimator() {
        Animator animator = vrcAvatar.GetComponent<Animator>();
        animator.runtimeAnimatorController = null;
        AddFinalAnimatorControllerToAvatar();
    }

    string GetFileNameForGestures(Gestures gestureLeft, Gestures gestureRight) {
        return "left_" + Enum.GetName(typeof (Gestures), gestureLeft) + "_right_" + Enum.GetName(typeof (Gestures), gestureRight);
    }

    async Task ActuallyCreateSingleSticker(Gestures gestureLeft, Gestures gestureRight) {
        // the only guaranteed way to make sure gestures do not do weird behavior to each other
        ReplaceAnimator();

        Animator animator = vrcAvatar.GetComponent<Animator>();

        animator.SetInteger("GestureLeft", (int)gestureLeft);
        animator.SetInteger("GestureRight", (int)gestureRight);

        await WaitForGestureToApply();

        string filename = GetFileNameForGestures(gestureLeft, gestureRight);
        
        Debug.Log(filename);

        FocusCameraOnAvatar();
        
        await WaitForGestureToApply();

        CreateStickerUsingCamera(filename);
    }

    async Task ActuallyCreateAllStickers() {
        int leftHandValue = 0;
        int rightHandValue = 0;

        string workingOn = "LeftHand";

        List<string> completedFilenames = new List<string>();

        for (int i = 0; i < 64; i++) {
            // catch weird case where it keeps going even after exit play mode
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            // the only guaranteed way to make sure gestures do not do weird behavior to each other
            ReplaceAnimator();

            Animator animator = vrcAvatar.GetComponent<Animator>();

            animator.SetInteger("GestureLeft", leftHandValue);
            animator.SetInteger("GestureRight", rightHandValue);

            await WaitForGestureToApply();

            string filename = GetFileNameForGestures((Gestures)leftHandValue, (Gestures)rightHandValue);

            Debug.Log(filename);

            FocusCameraOnAvatar();
            
            await WaitForGestureToApply();

            CreateStickerUsingCamera(filename);

            if (completedFilenames.Exists(item => item == filename)) {
                Debug.Log("Filename " + filename + " already exists!");
            }

            completedFilenames.Add(filename);

            if (workingOn == "LeftHand") {
                if (leftHandValue < 7) {
                    leftHandValue = leftHandValue + 1;
                } else {
                    workingOn = "LeftHandWithRight";
                    leftHandValue = 0;
                    rightHandValue = 0;
                }
            } else if (workingOn == "LeftHandWithRight") {
                if (rightHandValue < 7) {
                    rightHandValue = rightHandValue + 1;
                } else if (leftHandValue < 7) {
                    leftHandValue = leftHandValue + 1;
                    rightHandValue = 0;
                } else {
                    workingOn = "RightHand";
                    leftHandValue = 0;
                    rightHandValue = 0;
                }
            } else if (workingOn == "RightHand") {
                if (rightHandValue < 7) {
                    rightHandValue = rightHandValue + 1;
                } else {
                    workingOn = "RightHandWithLeft";
                    leftHandValue = 0;
                    rightHandValue = 0;
                }
            } else if (workingOn == "RightHandWithLeft") {
                if (leftHandValue < 7) {
                    leftHandValue = leftHandValue + 1;
                } else if (rightHandValue < 7) {
                    rightHandValue = rightHandValue + 1;
                    leftHandValue = 0;
                } else {
                    // done
                }
            }
        }
    }

    void AddOutline() {
        Debug.Log("Adding outline to avatar...");

        Renderer[] renderers = vrcAvatar.GetComponentsInChildren<Renderer>();

        Debug.Log("Found " + renderers.Length + " renderers");

        JumpFloodOutlineRenderer jumpFloodOutlineRenderer = vrcAvatar.AddComponent<JumpFloodOutlineRenderer>();
        jumpFloodOutlineRenderer.outlinePixelWidth = borderThickness;
        jumpFloodOutlineRenderer.renderers = new List<Renderer>(renderers);

        Debug.Log("Done");
    }

    void RemoveOutline() {
        Debug.Log("Removing outline from avatar...");

        DestroyImmediate(vrcAvatar.GetComponent<JumpFloodOutlineRenderer>());

        Debug.Log("Done");
    }

    async Task WaitForAnimatorToApply() {
        await Task.Delay(1000);
    }

    async Task WaitForGestureToApply() {
        await Task.Delay(1000);
    }

    void CreateCamera()
    {
        Debug.Log("Creating camera...");

        cameraObject = new GameObject("StickerCam");

        // position
        Vector3 headPosition = GetHeadPosition();
        cameraObject.transform.position = new Vector3(headPosition.x, headPosition.y, headPosition.z + distanceFromHead);
        cameraObject.transform.LookAt(headPosition);
        cameraObject.transform.position = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y + headOffset, cameraObject.transform.position.z);

        camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.01f; // 0 will break camera!
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);

        RenderTexture renderTexture = new RenderTexture(pixelWidth, pixelWidth, 24);
        camera.targetTexture = renderTexture;
        
        Debug.Log("Camera created");
    }

    void RemoveCamera() {
        if (cameraObject == null) {
            return;
        }
        DestroyImmediate(cameraObject);
    }

    void CreateStickerUsingCamera(string filenameWithoutExt) {
        // catch end play mode
        if (camera == null) {
            return;
        }

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
 
        Texture2D Image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        Image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        Image.Apply();
        RenderTexture.active = currentRT;
 
        var Bytes = Image.EncodeToPNG();
        Destroy(Image);
 
        File.WriteAllBytes(pathToStickersFolder + "/" + filenameWithoutExt + ".png", Bytes);
    }

    Bounds GetBoundsFromMesh(Mesh mesh) {
        Vector3 center = new Vector3(0, 0, 0);

        Quaternion newRotation = new Quaternion();
        newRotation.eulerAngles = new Vector3(-90, 0, 0);

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 verticePosition = newRotation * (vertices[i] - center) + center;

            min = Vector3.Min(min, verticePosition);
            max = Vector3.Max(max, verticePosition);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);

        return bounds;
    }

    void FocusCameraOnAvatar() {
        // catch when we exit play mode
        if (camera == null) {
            return;
        }

        SkinnedMeshRenderer skinnedMeshRenderer = vrcAvatar.transform.Find("Body").gameObject.GetComponent<SkinnedMeshRenderer>();

        Mesh mesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(mesh);

        // Vector3[] vertices = mesh.vertices;
        // Vector3 center = new Vector3(0, 0, 0);
        // Quaternion newRotation = new Quaternion();
        // newRotation.eulerAngles = new Vector3(-90, 0, 0);
        
        // for (int i = 0; i < vertices.Length; i++) {
        //     vertices[i] = newRotation * (vertices[i] - center) + center;
        // }

        // mesh.vertices = vertices;

        // GameObject tempObject = new GameObject("TempMesh");
        // tempObject.AddComponent<MeshRenderer>();
        // MeshFilter meshFilter = tempObject.AddComponent<MeshFilter>();
        // meshFilter.mesh = mesh;

        // tempObject.transform.position = new Vector3(0, 0, 0);

        Bounds bounds = GetBoundsFromMesh(mesh);

        // Vector3 xyz = bounds.size;
        // float distance = Mathf.Max(xyz.x, xyz.y, xyz.z);
        // distance /= (2.0f * Mathf.Tan(0.5f * camera.fieldOfView * Mathf.Deg2Rad));
        // camera.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, distance - 0.25f);



        float cameraDistance = distanceFromHead; // Constant factor
Vector3 objectSizes = bounds.max - bounds.min;
float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView); // Visible height 1 meter in front
float distance = cameraDistance * objectSize / cameraView; // Combined wanted distance from the object
distance += 0.5f * objectSize; // Estimated offset from the center to the outside of the object
camera.transform.position = bounds.center - distance * camera.transform.forward;
camera.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y + headOffset, camera.transform.position.z);


// camera.transform.position = vrcAvatar.transform.position * -(2 * bounds.size.y);


        // Vector3 objectFrontCenter = bounds.center - tempObject.transform.forward * bounds.extents.z;

        // //Get the far side of the triangle by going up from the center, at a 90 degree angle of the camera's forward vector.
        // Vector3 triangleFarSideUpAxis = Quaternion.AngleAxis(90, tempObject.transform.right) * camera.transform.forward;
        // //Calculate the up point of the triangle.
        // const float MARGIN_MULTIPLIER = 2f;
        // Vector3 triangleUpPoint = objectFrontCenter + triangleFarSideUpAxis * bounds.extents.y * MARGIN_MULTIPLIER;

        // //The angle between the camera and the top point of the triangle is half the field of view.
        // //The tangent of this angle equals the length of the opposing triangle side over the desired distance between the camera and the object's front.
        // float desiredDistance = Vector3.Distance(triangleUpPoint, objectFrontCenter) / Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView / 2);

        // camera.transform.position = -camera.transform.forward * desiredDistance + objectFrontCenter;
    }

    void AddFinalAnimatorControllerToAvatar()
    {
        Animator animator = vrcAvatar.GetComponent<Animator>();
        animator.runtimeAnimatorController = finalAnimatorController;

        foreach (ParameterOverride parameterOverride in parameterOverrides) {
            Debug.Log("Setting param " + parameterOverride.name + " to " + parameterOverride.value);

            switch (parameterOverride.type) {
                case "bool":
                    animator.SetBool(parameterOverride.name, bool.Parse(parameterOverride.value));
                    break;
                case "int":
                    animator.SetInteger(parameterOverride.name, Int32.Parse(parameterOverride.value));
                    break;
                case "float":
                    animator.SetFloat(parameterOverride.name, float.Parse(parameterOverride.value));
                    break;
            }
        }
    }

    void CreateFinalAnimatorController()
    {
        finalAnimatorController = new AnimatorController();
    }

    Vector3 GetHeadPosition()
    {
        return head.transform.position;
    }

    VRCAvatarDescriptor GetAvatarDescriptor()
    {
        return vrcAvatar.GetComponent<VRCAvatarDescriptor>();
    }

    AnimatorController[] GetAnimatorControllersForAvatar()
    {
        VRCAvatarDescriptor.CustomAnimLayer[] customAnimLayers =
            GetAvatarDescriptor().baseAnimationLayers;

        AnimatorController[] animatorControllers =
            new AnimatorController[customAnimLayers.Length];

        for (int i = 0; i < customAnimLayers.Length; i++)
        {
            animatorControllers[i] =
                customAnimLayers[i].animatorController as AnimatorController;
        }

        return animatorControllers;
    }

    void AddAnimators()
    {
        AnimatorController[] animatorControllers =
            GetAnimatorControllersForAvatar();

        Debug
            .Log("Found " +
            animatorControllers.Length +
            " animator controllers on VRC avatar");

        for (int i = 0; i < animatorControllers.Length; i++)
        {
            // if user has not specified one
            if (animatorControllers[i] == null)
            {
                continue;
            }

            AddAnimator(animatorControllers[i]);
        }
    }

    GameObject GetRootGameObjectForAvatar()
    {
        return vrcAvatar.gameObject;
    }

    void MergeVrcAnimatorIntoFinalAnimatorController(
        AnimatorController animatorToMerge
    )
    {
        Debug.Log("Merging vrc animator \"" + animatorToMerge.name + "\"...");

        // // we modify everything in place so we don't want to mutate the original
        // AnimatorController animatorToMerge = CopyVrcAnimatorForMerge(originalAnimatorController);
        AnimatorControllerParameter[] existingParams =
            finalAnimatorController.parameters;
        AnimatorControllerParameter[] newParams = animatorToMerge.parameters;

        Debug.Log("Found " + newParams.Length + " parameters in this animator");

        // for (int p = 0; p < newParams.Length; p++)
        // {
        //     newParams[p].type = AnimatorControllerParameterType.Float;
        // }
        finalAnimatorController.parameters =
            GetParametersWithoutDupes(newParams, existingParams);

        AnimatorControllerLayer[] existingLayers =
            finalAnimatorController.layers;

        AnimatorControllerLayer[] layersToMerge = animatorToMerge.layers;

        Debug.Log("Found " + layersToMerge.Length + " layers to merge");

        // CVR breaks if any layer names are the same
        layersToMerge = FixDuplicateLayerNames(layersToMerge, existingLayers);

        AnimatorControllerLayer[] newLayers =
            new AnimatorControllerLayer[existingLayers.Length +
            layersToMerge.Length];

        int newLayersIdx = 0;

        for (int i = 0; i < existingLayers.Length; i++)
        {
            newLayers[newLayersIdx] = existingLayers[i];
            newLayersIdx++;
        }

        for (int i = 0; i < layersToMerge.Length; i++)
        {
            AnimatorControllerLayer layer = layersToMerge[i];

            Debug
                .Log("Layer \"" +
                layer.name +
                "\" with " +
                layer.stateMachine.states.Length +
                " states");

            // ProcessStateMachine(layer.stateMachine);
            newLayers[newLayersIdx] = layer;
            newLayersIdx++;
        }

        finalAnimatorController.layers = newLayers;

        Debug.Log("Merged");
    }

    AnimatorControllerLayer[]
    FixDuplicateLayerNames(
        AnimatorControllerLayer[] newLayers,
        AnimatorControllerLayer[] existingLayers
    )
    {
        foreach (AnimatorControllerLayer newLayer in newLayers)
        {
            foreach (AnimatorControllerLayer existingLayer in existingLayers)
            {
                if (existingLayer.name == newLayer.name)
                {
                    Debug
                        .Log("Layer \"" +
                        newLayer.name +
                        "\" clashes with an existing layer, renaming...");

                    // TODO: This is fragile cause they could have another layer with the same name
                    // Maybe check again if it exists whenever we rename it
                    newLayer.name = newLayer.name + "_1";
                }
            }
        }

        return newLayers;
    }

    AnimatorControllerParameter[]
    GetParametersWithoutDupes(
        AnimatorControllerParameter[] newParams,
        AnimatorControllerParameter[] existingParams
    )
    {
        List<AnimatorControllerParameter> finalParams =
            new List<AnimatorControllerParameter>(existingParams);

        for (int x = 0; x < newParams.Length; x++)
        {
            bool doesAlreadyExist = false;

            for (int y = 0; y < existingParams.Length; y++)
            {
                if (existingParams[y].name == newParams[x].name)
                {
                    doesAlreadyExist = true;
                }
            }

            //  Debug.Log("WITHOUT DUPE: " + newParams[x].name + " yes? " + (doesAlreadyExist == true ? "EXISTS" : " NO EXISTS"));
            if (doesAlreadyExist == false)
            {
                finalParams.Add(newParams[x]);
            }
        }

        return finalParams.ToArray();
    }

    void AddAnimator(AnimatorController animatorControllerToAdd)
    {
        Debug.Log("Adding " + animatorControllerToAdd.name + " to avatar...");

        MergeVrcAnimatorIntoFinalAnimatorController (animatorControllerToAdd);
    }
}
