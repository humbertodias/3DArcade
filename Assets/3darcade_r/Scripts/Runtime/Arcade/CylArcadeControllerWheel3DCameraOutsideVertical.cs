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
    public sealed class CylArcadeControllerWheel3DCameraOutsideVertical : CylArcadeControllerWheel3DCameraOutside
    {
        protected override Vector3 TransformVector => Vector3.right;

        public CylArcadeControllerWheel3DCameraOutsideVertical(ArcadeHierarchy arcadeHierarchy,
                                                               PlayerFpsControls playerFpsControls,
                                                               PlayerCylControls playerCylControls,
                                                               Database<EmulatorConfiguration> emulatorDatabase,
                                                               AssetCache<GameObject> gameObjectCache,
                                                               AssetCache<Texture> textureCache,
                                                               AssetCache<string> videoCache)
        : base(arcadeHierarchy, playerFpsControls, playerCylControls, emulatorDatabase, gameObjectCache, textureCache, videoCache)
        {
        }

        protected override bool MoveForwardCondition(Transform targetSelection) => targetSelection.position.y < 0f || targetSelection.position.z > _pivotPoint.transform.position.z;

        protected override bool MoveBackwardCondition(Transform targetSelection) => targetSelection.position.y > 0f || targetSelection.position.z > _pivotPoint.transform.position.z;

        protected override float GetSpacing(Transform previousModel, Transform currentModel) => GetVerticalSpacing(previousModel, currentModel);
    }
}
