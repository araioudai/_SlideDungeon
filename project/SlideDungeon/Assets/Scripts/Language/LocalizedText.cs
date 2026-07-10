using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    #region 変数
    [Header("表示したいテキスト（日本語）")]
    [TextArea(3, 10)] 
    [SerializeField] private string japaneseText;

    [Header("表示したいテキスト（英語）")]
    [TextArea(3, 10)]
    [SerializeField] private string englishText;

    [Header("フォントサイズの個別調整（0ならデフォルトのまま）")]
    [SerializeField] private int japaneseFontSize = 0;
    [SerializeField] private int englishFontSize = 0;

    private Text myText;                   //自身のTextコンポーネントを保持する変数

    #endregion

    #region Unityイベント関数
    void Awake()
    {
        //自身のTextコンポーネントを取得
        myText = GetComponent<Text>();
    }

    void OnEnable()
    {
        //パネルが開いた瞬間に、現在の言語に合わせてテキストを表示
        if (LanguageManager.Instance != null)
        {
            RefreshText(LanguageManager.Instance.CurrentLanguage);
        }

        //ゲーム中に言語が切り替わったら、文字を書き換える
        LanguageManager.OnLanguageChanged += RefreshText;
    }

    void Start()
    {
        //Startのタイミングで、もう一度画面の文字を更新
        if (LanguageManager.Instance != null)
        {
            RefreshText(LanguageManager.Instance.CurrentLanguage);
        }
    }

    void OnDisable()
    {
        //非表示になるときは通知の登録を解除
        LanguageManager.OnLanguageChanged -= RefreshText;
    }

    #endregion

    /// <summary>
    /// 言語に合わせて文字とフォントサイズを書き換える処理
    /// </summary>
    private void RefreshText(LanguageManager.Language lang)
    {
        if (myText == null) return;

        if (lang == LanguageManager.Language.JAPAN)
        {
            //日本語のテキストを設定
            myText.text = japaneseText.Replace("\\n", "\n"); //インスペクターでの改行コード（\n）を実際の改行に変換

            //フォントサイズの設定があれば適用する
            if (japaneseFontSize > 0) myText.fontSize = japaneseFontSize;
        }
        else
        {
            //英語のテキストを設定
            myText.text = englishText.Replace("\\n", "\n");

            //フォントサイズの設定があれば適用する
            if (englishFontSize > 0) myText.fontSize = englishFontSize;
        }
    }
}