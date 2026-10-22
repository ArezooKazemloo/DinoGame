using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Creates the required Run, Jump, Hit, and Death state machine.</summary>
public static class DinoAnimatorSetup
{
    private const string ArtFolder = "Assets/Art";
    private const string AnimationFolder = "Assets/Animations";
    private const string ControllerPath = AnimationFolder + "/DinoRunner.controller";

    [MenuItem("Dino Game/Setup Animator (Run, Jump, Hit, Death)")]
    public static void Setup()
    {
        if (GameObject.Find("Player") == null && System.IO.File.Exists("Assets/Scenes/SampleScene.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EnsureFolder(AnimationFolder);
        Sprite[] cartoonRun = LoadFrameSequence("Run", 8);
        Sprite[] cartoonJump = LoadFrameSequence("Jump", 12);
        Sprite[] cartoonDeath = LoadFrameSequence("Dead", 8);
        bool useCartoonArt = cartoonRun.Length == 8 && cartoonJump.Length == 12 && cartoonDeath.Length == 8;
        Sprite[] runFrames;
        Sprite[] jumpFrames;
        Sprite[] hitFrames;
        Sprite[] deathFrames;
        Sprite run1;
        Sprite run2;
        if (useCartoonArt)
        {
            runFrames = cartoonRun;
            jumpFrames = cartoonJump;
            deathFrames = cartoonDeath;
            hitFrames = new[] { cartoonDeath[0] };
            run1 = cartoonRun[0];
            run2 = cartoonRun[1];
        }
        else
        {
            run1 = LoadSprite("Dino_Run1.png");
            run2 = LoadSprite("Dino_Run2.png");
            Sprite jump = LoadSprite("Dino_Jump.png");
            Sprite hit = LoadSprite("Dino_Hit.png");
            Sprite death = LoadSprite("Dino_Death.png");
            runFrames = new[] { run1, run2 };
            jumpFrames = new[] { jump };
            hitFrames = new[] { hit };
            deathFrames = new[] { death };
        }
        if (run1 == null || run2 == null || runFrames.Length == 0 || jumpFrames.Length == 0 || hitFrames.Length == 0 || deathFrames.Length == 0)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Dino Animator", "Unity is still importing the dinosaur art. Wait a moment and run this command again.", "OK");
            return;
        }

        AnimationClip runClip = CreateClip("Dino_Run.anim", runFrames, useCartoonArt ? .10f : .13f, true);
        AnimationClip jumpClip = CreateClip("Dino_Jump.anim", jumpFrames, useCartoonArt ? .07f : .22f, false);
        AnimationClip hitClip = CreateClip("Dino_Hit.anim", hitFrames, .16f, false);
        AnimationClip deathClip = CreateClip("Dino_Death.anim", deathFrames, useCartoonArt ? .14f : .4f, false);
        AnimationClip dashClip = CreateClip("Dino_Dash.anim", new[] { runFrames[Mathf.Min(1, runFrames.Length - 1)], runFrames[0] }, .07f, false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AddParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "Running", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Death", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Dash", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState runState = FindOrCreateState(machine, "Run", runClip, new Vector3(300, 100));
        AnimatorState jumpState = FindOrCreateState(machine, "Jump", jumpClip, new Vector3(550, 0));
        AnimatorState hitState = FindOrCreateState(machine, "Hit", hitClip, new Vector3(550, 130));
        AnimatorState deathState = FindOrCreateState(machine, "Death", deathClip, new Vector3(550, 260));
        AnimatorState dashState = FindOrCreateState(machine, "Dash", dashClip, new Vector3(300, 300));
        machine.defaultState = runState;

        AddAnyTransition(machine, jumpState, "Jump");
        AddAnyTransition(machine, hitState, "Hit");
        AddAnyTransition(machine, deathState, "Death");
        AddAnyTransition(machine, dashState, "Dash");
        AnimatorStateTransition land = jumpState.AddTransition(runState);
        land.hasExitTime = false; land.duration = .04f; land.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        AnimatorStateTransition hitReturn = hitState.AddTransition(runState);
        hitReturn.hasExitTime = true; hitReturn.exitTime = 1f; hitReturn.duration = .04f;
        AnimatorStateTransition dashReturn = dashState.AddTransition(runState);
        dashReturn.hasExitTime = true; dashReturn.exitTime = 1f; dashReturn.duration = .02f;

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Dino Animator", "Open the game scene, then run this command again.", "OK");
            return;
        }
        Animator animator = player.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(player);
        Undo.RecordObject(animator, "Assign Dino Animator Controller");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer != null) { Undo.RecordObject(playerRenderer, "Set Dino Art"); playerRenderer.sprite = run1; }
        GameObject ground = GameObject.Find("Ground");
        if (ground != null && ground.GetComponent<SpriteRenderer>() is SpriteRenderer groundRenderer)
        {
            Undo.RecordObject(groundRenderer, "Style Ground");
            groundRenderer.color = new Color(.11f, .29f, .25f);
        }
        GameObject scoreItem = GameObject.Find("ScoreItem");
        if (scoreItem != null && scoreItem.GetComponent<SpriteRenderer>() is SpriteRenderer itemRenderer)
        {
            Undo.RecordObject(itemRenderer, "Style Score Item");
            itemRenderer.color = new Color(1f, .76f, .27f);
        }
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        AssetDatabase.SaveAssets();
        if (!Application.isBatchMode) EditorUtility.DisplayDialog("Dino Animator", useCartoonArt
            ? "Cartoon dinosaur animations are ready and assigned to Player. Save the scene."
            : "Animator states and transitions are ready and assigned to Player. Save the scene.", "OK");
    }

    private static Sprite[] LoadFrameSequence(string animation, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string path = $"Assets/Art/FreeDinoPack/png/{animation} ({i + 1}).png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return System.Array.Empty<Sprite>();
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           !Mathf.Approximately(importer.spritePixelsPerUnit, 512f) ||
                           importer.filterMode != FilterMode.Bilinear ||
                           importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 512f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null) return System.Array.Empty<Sprite>();
        }
        return frames;
    }

    private static Sprite LoadSprite(string file)
    {
        string path = ArtFolder + "/" + file;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 64 || importer.filterMode != FilterMode.Point))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AnimationClip CreateClip(string file, Sprite[] sprites, float frameDuration, bool loop)
    {
        string path = AnimationFolder + "/" + file;
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = 12f };
            AssetDatabase.CreateAsset(clip, path);
        }
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i * frameDuration, value = sprites[i] };
        keys[keys.Length - 1] = new ObjectReferenceKeyframe
        {
            time = sprites.Length * frameDuration,
            value = loop ? sprites[0] : sprites[sprites.Length - 1]
        };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name, AnimationClip clip, Vector3 position)
    {
        foreach (ChildAnimatorState child in machine.states) if (child.state.name == name) { child.state.motion = clip; return child.state; }
        AnimatorState state = machine.AddState(name, position);
        state.motion = clip;
        return state;
    }

    private static void AddAnyTransition(AnimatorStateMachine machine, AnimatorState destination, string trigger)
    {
        foreach (AnimatorStateTransition child in machine.anyStateTransitions)
            if (child.destinationState != null && child.destinationState.name == destination.name) return;
        AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
        transition.hasExitTime = false; transition.duration = .035f;
        transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters) if (parameter.name == name) return;
        controller.AddParameter(name, type);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
