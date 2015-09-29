// Amplify Color - Advanced Color Grading for Unity Pro
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4  || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#define UNITY_4
#endif
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4  || UNITY_5_5 || UNITY_5_6 || UNITY_5_7 || UNITY_5_8 || UNITY_5_9
#define UNITY_5
#endif
#if !UNITY_4 && !UNITY_5
#define UNITY_3
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using System.Reflection;
using AmplifyColor;

[CustomEditor( typeof( AmplifyColorEffect ) )]
class AmplifyColorEffectEditor : Editor
{
	void OnEnable()
	{
		if ( !Application.isPlaying )
		{
			AmplifyColorBase effect = target as AmplifyColorBase;

			bool needsNewID = string.IsNullOrEmpty( effect.SharedInstanceID );
			if ( !needsNewID )
				needsNewID = FindClone( effect );

			if ( needsNewID )
			{
				effect.NewSharedInstanceID();
				EditorUtility.SetDirty( target );
				//Debug.Log( "Assigned new instance ID to " + effect.name + " => " + effect.SharedInstanceID );
			}
		}
	}

	bool FindClone( AmplifyColorBase effect )
	{
		GameObject effectPrefab = PrefabUtility.GetPrefabParent( effect.gameObject ) as GameObject;
		PrefabType effectPrefabType = PrefabUtility.GetPrefabType( effect.gameObject );
		bool effectIsPrefab = ( effectPrefabType != PrefabType.None && effectPrefabType != PrefabType.PrefabInstance );
		bool effectHasPrefab = ( effectPrefab != null );

		AmplifyColorBase[] all = Resources.FindObjectsOfTypeAll( typeof( AmplifyColorBase ) ) as AmplifyColorBase[];
		bool foundClone = false;

		foreach ( AmplifyColorBase other in all )
		{
			if ( other == effect || other.SharedInstanceID != effect.SharedInstanceID )
			{
				// skip: same effect or already have different ids
				continue;
			}

			GameObject otherPrefab = PrefabUtility.GetPrefabParent( other.gameObject ) as GameObject;
			PrefabType otherPrefabType = PrefabUtility.GetPrefabType( other.gameObject );
			bool otherIsPrefab = ( otherPrefabType != PrefabType.None && otherPrefabType != PrefabType.PrefabInstance );
			bool otherHasPrefab = ( otherPrefab != null );

			if ( otherIsPrefab && effectHasPrefab && effectPrefab == other.gameObject )
			{
				// skip: other is a prefab and effect's prefab is other
				continue;
			}

			if ( effectIsPrefab && otherHasPrefab && otherPrefab == effect.gameObject )
			{
				// skip: effect is a prefab and other's prefab is effect
				continue;
			}

			if ( !effectIsPrefab && !otherIsPrefab && effectHasPrefab && otherHasPrefab && effectPrefab == otherPrefab )
			{
				// skip: both aren't prefabs and both have the same parent prefab
				continue;
			}

			foundClone = true;
		}

		return foundClone;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		//AmplifyColorBase effect = target as AmplifyColorBase;
		//GUILayout.Label( effect.SharedInstanceID.ToString() );
		//
		//if ( GUILayout.Button( "NEW ID" ) )
		//{
		//	effect.NewSharedInstanceID();
		//	EditorUtility.SetDirty( target );
		//}
	}
}
