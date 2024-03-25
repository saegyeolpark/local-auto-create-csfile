using System;
using Cysharp.Threading.Tasks;
using Glider.Core.Ui.Bundles;
using Glider.Util;
using UnityEngine;

namespace Glider.Core.Ui
{
    public class GliderUi : MonoBehaviour
    {
        private static GliderUi instance;
        private static bool Initialized = false;

        //Alert or Prompt
        private GliderAlertOrPrompt _gliderAlertOrPrompt;
        private ButtonResponse promptResponse;

        //Toast
        private GliderToast _gliderToast;
        private const string StringOpen = "Open";
        private static readonly int HashOpen = Animator.StringToHash(StringOpen);
        private const string StringClose = "Close";
        private static readonly int HashClose = Animator.StringToHash(StringClose);
        private int _toastShowCount;

        private void OnDestroy()
        {
            Initialized = false;
            instance = null;
            _gliderToast = null;
            _gliderAlertOrPrompt = null;
        }

        public static GliderUi Get()
        {
            if (Initialized)
                return instance;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            var gameObject = new GameObject(nameof(GliderUi));
            instance = gameObject.AddComponent<GliderUi>();
            DontDestroyOnLoad(gameObject);
            Initialized = true;
            return instance;
        }

        public void Toast(string message, float time = 3f)
        {
            if (_gliderToast == null)
            {
                _gliderToast = GameObject.Instantiate(Resources.Load<GliderToast>("GliderToast"), transform);
                GliderUtil.UpdateList(ref _gliderToast.viewToasts, 5);
            }

            _gliderToast.viewToasts[_toastShowCount].StartMessage(message, time);
            _toastShowCount = (_toastShowCount + 1) % _gliderToast.viewToasts.Length;
        }

        public async UniTask<PromptResponse> Prompt(string title, string desc,
            ButtonResponseSet buttonResponseSet = ButtonResponseSet.OK)
        {
            if (_gliderAlertOrPrompt == null)
            {
                _gliderAlertOrPrompt =
                    GameObject.Instantiate(Resources.Load<GliderAlertOrPrompt>("GliderAlertOrPrompt"), transform);
                for (int i = 0; i < _gliderAlertOrPrompt.Buttons.Length; i++)
                {
                    var button = (ButtonResponse)i;
                    _gliderAlertOrPrompt.Buttons[i].SetOnClick(() => { promptResponse = button; });
                }
            }

            _gliderAlertOrPrompt.SetVisible(true);
            _gliderAlertOrPrompt.SetTitle(title);
            _gliderAlertOrPrompt.SetDesc(desc);
            _gliderAlertOrPrompt.SetVisibleInputField(true);
            _gliderAlertOrPrompt.InputFieldForPrompt.text = string.Empty;
            switch (buttonResponseSet)
            {
                case ButtonResponseSet.OK:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(false);
                    break;
                case ButtonResponseSet.YES_NO:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(true);
                    break;
                case ButtonResponseSet.OK_CANCEL:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(false);
                    break;
                case ButtonResponseSet.YES_NO_CANCEL:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(true);
                    break;
                case ButtonResponseSet.NONE:
                    _gliderAlertOrPrompt.SetVisibleButtons(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    break;
            }

            promptResponse = ButtonResponse.UNDEFINED;
            await UniTask.WaitUntil(() => promptResponse != ButtonResponse.UNDEFINED, PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy());
            _gliderAlertOrPrompt.SetVisible(false);

            string input = _gliderAlertOrPrompt.InputFieldForPrompt.text;
            return new PromptResponse(promptResponse, input);
        }

        public async UniTask<ButtonResponse> Alert(string title, string desc,
            ButtonResponseSet buttonResponseSet = ButtonResponseSet.OK)
        {
            if (_gliderAlertOrPrompt == null)
            {
                _gliderAlertOrPrompt =
                    GameObject.Instantiate(Resources.Load<GliderAlertOrPrompt>("GliderAlertOrPrompt"), transform);
                for (int i = 0; i < _gliderAlertOrPrompt.Buttons.Length; i++)
                {
                    var button = (ButtonResponse)i;
                    _gliderAlertOrPrompt.Buttons[i].SetOnClick(() => { promptResponse = button; });
                }
            }

            _gliderAlertOrPrompt.SetVisible(true);
            _gliderAlertOrPrompt.SetTitle(title);
            _gliderAlertOrPrompt.SetDesc(desc);
            _gliderAlertOrPrompt.SetVisibleInputField(false);
            switch (buttonResponseSet)
            {
                case ButtonResponseSet.OK:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(false);
                    break;
                case ButtonResponseSet.YES_NO:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(true);
                    break;
                case ButtonResponseSet.OK_CANCEL:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(false);
                    break;
                case ButtonResponseSet.YES_NO_CANCEL:
                    _gliderAlertOrPrompt.SetVisibleButtons(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.OK].SetVisible(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.YES].SetVisible(true);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.NO].SetVisible(true);
                    break;
                case ButtonResponseSet.NONE:
                    _gliderAlertOrPrompt.SetVisibleButtons(false);
                    _gliderAlertOrPrompt.Buttons[(int)ButtonResponse.CANCEL].SetVisible(false);
                    break;
            }

            promptResponse = ButtonResponse.UNDEFINED;
            await UniTask.WaitUntil(() => promptResponse != ButtonResponse.UNDEFINED, PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy());
            _gliderAlertOrPrompt.SetVisible(false);
            return promptResponse;
        }

        public enum ButtonResponseSet
        {
            OK,
            OK_CANCEL,
            YES_NO,
            YES_NO_CANCEL,
            NONE,
        }

        public enum ButtonResponse
        {
            UNDEFINED = -1,
            OK,
            CANCEL,
            YES,
            NO
        }

        public class PromptResponse
        {
            public readonly ButtonResponse buttonResponse;
            public readonly string inputMessage;

            public PromptResponse(ButtonResponse buttonResponse, string inputMessage)
            {
                this.buttonResponse = buttonResponse;
                this.inputMessage = inputMessage;
            }
        }
    }
}