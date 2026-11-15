using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Generates a base Animator Controller for the NetworkPlayer with all required
/// parameters and placeholder states so the game compiles and runs without
/// a hand-authored animator.
///
/// Usage: Tools → Battleground X → Generate Animator Controller
/// </summary>
public static class AnimatorControllerGenerator
{
    private const string OutputPath = "Assets/Animations/NetworkPlayer.controller";

    [MenuItem("Tools/Battleground X/Generate Animator Controller")]
    public static AnimatorController Generate()
    {
        // ── Ensure folder exists ──────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        // ── Create controller ─────────────────────────────────────────────
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);

        // ── Parameters ────────────────────────────────────────────────────
        ctrl.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsAiming",    AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsGrounded",  AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsDead",      AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Fire",        AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Reload",      AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Jump",        AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("TakeDamage",  AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("WeaponIndex", AnimatorControllerParameterType.Int);

        // ── Base layer ────────────────────────────────────────────────────
        var root = ctrl.layers[0].stateMachine;

        // States
        var idle    = root.AddState("Idle");
        var walk    = root.AddState("Walk");
        var run     = root.AddState("Run");
        var sprint  = root.AddState("Sprint");
        var crouch  = root.AddState("Crouch_Idle");
        var crWalk  = root.AddState("Crouch_Walk");
        var jump    = root.AddState("Jump");
        var airFall = root.AddState("AirFall");
        var fire    = root.AddState("Fire");
        var reload  = root.AddState("Reload");
        var damage  = root.AddState("TakeDamage");
        var dead    = root.AddState("Dead");

        // Position states in graph (for readability in Animator window)
        idle.position    = new Vector3(300,   0, 0);
        walk.position    = new Vector3(500,  -80, 0);
        run.position     = new Vector3(500,    0, 0);
        sprint.position  = new Vector3(500,   80, 0);
        crouch.position  = new Vector3(300, -160, 0);
        crWalk.position  = new Vector3(500, -160, 0);
        jump.position    = new Vector3(300,  160, 0);
        airFall.position = new Vector3(500,  160, 0);
        fire.position    = new Vector3(700,    0, 0);
        reload.position  = new Vector3(700,  -80, 0);
        damage.position  = new Vector3(700,   80, 0);
        dead.position    = new Vector3(700,  160, 0);

        root.defaultState = idle;

        // ── Transitions ───────────────────────────────────────────────────

        // Idle → Walk (Speed >= 0.5)
        AddTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.5f);
        // Walk → Idle
        AddTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.5f);
        // Walk → Run
        AddTransition(walk, run, "Speed", AnimatorConditionMode.Greater, 1.5f);
        // Run → Walk
        AddTransition(run, walk, "Speed", AnimatorConditionMode.Less, 1.5f);
        // Run → Sprint
        AddTransition(run, sprint, "Speed", AnimatorConditionMode.Greater, 2.5f);
        // Sprint → Run
        AddTransition(sprint, run, "Speed", AnimatorConditionMode.Less, 2.5f);

        // Any → Crouch (IsCrouching = true)
        AddAnyTransition(root, crouch, "IsCrouching", true, hasExitTime: false);
        AddTransition(crouch, idle,  "IsCrouching", false, hasExitTime: false);
        AddTransition(crouch, crWalk,"Speed", AnimatorConditionMode.Greater, 0.3f);
        AddTransition(crWalk, crouch,"Speed", AnimatorConditionMode.Less,    0.3f);

        // Jump trigger
        AddTriggerTransition(idle,   jump, "Jump");
        AddTriggerTransition(walk,   jump, "Jump");
        AddTriggerTransition(run,    jump, "Jump");
        AddTriggerTransition(sprint, jump, "Jump");
        // Jump → AirFall (exit time 0.4)
        var jToFall = jump.AddTransition(airFall);
        jToFall.hasExitTime = true;
        jToFall.exitTime    = 0.4f;
        // AirFall → Idle (IsGrounded = true)
        AddTransition(airFall, idle, "IsGrounded", true);

        // Fire trigger from any
        AddTriggerAnyTransition(root, fire,   "Fire");
        var fireExit = fire.AddTransition(idle);
        fireExit.hasExitTime = true; fireExit.exitTime = 1f;

        // Reload trigger
        AddTriggerAnyTransition(root, reload, "Reload");
        var reloadExit = reload.AddTransition(idle);
        reloadExit.hasExitTime = true; reloadExit.exitTime = 1f;

        // TakeDamage
        AddTriggerAnyTransition(root, damage, "TakeDamage");
        var dmgExit = damage.AddTransition(idle);
        dmgExit.hasExitTime = true; dmgExit.exitTime = 1f;

        // Death — IsDead = true overrides everything
        AddAnyTransition(root, dead, "IsDead", true, hasExitTime: false);

        // ── Save ──────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BattlegroundX] Animator Controller generated at: " + OutputPath);
        EditorUtility.DisplayDialog("Animator Controller",
            "Generated at:\n" + OutputPath +
            "\n\nAssign motion clips to each state in the Animator window.\n" +
            "The controller is already wired into the NetworkPlayer prefab by the Scene Generator.",
            "OK");

        return ctrl;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AddTransition(AnimatorState from, AnimatorState to,
        string param, AnimatorConditionMode mode, float threshold)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0.1f;
        t.AddCondition(mode, threshold, param);
    }

    private static void AddTransition(AnimatorState from, AnimatorState to,
        string boolParam, bool value, bool hasExitTime = false)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExitTime;
        t.duration    = 0.1f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, boolParam);
    }

    private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState to,
        string boolParam, bool value, bool hasExitTime)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime  = hasExitTime;
        t.duration     = 0.1f;
        t.canTransitionToSelf = false;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, boolParam);
    }

    private static void AddTriggerAnyTransition(AnimatorStateMachine sm, AnimatorState to, string trigger)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime         = false;
        t.duration            = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }
}
