using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace Yueby.AvatarTools.ClothesManager
{
    public class CMClothesCategorySo : ScriptableObject
    {
        public VRCExpressionsMenu ParentMenu;
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
                if (clothes.HasParameterDriver && (clothes.EnterParameter.Parameters.Count > 0 || clothes.ExitParameter.Parameters.Count > 0))
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
        public List<ClothesAnimParameter> SMRParameters = new List<ClothesAnimParameter>();

        public bool HasParameterDriver;
        public ParameterDriver EnterParameter = new ParameterDriver();
        public ParameterDriver ExitParameter = new ParameterDriver();

        public void Clear()
        {
            Name = string.Empty;
            Icon = null;

            ShowParameters = new List<ClothesAnimParameter>();
            HideParameters = new List<ClothesAnimParameter>();
            SMRParameters = new List<ClothesAnimParameter>();
        }

        public List<ClothesAnimParameter> GetNotEmptyParameters(List<ClothesAnimParameter> parameters)
        {
            var list = new List<ClothesAnimParameter>();
            foreach (var pa in parameters)
            {
                if (string.IsNullOrEmpty(pa.Path)) continue;
                list.Add(pa);
            }

            return list;
        }

        public bool HasRealParameterDriver()
        {
            return HasParameterDriver && (EnterParameter.Parameters.Count > 0 || ExitParameter.Parameters.Count > 0);
        }

        public bool ContainsInList(ClothesAnimParameter clothesAnimParameter, List<ClothesAnimParameter> animParameters)
        {
            foreach (var parameter in animParameters)
            {
                if (parameter == clothesAnimParameter) continue;
                if (parameter.IsSame(clothesAnimParameter))
                {
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
                if (parameter.IsSame(clothesAnimParameter))
                {
                    animParameters.Remove(parameter);
                }
            }
        }

        [Serializable]
        public class ParameterDriver
        {
            public bool IsLocal = true;
            public List<Parameter> Parameters;

            public List<VRC_AvatarParameterDriver.Parameter> Convert()
            {
                return Parameters.Cast<VRC_AvatarParameterDriver.Parameter>().ToList();
            }
        }

        [Serializable]
        public class ClothesAnimParameter
        {
            public string Path;
            public string Type;

            public SMRParameter SmrParameter = new SMRParameter();

            public ClothesAnimParameter()
            {
            }

            public ClothesAnimParameter(ClothesAnimParameter clothesAnimParameter)
            {
                Path = clothesAnimParameter.Path;
                Type = clothesAnimParameter.Type;
                SmrParameter = new SMRParameter
                {
                    Type = clothesAnimParameter.SmrParameter.Type,
                    Material = clothesAnimParameter.SmrParameter.Material,
                    Index = clothesAnimParameter.SmrParameter.Index,
                    BlendShapeName = clothesAnimParameter.SmrParameter.BlendShapeName,
                    BlendShapeValue = clothesAnimParameter.SmrParameter.BlendShapeValue
                };
            }

            public bool IsSame(ClothesAnimParameter parameter)
            {
                if (parameter.Path != Path || parameter.Type != Type) return false;

                if (parameter.Type == nameof(GameObject))
                {
                    return true;
                }

                if (parameter.Type == nameof(SkinnedMeshRenderer))
                {
                    if (parameter.SmrParameter.IsSame(SmrParameter))
                        return true;
                }

                return false;
            }

            [Serializable]
            public class SMRParameter
            {
                [Serializable]
                public enum SMRType
                {
                    BlendShapes,
                    Materials
                }

                public SMRType Type = SMRType.BlendShapes;
                public Material Material;

                public int Index = -1;
                public string BlendShapeName;
                public float BlendShapeValue;

                public bool IsSame(SMRParameter parameter)
                {
                    if (parameter.Type == Type && parameter.Index == Index) return true;
                    return false;
                }
            }
        }
    }
}