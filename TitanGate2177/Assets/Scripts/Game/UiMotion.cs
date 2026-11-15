using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace TitanGate.Game
{
    // Unscaled, cancellable feedback. Detached elements never keep a scheduled animation alive.
    public static class UiMotion
    {
        public const float Micro=.09f, Quick=.15f, Base=.22f, Panel=.32f, Exit=.18f, Stagger=.04f, Alert=1.2f;
        static readonly Dictionary<VisualElement,IVisualElementScheduledItem> Runs=new();
        public static void Stop(VisualElement e) { if(Runs.TryGetValue(e,out var run)){run.Pause();Runs.Remove(e);} e.style.translate=new Translate(0,0);e.style.scale=new Scale(Vector3.one);e.style.opacity=1; }
        static void Run(VisualElement e,float seconds,Action<float> frame,bool loop=false,float delay=0)
        {
            Stop(e);if(Settings.ReducedMotion){frame(1);return;}
            float start=Time.unscaledTime+delay;
            IVisualElementScheduledItem job=null;
            job=e.schedule.Execute(()=>{
                if(e.panel==null){Stop(e);return;}
                if(Settings.ReducedMotion){Stop(e);frame(1);return;}
                float t=(Time.unscaledTime-start)/seconds;if(t<0)return;
                frame(loop ? t%1:Mathf.Clamp01(t));
                if(!loop&&t>=1){Stop(e);frame(1);}
            }).Every(16);Runs[e]=job;
            e.RegisterCallback<DetachFromPanelEvent>(_=>Stop(e));
        }
        public static void FadeSlideIn(VisualElement e,int index=0,float dy=14){Run(e,Base,t=>{float k=1-Mathf.Pow(1-t,3);e.style.opacity=k;e.style.translate=new Translate(0,dy*(1-k));},false,Mathf.Min(index,8)*Stagger);}
        public static void Pop(VisualElement e,float scale=1.12f)=>Run(e,Base,t=>e.style.scale=new Scale(Vector3.one*(1+(scale-1)*Mathf.Sin(t*Mathf.PI))));
        public static void Shake(VisualElement e,float px=6,float sec=.25f)=>Run(e,sec,t=>e.style.translate=new Translate(Mathf.Sin(t*Mathf.PI*6)*px*(1-t),0));
        public static void Pulse(VisualElement e,float period=.8f)=>Run(e,period,t=>e.style.opacity=Time.timeScale==0?1:.85f+.15f*Mathf.Cos(t*Mathf.PI*2),true);
        public static void CountTo(Label l,int from,int to,float sec=.35f)=>Run(l,sec,t=>l.text=GameUI.Thai(Mathf.RoundToInt(Mathf.Lerp(from,to,t)).ToString()));
        public static void Drain(VisualElement bar,float from,float to)=>Run(bar,Base,t=>bar.style.scale=new Scale(new Vector3(Mathf.Lerp(from,to,t),1,1)));
    }
}
