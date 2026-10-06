using UnityEngine;

namespace ReefExplorer.Audio
{
    /// <summary>
    /// Lightweight static audio facade. Spatial blips use PlayClipAtPoint (3D by default).
    /// Important feedback is also shown as on-screen text via MissionEvents.
    /// </summary>
    public static class GameAudio
    {
        static GameAudioHub hub;

        public static void Bind(GameAudioHub audioHub) => hub = audioHub;

        public static void PlayScannerStart(Vector3 position) => hub?.Play(GameAudioHub.Cue.ScannerStart, position);
        public static void PlayScannerProgress(Vector3 position, float t01) => hub?.PlayProgress(position, t01);
        public static void PlayScannerSuccess(Vector3 position) => hub?.Play(GameAudioHub.Cue.ScannerSuccess, position);
        public static void PlayInvalid(Vector3 position) => hub?.Play(GameAudioHub.Cue.Invalid, position);
        public static void PlaySampleFill(Vector3 position) => hub?.Play(GameAudioHub.Cue.SampleFill, position);
        public static void PlayObjectiveComplete(Vector3 position) => hub?.Play(GameAudioHub.Cue.ObjectiveComplete, position);
        public static void PlayMissionSuccess() => hub?.Play(GameAudioHub.Cue.MissionSuccess, Vector3.zero, false);
    }
}
