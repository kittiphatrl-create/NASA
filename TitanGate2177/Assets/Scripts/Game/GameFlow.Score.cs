// AI-assisted implementation: OpenAI Codex, directed and reviewed through the user's soundtrack request.
namespace TitanGate.Game
{
    public partial class GameFlow
    {
        StageMusicPlayer _chapterMusic;
        bool UserScoreActive => _chapterMusic != null && _chapterMusic.HasOverride;

        void LdScore(StageMusicPlayer.Cue cue)
        {
            if (!LastDockMode || _sfx == null) return;
            if (_chapterMusic == null) _chapterMusic = _sfx.gameObject.AddComponent<StageMusicPlayer>();
            if (_chapterMusic.Play(cue)) LdMusicStop();
        }

        // Route by the shot actually being entered, so launch music changes at Mars entry,
        // not while the lifeboat is still escaping the ship. Direct/skip paths have explicit fallbacks.
        void LdScoreShot(string shot)
        {
            if (shot == "p01_earth") LdScore(StageMusicPlayer.Cue.Opening);
            else if (shot == "i01_debris" || shot == "l01_away") LdScore(StageMusicPlayer.Cue.Ship);
            else if (shot == "l02_entry" || shot == "l04_land" || shot == "l05_base") LdScore(StageMusicPlayer.Cue.Mars);
            else if (shot == "w01_rescue" || shot == "x01_dusk") LdScore(StageMusicPlayer.Cue.Ending);
        }
    }
}
