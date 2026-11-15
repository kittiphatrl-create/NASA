using TitanGate.Core.LastDock;
using UnityEngine;
using UnityEngine.UIElements;
namespace TitanGate.Game
{
    public partial class GameFlow
    {
        VisualElement OpIcon(string id,int size=40)
        {
            string mapped=id switch {"parts" or "wrench"=>"gear","time"=>"clock","signal"=>"radio","walk"=>"hand","rescue"=>"ship",_=>id};
            if(id!="warning"&&id!="book"&&id!="dice"&&id!="lock")return PauseIcon(mapped,size);
            var e=new VisualElement{pickingMode=PickingMode.Ignore};e.style.width=e.style.height=size;e.style.flexShrink=0;
            e.generateVisualContent+=ctx=>{var p=ctx.painter2D;float u=size/32f;p.lineWidth=2*u;p.strokeColor=FieldBook.Ink;p.fillColor=FieldBook.Amber;
                p.BeginPath();
                if(id=="warning"){p.MoveTo(new Vector2(16,2)*u);p.LineTo(new Vector2(30,29)*u);p.LineTo(new Vector2(2,29)*u);}
                else{p.MoveTo(new Vector2(5,3)*u);p.LineTo(new Vector2(27,3)*u);p.LineTo(new Vector2(27,29)*u);p.LineTo(new Vector2(5,29)*u);}
                p.ClosePath();p.Fill();p.Stroke();p.BeginPath();
                if(id=="warning"){p.MoveTo(new Vector2(16,10)*u);p.LineTo(new Vector2(16,20)*u);p.MoveTo(new Vector2(16,24)*u);p.LineTo(new Vector2(16,26)*u);}
                else if(id=="lock"){p.MoveTo(new Vector2(10,12)*u);p.LineTo(new Vector2(10,6)*u);p.LineTo(new Vector2(22,6)*u);p.LineTo(new Vector2(22,12)*u);p.MoveTo(new Vector2(16,17)*u);p.LineTo(new Vector2(16,23)*u);}
                else if(id=="book"){p.MoveTo(new Vector2(10,3)*u);p.LineTo(new Vector2(10,29)*u);p.MoveTo(new Vector2(15,10)*u);p.LineTo(new Vector2(24,10)*u);p.MoveTo(new Vector2(15,16)*u);p.LineTo(new Vector2(23,16)*u);}
                else{p.MoveTo(new Vector2(10,10)*u);p.LineTo(new Vector2(12,10)*u);p.MoveTo(new Vector2(15,16)*u);p.LineTo(new Vector2(17,16)*u);p.MoveTo(new Vector2(21,23)*u);p.LineTo(new Vector2(23,23)*u);}
                p.Stroke();};return e;
        }
        VisualElement OpRow(){var e=new VisualElement();e.style.flexDirection=FlexDirection.Row;e.style.alignItems=Align.Center;e.style.flexWrap=Wrap.Wrap;return e;}
        VisualElement OpBadge(string icon,string text,string hint,Color? color=null)
        {
            var e=OpRow();e.tooltip=hint;e.focusable=true;e.style.backgroundColor=FieldBook.Ink;e.style.paddingLeft=e.style.paddingRight=10;e.style.paddingTop=e.style.paddingBottom=5;e.style.marginRight=8;e.style.marginBottom=6;
            GameUI.Border(e,color??FieldBook.Rim,2,7);e.Add(OpIcon(icon,32));var l=_ui.L(text,22,true,color??FieldBook.Paper);l.style.marginLeft=8;e.Add(l);return e;
        }
        void OpPanel(VisualElement panel)
        {
            panel.style.maxWidth=Length.Percent(94);panel.style.maxHeight=Length.Percent(94);panel.style.minWidth=Mathf.Min(640,_ui.Root.resolvedStyle.width*.9f);
            var outer=panel.parent;panel.style.backgroundColor=Color.clear;
            var old=new System.Collections.Generic.List<VisualElement>();foreach(var child in panel.Children())if(child.style.position.value!=Position.Absolute)old.Add(child);
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;scroll.style.flexShrink=1;scroll.style.maxHeight=Mathf.Max(300,_ui.Root.resolvedStyle.height*.78f);scroll.style.minHeight=0;
            foreach(var child in old){child.RemoveFromHierarchy();scroll.Add(child);}panel.Add(scroll);
            panel.RegisterCallback<KeyDownEvent>(evt=>{if(evt.keyCode==KeyCode.Escape){_ui.CloseModal();evt.StopPropagation();}});
            panel.schedule.Execute(()=>{var first=scroll.Q<Button>();first?.Focus();}).StartingIn(50);
            UiMotion.FadeSlideIn(panel);
        }
        Label _opDay;VisualElement _opDayBadge,_opClock,_opEvac;Label _opSeconds;VisualElement _opHands,_opPod;int _opDaySeen=-1,_opWorkSeen=-1,_opSecondSeen=-1,_opStored=-1,_opHeld=-1;
        void OpSlots(VisualElement host,int count,int total,Color color)
        {
            host.Clear();for(int i=0;i<total;i++){var slot=new VisualElement();slot.style.width=slot.style.height=17;slot.style.marginRight=4;slot.style.backgroundColor=i<count?color:FieldBook.Ink;GameUI.Border(slot,FieldBook.Rim,2,3);host.Add(slot);}
        }
        void OpHud(bool show)
        {
            if(_opDayBadge==null||_opDayBadge.panel==null){
                _opDayBadge=new VisualElement{pickingMode=PickingMode.Ignore};_opDayBadge.style.position=Position.Absolute;_opDayBadge.style.right=24;_opDayBadge.style.top=20;_opDayBadge.style.paddingLeft=_opDayBadge.style.paddingRight=18;_opDayBadge.style.paddingTop=_opDayBadge.style.paddingBottom=8;
                _opDayBadge.style.backgroundColor=FieldBook.Ink;GameUI.Border(_opDayBadge,FieldBook.Paper,4,10);_opDayBadge.style.rotate=new Rotate(new Angle(-4));
                _opDay=_ui.L("",46,true,FieldBook.Amber);_opDayBadge.Add(_opDay);_opClock=OpRow();_opDayBadge.Add(_opClock);_ui.Root.Add(_opDayBadge);
            }
            _opDayBadge.style.display=show&&_bMode==2?DisplayStyle.Flex:DisplayStyle.None;
            if(show&&_bMode==2&&_ld!=null&&(_opDaySeen!=_ld.Day||_opWorkSeen!=_ld.WorkUnits)){
                bool day=_opDaySeen!=_ld.Day;_opDay.text=GameUI.Thai(TT("DAY ","วันที่ ")+_ld.Day.ToString("00"));_opClock.Clear();_opClock.Add(OpIcon("time",28));var slots=OpRow();OpSlots(slots,_ld.WorkUnits,_ld.C.workUnits,_ld.WorkUnits<=1?FieldBook.Red:FieldBook.Amber);_opClock.Add(slots);UiMotion.Pop(_opDayBadge,day?1.16f:1.04f);if(day&&_opDaySeen>=0)_sfx.Play("stamp",.5f);_opDaySeen=_ld.Day;_opWorkSeen=_ld.WorkUnits;
            }
            bool evac=show&&_bMode==1&&_sScramble&&!_sDone;
            if(_opEvac==null||_opEvac.panel==null){_opEvac=OpRow();_opEvac.name="evacuation-countdown";_opEvac.pickingMode=PickingMode.Ignore;_opEvac.style.position=Position.Absolute;_opEvac.style.top=18;_opEvac.style.left=Length.Percent(50);_opEvac.style.translate=new Translate(Length.Percent(-50),0);_opEvac.style.paddingLeft=_opEvac.style.paddingRight=20;_opEvac.style.paddingTop=_opEvac.style.paddingBottom=8;_opEvac.style.backgroundColor=FieldBook.Ink;GameUI.Border(_opEvac,FieldBook.Red,4,12);OpHazard(_opEvac);_opEvac.Add(OpIcon("warning",48));_opSeconds=_ui.L("",52,true,FieldBook.Paper);_opSeconds.style.minWidth=115;_opEvac.Add(_opSeconds);var carry=new VisualElement();carry.Add(OpIcon("hand",30));_opHands=OpRow();carry.Add(_opHands);_opEvac.Add(carry);var pod=new VisualElement();pod.style.marginLeft=18;pod.Add(OpIcon("ship",30));_opPod=OpRow();pod.Add(_opPod);_opEvac.Add(pod);_ui.Root.Add(_opEvac);}
            _opEvac.style.display=evac?DisplayStyle.Flex:DisplayStyle.None;
            if(!evac)return;
            int seconds=Mathf.CeilToInt(_sLeft);_opSeconds.text=GameUI.Thai(seconds.ToString("00"));_opSeconds.style.color=seconds<=10?FieldBook.Red:seconds<=30?FieldBook.Amber:FieldBook.Paper;
            if(_opHeld!=_sHands.Count){OpSlots(_opHands,_sHands.Count,HandSlots,FieldBook.Amber);_opHeld=_sHands.Count;} if(_opStored!=_stowed.Count)OpSlots(_opPod,_stowed.Count,StowSlots,FieldBook.Green);
            if(seconds!=_opSecondSeen){if(seconds<=10&&seconds>0){_sfx.Play(seconds<=3?"tick_hi":"tick",.45f);UiMotion.Pop(_opSeconds);} _opSecondSeen=seconds;}
            if(_stowed.Count!=_opStored){if(_opStored>=0){UiMotion.Pop(_opPod);_sfx.Play("slot",.5f);}_opStored=_stowed.Count;}
        }
        static void OpHazard(VisualElement element)
        {
            element.generateVisualContent+=ctx=>{var p=ctx.painter2D;float w=element.contentRect.width,h=element.contentRect.height;p.fillColor=FieldBook.Red;for(float x=0;x<w;x+=28){p.BeginPath();p.MoveTo(new Vector2(x,h-7));p.LineTo(new Vector2(x+9,h-7));p.LineTo(new Vector2(x+16,h));p.LineTo(new Vector2(x+7,h));p.ClosePath();p.Fill();}};
        }
        void OpEvacuationRules()
        {
            _sfx.Play("stinger",.55f);_chapterMusic?.DuckFor(UiMotion.Alert+1);
            _ui.ShowModal(p=>{
                var banner=OpRow();banner.style.backgroundColor=FieldBook.Red;banner.style.paddingTop=banner.style.paddingBottom=12;banner.style.rotate=new Rotate(new Angle(-2));banner.Add(OpIcon("warning",70));var title=_ui.L(TT("EVACUATE!","อพยพ!"),44,true,FieldBook.Ink);banner.Add(title);OpHazard(banner);p.Add(banner);UiMotion.FadeSlideIn(banner);
                var row=OpRow();row.style.marginTop=18;row.Add(OpBadge("time",$"{_cfg.chapter1.scrambleSeconds:0}",TT("Seconds","วินาที")));row.Add(OpBadge("hand",$"{ScavengeInventory.CarryMax}",TT("Carry capacity","ถือได้ครั้งละ")));row.Add(OpBadge("ship",$"{ScavengeInventory.PodMax}",TT("Pod capacity","ยานจุได้")));row.Add(OpBadge("lock","1",TT("Keycard opens one vault","คีย์การ์ดเปิดตู้หนึ่งใบ")));p.Add(row);
                p.Add(LdPara(TT("[E] Pick up / Load","[E] หยิบ / ใส่ยาน"),FieldBook.Paper,28));p.Add(LdPara(TT("Time up: held items stay behind","หมดเวลา: ของในมือจะตกค้าง"),FieldBook.Red,22));
                var go=_ui.B(TT("Start evacuation","เริ่มอพยพ"),_ui.CloseModal,30,true);go.style.marginTop=18;p.Add(go);OpPanel(p);
            },false);
        }
    }
}
