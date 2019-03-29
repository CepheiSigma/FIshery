using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fishery.Core
{
    public delegate void CallBack(object sender, object eventArgs);

    public class EventRouter:SharedObject
    {
        private static EventRouter _instance;
        private readonly Dictionary<string, List<EventCallBack>> _eventRouteList;

        private EventRouter()
        {
            _eventRouteList = new Dictionary<string, List<EventCallBack>>();
        }

        public Guid ListenTo(string eventName, CallBackDelegate callBack, bool async)
        {
            EventCallBack eventCallBack = new EventCallBack();
            eventCallBack.Method = callBack;
            eventCallBack.IsAsync = async;
            if (!_eventRouteList.ContainsKey(eventName))
            {
                _eventRouteList.Add(eventName, new List<EventCallBack>());
            }
            _eventRouteList[eventName].Add(eventCallBack);
            return eventCallBack.Handle;
        }

        public void UnListen(Guid handle)
        {
            foreach (var eventRoute in _eventRouteList)
            {
                int index = eventRoute.Value.FindIndex(callback => callback.Handle.Equals(handle));
                if(index>=0)
                    eventRoute.Value.RemoveAt(index);
            }
        }

        public void FireEvent(string eventName, object sender, object eventArgs, bool isSingle = false)
        {
            if (_eventRouteList.ContainsKey(eventName))
            {
                foreach (var _event in _eventRouteList[eventName])
                {
                    if (_event.IsAsync)
                    {
                        new Task(() => { _event.Method.Invoke(sender, eventArgs); }).Start();
                    }
                    else
                    {
                        //_event.Method.GetDelegate();
                       _event.Method.Invoke(sender, eventArgs);
                    }
                    if (isSingle)
                        break;
                }
            }
        }

        public static EventRouter GetInstance(EventRouter initialExtensionManager = null)
        {
            return _instance = _instance ?? initialExtensionManager ?? new EventRouter();
        }
    }

    public class CallBackDelegate : SharedObject
    {
        private CallBack _callBack;

        public CallBackDelegate(CallBack callBack)
        {
            _callBack = callBack;
        }

        public void Invoke(object sender, object eventArgs)
        {
            _callBack.Invoke(sender,eventArgs);
        }
    }

    public class EventCallBack
    {
        public CallBackDelegate Method { get; set; }
        public bool IsAsync { get; set; }
        public Guid Handle { get; set; }

        public EventCallBack()
        {
            Method = null;
            IsAsync = false;
            Handle = Guid.NewGuid();
        }
    }
}