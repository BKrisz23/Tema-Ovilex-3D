using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PG 
{
    public class Menu :MonoBehaviour
    {
        public List<ButtonScene> ButtonScenes = new List<ButtonScene>();
        public bool CanHideMainMenu;
        public GameObject MainParent;
        public GameObject HelpUIParent;
        public TMP_Dropdown GamepadPlayer1;
        public TMP_Dropdown GamepadPlayer2;

        public int LastSelectedGamepadP1
        {
            get
            {
                return PlayerPrefs.GetInt ("GamePadP1");
            }
            set
            {
                PlayerPrefs.SetInt ("GamePadP1", value);
            }
        }

        public int LastSelectedGamepadP2
        {
            get
            {
                return PlayerPrefs.GetInt ("GamePadP2");
            }
            set
            {
                PlayerPrefs.SetInt ("GamePadP2", value);
            }
        }

        private void Awake ()
        {
            if (CanHideMainMenu)
            {
                MainParent.SetActive (false);
            }

            foreach(var bs in ButtonScenes)
            {
                bs.Btn.onClick.AddListener (()=> 
                {
                    SceneManager.LoadScene (bs.Scene.SceneName);
                });
            }

            if (Application.isMobilePlatform)
            {
                HelpUIParent.SetActive (false);
            }
        }

        private void Start ()
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            options.Add (new TMP_Dropdown.OptionData ("None"));
            foreach (var gamePad in Gamepad.all)
            {
                options.Add (new TMP_Dropdown.OptionData (gamePad.displayName));
            }
            GamepadPlayer1.onValueChanged.AddListener (OnChangeGamepadP1);
            GamepadPlayer2.onValueChanged.AddListener (OnChangeGamepadP2);

            GamepadPlayer1.options = options;
            GamepadPlayer1.value = LastSelectedGamepadP1 < GamepadPlayer1.options.Count? LastSelectedGamepadP1: 0;
            GamepadPlayer2.options = options;
            GamepadPlayer2.value = LastSelectedGamepadP2 < GamepadPlayer2.options.Count ? LastSelectedGamepadP2 : 0;
        }

        void OnChangeGamepadP1 (int value)
        {
            LastSelectedGamepadP1 = value;
            if (value == 0)
            {
                UserInput.DevicePlayer1 = null;
            }
            else
            {
                if (Gamepad.all.Count < value)
                {
                    GamepadPlayer1.value = 0;
                }
                else
                {
                    UserInput.DevicePlayer1 = Gamepad.all[value - 1];
                    if (GamepadPlayer2.value == value)
                    {
                        GamepadPlayer2.value = 0;
                    }
                }
            }
        }

        void OnChangeGamepadP2 (int value)
        {
            LastSelectedGamepadP2 = value;
            if (value == 0)
            {
                UserInput.DevicePlayer2 = null;
            }
            else
            {
                if (Gamepad.all.Count < value)
                {
                    GamepadPlayer2.value = 0;
                }
                else
                {
                    UserInput.DevicePlayer2 = Gamepad.all[value - 1];
                    if (GamepadPlayer1.value == value)
                    {
                        GamepadPlayer1.value = 0;
                    }
                }
            }
        }

        private void Update ()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame && HelpUIParent)
            {
                HelpUIParent.SetActive (!HelpUIParent.activeSelf);
            }

            if (CanHideMainMenu && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                MainParent.SetActive(SceneManager.sceneCountInBuildSettings > 1 && !MainParent.activeSelf);
            }
        }

        [System.Serializable]
        public class ButtonScene
        {
            public Button Btn;
            public SceneField Scene;
        }
    }
}
