

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;




namespace DNExtensions
{
    public class PlayFromCameraSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Play from Camera Settings", false, 169)]
        public static void ShowWindow()
        {
            PlayFromCameraSettingsWindow window = GetWindow<PlayFromCameraSettingsWindow>();
            window.titleContent = new GUIContent("Play from Camera Settings");
            window.minSize = new Vector2(350, 250);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Play from Camera Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            PlayFromCameraSettings.AlsoSetRotation = EditorGUILayout.Toggle(
                new GUIContent("Also Set Player Rotation",
                    "If enabled, the player's rotation will also be set to match the camera rotation"),
                PlayFromCameraSettings.AlsoSetRotation
            );

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Player Object Selection", EditorStyles.boldLabel);

            PlayFromCameraSettings.PlayerSelectionMode = (PlayerSelectionMode)EditorGUILayout.EnumPopup(
                new GUIContent("Selection Mode", "How to find the player object in the scene"),
                PlayFromCameraSettings.PlayerSelectionMode
            );

            EditorGUILayout.Space();

            switch (PlayFromCameraSettings.PlayerSelectionMode)
            {
                case PlayerSelectionMode.ByTag:
                    PlayFromCameraSettings.PlayerTag = EditorGUILayout.TagField(
                        new GUIContent("Player Tag", "Tag to search for when finding the player object"),
                        PlayFromCameraSettings.PlayerTag
                    );
                    break;

                case PlayerSelectionMode.ByPath:
                    PlayFromCameraSettings.PlayerObjectPath = EditorGUILayout.TextField(
                        new GUIContent("Player Object Path",
                            "Hierarchy path to the player object (e.g., 'Player', 'Managers/Player', etc.)"),
                        PlayFromCameraSettings.PlayerObjectPath
                    );

                    EditorGUILayout.HelpBox(
                        "Enter the hierarchy path to your player object. Examples:\n" +
                        "• 'Player' - for object named Player at root level\n" +
                        "• 'GameManager/Player' - for nested objects\n",
                        MessageType.Info
                    );
                    break;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Find Player Object"))
            {
                GameObject playerObj = null;

                switch (PlayFromCameraSettings.PlayerSelectionMode)
                {
                    case PlayerSelectionMode.ByTag:
                        try
                        {
                            playerObj = GameObject.FindWithTag(PlayFromCameraSettings.PlayerTag);
                        }
                        catch (UnityException e)
                        {
                            Debug.LogError($"Tag '{PlayFromCameraSettings.PlayerTag}' is not defined: {e.Message}");
                        }

                        break;
                    case PlayerSelectionMode.ByPath:
                        playerObj = GameObject.Find(PlayFromCameraSettings.PlayerObjectPath);
                        break;
                }

                if (playerObj != null)
                {
                    EditorGUIUtility.PingObject(playerObj);
                    Debug.Log($"Found player object: {playerObj.name} at {playerObj.transform.position}");
                }
                else
                {
                    Debug.LogWarning("Could not find player object with current settings!");
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "How to use Play from Camera:\n\n" +
                "1. Position your Scene View camera where you want the player to spawn\n" +
                "2. Use the menu: Tools > Play from Camera Position (or shortcut Ctrl+Alt+P)\n",
                MessageType.Info
            );

            EditorGUILayout.EndScrollView();
        }
    }
}

#endif