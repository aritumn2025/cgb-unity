using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-180)]
[RequireComponent(typeof(target))]
public class TargetPersonaBinder : MonoBehaviour
{
    private target targetComponent;
    private target.TYPE defaultType;
    private string currentUserId = string.Empty;
    private string currentPersonality = string.Empty;

    private void Awake()
    {
        targetComponent = GetComponent<target>();
        if (targetComponent != null)
        {
            defaultType = targetComponent.type;
        }
    }

    private void Update()
    {
        if (targetComponent == null)
        {
            return;
        }

        if (!TargetHubInputDriver.TryResolveControllerId(targetComponent.ptype, out var slotId))
        {
            ResetToDefault();
            return;
        }

        if (!HubGameService.TryGetAssignment(slotId, out var assignment) || !assignment.HasUser)
        {
            ResetToDefault();
            return;
        }

        if (assignment.UserId == currentUserId && assignment.Personality == currentPersonality)
        {
            return;
        }

        currentUserId = assignment.UserId;
        currentPersonality = assignment.Personality;
        ApplyPersonality(currentPersonality);
    }

    private void ResetToDefault()
    {
        currentUserId = string.Empty;
        currentPersonality = string.Empty;
        if (targetComponent != null)
        {
            targetComponent.type = defaultType;
        }
    }

    private void ApplyPersonality(string personality)
    {
        switch (ResolvePersonaType(personality))
        {
            case PersonaType.Analyst:
                targetComponent.type = target.TYPE.A;
                break;
            case PersonaType.Guardian:
                targetComponent.type = target.TYPE.B;
                break;
            case PersonaType.Diplomat:
                targetComponent.type = target.TYPE.C;
                break;
            case PersonaType.Explorer:
                targetComponent.type = target.TYPE.D;
                break;
            default:
                targetComponent.type = defaultType;
                break;
        }
    }

    private PersonaType ResolvePersonaType(string personality)
    {
        if (string.IsNullOrWhiteSpace(personality))
        {
            return PersonaType.Unknown;
        }

        if (!int.TryParse(personality, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return PersonaType.Unknown;
        }

        if (value >= 0 && value <= 3)
        {
            return PersonaType.Analyst;
        }

        if (value >= 12 && value <= 15)
        {
            return PersonaType.Guardian;
        }

        if (value >= 8 && value <= 11)
        {
            return PersonaType.Diplomat;
        }

        if (value >= 4 && value <= 7)
        {
            return PersonaType.Explorer;
        }

        return PersonaType.Unknown;
    }

    private enum PersonaType
    {
        Unknown,
        Analyst,
        Guardian,
        Diplomat,
        Explorer,
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        AttachToTargets();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToTargets();
    }

    private static void AttachToTargets()
    {
        var targets = Resources.FindObjectsOfTypeAll<target>();
        foreach (var tgt in targets)
        {
            if (tgt == null)
            {
                continue;
            }

            var go = tgt.gameObject;
            if (!go.scene.IsValid())
            {
                continue;
            }

            if (go.GetComponent<TargetPersonaBinder>() != null)
            {
                continue;
            }

            go.AddComponent<TargetPersonaBinder>();
        }
    }
}
