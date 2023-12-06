using System.Collections.Generic;
using UnityEngine;

namespace Yueby.AvatarTools
{
    [CreateAssetMenu(menuName = "Clothes Manager/ Create CM Data", fileName = "New CM Data")]
    public class CMCDataSo : ScriptableObject
    {
        public List<CMClothesCategorySo> Categories;
    }
}