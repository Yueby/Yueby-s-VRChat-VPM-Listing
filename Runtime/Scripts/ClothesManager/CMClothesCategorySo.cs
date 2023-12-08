using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;

namespace Yueby.AvatarTools
{
    public class CMClothesCategorySo : ScriptableObject
    {
        public int Selected;
        public int Default;
        public string Name;
        public Texture2D Icon;
        public List<CMClothesData> Clothes = new List<CMClothesData>();

        public void Clear()
        {
            Name = string.Empty;
            Icon = null;

            Clothes = new List<CMClothesData>();
            foreach (var cloth in Clothes)
                cloth.Clear();
        }

        public bool HasParameterDriver()
        {
            foreach (var clothes in Clothes)
            {
                if (clothes.HasParameterDriver)
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public class CMClothesData
    {
        public string Name;
        public Texture2D Icon;

        public List<ClothesAnimParameter> ShowParameters = new List<ClothesAnimParameter>();
        public List<ClothesAnimParameter> HideParameters = new List<ClothesAnimParameter>();
        public List<ClothesAnimParameter> BlendShapeParameters = new List<ClothesAnimParameter>();

        public bool HasParameterDriver;
        public ParameterDriver EnterParameter = new ParameterDriver();
        public ParameterDriver ExitParameter = new ParameterDriver();

        public void Clear()
        {
            Name = string.Empty;
            Icon = null;

            ShowParameters = new List<ClothesAnimParameter>();
            HideParameters = new List<ClothesAnimParameter>();
            BlendShapeParameters = new List<ClothesAnimParameter>();
        }

        public bool HasRealParameterDriver()
        {
            return HasParameterDriver && (EnterParameter.Parameters.Count > 0 || ExitParameter.Parameters.Count > 0);
        }

        public bool ContainsInList(ClothesAnimParameter clothesAnimParameter, List<ClothesAnimParameter> animParameters)
        {
            foreach (var parameter in animParameters)
            {
                if (parameter.Type != clothesAnimParameter.Type || parameter.Path != clothesAnimParameter.Path) continue;
                if (parameter == clothesAnimParameter) continue;
                switch (clothesAnimParameter.Type)
                {
                    case nameof(GameObject):
                    case nameof(SkinnedMeshRenderer) when parameter.BlendShapeIndex == clothesAnimParameter.BlendShapeIndex:
                        return true;
                }
            }

            return false;
        }

        public void DeleteInList(ClothesAnimParameter clothesAnimParameter, ref List<ClothesAnimParameter> animParameters)
        {
            if (!ContainsInList(clothesAnimParameter, animParameters)) return;

            foreach (var parameter in animParameters.ToList())
            {
                if (parameter.Type != clothesAnimParameter.Type || parameter.Path != clothesAnimParameter.Path) continue;

                if (clothesAnimParameter.Type == nameof(GameObject))
                {
                    animParameters.Remove(parameter);
                }
                else if (clothesAnimParameter.Type == nameof(SkinnedMeshRenderer))
                {
                    if (parameter.BlendShapeIndex == clothesAnimParameter.BlendShapeIndex)
                        animParameters.Remove(parameter);
                }
            }
        }


        [Serializable]
        public class ParameterDriver
        {
            public bool IsLocal = true;
            public List<VRC_AvatarParameterDriver.Parameter> Parameters;
        }

        [Serializable]
        public class ClothesAnimParameter
        {
            public string Path;
            public string Type;


            public int BlendShapeIndex = -1;
            public string BlendShapeName;
            public float BlendShapeValue;
        }
    }
}