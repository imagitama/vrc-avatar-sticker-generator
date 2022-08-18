using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using System.Reflection;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

public class VRCAvatarStickerGenerator : MonoBehaviour
{
    public class Frame {
        public byte[] bytes;
    }

    public Camera camera;

    // video
    public bool doVideo = false;
    public int frameLimit = 90;

    // photo
    public Transform head;
    public Animator animator;
    public RuntimeAnimatorController[] animatorControllers;
    public bool repositionCamera = true;
    public float cameraOffset = 0f;
    public bool disableTransitions = true;
    public int transitionDelay = 100;
    public ParameterSetting[] parameterSettings = new ParameterSetting[0];
    public bool randomlyRotateVertically = true;
    public bool randomlyRotateHorizontally = true;
    public int angleLimit = 10;
    public bool lookAtCamera = true;
    public Transform eyeLeft;
    public Transform eyeRight;
    public bool stripDynamicBones = true;
    public bool stopPlayingAtEnd = true;
    public bool hideBody = true;
    public Transform armatureToHide;
    public bool addBorder = true;
    public int borderWidth = 2;
    public bool emptyDirectories = true;
    public bool debugHideBody = false;

    // video
    private int currentFrameIdx = 0;
    private Frame[] frames;

    private bool done = false;
    private bool hasStopped = false;
    private System.Random random = new System.Random();
    private Vector3 originalArmatureScale;
    private Vector3 originalHeadScale;
    private string rawOutputPath;
    private string processedOutputPath;
    private Quaternion originalLeftEyeRotation;
    private Quaternion originalRightEyeRotation;

    public enum ParameterTypes {
        Float,
        Int,
        Bool
    }

