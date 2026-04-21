using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace Elympics
{
    // source: https://gamedev.stackexchange.com/a/193912
    internal static class HierarchicalSorting
    {
#if !(UNITY_2022_3 || UNITY_6000_1_OR_NEWER)
        private static readonly List<Component> ComponentListCached = new();
#endif

        private static int GetComponentIndex(Component component)
        {
#if UNITY_2022_3 || UNITY_6000_1_OR_NEWER
            return component.GetComponentIndex();
#else
            component.gameObject.GetComponents(ComponentListCached);
            var index = ComponentListCached.IndexOf(component);
            ComponentListCached.Clear();
            return index;
#endif
        }

        private static int Compare(Component? x, Component? y)
        {
            var compare = Compare(x?.transform, y?.transform);
            if (compare != 0 || x is null || y is null)
                return compare;
            return GetComponentIndex(x).CompareTo(GetComponentIndex(y));
        }

        private static int Compare(GameObject? x, GameObject? y) =>
            Compare(x?.transform, y?.transform);

        private static int Compare(Transform? x, Transform? y)
        {
            if (x is null && y is null)
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return +1;

            if (x.gameObject.scene != y.gameObject.scene)
                throw new ArgumentException("Cannot compare hierarchy of objects from different scenes", nameof(y));

            var hierarchy1 = GetHierarchy(x);
            var hierarchy2 = GetHierarchy(y);

            while (hierarchy1.Any() || hierarchy2.Any())
            {
                if (!hierarchy1.Any())
                    return -1;
                if (!hierarchy2.Any())
                    return +1;

                var compare = hierarchy1.Pop().CompareTo(hierarchy2.Pop());
                if (compare != 0)
                    return compare;
            }
            return 0;
        }

        public static Stack<int> GetHierarchy(Transform transform)
        {
            if (transform is null)
                throw new ArgumentNullException(nameof(transform));

            var stack = new Stack<int>();
            var current = transform;
            while (current is not null)
            {
                stack.Push(current.GetSiblingIndex());
                current = current.parent;
            }
            return stack;
        }

        [PublicAPI]
        public static void Sort<T>(T[] components) where T : Component
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            Array.Sort(components, Compare);
        }

        [PublicAPI]
        public static void Sort(GameObject[] gameObjects)
        {
            if (gameObjects == null)
                throw new ArgumentNullException(nameof(gameObjects));

            Array.Sort(gameObjects, Compare);
        }

        [PublicAPI]
        public static void Sort(Transform[] transforms)
        {
            if (transforms == null)
                throw new ArgumentNullException(nameof(transforms));

            Array.Sort(transforms, Compare);
        }

        [PublicAPI]
        public static void Sort<T>(List<T> components) where T : Component
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            components.Sort(Compare);
        }

        [PublicAPI]
        public static void Sort(List<GameObject> gameObjects)
        {
            if (gameObjects == null)
                throw new ArgumentNullException(nameof(gameObjects));

            gameObjects.Sort(Compare);
        }

        [PublicAPI]
        public static void Sort(List<Transform> transforms)
        {
            if (transforms == null)
                throw new ArgumentNullException(nameof(transforms));

            transforms.Sort(Compare);
        }
    }
}
