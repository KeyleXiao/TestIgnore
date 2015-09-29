using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Lightning : MonoBehaviour 
{
	public Light TargetLight = null;
	
	public bool Enabled = false;
	
	public float Intensity = 3.0f;
//	public Color TargetColor; //Need this eventually
	
	public float MinWaitTime = 0.0f;
	public float MaxWaitTime = 0.0f;
	
	public AudioSource Sound = null;
	
	private float m_Intensity = 0.0f;
	
	public void Awake()
	{
		if( MinWaitTime == 0.0f || MaxWaitTime == 0.0f )
		{
			return;
		}
		if( TargetLight == null )
		{
			return;
		}

		m_Intensity = TargetLight.intensity;
		
		if( Enabled )
		{
			StartCoroutine( PlayLightning() );
		}
	}
	
	private IEnumerator PlayLightning()
	{
		while( Enabled )
		{
			yield return new WaitForSeconds( Random.Range( MinWaitTime,MaxWaitTime ) );
			
			TargetLight.intensity = m_Intensity;
			
			if( Sound != null )
			{
				Sound.Play ();
			}
			
			while( TargetLight.intensity <= Intensity-0.1f )
			{
				TargetLight.intensity = Mathf.Lerp( TargetLight.intensity, Intensity, Time.deltaTime*30.0f );
				yield return new WaitForSeconds( 0.0f );
			}
			while( TargetLight.intensity > m_Intensity )
			{
				TargetLight.intensity = Mathf.Lerp( TargetLight.intensity, m_Intensity-0.01f, 0.25f );
				yield return new WaitForSeconds( 0.0f ); 
			}
			
			TargetLight.intensity = m_Intensity;
		}
	}
}
