using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
[ExecuteInEditMode]
public class GliderCanvasScaler : MonoBehaviour
{
    /// <summary>
    /// 세로뷰에서 '이거보다 좁혀지면 스케일을 줄여야 한다'의 기준. 1920 높이와 비율을 계산하여 적용됨.
    /// </summary>
    public float safeWidth = 1080f;

    /// <summary>
    /// 세로뷰에서 고정 높이 해상도
    /// </summary>
    public float safeHeight = 1920f;

    void OnEnable()
    {
        Set();
    }

    void Set()
    {
        var canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
            canvasScaler = gameObject.AddComponent<CanvasScaler>();

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
#if UNITY_EDITOR
        screenSize = GetGameViewSize();
#endif
        float bi = screenSize.x / screenSize.y;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        if (bi > 1) // 가로모드
        {
            canvasScaler.referenceResolution = new Vector2(safeHeight, safeWidth);
            canvasScaler.matchWidthOrHeight = GetMatchWidthOrHeightLandscape();
        }
        else // 세로모드
        {
            canvasScaler.referenceResolution = new Vector2(safeWidth, safeHeight);
            canvasScaler.matchWidthOrHeight = GetMatchWidthOrHeightPortrait();
        }
    }

    float GetMatchWidthOrHeightPortrait()
    {
        var minAspect = safeHeight / safeWidth;
        var aspect = (float)Screen.height / Screen.width;

        return aspect > minAspect ? 0f : 1f;
    }

    float GetMatchWidthOrHeightLandscape()
    {
        var minAspect = safeWidth / safeHeight;
        var aspect = (float)Screen.width / Screen.height;
        return aspect > minAspect ? 1f : 0f;
    }

#if UNITY_EDITOR
    private int _screenWidth = -1;
    private int _screenHeight = -1;

    void Update()
    {
        if (Screen.width != _screenWidth || Screen.height != _screenHeight)
        {
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            Set();
        }
    }
#if UNITY_EDITOR
    public static Vector2 GetGameViewSize()
    {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
    }
#endif

    private void OnValidate()
    {
        Set();
    }
#endif
}