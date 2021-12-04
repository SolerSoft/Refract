using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass
{
    /*
    public enum PortraitButtonType
    {
        None = -1,
        Previous = 0,
        Next = 1,
        Pause = 2
    }

    public class PortraitButtons
    {
        #region Constants
        static private readonly int VK_MEDIA_NEXT_TRACK = 0xB0;
        static private readonly int VK_MEDIA_PREV_TRACK = 0xB1;
        static private readonly int VK_MEDIA_PLAY_PAUSE = 0xB3;

        #endregion // Constants
        [DllImport("User32.dll")]
        static private extern short GetAsyncKeyState(int vKey);


        static public PortraitButtonType GetPressedButton()
        {
            PortraitButtonType pressed = PortraitButtonType.None;

            byte[] result = System.BitConverter.GetBytes(GetAsyncKeyState(VK_MEDIA_NEXT_TRACK));
            if (result[0] == 1) return PortraitButtonType.Next;

            result = System.BitConverter.GetBytes(GetAsyncKeyState(VK_MEDIA_PREV_TRACK));
            if (result[0] == 1) return PortraitButtonType.Previous;

            result = System.BitConverter.GetBytes(GetAsyncKeyState(VK_MEDIA_PLAY_PAUSE));
            if (result[0] == 1) return PortraitButtonType.Pause;

            //if (result[1] == 0x80)
            //    Debug.Log("The key is down");

            return pressed;
        }
    }
    */

    /// <summary>
    /// Represents the hardware buttons present on all generations of Looking Glass displays.
    /// </summary>
    public enum HardwareButton
    {
        /// <summary>
        /// Square button on Looking Glass classic devices.
        /// </summary>
        Square = 0,

        /// <summary>
        /// Left button on Looking Glass classic devices.
        /// </summary>
        Left = 1,

        /// <summary>
        /// Right button on Looking Glass classic devices.
        /// </summary>
        Right = 2,

        /// <summary>
        /// Circle button on Looking Glass classic devices.
        /// </summary>
        Circle = 3,

        /// <summary>
        /// Forward button on Portrait and Gen2 devices.
        /// </summary>
        Forward = 4,

        /// <summary>
        /// Back button on Portrait and Gen2 devices.
        /// </summary>
        Back = 5,

        /// <summary>
        /// Play / Pause / Loop button on Portrait and Gen2 devices.
        /// </summary>
        PlayPause = 6
    }

    /// <summary>
    /// Indicates when hardware input will be emulated.
    /// </summary>
    public enum InputEmulationMode
    {
        /// <summary>
        /// Hardware input will never be emulated.
        /// </summary>
        Never,

        /// <summary>
        /// Hardware input will only be emulated while in the Unity editor.
        /// </summary>
        EditorOnly,

        /// <summary>
        /// Hardware input will always be emulated.
        /// </summary>
        Always
    }

    /// <summary>
    /// Manages hardware inputs on Looking Glass displays.
    /// </summary>
    /// <remarks>
    /// This is an updated version of the
    /// <see href="https://docs.lookingglassfactory.com/Unity/Scripts/ButtonManager">ButtonManager</see>
    /// designed to work with both Gen1 and Gen2 hardware.
    /// </remarks>
    public class InputManager : MonoBehaviour
    {
        #region Nested Types
        /// <summary>
        /// Different button states we can check for.
        /// </summary>
        private enum ButtonState
        {
            /// <summary>
            /// The button was pressed on this frame.
            /// </summary>
            Down,

            /// <summary>
            /// The button was released on this frame.
            /// </summary>
            Up,

            /// <summary>
            /// The button is held.
            /// </summary>
            Held
        }

        /// <summary>
        /// Used in a lookup table to map a <see cref="HardwareButton"/> to one or more <see cref="KeyCode"/>s or
        /// <see cref="ExtendedKeyCode"/>s.
        /// </summary>
        private class KeyMap
        {
            #region Member Variables
            private List<KeyCode> keys = new List<KeyCode>();
            private List<ExtendedKeyCode> extendedKeys = new List<ExtendedKeyCode>();
            #endregion // Member Variables

            #region Public Properties
            /// <summary>
            /// Gets the keys in the map.
            /// </summary>
            public List<KeyCode> Keys => keys;

            /// <summary>
            /// Gets the extended keys in the map.
            /// </summary>
            public List<ExtendedKeyCode> ExtendedKeys => extendedKeys;
            #endregion // Public Properties
        }
        #endregion // Nested Types



        #region Static Version of InputManager

        #region Constants
        private const string CLASSIC_JOY_KEY = "holoplay";
        private const float JOY_CHECK_INTERVAL = 3.0f;
        private const string SINGLETON_NAME = "HoloPlay Input Manager";
        #endregion // Constants

        #region Member Variables
        static private InputManager instance;
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Gets the <see cref="KeyCode"/> that represents the specified joystick and button.
        /// </summary>
        /// <param name="joystick">
        /// The number of the joystick.
        /// </param>
        /// <param name="button">
        /// The number of the button.
        /// </param>
        /// <returns>
        /// The <see cref="KeyCode"/> that represents the joystick and button.
        /// </returns>
        static private KeyCode JoyButtonCode(int joystick, int button)
        {
            // Validate
            if ((joystick < 1) || (joystick > 8)) { throw new ArgumentOutOfRangeException(nameof(joystick)); }
            if ((button < 0) || (button > 19)) { throw new ArgumentOutOfRangeException(nameof(button)); }

            // Convert
            return (KeyCode)Enum.Parse(
                       typeof(KeyCode), "Joystick" + joystick + "Button" + button
                   );
        }
        #endregion // Internal Methods

        #region Public Properties
        /// <summary>
        /// Gets the singleton instance of the <see cref="InputManager"/>.
        /// </summary>
        /// <remarks>
        /// Reading this property will add an <see cref="InputManager"/> to the scene if one does not already exist.
        /// </remarks>
        static public InputManager Instance
        {
            get
            {
                // If an instance has already been cached, return it
                if (instance != null)
                {
                    return instance;
                }

                // Not cached, search for it
                instance = FindObjectOfType<InputManager>();

                // If found, return it
                if (instance != null)
                {
                    return instance;
                }

                // If still not found, create a GameObject to hold it
                GameObject inputManagerGO = new GameObject(SINGLETON_NAME);

                // Create the instance and cache the reference
                instance = inputManagerGO.AddComponent<InputManager>();

                // Return the cached instance
                return instance;
            }
        }
        #endregion // Public Properties

        #endregion // Static Version  of InputManager



        #region Instance Version of InputManager

        #region Member Variables
        private int classicJoyNumber = -2;
        private float timeSinceClassicCheck = -3f;
        private Dictionary<HardwareButton, KeyMap> buttonKeyMap;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Emulation")]
        [Tooltip("When to emulate hardware buttons.")]
        private InputEmulationMode emulationMode = InputEmulationMode.Always;

        [Header("Classic")]
        [Tooltip("Whether to search for classic hardware which appears as a Joystick device.")]
        private bool searchForClassic = true;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Check to see if the specified button matches the specified state.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to check.
        /// </param>
        /// <param name="state">
        /// The <see cref="ButtonState"/> to check for.
        /// </param>
        /// <returns>
        /// <c>true</c> if the button matches the specified state; otherwise <c>false</c>.
        /// </returns>
        private bool CheckButtonState(HardwareButton button, ButtonState state)
        {
            // If we haven't found a classic joystick yet and we're searching for one, try to search again now
            if ((searchForClassic) && (classicJoyNumber < 1))
            {
                SearchForClassic();
            }

            // Which functions are we using to test keys and extended keys?
            Func<KeyCode, bool> keyFunc;
            Func<ExtendedKeyCode, bool> extendedKeyFunc;
            switch (state)
            {
                case ButtonState.Down:
                    keyFunc = Input.GetKeyDown;
                    extendedKeyFunc = ExtendedInput.GetKeyDown;
                    break;
                case ButtonState.Up:
                    keyFunc = Input.GetKeyUp;
                    extendedKeyFunc = ExtendedInput.GetKeyUp;
                    break;
                case ButtonState.Held:
                    keyFunc = Input.GetKey;
                    extendedKeyFunc = ExtendedInput.GetKey;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown state '{state}'.");
            }

            // Get the KeyMap for the specified hardware button
            KeyMap keyMap = GetKeyMap(button);

            // Check standard keys first
            foreach (var key in keyMap.Keys)
            {
                if (keyFunc(key)) { return true; }
            }

            // Check extended keys
            foreach (var eKey in keyMap.ExtendedKeys)
            {
                if (extendedKeyFunc(eKey)) { return true; }
            }

            // No key or extended key matched the target state
            return false;
        }

        /// <summary>
        /// Gets the <see cref="KeyMap"/> for the specified button, creating it if necessary.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to get the <see cref="KeyMap"/> for.
        /// </param>
        /// <returns>
        /// The <see cref="KeyMap"/> for the button.
        /// </returns>
        private KeyMap GetKeyMap(HardwareButton button)
        {
            // Make sure the overall lookup table is crated
            if (buttonKeyMap == null)
            {
                buttonKeyMap = new Dictionary<HardwareButton, KeyMap>();
            }

            // Try to get an existing KeyMap. If not found, create it.
            KeyMap keyMap;
            if (!buttonKeyMap.TryGetValue(button, out keyMap))
            {
                // Create it
                keyMap = new KeyMap();

                // Store it
                buttonKeyMap[button] = keyMap;
            }

            // Return the map
            return keyMap;
        }

        /// <summary>
        /// Searches for a joystick representing a classic hardware display.
        /// </summary>
        private void SearchForClassic()
        {
            // If already found, ignore
            if (classicJoyNumber > 0) { return; }

            // If too little time has passed since last check, ignore
            if ((Time.unscaledTime - timeSinceClassicCheck) < JOY_CHECK_INTERVAL) { return; }

            // Checking now
            timeSinceClassicCheck = Time.unscaledTime;

            // Get all joystick names
            string[] joyNames = Input.GetJoystickNames();

            // Look at each name
            for (int i=0; i < joyNames.Length; i++)
            {
                if (joyNames[i].ToLower().Contains(CLASSIC_JOY_KEY))
                {
                    classicJoyNumber = i + 1; // Unity joystick IDs are 1 bound not 0 bound
                    break;
                }
            }

            // Warn if not found, but only once
            if (classicJoyNumber == -2)
            {
                Debug.LogWarning($"{nameof(InputManager)} - No HoloPlay joystick found but will continue to search.");
                classicJoyNumber = -1;
            }

            // If the joystick has been found, add KeyMap entries that map the joystick buttons to classic hardware buttons
            if (classicJoyNumber > 0)
            {
                GetKeyMap(HardwareButton.Square).Keys.Add(JoyButtonCode(classicJoyNumber, 1));
                GetKeyMap(HardwareButton.Left).Keys.Add(JoyButtonCode(classicJoyNumber, 2));
                GetKeyMap(HardwareButton.Right).Keys.Add(JoyButtonCode(classicJoyNumber, 3));
                GetKeyMap(HardwareButton.Circle).Keys.Add(JoyButtonCode(classicJoyNumber, 4));
            }
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <inheritdoc/>
        protected virtual void OnEnable()
        {
            // Make sure more than one instance doesn't start up
            if ((instance != null) && (instance != this))
            {
                Debug.LogError($"More than one instance of {nameof(InputManager)} detected in the scene. Terminating '{name}'.");
                Destroy(this);
                return;
            }

            // Make sure we don't get destroyed across scene changes
            DontDestroyOnLoad(this);
        }

        /// <inheritdoc/>
        protected virtual void Start()
        {
            // Add KeyMap entries that map the media keys to Portrait buttons
            GetKeyMap(HardwareButton.Forward).ExtendedKeys.Add(ExtendedKeyCode.MediaNext);
            GetKeyMap(HardwareButton.Back).ExtendedKeys.Add(ExtendedKeyCode.MediaPrevious);
            GetKeyMap(HardwareButton.PlayPause).ExtendedKeys.Add(ExtendedKeyCode.MediaPlayPause);

            // If emulation is enabled, add emulation key map entries as well
            if ((emulationMode == InputEmulationMode.Always) || (emulationMode == InputEmulationMode.EditorOnly && Application.isEditor))
            {
                GetKeyMap(HardwareButton.Square).Keys.Add(KeyCode.Alpha1);
                GetKeyMap(HardwareButton.Left).Keys.Add(KeyCode.Alpha2);
                GetKeyMap(HardwareButton.Right).Keys.Add(KeyCode.Alpha3);
                GetKeyMap(HardwareButton.Circle).Keys.Add(KeyCode.Alpha4);
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Returns <c>true</c> if any button is held.
        /// </summary>
        /// <returns>
        /// <c>true</c> if any button is held; otherwise <c>false</c>.
        /// </returns>
        public bool GetAnyButton()
        {
            // Get all buttons
            var allButtons = Enum.GetValues(typeof(HardwareButton));

            // Check for any button
            foreach (var button in allButtons)
            {
                if (GetButton((HardwareButton)button)) { return true; }
            }

            // None found
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> on the first frame that any button is pressed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if any button was pressed on this frame; otherwise <c>false</c>.
        /// </returns>
        public bool GetAnyButtonDown()
        {
            // Get all buttons
            var allButtons = Enum.GetValues(typeof(HardwareButton));

            // Check for any button
            foreach (var button in allButtons)
            {
                if (GetButtonDown((HardwareButton)button)) { return true; }
            }

            // None found
            return false;
        }

        /// <summary>
        /// Indicates if the specified button is held down.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to test.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified button is held down; otherwise <c>false</c>.
        /// </returns>
        public bool GetButton(HardwareButton button)
        {
            return CheckButtonState(button, ButtonState.Held);
        }

        /// <summary>
        /// Returns <c>true</c> on the first frame when the specified button is pressed.
        /// </summary>
        /// The <see cref="HardwareButton"/> to test.
        /// <returns>
        /// <c>true</c> if the specified button was pressed on this frame; otherwise <c>false</c>.
        /// </returns>
        public bool GetButtonDown(HardwareButton button)
        {
            return CheckButtonState(button, ButtonState.Down);
        }

        /// <summary>
        /// Returns <c>true</c> on the first frame when the specified button is released.
        /// </summary>
        /// The <see cref="HardwareButton"/> to test.
        /// <returns>
        /// <c>true</c> if the specified button was released on this frame; otherwise <c>false</c>.
        /// </returns>
        public bool GetButtonUp(HardwareButton button)
        {
            return CheckButtonState(button, ButtonState.Up);
        }
        #endregion // Public Methods

        #endregion // Instance Version of InputManager
    }
}