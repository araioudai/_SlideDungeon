using UnityEngine;

public class RankingReset : MonoBehaviour
{
    #region これを経由して呼ぶことで常に正しいインスタンスを呼べるようにする
    public void OnClickReset()
    {
        if (OffLineRankingManager.Instance != null)
        {
            OffLineRankingManager.Instance.ResetAllRanking();
        }
        else
        {
            Debug.LogWarning("OffLineRankingManager.Instance が存在しません");
        }
    }
    #endregion
}