    [Serializable]
    public struct ParameterSetting {
        public string name;
        public ParameterTypes type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    void Start()
    {
        rawOutputPath = Application.dataPath + "/../stickers";
        processedOutputPath = rawOutputPath + "/output";

        if (camera == null) {
            throw new System.Exception("No camera");
        }

        if (doVideo) {
            Application.targetFrameRate = 30;
            currentFrameIdx = 0;
            return;
        }

        if (animator == null) {
            throw new System.Exception("No animator");
        }

        if (animator.runtimeAnimatorController == null) {
            throw new System.Exception("No animator controller");
        }

        if (head == null) {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (hideBody && armatureToHide == null) {
            throw new System.Exception("No armature to hide");
        }

        if (lookAtCamera) {
            if (eyeLeft == null || eyeRight == null) {
                throw new System.Exception("No left or right eye");
            }

            if (animator.avatar != null && animator.GetBoneTransform(HumanBodyBones.LeftEye) != null) {
                throw new System.Exception("Found an avatar with left eye set (this breaks eye looking)");
            }

            originalLeftEyeRotation = eyeLeft.rotation;
            originalRightEyeRotation = eyeRight.rotation;
        }
    }

    void DebugHideBody() {
        ScaleAvatar();
    }

    bool IsAtRecordingLimit() {
        return currentFrameIdx == frameLimit;
    }

    void LateUpdate()
    {  
        if (doVideo) {
            if (IsAtRecordingLimit()) {
                StopPlaying();
                ProcessTextures();
                ProcessVideo();
            } else {
                if (!done) {
                    BeginVideo();
                    done = true;
                    return;
                }

                RecordVideo();
            }
            return;
        }

        if (debugHideBody) {
            DebugHideBody();

            if (repositionCamera) {
                PositionCamera();
            }
            return;
        }

        if (!done) {
            Begin();
            done = true;
        }
    }

    void RecordVideo() {
        StoreCameraToTexture(currentFrameIdx);
        currentFrameIdx++;
    }

    void CreateMissingDirs() {
        if (!Directory.Exists(rawOutputPath)) {
            Debug.Log("Creating output directory...");
            Directory.CreateDirectory(rawOutputPath);
        }

        string videoFramesDirPath = rawOutputPath + "/video-frames";

        if (!Directory.Exists(videoFramesDirPath)) {
            Debug.Log("Creating video frames directory...");
            Directory.CreateDirectory(videoFramesDirPath);
        }

        if (!Directory.Exists(processedOutputPath)) {
            Debug.Log("Creating processed output directory...");
            Directory.CreateDirectory(processedOutputPath);
        }
    }

    public void PositionCamera() {
        Debug.Log("Positioning camera...");

        if (head == null) {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        // if no avatar
        if (head == null) {
            return;
        }
        
        Vector3 cameraPosition = camera.transform.position;
        camera.transform.position = new Vector3(cameraPosition.x, head.position.y + cameraOffset, cameraPosition.z);
    }

    void OnApplicationQuit() {
        hasStopped = true;
    }

    void RotateMeshAroundHead(Vector3 axis, float angle) {
        animator.transform.RotateAround(head.position, axis, angle);
    }

    void ResetMeshRotation(Vector3 axis, float angle) {
        RotateMeshAroundHead(axis * -1, angle);
    }

    Type GetType(string name) {
        var result = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.Name == name
            select type).FirstOrDefault();

        return result;
    }

    void StripPhysBones() {
        var type = GetType("VRCPhysBone");
        
        if (type == null) {
            return;
        }

        Debug.Log("VRChat SDK (PhysBones) is loaded, stripping...");

        var physBones = animator.transform.GetComponentsInChildren(type);

        foreach (var physBone in physBones) {
            Destroy(physBone);
        }
    }

    void StripDynamicBones() {
        var type = GetType("DynamicBone");

        if (type == null) {
            return;
        }
        
        Debug.Log("DynamicBone is loaded, stripping...");

        var components = animator.transform.GetComponentsInChildren(type);

        foreach (var component in components) {
            Destroy(component);
        }
    }

    void PrepareAvatar() {
        if (stripDynamicBones) {
            StripPhysBones();
            StripDynamicBones();
        }
    }

    void EmptyDirs() {
        Directory.Delete(rawOutputPath, true);
    }

    async void BeginVideo() {
        Debug.Log("Begin video!");

        PrepareCamera();

        CreateMissingDirs();
        
        frames = new Frame[frameLimit];
    }

    void PrepareCamera() {
        // if we don't do this here then all images come out super dark
        RenderTexture renderTexture = new RenderTexture(1024, 1024, 24);
        camera.targetTexture = renderTexture;

        // sets the "main" thing we are rendering to the camera
        RenderTexture.active = camera.targetTexture;
    }

    async void Begin() {
        Debug.Log("Begin!");

        if (emptyDirectories) {
            EmptyDirs();
        }

        CreateMissingDirs();

        PrepareAvatar();

        if (hideBody) {
            ScaleAvatar();
        }

        if (repositionCamera) {
            PositionCamera();
        }

        PrepareCamera();

        var prefix = "";
        
        await ProcessGestures(prefix);

        if (hideBody) {
            RevertAvatarScale();
        }

        if (hasStopped) {
            return;
        }

        if (addBorder) {
            ProcessAllImages();
        }

        Debug.Log("Done!");

        if (stopPlayingAtEnd) {
            StopPlaying();
        }
    }

    void StopPlaying() {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void ResetParameters() {
        for (var i = 0; i < animator.parameters.Length; i++) {
            var parameter = animator.parameters[i];

            switch (parameter.type) {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(parameter.name, parameter.defaultBool);
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(parameter.name, parameter.defaultFloat);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(parameter.name, parameter.defaultInt);
                    break;
            }
        }
    }

    float GetRandomFloat(float min, float max) {
        double range = max - min;
        double sample = random.NextDouble();
        double scaled = (sample * range) + min;
        float f = (float)scaled;
        return f;
    }
    
    void CleanupAvatar() {
        animator.transform.position = new Vector3(0, 0, 0);
        animator.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void ScaleAvatar() {
        originalArmatureScale = armatureToHide.transform.localScale;

        armatureToHide.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);

        originalHeadScale = head.transform.localScale;

        head.transform.localScale = new Vector3(10000, 10000, 10000);
    }

    void RevertAvatarScale() {
        if (originalArmatureScale == null) {
            return;
        }
        armatureToHide.transform.localScale = originalArmatureScale;
        head.transform.localScale = originalHeadScale;
    }

    public void AutoDetectAnimators() {
        #if VRC_SDK_VRCSDK3
        var type = GetType("VRCAvatarDescriptor");

        if (type == null) {
            Debug.Log("VRC SDK is not loaded");
            return;
        }

        var component = animator.transform.GetComponent<VRCAvatarDescriptor>();

        var newAnimatorControllers = new List<RuntimeAnimatorController>();

        for (var i = 0; i < component.baseAnimationLayers.Length; i++) {
            var baseAnimationLayer = component.baseAnimationLayers[i];
            var animatorController = baseAnimationLayer.animatorController as RuntimeAnimatorController;

            if (animatorController != null) {
                newAnimatorControllers.Add(animatorController);
            }
        }

        animatorControllers = newAnimatorControllers.ToArray();
        #endif
    }
    
    public void AutoDetectVRCEyes() {
        #if VRC_SDK_VRCSDK3
        var type = GetType("VRCAvatarDescriptor");

        if (type == null) {
            Debug.Log("VRC SDK is not loaded");
            return;
        }

        var component = animator.transform.GetComponent<VRCAvatarDescriptor>();

        eyeLeft = component.customEyeLookSettings.leftEye;
        eyeRight = component.customEyeLookSettings.rightEye;
        #endif       
    }

    void RotateEyesToLookAtCamera() {
        eyeLeft.LookAt(camera.transform.position);
        eyeLeft.rotation *= originalLeftEyeRotation;
        eyeRight.LookAt(camera.transform.position);
        eyeRight.rotation *= originalRightEyeRotation;
    }

    void ApplyCustomParameters() {
        if (hasStopped) {
            return;
        }

        for (var i = 0; i < parameterSettings.Length; i++) {
            var setting = parameterSettings[i];

            switch (setting.type) {
                case ParameterTypes.Int:
                    animator.SetInteger(setting.name, setting.intValue);
                    break;
                case ParameterTypes.Float:
                    animator.SetFloat(setting.name, setting.floatValue);
                    break;
                case ParameterTypes.Bool:
                    animator.SetBool(setting.name, setting.boolValue);
                    break;
            }
        }
    }

    async Task ProcessGestures(string prefix) {
        for (int gestureLeftIdx = 0; gestureLeftIdx < 8; gestureLeftIdx++) {
            if (hasStopped) {
                CleanupAvatar();
                return;
            }

            Debug.Log("Gesture Left " + gestureLeftIdx);

            animator.SetInteger("GestureLeft", gestureLeftIdx);

            for (int gestureRightIdx = 0; gestureRightIdx < 8; gestureRightIdx++) {
                if (hasStopped) {
                    CleanupAvatar();
                    return;
                }

                Debug.Log("Gesture Right " + gestureRightIdx);
                
                animator.SetInteger("GestureRight", gestureRightIdx);

                ApplyCustomParameters();

                Vector3 rotationAxis = Vector3.up;
                float rotationAngle = 0f;
                
                if (randomlyRotateVertically || randomlyRotateHorizontally) {
                    rotationAxis = new Vector3(randomlyRotateVertically ? GetRandomFloat(-0.5f, 0.5f) : 0, randomlyRotateHorizontally ? GetRandomFloat(-0.5f, 0.5f) : 0, 0);
                    rotationAngle = (float)random.Next(angleLimit * -1, angleLimit);

                    RotateMeshAroundHead(rotationAxis, rotationAngle);
                }
                
                await Task.Delay(10);

                if (lookAtCamera) {
                    RotateEyesToLookAtCamera();
                }

                await Task.Delay(transitionDelay);

                string fileName = (prefix != "" ? prefix + "_" : "") + gestureLeftIdx + "_" + gestureRightIdx + ".png";
                string pngFilePath =  rawOutputPath + "/" + fileName;

                CameraToPng(pngFilePath);
                
                if (randomlyRotateVertically || randomlyRotateHorizontally) {
                    ResetMeshRotation(rotationAxis, rotationAngle);
                }
            }
        }
        
    }

    void CameraToPng(string filePath) {
        if (hasStopped) {
            return;
        }

        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
 
        camera.Render();
 
        Texture2D texture2d = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
        texture2d.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        texture2d.Apply();

        RenderTexture.active = activeRenderTexture;
 
        var bytes = texture2d.EncodeToPNG();

        Debug.Log(filePath);

        File.WriteAllBytes(filePath, bytes);
    }

    void ProcessAllImages() {
        Debug.Log("Processing all images...");

        string globToImages = rawOutputPath + "/*.png";

        if (!Directory.Exists(processedOutputPath)) {
            Debug.Log("Creating output directory...");
            Directory.CreateDirectory(processedOutputPath);
        }

        string fileName = Application.dataPath.Replace("/", "\\") + "\\PeanutTools\\VRC_Avatar_Sticker_Generator\\bin\\ImageMagick\\magick.exe";
        string args = "mogrify -path \"" + processedOutputPath + "\" -bordercolor none -border " + borderWidth.ToString() + " -background white -alpha background -channel A -blur 0x" + borderWidth.ToString() + " -level 0,0% -trim +repage -resize 512x512 \"" + globToImages + "\"";

        Debug.Log(fileName + " " + args);

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = args;
        process.Start();
        process.WaitForExit();
    }

    public void OpenOutputFolder() {
        OpenInExplorer(processedOutputPath);
    }

    void OpenInExplorer(string pathToOpen) {
        System.Diagnostics.Process.Start(pathToOpen);
    }

    IEnumerator StoreCameraToTexture(int frameIdx) {
        Debug.Log("Store camera frame " + frameIdx.ToString());

        yield return new WaitForEndOfFrame();

        Debug.Log("Render!");

        camera.Render();
 
        Texture2D texture2d = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);

        // Graphics.CopyTexture(RenderTexture.active, texture2d);

        // // reads pixels from active render target (which is RenderTexture.active)
        texture2d.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
        texture2d.Apply();

        byte[] bytes = texture2d.EncodeToPNG();

        UnityEngine.Object.Destroy(texture2d);

        frames[frameIdx] = new Frame() {
            bytes = bytes
        };
    }

    void ProcessTextures() {
        for (var i = 0; i < frames.Length; i++) {
            Frame frame = frames[i];

            // var bytes = texture2d.EncodeToPNG();

            var bytes = frame.bytes;

            var filePath = rawOutputPath + "/video-frames/" + i + ".png";

            Debug.Log(filePath);

            File.WriteAllBytes(filePath, bytes);
        }
    }
    
    void ProcessVideo() {
        Debug.Log("Processing video...");

        string globToImages = rawOutputPath + "/video-frames/%01d.png";

        if (!Directory.Exists(processedOutputPath)) {
            Debug.Log("Creating output directory...");
            Directory.CreateDirectory(processedOutputPath);
        }

        string outputPath = processedOutputPath + "/sticker.webm";

        // string fileName = Application.dataPath.Replace("/", "\\") + "\\PeanutTools\\VRC_Avatar_Sticker_Generator\\bin\\ImageMagick\\magick.exe";
        string fileName = "ffmpeg";
        string args = "-i '" + globToImages + "' -framerate 30 -vcodec libvpx-vp9 -b:v 400k -an -s 512x512 -pix_fmt yuva420p \"" + outputPath + "\"";

        Debug.Log(fileName + " " + args);

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = args;
        process.Start();
        process.WaitForExit();
    }
}
