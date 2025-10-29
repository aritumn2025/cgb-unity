using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hub の入力を target コンポーネントへ渡すための補助クラスです。
/// </summary>
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(target))]
public class TargetHubInputDriver : MonoBehaviour
{
    // target の private メソッドを呼び出すために MethodInfo を保持します。
    private static MethodInfo keySettingsMethod;

    // 初期化処理が重複しないようにするためのフラグです。
    private static bool isBootstrapped;

    // 毎フレームの引数生成を避けるために使い回すバッファです。
    private readonly object[] keyArgs = new object[1];

    // 軸入力の判定に用いる閾値です。
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

        if (!TryResolveControllerId(targetComponent, out string controllerId))
        {
            return;
        }

        if (!HubGameClient.TryGetState(controllerId, out var state))
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

    // target の KeySettings を private のまま利用するためのラッパーです。
    private void InvokeKeySettings(int index)
    {
        keyArgs[0] = index;
        keySettingsMethod.Invoke(targetComponent, keyArgs);
    }

    // プレイヤー種別から Hub の ID を解決します。
    private static bool TryResolveControllerId(target tgt, out string controllerId)
    {
        switch (tgt.ptype)
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

    // 反射を一度だけ準備するためのヘルパーです。
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

    // ゲームシーンに存在する target へ自動で本スクリプトを付与します。
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
        foreach (var tgt in Resources.FindObjectsOfTypeAll<target>())
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
