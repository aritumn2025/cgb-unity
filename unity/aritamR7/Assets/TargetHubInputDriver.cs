using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(target))]
public class TargetHubInputDriver : MonoBehaviour
{
    private static MethodInfo keySettingsMethod;
    private static bool isBootstrapped;

    private readonly object[] keyArgs = new object[1];

    [SerializeField] private float axisThreshold = 0.3f;

    private target targetComponent;

    private void Awake()
    {
        targetComponent = GetComponent<target>();
        EnsureKeySettingsMethod();
    }

    private void FixedUpdate()
    {
        if (targetComponent == null || keySettingsMethod == null)
        {
            return;
        }

        string controllerId;
        if (!TryResolveControllerId(targetComponent.ptype, out controllerId))
        {
            return;
        }

        HubGameClient.ControllerState state;
        if (!HubGameClient.TryGetState(controllerId, out state))
        {
            return;
        }

        if (state.ButtonA)
        {
            InvokeKeySettings(0);
        }

        if (state.Axes.y > axisThreshold)
        {
            InvokeKeySettings(1);
        }
        else if (state.Axes.y < -axisThreshold)
        {
            InvokeKeySettings(2);
        }

        if (state.Axes.x < -axisThreshold)
        {
            InvokeKeySettings(3);
        }
        else if (state.Axes.x > axisThreshold)
        {
            InvokeKeySettings(4);
        }
    }

    private void InvokeKeySettings(int index)
    {
        keyArgs[0] = index;
        keySettingsMethod.Invoke(targetComponent, keyArgs);
    }

    public static bool TryResolveControllerId(target.Ptype playerType, out string controllerId)
    {
        switch (playerType)
        {
            case target.Ptype.Player1:
                controllerId = "p1";
                return true;
            case target.Ptype.Player2:
                controllerId = "p2";
                return true;
            case target.Ptype.Player3:
                controllerId = "p3";
                return true;
            case target.Ptype.Player4:
                controllerId = "p4";
                return true;
            default:
                controllerId = string.Empty;
                return false;
        }
    }

    private static void EnsureKeySettingsMethod()
    {
        if (keySettingsMethod != null)
        {
            return;
        }

        keySettingsMethod = typeof(target).GetMethod("KeySettings", BindingFlags.Instance | BindingFlags.NonPublic);

        if (keySettingsMethod == null)
        {
            Debug.LogError("[TargetHubInputDriver] KeySettings method not found");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallDriver()
    {
        if (isBootstrapped)
        {
            return;
        }

        isBootstrapped = true;
        AttachDriversToTargets();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachDriversToTargets();
    }

    private static void AttachDriversToTargets()
    {
        target[] targets = Resources.FindObjectsOfTypeAll<target>();
        foreach (target tgt in targets)
        {
            if (tgt == null)
            {
                continue;
            }

            if (!tgt.gameObject.scene.IsValid())
            {
                continue;
            }

            if (tgt.GetComponent<TargetHubInputDriver>() != null)
            {
                continue;
            }

            tgt.gameObject.AddComponent<TargetHubInputDriver>();
        }
    }
}
