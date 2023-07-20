using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
	public event System.Action onValuesUpdated;

	#if UNITY_EDITOR
	protected virtual void OnValidate() {
		UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
	}

	protected virtual void NotifyOfUpdatedValues() {
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		onValuesUpdated?.Invoke();
	}
	#endif
}