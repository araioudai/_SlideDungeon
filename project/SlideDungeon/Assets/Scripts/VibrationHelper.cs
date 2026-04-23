using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrationHelper : MonoBehaviour
{
    /// <summary>
    /// 振動を発生させる
    /// </summary>
    /// <param name="milliseconds">振動の長さ (ms)</param>
    /// <param name="amplitude">振動の強さ (0〜255, -1=デフォルト強度)</param>
    public static void WeakVibrate(long milliseconds = 50, int amplitude = 50)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            //UnityPlayer.currentActivity から Android の Activity を取得
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                //Activity から Vibrator システムサービスを取得
                AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                if (vibrator != null)
                {
                    //Android 8.0 (API 26) 以降は VibrationEffect クラスで強さ指定が可能
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        //指定した長さと強さでワンショット振動を生成
                        AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            amplitude
                        );

                        //実際に振動させる
                        vibrator.Call("vibrate", effect);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            //万一失敗してもクラッシュしないようにログを出す
            Debug.LogWarning("Vibration failed: " + e.Message);
        }
#elif UNITY_EDITOR || UNITY_IOS
        //Android以外 (iOS や Unityエディタ) では Handheld.Vibrate を使用
        Handheld.Vibrate();
#endif
    }
}
