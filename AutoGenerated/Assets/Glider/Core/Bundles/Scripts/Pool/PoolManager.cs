using System.Collections.Generic;
using Glider.Util;
using UnityEngine;

namespace Glider.Core.Bundles
{
    public static class PoolManager
    {
        #region Pool

        class Pool
        {
            public GameObject Original { get; private set; }
            public string OriginalName { get; private set; }
            public Transform Root { get; set; }

            Stack<Poolable> _poolStack = new Stack<Poolable>();

            public void Init(GameObject original, int count = 1)
            {
                Original = original;
                OriginalName = Original.name;
                Root = new GameObject().transform;
                Root.name = $"{OriginalName}_Root";

                for (int i = 0; i < count; i++)
                    Push(Create());
            }

            public Poolable Create()
            {
                GameObject go = Object.Instantiate<GameObject>(Original);
#if UNITY_EDITOR
                GliderUtil.ReAssignShader(go);
#endif
                go.name = OriginalName;
                return go.GetOrAddComponent<Poolable>();
            }

            public void Push(Poolable poolable)
            {
                if (poolable == null)
                    return;

                //poolable.transform.parent = Root;
                poolable.transform.SetParent(Root, false);
                poolable.gameObject.SetActive(false);
                poolable.isUsing = false;

                _poolStack.Push(poolable);
            }

            public Poolable Pop(Transform parent)
            {
                Poolable poolable;

                if (_poolStack.Count > 0)
                    poolable = _poolStack.Pop();
                else
                    poolable = Create();

                poolable.gameObject.SetActive(true);

                // DontDestroyOnLoad 해제 용도
                // if (parent == null)
                //     poolable.transform.parent = SceneBase.CurrentScene.transform;

                //poolable.transform.parent = parent;
                var transform = poolable.transform;
                transform.SetParent(parent, false);
                poolable.isUsing = true;

                return poolable;
            }

            public int GetCount()
            {
                return _poolStack.Count;
            }
        }

        #endregion

        static Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();
        static Transform _root;


        public static void Init()
        {
            if (_root == null)
            {
                _root = new GameObject { name = "@Pool_Root" }.transform;
                //Object.DontDestroyOnLoad(_root);
            }
        }

        public static bool ContainsPool(string name)
        {
            return _pools.ContainsKey(name);
        }

        public static void CreatePool(GameObject original, int count = 5)
        {
            if (_pools.ContainsKey(original.name)) return;
            Pool pool = new Pool();
            pool.Init(original, count);
            //pool.Root.parent = _root;
            pool.Root.SetParent(_root, false);

            _pools.Add(pool.OriginalName, pool);
        }

        public static void Push(Poolable poolable)
        {
            string name = poolable.gameObject.name;
            if (_pools.ContainsKey(name) == false)
            {
                GameObject.Destroy(poolable.gameObject);
                return;
            }

            _pools[name].Push(poolable);
        }

        public static Poolable Pop(GameObject original, Transform parent, int count = 5)
        {
            string name = original.name;
            if (_pools.ContainsKey(name) == false)
                CreatePool(original, count);

            return _pools[name].Pop(parent);
        }

        public static int GetPoolNowPoolingObjectsCount(GameObject original)
        {
            string name = original.name;
            if (_pools.ContainsKey(name) == false)
                return 0;
            return _pools[name].GetCount();
        }

        public static void CreateElement(GameObject original)
        {
            string name = original.name;
            if (_pools.ContainsKey(name) == false)
                CreatePool(original);
            _pools[name].Push(_pools[name].Create());
        }

        public static T Pop<T>(T original, Transform parent, int count = 5) where T : Poolable
        {
            return Pop(original.gameObject, parent, count).GetComponent<T>();
        }

        public static GameObject GetOriginal(string name)
        {
            if (_pools.ContainsKey(name) == false)
                return null;
            return _pools[name].Original;
        }

        public static void Clear()
        {
            foreach (Transform child in _root)
                GameObject.Destroy(child.gameObject);

            _pools.Clear();
        }


        public static void TemporaryPooling(Poolable p)
        {
            p.gameObject.SetActive(false);
            p.transform.SetParent(_root);
        }
    }
}