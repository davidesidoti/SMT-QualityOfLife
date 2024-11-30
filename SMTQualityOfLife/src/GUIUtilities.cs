using UnityEngine;

namespace SMTQualityOfLife;

public class GUIUtilities
{
    public GUIStyle HeaderStyle;
    public GUIStyle DescriptionStyle;
    public GUIStyle LabelStyle;
    
    public void InitializeStyles()
    {
        // Initialize header style
        HeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        // Initialize description style
        DescriptionStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };

        // Initialize label style
        LabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };
    }

    public void DrawModDisabledContent(string modName)
    {
        // Section Header
        GUILayout.Label($"Mod {modName} disabled", HeaderStyle);
        
        // Description Text
        GUILayout.Label($"Mod {modName} is currently disabled. Enable it to access it's features.", DescriptionStyle);
    }

    public void DrawModUnavailableContent(string modName, string reason)
    {
        // Section Header
        GUILayout.Label($"Mod {modName} is not available", HeaderStyle);
        
        // Description Text
        GUILayout.Label($"Mod {modName} is currently unavailable: {reason}", DescriptionStyle);
    }
    
    public void DrawModSection(string header, string description, ref bool isEnabled, System.Action settingsAction)
    {
        // Section Header
        GUILayout.Label(header, HeaderStyle);

        // Description Text
        GUILayout.Label(description, DescriptionStyle);

        GUILayout.Space(5);

        // Toggle for enabling/disabling the mod
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{header} Enabled?", LabelStyle);
        isEnabled = GUILayout.Toggle(isEnabled, "");
        GUILayout.EndHorizontal();

        // Settings button if applicable
        if (settingsAction != null)
        {
            if (GUILayout.Button("Mod Settings", GUILayout.Width(150)))
            {
                settingsAction.Invoke();
            }
        }

        // Horizontal separator
        GUILayout.Space(20);
        DrawHorizontalLine();
        GUILayout.Space(20);
    }
    
    public void DrawIntButtonAddSection(string header, string description, int currentValue , System.Action addAction, System.Action removeAction)
    {
        // Section Header
        GUILayout.Label(header, HeaderStyle);

        // Description Text
        GUILayout.Label(description, DescriptionStyle);

        GUILayout.Space(5);

        // Displaying the current value
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Current {header}: {currentValue}", LabelStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        // Add button if applicable
        if (addAction != null)
        {
            if (GUILayout.Button("+ Add", GUILayout.Width(150)))
            {
                addAction.Invoke();
            }
        }
        
        // Add button if applicable
        if (removeAction != null)
        {
            if (GUILayout.Button("- Remove", GUILayout.Width(150)))
            {
                removeAction.Invoke();
            }
        }
        GUILayout.EndHorizontal();

        // Horizontal separator
        GUILayout.Space(20);
        DrawHorizontalLine();
        GUILayout.Space(20);
    }
    
    public void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }
}