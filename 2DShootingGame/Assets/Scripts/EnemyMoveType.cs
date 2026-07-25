/// <summary>
/// 敵の移動パターン。
/// プレハブの Inspector、または EnemyCreater から設定します。
/// </summary>
public enum EnemyMoveType
{
    /// <summary>一直線に進む</summary>
    Straight = 0,

    /// <summary>進みながら左右（進行方向に対して横）に往復する</summary>
    SideWays = 1,
}
