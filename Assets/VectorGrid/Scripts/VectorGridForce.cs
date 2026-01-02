using UnityEngine;
using System.Collections;

public class VectorGridForce : MonoBehaviour 
{
	public VectorGrid m_VectorGrid;
	public float m_ForceScale = 10f;
	public bool m_Directional;
	public Vector3 m_ForceDirection;
	public float m_Radius = 3f;
	public Color m_Color = Color.white;
	public bool m_HasColor;

	void Awake()
	{
		if (m_VectorGrid == null)
		{
			m_VectorGrid = FindObjectOfType<VectorGrid>();
		}
	}

	// Update is called once per frame
	void Update () 
	{
		if(m_VectorGrid)
		{
			// Fix: Project our position onto the Grid's plane so Z-depth doesn't prevent interaction
			Vector3 localPos = m_VectorGrid.transform.InverseTransformPoint(this.transform.position);
			localPos.z = 0f;
			Vector3 projectedWorldPos = m_VectorGrid.transform.TransformPoint(localPos);

			if(m_Directional)
			{
				m_VectorGrid.AddGridForce(projectedWorldPos, m_ForceDirection * m_ForceScale, m_Radius, m_Color, m_HasColor);
			}
			else
			{
				m_VectorGrid.AddGridForce(projectedWorldPos, m_ForceScale, m_Radius, m_Color, m_HasColor);
			}
		}
	}
}
