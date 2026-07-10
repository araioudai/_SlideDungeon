using UnityEngine;
using TMPro;

public class InputCounter : MonoBehaviour
{
    [Header("入力用InputField")]
    [SerializeField] private TMP_InputField inputField;
    [Header("表示用テキスト")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private int maxLimit = 6;

    void Start()
    {
        //インスペクターでの制限設定を適用
        inputField.characterLimit = maxLimit;
        UpdateCount(inputField.text);

        //値が変わるたびに実行されるイベントを登録
        inputField.onValueChanged.AddListener(UpdateCount);
    }

    void UpdateCount(string text)
    {
        //「現在の文字数 / 最大文字数」を表示
        counterText.text = $"{text.Length} / {maxLimit}";

        //制限ギリギリになったら色を変える
        if (text.Length >= maxLimit)
        {
            counterText.color = Color.red;
        }
        else
        {
            counterText.color = Color.white;
        }
    }
}