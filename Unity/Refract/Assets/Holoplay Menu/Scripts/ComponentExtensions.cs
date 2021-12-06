using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LookingGlass.Menu
{
    /// <summary>
    /// Helps manage components.
    /// </summary>
    /// <remarks>Based on
    /// https://forum.unity.com/threads/quickly-retrieving-the-immediate-children-of-a-gameobject.39451/#post-3554195
    /// </remarks>
    static public class ComponentExtensions
    {
        public static T GetComponentInDirectChildren<T>(this Component parent) where T : Component
        {
            return parent.GetComponentInDirectChildren<T>(false);
        }

        public static T GetComponentInDirectChildren<T>(this Component parent, bool includeInactive) where T : Component
        {
            foreach (Transform transform in parent.transform)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return null;
        }

        public static T[] GetComponentsInDirectChildren<T>(this Component parent) where T : Component
        {
            return parent.GetComponentsInDirectChildren<T>(false);
        }

        public static T[] GetComponentsInDirectChildren<T>(this Component parent, bool includeInactive) where T : Component
        {
            List<T> tmpList = new List<T>();
            foreach (Transform transform in parent.transform)
            {
                if (includeInactive || transform.gameObject.activeInHierarchy)
                {
                    tmpList.AddRange(transform.GetComponents<T>());
                }
            }
            return tmpList.ToArray();
        }
    }
}
