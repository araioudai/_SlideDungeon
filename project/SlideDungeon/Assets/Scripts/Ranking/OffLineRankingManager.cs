using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//ランキング1件分（プレイヤー名とクリアタイム）
[System.Serializable]
public class ScoreEntry
{
    public string playerName; //プレイヤー名
    public float clearTime;   //クリアタイム（短いほど良い）
}

//各ステージのランキング（上位9件まで保持）
[System.Serializable]
public class StageRanking
{
    public List<ScoreEntry> scores = new List<ScoreEntry>();
}

//ゲーム全体のランキングデータ
[System.Serializable]
public class RankingData
{
    public List<StageRanking> stageRankings = new List<StageRanking>();
}

public class OffLineRankingManager : MonoBehaviour
{
    #region シングルトン
    public static OffLineRankingManager Instance { get; private set; }
    #endregion

    #region private変数

    private string filePath;                             //JSON保存先のパス
    private RankingData rankingData = new RankingData(); //全ステージ分のランキングデータ

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

        filePath = Path.Combine(Application.persistentDataPath, "offline_ranking.json");

        LoadRanking();
    }
    #endregion

    #region ランキング関係

    #region ランキングに入れるか判定
    public bool IsHightScore(int stageIndex, float time)
    {
        EnsureStageExists(stageIndex); //ステージ数が足りなければ拡張

        var stageRanking = rankingData.stageRankings[stageIndex - 1];
        if (stageRanking.scores.Count < 9) return true; //9位未満なら必ず入れる

        //一番遅いタイムより早ければ入れる
        return time < stageRanking.scores.Last().clearTime;
    }
    #endregion

    #region スコア追加

    public void AddScore(int stageIndex, string name, float time)
    {
        EnsureStageExists(stageIndex);

        var stageRanking = rankingData.stageRankings[stageIndex - 1];
        stageRanking.scores.Add(new ScoreEntry { playerName = name, clearTime = time });

        //タイム順にソートして上位9件だけ残す
        stageRanking.scores = stageRanking.scores
              .OrderBy(e => e.clearTime) //早い順
              .Take(9)                   //上位9件のみ保存
              .ToList();

        SaveRanking();
    }

    #endregion

    #region 指定ステージのランキング取得

    public List<ScoreEntry> GetRanking(int stageIndex)
    {
        EnsureStageExists(stageIndex);

        var stageRanking = rankingData.stageRankings[stageIndex - 1];
        if (stageRanking == null || stageRanking.scores == null)
        {
            return new List<ScoreEntry>(); //空のリストを返す
        }

        return stageRanking.scores;
    }

    #endregion

    #region ランキングをJSONファイルに保存

    private void SaveRanking()
    {
        string json = JsonUtility.ToJson(rankingData, true); //trueで整形出力
        File.WriteAllText(filePath, json);
    }

    #endregion

    #region JSONファイル読み込み

    private void LoadRanking()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            rankingData = JsonUtility.FromJson<RankingData>(json);
        }
        else
        {
            rankingData = new RankingData();
        }
    }

    #endregion

    #region 指定ステージ分のランキングリストが存在するか確認、なければ追加
    private void EnsureStageExists(int stageIndex)
    {
        while (rankingData.stageRankings.Count < stageIndex)
        {
            rankingData.stageRankings.Add(new StageRanking());
        }
    }
    #endregion

    #region ランキングデータリセット

    public void ResetAllRanking()
    {
        rankingData = new RankingData(); //全ステージのランキングデータを空にする
        SaveRanking();                   //空データで上書き保存
        //Debug.Log("ランキングをすべてリセット");
    }

    #endregion

    #endregion
}
