using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Glider.Core.Nav
{
    public class GliderNav : MonoBehaviour
    {
        private static GliderNav instance;
        private static CancellationTokenSource _cts;

        public static GliderNav Get()
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(GliderNav));
                instance = go.AddComponent<GliderNav>();
            }

            return instance;
        }

        // 이미 존재하는 GliderNav가 우선
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }

            instance = this;
        }

        public static GliderNavMap NavMap { get; private set; }
        private static Dictionary<string, GliderNavActivity> _navMapDic = new Dictionary<string, GliderNavActivity>();
        private static Dictionary<string, Type> _initialReflection = new Dictionary<string, Type>();

        // 게임 시작시에 GliderNavMap 에셋 로드
        // 에셋 번들 다운로드 체크 후 로딩해야 함
        public static async UniTask Load()
        {
            _cts = new CancellationTokenSource();

            try
            {
                NavMap = (await Addressables.LoadAssetAsync<GliderNavMap>("GliderNavMap")
                    .ToUniTask(cancellationToken: _cts.Token));
            }
            catch (Exception e)
            {
                Debug.LogError("[GliderNav] Please add \"GliderNavMap\" object in Addressables!!!");
                return;
            }

            if (NavMap != null)
            {
                foreach (var activity in NavMap.activities)
                {
                    _navMapDic.Add(activity.id, activity);
                }

                Debug.Log($"[GliderNav] activities Count: {_navMapDic.Count}");
            }

            var lists = FindClassesAttributesNavController();

            Debug.Log($"[GliderNav] Find Controllers Start");
            foreach (var list in lists)
            {
                Debug.Log(list.Name);
                _initialReflection.Add(list.Name, list);
            }

            Debug.Log($"[GliderNav] Find Controllers End");
        }

        public static List<Type> FindClassesAttributesNavController()
        {
            List<Type> attributtedClasses = new List<Type>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type classType in types)
                {
                    var controllerAttribute =
                        (NavControllerAttribute)classType.GetCustomAttribute(typeof(NavControllerAttribute));
                    if (controllerAttribute != null)
                    {
                        var Type = controllerAttribute.type;
                        attributtedClasses.Add(classType);
                    }
                }
            }

            return attributtedClasses;
        }

        // 클래스 이름으로 저장, payload의 클래스명을 통해 검색
        private Dictionary<string, NavControllerWrapper> _openedControllersDic =
            new Dictionary<string, NavControllerWrapper>();

        public void PushOpenedNavController(NavControllerWrapper wrapper)
        {
            // override controller
            if (_openedControllersDic.ContainsKey(wrapper.type.Name))
                _openedControllersDic.Remove(wrapper.type.Name);
            _openedControllersDic.Add(wrapper.type.Name, wrapper);

            Debug.Log($"[GliderNav] push base {wrapper.type}");
        }

        public async UniTask Show(string navMapId, params object[] parameters)
        {
            if (_navMapDic.ContainsKey(navMapId) == false)
            {
                Debug.LogError($"[GliderNav] not in map: {navMapId}");
                return;
            }

            var className = _navMapDic[navMapId].controller;
            var methodName = _navMapDic[navMapId].method;
            var methodParameters = parameters.ToArray();

            // 컨트롤러가 생성되어 있는지 확인, 없으면 생성
            await CheckControllerExists(className, _cts.Token);

            _openedControllersDic.TryGetValue(className, out var wrapper);
            if (wrapper == null)
            {
                Debug.LogError($"[GliderNav] not opened : {className}");
                return;
            }

            Type t = wrapper.type;
            var method = t.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Debug.Log($"[GliderNav] Show\n" +
                      $"{t.Name}\n" +
                      $"{methodName}\n" +
                      $"{method}");
            var objInstance = wrapper.instance;
            if (method != null)
            {
                method.Invoke(objInstance, methodParameters);
                PushHistory(new GliderNavPayload(navMapId, methodParameters));
            }
            else
            {
                Debug.LogError("[GliderNav] method is null");
            }
        }

        private async UniTask CheckControllerExists(string className, CancellationToken token)
        {
            // 현재 생성된 컨트롤러가 없음
            if (!_openedControllersDic.ContainsKey(className))
            {
                // 초기 로딩때 찾은 하위 스크립트 중에 해당 컨트롤러가 있는지 확인
                if (_initialReflection.ContainsKey(className))
                {
                    // 있으면 새로 생성해서 Activate.
                    var type = _initialReflection[className];
                    var obj = Activator.CreateInstance(type);
                    var controller = obj;
                    _openedControllersDic.Add(type.Name, new NavControllerWrapper(type, obj));
                }
                else
                {
                    Debug.LogError($"[GliderNav] {className} does not exists.");
                }
            }
        }

        private List<GliderNavView> _stackedViews = new List<GliderNavView>();

        public void PushNavView(GliderNavView view)
        {
            var type = view.GetType();
            _stackedViews.Add(view);
        }

        public async UniTask TryRevertLastView(ViewType viewType, CancellationToken token)
        {
            var viewTarget = (int)viewType;
            for (var i = _stackedViews.Count - 1; i >= 0; i--)
            {
                var index = i;
                // Debug.Log($"[GliderNav Manager] descending: {_stackedViews[i].GetType()}");
                // 더 낮은 단계의 UI부터 DeActivate
                if (viewTarget <= (int)_stackedViews[index].ViewType)
                {
                    Debug.Log($"[GliderNav Manager] deActivated {index} / {_stackedViews[index].GetType()}");
                    _stackedViews[index].DeActivate();
                    // _stackedViews.RemoveAt(index);
                    break;
                }
            }
        }

        public void TryRemoveTargetViewInHistory(GliderNavView view)
        {
            for (var i = _stackedViews.Count - 1; i > 0; i--)
            {
                if (_stackedViews[i] == view)
                {
                    Debug.Log($"[GliderNav Manager] deActivated {i} / {_stackedViews[i].GetType()}");
                    _stackedViews.RemoveAt(i);
                    break;
                }
            }
        }

        internal class NavHistoryQueue<T> where T : GliderNavPayload
        {
            private List<T> _list = new List<T>();
            private int _capacity;
            public int Count => _list.Count;

            public NavHistoryQueue()
            {
                _capacity = 15;
            }

            public NavHistoryQueue(int capacity)
            {
                _capacity = capacity;
            }

            public T this[int index]
            {
                get => _list[index];
                set { }
            }

            public void Add(T item)
            {
                _list.Add(item);
                if (_list.Count > _capacity)
                    _list.RemoveAt(0);
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
            }

            public void Clear()
            {
                _list.Clear();
            }
        }

        private NavHistoryQueue<GliderNavPayload> _queue = new NavHistoryQueue<GliderNavPayload>();

        private void PushHistory(GliderNavPayload payload)
        {
            _queue.Add(payload);
        }

        public GliderNavPayload ViewLastHistory()
        {
            return _queue[_queue.Count - 1];
        }
    }

    [Serializable]
    public class GliderNavPayload
    {
        public string activityId;
        public object[] methodParameters;

        public GliderNavPayload(string activityId)
        {
            this.activityId = activityId;
        }

        public GliderNavPayload(string activityId, object[] methodParameters)
        {
            this.activityId = activityId;
            this.methodParameters = methodParameters;
        }
    }

    [Serializable]
    public class NavControllerWrapper
    {
        public Type type;
        public object instance;

        public T GetInstance<T>() where T : class
        {
            return (T)instance;
        }

        public NavControllerWrapper(Type type, object instance)
        {
            this.type = type;
            this.instance = instance;
        }
    }
}