using UnityEngine;

public sealed class VoyageHUD : MonoBehaviour
{
    GUIStyle style;

    void Awake()
    {
        if (GetComponent<VoyagePhonePauseMenu>() == null)
            gameObject.AddComponent<VoyagePhonePauseMenu>();
    }

    void OnGUI()
    {
        DrivingCore core = DrivingCore.Instance;
        if (core == null || core.Player == null) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
        }
        if (!core.HudPaused && (Voyage.Exploration.ExplorationSession.Instance == null || !Voyage.Exploration.ExplorationSession.Instance.OnFoot))
        {
            GUI.Label(new Rect(24f, 20f, 280f, 32f), Mathf.RoundToInt(core.HudSpeedKmh) + " KM/H", style);
            GUI.Label(new Rect(24f, 52f, 280f, 32f), $"FUEL {FuelTank.SharedFuel:0.0} / {FuelTank.SharedCapacity:0}", style);
        }
    }
}
