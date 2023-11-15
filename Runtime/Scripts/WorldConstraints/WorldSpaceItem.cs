using UnityEngine;

namespace Yueby.AvatarTools.WorldConstraints
{
#if UNITY_EDITOR
    public class WorldSpaceItem : MonoBehaviour
    {
        [SerializeField] private Transform _itemContainer;

        public void SetParent(GameObject target)
        {
            target.transform.SetParent(_itemContainer);
        }
    }
#endif
}