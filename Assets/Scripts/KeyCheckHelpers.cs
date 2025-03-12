using UnityEngine;

public static class KeyCheckHelpers
{
    public static KeyCode? GetCurrentKeypadPressed()
    {
        for (KeyCode key = KeyCode.Keypad0; key <= KeyCode.Keypad9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                return key;
            }
        }

        return null;
    }


}
