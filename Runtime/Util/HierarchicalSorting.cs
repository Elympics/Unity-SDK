using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace Elympics
{
    // source: https://gamedev.stackexchange.com/a/193912
    public static class HierarchicalSorting
    {
        private static int GetComponentIndex(Component component)
        {
#if UNITY_2022_3 || UNITY_6000_1_OR_NEWER
            return component.GetComponentIndex();
#else
            return Array.IndexOf(component.gameObject.GetComponents<Component>(), component);
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
            if (x == null && y == null)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return +1;

            var hierarchy1 = GetHierarchy(x);
            var hierarchy2 = GetHierarchy(y);

            while (true)
            {
                if (!hierarchy1.Any())
                    return -1;

                var pop1 = hierarchy1.Pop();

                if (!hierarchy2.Any())
                    return +1;

                var pop2 = hierarchy2.Pop();

                var compare = pop1.CompareTo(pop2);

                if (compare == 0)
                    continue;

                return compare;
            }
        }

        private static Stack<int> GetHierarchy(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var stack = new Stack<int>();
            var current = transform;
            while (current != null)
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
