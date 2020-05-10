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
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Arcade_r
{
    public static class MoveCab
    {
        public interface IMovable
        {
        }

        public interface IGrabbable
        {
        }

        public sealed class Data
        {
            public ModelSetup ModelSetup;
            public Transform Transform;
            public Collider Collider;
            public Rigidbody Rigidbody;
            public Vector2 ScreenPoint;
        }

        public sealed class Input
        {
            public Vector2 AimPosition;
            public float AimRotation;
        }

        public sealed class SavedValues
        {
            public int Layer;
            public bool ColliderIsTrigger;
            public bool RigidbodyIsKinematic;
            public RigidbodyInterpolation RigidbodyInterpolation;
            public CollisionDetectionMode CollisionDetectionMode;
        }

        private static GameObject _loadedModel;

        public static void AddModelSetup(in Camera camera, in Vector3 position, in Vector3 forward, in float maxDistance, in LayerMask layerMask)
        {
            if (_loadedModel == null)
            {
                _loadedModel = Resources.Load<GameObject>("Games/starwarsc");
            }
            GameObject newModel = Object.Instantiate(_loadedModel, position + (forward * 2f), Quaternion.LookRotation(-forward));
            newModel.layer = LayerMask.NameToLayer("Arcade/GameModels");
            _ = newModel.AddComponent<GameModelSetup>();

            //Assert.IsNotNull(camera);
            //Vector2 raySource;
            //if (Mouse.current != null && Cursor.visible)
            //{
            //    raySource = Mouse.current.position.ReadValue();
            //}
            //else
            //{
            //    raySource = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            //}

            //Ray ray = camera.ScreenPointToRay(raySource);
            //if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
            //{
            //    if (_loadedModel == null)
            //    {
            //        _loadedModel = Resources.Load<GameObject>("Games/starwarsc");
            //    }
            //    GameObject newModel = Object.Instantiate(_loadedModel, hitInfo.point, Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * Quaternion.LookRotation(-forward));
            //    newModel.layer = LayerMask.NameToLayer("Arcade/GameModels");
            //    _ = newModel.AddComponent<GameModelSetup>();
            //}
        }

        public static void FindModelSetup(in Data data, in Camera camera, in float maxDistance, in LayerMask layerMask)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(camera);

            Vector2 raySource;
            if (Mouse.current != null && Cursor.visible)
            {
                raySource = Mouse.current.position.ReadValue();
            }
            else
            {
                raySource = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            Ray ray = camera.ScreenPointToRay(raySource);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
            {
                ModelSetup targetModel = hitInfo.transform.GetComponent<ModelSetup>();
                if (targetModel != data.ModelSetup)
                {
                    data.ModelSetup = targetModel;
                    data.Transform  = hitInfo.transform;
                    data.Collider   = hitInfo.collider;
                    if (data.Rigidbody != null)
                    {
                        data.Rigidbody.angularVelocity = Vector3.zero;
                    }
                    data.Rigidbody  = hitInfo.rigidbody;
                }
            }
            else
            {
                data.ModelSetup = null;
                data.Transform  = null;
                data.Collider   = null;
                if (data.Rigidbody != null)
                {
                    data.Rigidbody.angularVelocity = Vector3.zero;
                }
                data.Rigidbody  = null;
            }
        }

        public static void ManualMoveAndRotate(in Data data, in Input input)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Transform);
            Assert.IsNotNull(data.Rigidbody);
            Assert.IsNotNull(input);

            Transform transform = data.Transform;
            Rigidbody rigidbody = data.Rigidbody;

            rigidbody.constraints = RigidbodyConstraints.None;

            // Position
            Vector2 positionInput = input.AimPosition;
            if (positionInput.sqrMagnitude > 0.001f)
            {
                rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

                rigidbody.AddForce(-transform.forward * positionInput.y, ForceMode.VelocityChange);
                rigidbody.AddForce(-transform.right * positionInput.x, ForceMode.VelocityChange);
            }
            rigidbody.AddForce(Vector3.right * -rigidbody.velocity.x, ForceMode.VelocityChange);
            rigidbody.AddForce(Vector3.forward * -rigidbody.velocity.z, ForceMode.VelocityChange);

            // Rotation
            float rotationInput = input.AimRotation;
            if (rotationInput < -0.5f || rotationInput > 0.5f)
            {
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;

                float angle           = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
                float targetAngle     = angle + rotationInput;
                float angleDifference = (targetAngle - angle);

                if (Mathf.Abs(angleDifference) > 180f)
                {
                    if (angleDifference < 0f)
                    {
                        angleDifference = (360f + angleDifference);
                    }
                    else if (angleDifference > 0f)
                    {
                        angleDifference = (360f - angleDifference) * -1f;
                    }
                }

                rigidbody.AddTorque(Vector3.up * angleDifference, ForceMode.VelocityChange);
                rigidbody.AddTorque(Vector3.up * -rigidbody.angularVelocity.y, ForceMode.VelocityChange);
            }
        }

        public static void AutoMoveAndRotate(in Data data, in Camera camera, in Vector3 forward, in float maxDistance, in LayerMask layerMask)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Transform);
            Assert.IsNotNull(data.Collider);
            Assert.IsNotNull(camera);

            bool useMousePosition = Mouse.current != null && Cursor.visible;
            Vector2 raySource = useMousePosition ? Mouse.current.position.ReadValue() : data.ScreenPoint;

            Ray ray = camera.ScreenPointToRay(raySource);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, layerMask))
            {
                Transform transform = data.Transform;
                Collider collider   = data.Collider;
                Vector3 position    = hitInfo.point;
                Vector3 normal      = hitInfo.normal;
                float dot           = Vector3.Dot(Vector3.up, normal);
                if (dot > 0.05f)
                {
                    transform.position      = Vector3.Lerp(transform.position, position, Time.deltaTime * 12f);
                    transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.LookRotation(-forward);
                }
                else if (dot < -0.05f)
                {
                    Vector3 newPosition     = new Vector3(position.x, transform.position.y, position.z);
                    transform.position      = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 12f);
                    transform.localRotation = Quaternion.FromToRotation(Vector3.up, -normal) * Quaternion.LookRotation(-forward);
                }
                else
                {
                    Vector3 positionOffset  = normal * Mathf.Max(collider.bounds.extents.x + 0.1f, collider.bounds.extents.z + 0.1f);
                    Vector3 newPosition     = new Vector3(position.x, transform.position.y, position.z) + positionOffset;
                    transform.position      = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 12f);
                    transform.localRotation = Quaternion.LookRotation(normal);
                }
            }
        }

        public static SavedValues InitGrabMode(in Data data, in Camera camera)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Transform);
            Assert.IsNotNull(data.Collider);
            Assert.IsNotNull(data.Rigidbody);
            Assert.IsNotNull(camera);

            data.ScreenPoint = camera.WorldToScreenPoint(data.Transform.position);

            SavedValues result = new SavedValues
            {
                Layer                  = data.ModelSetup.gameObject.layer,
                ColliderIsTrigger      = data.Collider.isTrigger,
                RigidbodyIsKinematic   = data.Rigidbody.isKinematic,
                RigidbodyInterpolation = data.Rigidbody.interpolation,
                CollisionDetectionMode = data.Rigidbody.collisionDetectionMode
            };

            data.ModelSetup.gameObject.layer      = 0;
            data.Collider.isTrigger               = true;
            data.Rigidbody.isKinematic            = true;
            data.Rigidbody.interpolation          = RigidbodyInterpolation.Interpolate;
            data.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            return result;
        }

        public static void RestoreSavedValues(in Data data, SavedValues savedValues)
        {
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.Collider);
            Assert.IsNotNull(data.Rigidbody);
            Assert.IsNotNull(savedValues);

            data.ModelSetup.gameObject.layer      = savedValues.Layer;
            data.Collider.isTrigger               = savedValues.ColliderIsTrigger;
            data.Rigidbody.isKinematic            = savedValues.RigidbodyIsKinematic;
            data.Rigidbody.interpolation          = savedValues.RigidbodyInterpolation;
            data.Rigidbody.collisionDetectionMode = savedValues.CollisionDetectionMode;
        }
    }
}