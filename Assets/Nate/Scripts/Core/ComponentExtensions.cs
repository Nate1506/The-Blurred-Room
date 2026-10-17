using UnityEngine;

namespace BlurredRoom
{
    public static class ComponentExtensions
    {
        /// <summary>Finds a component anywhere in the same root hierarchy (e.g. any player part from any other part).</summary>
        public static T FindInRoot<T>(this GameObject go) where T : Component
        {
            return go ? go.transform.root.GetComponentInChildren<T>(true) : null;
        }

        public static T FindInRoot<T>(this Component c) where T : Component
        {
            return c ? c.transform.root.GetComponentInChildren<T>(true) : null;
        }
    }
}
