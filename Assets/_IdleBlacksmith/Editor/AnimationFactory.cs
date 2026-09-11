using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IdleBlacksmith.EditorTools
{
    /// <summary>
    /// Authors AnimationClips in code (idle / walk / carry / hammer) and builds the
    /// AnimatorControllers with proper condition-driven transitions.
    /// Curve paths target the character rig: Model/Body, Model/Head, Model/ArmL, ...
    /// </summary>
    public static class AnimationFactory
    {
        const string Body = "Model/Body";
        const string Head = "Model/Head";
        const string ArmL = "Model/ArmL";
        const string ArmR = "Model/ArmR";
        const string LegL = "Model/LegL";
        const string LegR = "Model/LegR";

        public static void BuildAll()
        {
            AnimationClip idle = BuildIdle();
            AnimationClip walk = BuildWalk();
            AnimationClip carry = BuildCarry();
            AnimationClip hammer = BuildHammer();

            AnimatorController worker = BuildWorkerController(idle, walk, carry, hammer);
            AnimatorController customer = BuildCustomerController(idle, walk);

            AssignController(ModelFactory.WorkerPrefab, worker);
            AssignController(ModelFactory.HelperPrefab, worker);
            AssignController(ModelFactory.CustomerAPrefab, customer);
            AssignController(ModelFactory.CustomerBPrefab, customer);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------ clips

        static AnimationClip NewClip(string name)
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        static AnimationClip Save(AnimationClip clip)
        {
            string path = $"{Paths.Animations}/{clip.name}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static void Curve(AnimationClip clip, string path, string prop, params float[] timeValues)
        {
            var curve = new AnimationCurve();
            for (int i = 0; i + 1 < timeValues.Length; i += 2)
                curve.AddKey(new Keyframe(timeValues[i], timeValues[i + 1], 0f, 0f));
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        static AnimationClip BuildIdle()
        {
            var c = NewClip("Char_Idle");
            Curve(c, Body, "localScale.y", 0f, 1f, 1.3f, 0.978f, 2.6f, 1f);
            Curve(c, ArmL, "localEulerAngles.x", 0f, 3f, 1.3f, -2f, 2.6f, 3f);
            Curve(c, ArmR, "localEulerAngles.x", 0f, -3f, 1.3f, 2f, 2.6f, -3f);
            Curve(c, Head, "localEulerAngles.z", 0f, 0f, 1.3f, 2.5f, 2.6f, 0f);
            return Save(c);
        }

        static AnimationClip BuildWalk()
        {
            var c = NewClip("Char_Walk");
            Curve(c, LegL, "localEulerAngles.x", 0f, 28f, 0.275f, -28f, 0.55f, 28f);
            Curve(c, LegR, "localEulerAngles.x", 0f, -28f, 0.275f, 28f, 0.55f, -28f);
            Curve(c, ArmL, "localEulerAngles.x", 0f, -22f, 0.275f, 22f, 0.55f, -22f);
            Curve(c, ArmR, "localEulerAngles.x", 0f, 22f, 0.275f, -22f, 0.55f, 22f);
            Curve(c, Body, "localPosition.y", 0f, 0.34f, 0.1375f, 0.375f, 0.275f, 0.34f, 0.4125f, 0.375f, 0.55f, 0.34f);
            Curve(c, Body, "localEulerAngles.z", 0f, 2.5f, 0.275f, -2.5f, 0.55f, 2.5f);
            return Save(c);
        }

        static AnimationClip BuildCarry()
        {
            var c = NewClip("Char_Carry");
            Curve(c, LegL, "localEulerAngles.x", 0f, 28f, 0.275f, -28f, 0.55f, 28f);
            Curve(c, LegR, "localEulerAngles.x", 0f, -28f, 0.275f, 28f, 0.55f, -28f);
            Curve(c, ArmL, "localEulerAngles.x", 0f, -52f, 0.275f, -48f, 0.55f, -52f);
            Curve(c, ArmR, "localEulerAngles.x", 0f, -52f, 0.275f, -48f, 0.55f, -52f);
            Curve(c, Body, "localPosition.y", 0f, 0.34f, 0.1375f, 0.37f, 0.275f, 0.34f, 0.4125f, 0.37f, 0.55f, 0.34f);
            Curve(c, Body, "localEulerAngles.x", 0f, 4f, 0.55f, 4f);
            return Save(c);
        }

        static AnimationClip BuildHammer()
        {
            var c = NewClip("Char_Hammer");
            // Wind up, hold, strike fast, recover.
            Curve(c, ArmR, "localEulerAngles.x", 0f, 150f, 0.15f, 158f, 0.26f, -42f, 0.5f, 150f);
            Curve(c, ArmL, "localEulerAngles.x", 0f, 12f, 0.26f, 24f, 0.5f, 12f);
            Curve(c, Body, "localEulerAngles.x", 0f, -4f, 0.15f, -6f, 0.26f, 7f, 0.5f, -4f);
            AnimationUtility.SetAnimationEvents(c, new[]
            {
                new AnimationEvent { time = 0.26f, functionName = "OnHammerHit" }
            });
            return Save(c);
        }

        // ------------------------------------------------------------ controllers

        static AnimatorStateTransition T(AnimatorState from, AnimatorState to, float duration,
            params (AnimatorConditionMode mode, float threshold, string param)[] conds)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = duration;
            t.interruptionSource = TransitionInterruptionSource.None;
            foreach ((AnimatorConditionMode mode, float threshold, string param) c in conds)
                t.AddCondition(c.mode, c.threshold, c.param);
            return t;
        }

        static AnimatorController BuildWorkerController(AnimationClip idle, AnimationClip walk,
            AnimationClip carry, AnimationClip hammer)
        {
            string path = Paths.Animations + "/Worker.controller";
            AssetDatabase.DeleteAsset(path);
            var c = AnimatorController.CreateAnimatorControllerAtPath(path);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("Carry", AnimatorControllerParameterType.Bool);
            c.AddParameter("Hammer", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = c.layers[0].stateMachine;
            AnimatorState sIdle = sm.AddState("Idle");
            AnimatorState sWalk = sm.AddState("Walk");
            AnimatorState sCarry = sm.AddState("Carry");
            AnimatorState sHammer = sm.AddState("Hammer");
            sIdle.motion = idle;
            sWalk.motion = walk;
            sCarry.motion = carry;
            sHammer.motion = hammer;
            sm.defaultState = sIdle;

            T(sIdle, sWalk, 0.12f, (AnimatorConditionMode.Greater, 0.1f, "Speed"), (AnimatorConditionMode.IfNot, 0, "Carry"));
            T(sIdle, sCarry, 0.15f, (AnimatorConditionMode.If, 0, "Carry"), (AnimatorConditionMode.Greater, 0.1f, "Speed"));
            T(sWalk, sIdle, 0.12f, (AnimatorConditionMode.Less, 0.1f, "Speed"), (AnimatorConditionMode.IfNot, 0, "Carry"));
            T(sWalk, sCarry, 0.15f, (AnimatorConditionMode.If, 0, "Carry"));
            T(sCarry, sIdle, 0.12f, (AnimatorConditionMode.Less, 0.1f, "Speed"));
            T(sCarry, sWalk, 0.15f, (AnimatorConditionMode.IfNot, 0, "Carry"), (AnimatorConditionMode.Greater, 0.1f, "Speed"));

            AnimatorStateTransition toHammer = sm.AddAnyStateTransition(sHammer);
            toHammer.hasExitTime = false;
            toHammer.duration = 0.1f;
            toHammer.canTransitionToSelf = false;
            toHammer.AddCondition(AnimatorConditionMode.If, 0, "Hammer");

            T(sHammer, sIdle, 0.15f, (AnimatorConditionMode.IfNot, 0, "Hammer"));
            return c;
        }

        static AnimatorController BuildCustomerController(AnimationClip idle, AnimationClip walk)
        {
            string path = Paths.Animations + "/Customer.controller";
            AssetDatabase.DeleteAsset(path);
            var c = AnimatorController.CreateAnimatorControllerAtPath(path);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine sm = c.layers[0].stateMachine;
            AnimatorState sIdle = sm.AddState("Idle");
            AnimatorState sWalk = sm.AddState("Walk");
            sIdle.motion = idle;
            sWalk.motion = walk;
            sm.defaultState = sIdle;
            T(sIdle, sWalk, 0.12f, (AnimatorConditionMode.Greater, 0.1f, "Speed"));
            T(sWalk, sIdle, 0.12f, (AnimatorConditionMode.Less, 0.1f, "Speed"));
            return c;
        }

        static void AssignController(string prefabPath, RuntimeAnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            var animator = instance.GetComponent<Animator>();
            if (animator != null) animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }
}
