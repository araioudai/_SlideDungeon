using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerSetting : MonoBehaviour
{
    #region シングルトン
    public static PlayerSetting Instance { get; private set; }
    #endregion

    #region private変数
    [Header("バイブレーション true:ON／false:OFF")]
    [SerializeField] private bool defaultVibration = true; //バイブレーションON／OFF設定用

    #endregion

    #region Set関数
    /// <summary>
    /// バイブレーションのオンオフセットと保存
    /// </summary>
    /// <param name="vib">オンならtrue、オフならfalse</param>
    public void SetVibration(bool vib)
    {
        PlayerPrefs.SetInt("vibration", vib ? 1 : 0);
        PlayerPrefs.Save(); //確実に保存する
    }

    #endregion

    #region Get関数
    /// <summary>
    /// バイブレーションの現在の設定を取得
    /// </summary>
    /// <returns>現在の設定</returns>
    public bool GetVibration()
    {
        var value = PlayerPrefs.GetInt("vibration", defaultVibration ? 1 : 0);
        return value == 1;
    }

    #endregion

    #region Unityイベント関数
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

}
