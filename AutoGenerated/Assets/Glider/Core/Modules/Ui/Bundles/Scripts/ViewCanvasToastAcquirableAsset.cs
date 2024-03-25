using UnityEngine;

namespace Glider.Toast.Bundles
{
    public class ViewCanvasToastAcquirableAsset 
    {
        [Header("Basic")]
        [SerializeField] private Transform parent;

        public Transform GetParent() => parent;
    }
}