using UnityEngine;

public static class GameplayData {

    private static GameplaySettings gameSettings;

    public static GameplaySettings Settings {
        get {
            if (gameSettings == null) {
                gameSettings = Resources.Load<GameplaySettings>("Settings/GameplaySettings");
            }
            return gameSettings;
        }
    }

    /*public static GameModesSettings Modes {
        get {
            if (modes == null) {
                modes = Resources.Load<GameModesSettings>("Settings/GameModesSettings");
            }
            return modes;
        }
    }*/
}