/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction;
using UnityEngine;

public class UnkinematicOnUnselect : MonoBehaviour
{
    private Grabbable _grabbable;
    private Rigidbody _rigidBody;

    private void Awake()
    {
        _grabbable = GetComponentInChildren<Grabbable>();
        _rigidBody = GetComponent<Rigidbody>();
        _grabbable.WhenPointerEventRaised += OnPointerEventRaised;
    }

    public void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
        }
    }

    private void OnPointerEventRaised(PointerEvent pointerEvent)
    {
        if (_grabbable == null || pointerEvent.Type != PointerEventType.Unselect)
        {
            return;
        }

        _rigidBody.isKinematic = false;
    }
}
