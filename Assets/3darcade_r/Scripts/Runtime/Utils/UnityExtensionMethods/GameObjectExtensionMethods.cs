﻿/* MIT License

 * Copyright (c) 2020 Skurdt
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. */

using UnityEngine;

namespace Arcade_r
{
    public static class GameObjectExtensionMethods
    {
        public static T AddComponentIfNotFound<T>(this GameObject gameObject)
            where T : Component
        {
            if (gameObject == null)
            {
                return null;
            }

            if (!gameObject.TryGetComponent(out T component))
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        public static void StripCloneFromName(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.name = gameObject.name.Substring(0, gameObject.name.Length - 7);
        }

        public static float GetWidth(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.size.x : 0f;
        }

        public static float GetHeight(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.size.y : 0f;
        }

        public static float GetDepth(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.size.z : 0f;
        }

        public static float GetHalfWidth(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.extents.x : 0f;
        }

        public static float GetHalfHeight(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.extents.y : 0f;
        }

        public static float GetHalfDepth(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return 0f;
            }

            return gameObject.TryGetComponent(out Collider collider) ? collider.bounds.extents.z : 0f;
        }
    }
}
