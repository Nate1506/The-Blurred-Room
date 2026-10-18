using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlurredRoom.Tests
{
    /// <summary>
    /// Base for all tests: tracks created objects and destroys them in TearDown, resets statics,
    /// and backs up / restores the BR_* PlayerPrefs.
    /// </summary>
    public abstract class NateTestBase
    {
        readonly List<Object> tracked = new List<Object>();
        PlayerPrefsBackup prefs;

        [SetUp]
        public void NateBaseSetUp()
        {
            prefs = PlayerPrefsBackup.Capture();
            TestStatics.ResetAll();
        }

        [TearDown]
        public void NateBaseTearDown()
        {
            for (int i = tracked.Count - 1; i >= 0; i--)
                if (tracked[i]) Object.DestroyImmediate(tracked[i]);
            tracked.Clear();
            TestStatics.ResetAll();
            prefs.Restore();
        }

        protected T Track<T>(T obj) where T : Object
        {
            tracked.Add(obj);
            return obj;
        }

        protected GameObject NewGO(string name, bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            return Track(go);
        }

        protected GameObject NewGO(string name, Vector3 position, bool active = true)
        {
            GameObject go = NewGO(name, active);
            go.transform.position = position;
            return go;
        }

        /// <summary>Cube with a BoxCollider.</summary>
        protected GameObject NewBox(string name, Vector3 position, Vector3 size, bool active = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            go.SetActive(active);
            Physics.SyncTransforms();
            return Track(go);
        }

        /// <summary>New GameObject with T added while inactive, so <paramref name="configure"/> runs before Awake.</summary>
        protected T NewComponent<T>(string name, Action<T> configure = null) where T : Component
            => NewComponent(name, Vector3.zero, configure);

        protected T NewComponent<T>(string name, Vector3 position, Action<T> configure = null) where T : Component
        {
            GameObject go = NewGO(name, position, false);
            T c = go.AddComponent<T>();
            configure?.Invoke(c);
            go.SetActive(true);
            return c;
        }

        protected TestPlayer NewPlayer(Vector3 position, bool enableMovement = false, Action<TestPlayer> configure = null)
            => NewPlayer(position, Quaternion.identity, enableMovement, configure);

        protected TestPlayer NewPlayer(Vector3 position, Quaternion rotation, bool enableMovement = false,
                                       Action<TestPlayer> configure = null)
        {
            TestPlayer p = TestPlayer.Build(position, rotation, enableMovement, configure);
            Track(p.Root);
            return p;
        }

        protected ClueData NewClue(string id) => Track(TestAssets.Clue(id));

        protected static void MoveTo(GameObject go, Vector3 position)
        {
            go.transform.position = position;
            Physics.SyncTransforms();
        }

        protected static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        /// <summary>Yields frames until the condition is true or the timeout (seconds) passes.</summary>
        protected static IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeout)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
    }
}
