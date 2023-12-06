using System.Collections.Generic;
using UnityEngine.Events;

namespace Yueby.AvatarTools
{
    public class TabBarElement
    {
        public bool IsDraw { get; private set; } = true;
        public bool IsVisible { get; set; } = true;

        public string Title { get; }
        public float Space { get; }

        private readonly UnityAction _onDraw;

        public List<TabBarElement> InvertElements = new List<TabBarElement>();

        public TabBarElement(string title, UnityAction onDraw, bool isDrawDefault = true, float space = 0f)
        {
            Title = title;
            _onDraw = onDraw;
            Space = space;

            IsDraw = isDrawDefault;
        }

        public void Draw()
        {
            if (IsDraw)
                _onDraw?.Invoke();
        }

        public void ChangeDrawState(bool value)
        {
            IsDraw = value;

            foreach (var invert in InvertElements)
            {
                invert.IsDraw = !IsDraw;
            }
        }
    }
}