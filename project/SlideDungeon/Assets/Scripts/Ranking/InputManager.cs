using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    #region private関数
    [SerializeField] private Text nameText;             //名前を表示するテキスト
    [SerializeField] private InputField nameInputField; //名前入力フィールド
    #endregion

    #region 入力されたプレイヤー名読み取り用
    public static string playerName { get; private set; }
    #endregion

    #region 名前の入力が完了した際に呼ぶ関数
    //名前の入力が完了した際に呼ぶ関数
    public void NameInputComplete()
    {
        string input = nameInputField.text.Trim();

        //入力チェック（空欄の場合はデフォルト名）
        if (string.IsNullOrEmpty(input))
        {
            playerName = "Guest";
        }
        else
        {
            playerName = input;
        }

        nameText.text = playerName;
        Time.timeScale = 1f;
    }
    #endregion
}
