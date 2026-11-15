using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TitanGate.Game
{
    public partial class GameFlow
    {
        string _actionPose;
        bool _petStay;
        SpriteRenderer _petCommand;
        Label _petPrompt;
        SpriteRenderer _mentor;
        int _actionCaptureIndex;
        string PetId => _bMode == 1 ? _sPet ?? "comet" : _life?.State.pet ?? "comet";
        bool ActionLocked => _actionPose != null || _sleepTransition || _bSleeping;

        void ResetActions()
        {
            _sleepTransition = _bSleeping = false;
            _actionPose = null; _petStay = false; _petCommand = null; _mentor = null;
            _petPrompt?.RemoveFromHierarchy(); _petPrompt = null;
        }

        IEnumerator Act(string pose, float seconds, Action complete)
        {
            if (ActionLocked) yield break;
            var owner = _bRoot;
            _actionPose = pose;
            _bPath = null; _bGoal = null; _bMoving = false;
            if (AutoShotRequested()) StartCoroutine(CaptureActionPose(pose, ++_actionCaptureIndex));
            yield return new WaitForSeconds(seconds);
            if (_bRoot != owner || owner == null) yield break;
            _actionPose = null;
            complete?.Invoke();
        }

        IEnumerator CaptureActionPose(string pose, int index)
        {
            yield return new WaitForSeconds(.3f);
            yield return new WaitForEndOfFrame();
            if (_actionPose == pose && _shotDir != null) ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_shotDir, $"action_{index:00}_{pose}.png"));
        }

        string TaskPose(string id) => id.StartsWith("plot") ? "plant" : id == "plants" ? "water"
            : id == "recycler" || id == "solar" || id == "drill" ? "repair"
            : id == "bowl" ? "feed" : id == "radio" ? "repair" : "lift";

        void PetMenu()
        {
            if (_sScramble && _bMode == 1) { _ui.Toast(CrewText("We need to reach the escape pod!", "ต้องไปยานกู้ชีพก่อนนะ!")); return; }
            _bPath = null; _bGoal = null;
            _ui.ShowModal(panel =>
            {
                panel.style.width = 560;
                var face = new Image { sprite = Art.PetFrame(PetId, -1, 0), scaleMode = ScaleMode.ScaleToFit }; face.style.height = 130; panel.Add(face);
                panel.Add(_ui.L(_loc.T("pet." + PetId + ".name"), 30, true, GameUI.CAmber));
                panel.Add(_ui.L(CrewText("A little company, no supplies or work hours needed.", "อยู่เป็นเพื่อนกัน ไม่เสียเสบียงหรือเวลาทำงาน"), 21));
                panel.Add(_ui.B(CrewText("Play together", "เล่นด้วยกัน"), () => { _ui.CloseModal(); StartCoroutine(Act("pet", 1.2f, () => { _bHeartT = 1.2f; _sfx.Play("good", .4f); })); }));
                panel.Add(_ui.B(CrewText("Wait here", "นั่งรอตรงนี้นะ"), () => { _petStay = true; _bTrail.Clear(); _ui.CloseModal(); }));
                panel.Add(_ui.B(CrewText("Come with me", "ตามมาด้วยกันนะ"), () => { _petStay = false; _bTrail.Clear(); _ui.CloseModal(); }));
                panel.Add(_ui.B(CrewText("See you in a moment", "ไว้เล่นกันใหม่นะ"), () => _ui.CloseModal()));
            });
        }

        void ActionPresentation()
        {
            if (_bPlayer == null) return;
            if (_bSleeping)
            {
                _bPlayer.sprite = Art.Cadet(0, -2, false, false);
                _bPlayer.transform.rotation = Quaternion.Euler(0, 0, 90);
                _bPlayer.transform.position = _sleepBed + new Vector2(.85f, .48f);
                _bPlayer.sortingOrder = SortY(_sleepBed.y) + 10;
                return;
            }
            _bPlayer.transform.rotation = Quaternion.identity;
            bool sit = _bGather || _bMode == 1 && _sSitting;
            string pose = sit ? "sit" : ActionLocked ? _actionPose : _bMode == 1 && (_sCarry != null || _sHands.Count > 0) ? "carry" : null;
            if (pose != null)
            {
                _bPlayer.sprite = Art.CrewAction("cadet", pose, pose == "carry" ? (_bMoving ? (int)_bGaitPhase % 4 : 0) : (int)(_bAnim * 6) % 4, BIsOutside(_bPos));
                // drawn poses face right; turn them toward the machine being used
                _bPlayer.flipX = pose == "sit" || !BFacesMachine(pose) ? !_bRight : false;
                if (pose != "sit") BCadetActionSprite(pose);   // carry/use in 8 directions (GameFlow.Feel.cs)
                if (pose == "sit") _bPlayer.sortingOrder = SortY(_bPos.y) + 8;
            }
            if (_petCommand == null) _petCommand = BSprite("pet-command", Art.PetCommandIcon(PetId), Vector2.zero, 31002);
            if (_petStay) _bPet.sprite = Art.PetRest(PetId);
            if (ActionLocked && _actionPose == "pet" && !Settings.ReducedMotion)
                _bPet.transform.position += Vector3.up * Mathf.Abs(Mathf.Sin(_bAnim * 9)) * .25f;
            bool near = _bPet.gameObject.activeSelf && Vector2.Distance(_bPos, _bPetPos) < 1.8f && !_ui.Blocking && !ActionLocked && !sit && BNearestSpot()?.id == "pet";
            _petCommand.gameObject.SetActive(false);   // no portrait popup over the pet (the [E] prompt is enough)
            if (near) _petCommand.transform.position = new Vector3(_bPetPos.x, _bPetPos.y + 1.45f, 0);
            if (_petPrompt == null)
            {
                _petPrompt = _ui.L("", 21, true, GameUI.CText); _petPrompt.style.position = Position.Absolute;
                _petPrompt.style.bottom = 78; _petPrompt.style.alignSelf = Align.Center; _petPrompt.style.backgroundColor = GameUI.CPanel;
                _petPrompt.style.paddingLeft = _petPrompt.style.paddingRight = 14; _ui.Root.Add(_petPrompt);
            }
            _petPrompt.style.display = near ? DisplayStyle.Flex : DisplayStyle.None;
            if (near) _petPrompt.text = GameUI.Thai("[E] " + _loc.T("pet." + PetId + ".name") + (_petStay ? CrewText(" · waiting", " · กำลังรอ") : ""));
            if (_mentor != null) { _mentor.sprite = Art.Crew("mentor", 0, -1, false, false, _sTarget == "sofa"); _mentor.gameObject.SetActive(!_sScramble && !_sDone); }
        }
    }
}
