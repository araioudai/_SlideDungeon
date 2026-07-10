using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PasswordToggle : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInputField; //パスワードの入力欄
    [SerializeField] private Image toggleButtonImage;           //ボタンのアイコン
    [SerializeField] private Sprite visibleIcon;                //見える時のアイコン
    [SerializeField] private Sprite hiddenIcon;                 //隠す時のアイコン

    private bool isPasswordVisible = false;

    /// <summary>
    /// パスワード表示、非表示用設定
    /// </summary>
    public void TogglePassword()
    {
        //状態を反転
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            //パスワードを表示する設定
            passwordInputField.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            //パスワードを隠す設定
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
        }

        //表示を即座に更新させる
        passwordInputField.ForceLabelUpdate();

        //アイコンも切り替える
        if (toggleButtonImage != null)
        {
            toggleButtonImage.sprite = isPasswordVisible ? visibleIcon : hiddenIcon;
        }
    }
}
