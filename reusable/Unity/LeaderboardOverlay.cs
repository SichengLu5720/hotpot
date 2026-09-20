using UnityEngine;

namespace Harness.Reusable.Unity
{
    // Own/backend leaderboard only. WeChat friend data must stay in open-data domain.
    public sealed class LeaderboardOverlay : MonoBehaviour
    {
        private LeaderboardController controller;
        private Vector2 scroll;
        public void Bind(ILeaderboardProvider provider, string selfId)
        { controller?.Dispose(); controller = new LeaderboardController(provider, selfId); Refresh(); }
        public async void Refresh() { if (controller != null) await controller.RefreshAsync(); }
        private void OnDestroy() { controller?.Dispose(); }
        private void OnDisable() { controller?.Dispose(); controller = null; }
        private Rect Bounds()
        {
            var safe = Screen.safeArea;
            float width = Mathf.Min(520, safe.width - 24), height = Mathf.Min(650, safe.height - 24);
            return new Rect(safe.center.x - width / 2, Screen.height - safe.center.y - height / 2, width, height);
        }
        public bool BlocksPointer(Vector2 screenPosition)
        { return isActiveAndEnabled && Bounds().Contains(new Vector2(screenPosition.x, Screen.height - screenPosition.y)); }
        private void OnGUI()
        {
            GUILayout.BeginArea(Bounds(), GUI.skin.box);
            GUILayout.Label("排行榜");
            if (controller == null) GUILayout.Label("请调用 Bind(provider, selfId) 接入数据源");
            else
            {
                if (controller.State == LeaderboardState.Loading) GUILayout.Label("加载中…");
                if (controller.State == LeaderboardState.Failure) GUILayout.Label("加载失败：" + controller.Error);
                if (controller.State == LeaderboardState.Empty) GUILayout.Label("暂无其他玩家记录");
                scroll = GUILayout.BeginScrollView(scroll);
                if (controller.Current != null)
                    foreach (var row in controller.Current.Rows)
                        GUILayout.Label(row.Rank + "   " + row.Entry.Name + "   " + row.Entry.Score + (row.IsSelf ? "  我" : ""));
                GUILayout.EndScrollView();
                var self = controller.Current?.Self;
                GUILayout.Label(self == null ? "我的排名：暂无可靠记录" : "我的排名：" + self.Rank + "    成绩：" + self.Entry.Score);
                if (GUILayout.Button("刷新 / 重试")) Refresh();
            }
            GUILayout.EndArea();
        }
    }
}
