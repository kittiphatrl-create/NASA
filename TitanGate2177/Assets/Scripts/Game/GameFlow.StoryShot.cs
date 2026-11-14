using System.Collections;
using UnityEngine;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        IEnumerator AutoStory()
        {
            _loc.Current = TitanGate.Core.Lang.TH;
            yield return new WaitForSeconds(2);
            StartLifeboat(null, "pixel");
            yield return new WaitForSeconds(1);
            PetMenu(); Check(_ui.ModalOpen, "pet commands open without changing daily resources");
            yield return Shot("01_pet_menu");
            Check(_ui.Press(CrewText("Wait here", "นั่งรอตรงนี้นะ")), "stay command uses actual menu button");
            var before = _bPetPos; _bPos += Vector2.right * 2;
            yield return new WaitForSeconds(1);
            Check(_petStay && Vector2.Distance(before, _bPetPos) < .01f, "stay command holds pet position");
            PetMenu(); _ui.Press(CrewText("Come with me", "ตามมาด้วยกันนะ"));
            Check(!_petStay && !_ui.ModalOpen, "follow command restores following and closes menu");
            foreach (var pose in new[] { "sit", "water", "repair", "plant", "carry", "pet", "feed", "lift" })
            {
                StartCoroutine(Act(pose, 1.2f, null));
                yield return new WaitForSeconds(.25f);
                Check(ActionLocked, pose + " has a bounded action state");
                yield return Shot("02_action_" + pose);
                while (ActionLocked) yield return null;
                Check(_actionPose == null, pose + " releases control after completion");
            }
            Check(_bKind[5, 11] != _bKind[20, 11], "escape pod and habitat have distinct floor treatments");
            StartCoroutine(BCutscene("intro"));
            yield return new WaitForSeconds(3); yield return Shot("03_earth");
            yield return new WaitForSeconds(4.6f); yield return Shot("03b_press_hall");
            yield return new WaitForSeconds(4.4f); yield return Shot("03c_cadet_on_stage");
            yield return new WaitForSeconds(4.4f); yield return Shot("03d_meet_arun");
            yield return new WaitForSeconds(5f); yield return Shot("04_jump");
            yield return new WaitForSeconds(5f); yield return Shot("05_mentor_cockpit");
            while (InCutscene) yield return null;
            Check(_bCam.enabled && !_rig.cam.enabled, "intro restores gameplay camera");
            StartCoroutine(BCutscene("evacuate"));
            yield return new WaitForSeconds(3); yield return Shot("06_manual_release");
            yield return new WaitForSeconds(4.4f); yield return Shot("07_separation");
            yield return new WaitForSeconds(3.2f); yield return Shot("07b_kestrel_explodes");
            while (InCutscene) yield return null;
            StartCoroutine(BCutscene("dock"));
            yield return new WaitForSeconds(4.6f); yield return Shot("08_docking");
            while (InCutscene) yield return null;
            Check(_bCam.enabled && !_ui.Blocking, "docking returns to playable habitat");
            foreach (var id in new[] { "mentor", "tik", "pip", "mu", "nok", "zee", "aya", "jules", "kira", "rex", "vela", "official" })
                SaveActorAtlas(System.IO.Path.Combine(_shotDir, "frames_" + id + ".png"), id);
            Log($"done errors={_errors} checksFailed={_checksFailed}");
            _autoLog?.Close(); _autoLog = null;
            yield return new WaitForSeconds(.5f); Application.Quit();
        }
    }
}
