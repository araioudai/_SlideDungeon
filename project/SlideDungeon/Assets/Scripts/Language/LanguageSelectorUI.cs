using UnityEngine;
using UnityEngine.UI;

public class LanguageSelectorUI : MonoBehaviour
{
    [Header("パネル内にあるJAボタン（要素0）とENボタン（要素1）を指定")]
    [SerializeField] private MaskableGraphic[] languageButtons;

    #region Unityイベント関数

    //オブジェクトがアクティブ（表示状態）になったときに自動で呼ばれる処理
    void OnEnable()
    {
        //パネルが開いた瞬間、現在選ばれている言語（JAPANかENGLISHか）に合わせてボタンの色を初期化
        if (LanguageManager.Instance != null)
        {
            UpdateUI(LanguageManager.Instance.CurrentLanguage);
        }

        //マネージャー側で言語が変更されたら、UpdateUI関数も自動的に実行されるように登録
        LanguageManager.OnLanguageChanged += UpdateUI;
    }

    //オブジェクト（パネル）が非アクティブ（非表示状態）になったときに自動で呼ばれる処理
    void OnDisable()
    {
        //パネルが閉じるときは、マネージャーの通知登録を必ず解除
        LanguageManager.OnLanguageChanged -= UpdateUI;
    }

    #endregion

    #region UIボタンのOnClickイベントから呼び出す関数

    /// <summary>
    /// パネル内の「JA（日本語）ボタン」が押されたときに実行する関数
    /// </summary>
    public void PushJapan()
    {
        //マネージャーに対して「日本語に切り替えて保存」
        LanguageManager.Instance.SetLanguage(LanguageManager.Language.JAPAN);
    }

    /// <summary>
    /// パネル内の「EN（英語）ボタン」が押されたときに実行する関数
    /// </summary>
    public void PushEnglish()
    {
        //マネージャーに対して「英語に切り替えて保存」
        LanguageManager.Instance.SetLanguage(LanguageManager.Language.ENGLISH);
    }

    #endregion

    /// <summary>
    /// 言語の状態（通知）を受け取って、このパネルにあるボタンの色を白（選択中）かグレー（未選択）に切り替える処理
    /// </summary>
    /// <param name="lang">現在の言語状態</param>
    private void UpdateUI(LanguageManager.Language lang)
    {
        //インスペクターへのボタン登録が漏れている（配列が空、または2個未満）場合はエラー防止のため何もしない
        if (languageButtons == null || languageButtons.Length < 2) return;

        //日本語が選択されている場合
        if (lang == LanguageManager.Language.JAPAN)
        {
            //JAボタン（要素0）を明るい白（選択中）にする
            languageButtons[(int)LanguageManager.Language.JAPAN].color = Color.white;

            //ENボタン（要素1）を薄暗いグレー（未選択状態）にする
            languageButtons[(int)LanguageManager.Language.ENGLISH].color = new Color(168f / 255f, 168f / 255f, 168f / 255f);
        }
        //英語が選択されている場合
        else
        {
            //JAボタン（要素0）を薄暗いグレーにする
            languageButtons[(int)LanguageManager.Language.JAPAN].color = new Color(168f / 255f, 168f / 255f, 168f / 255f);

            //ENボタン（要素1）を明るい白（選択中）にする
            languageButtons[(int)LanguageManager.Language.ENGLISH].color = Color.white;
        }
    }
}