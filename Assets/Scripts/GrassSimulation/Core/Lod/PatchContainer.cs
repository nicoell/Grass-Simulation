using UnityEngine;

namespace GrassSimulation.Core.Lod
{
	public abstract class PatchContainer : ScriptableObject, IInitializableWithCtx
	{
		protected SimulationContext Ctx;

		public void Init(SimulationContext context)
		{
			Ctx = context;
		}

		public abstract void Destroy();

		public abstract Bounds GetBounds();

		public void Draw()
		{
			UpdatePerFrameData();
			DrawImpl();
		}

		protected abstract void DrawImpl();

		public abstract void SetupContainer();

		public void DrawGizmo()
		{
			if (Ctx.EditorSettings.EnableLodDistanceGizmo)
			{
				Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceGeometryStart);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceGeometryEnd);
				Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceBillboardCrossedStart);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceBillboardCrossedEnd);
				Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceBillboardScreenStart);
				Gizmos.DrawWireSphere(Ctx.Camera.transform.position, Ctx.Settings.LodDistanceBillboardScreenEnd);
			}
			DrawGizmoImpl();
		}

		protected abstract void DrawGizmoImpl();

		public abstract void OnGUI();

		protected virtual void UpdatePerFrameData()
		{
			//TODO: Maybe outsource all the computeshader data settings to its own class
			Ctx.GrassSimulationComputeShader.SetBool("ApplyTransition", Ctx.Settings.EnableHeightTransition);
			Ctx.GrassGeometry.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassGeometry.SetFloat("specular", Ctx.Settings.Specular);
			Ctx.GrassGeometry.SetFloat("gloss", Ctx.Settings.Gloss);
			Ctx.GrassGeometry.SetVector("viewDir", Ctx.Camera.transform.forward);
			Ctx.GrassGeometry.SetVector("lightDir", Ctx.Light.transform.forward);
			Ctx.GrassGeometry.SetVector("lightColor", Ctx.Light.color);
			Ctx.GrassGeometry.SetMatrix("ViewProjMatrix",
				Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			

			Ctx.GrassBillboardCrossed.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassBillboardCrossed.SetFloat("specular", Ctx.Settings.Specular);
			Ctx.GrassBillboardCrossed.SetFloat("gloss", Ctx.Settings.Gloss);
			Ctx.GrassBillboardCrossed.SetVector("viewDir", Ctx.Camera.transform.forward);
			Ctx.GrassBillboardCrossed.SetVector("lightDir", Ctx.Light.transform.forward);
			Ctx.GrassBillboardCrossed.SetVector("lightColor", Ctx.Light.color);
			
			Ctx.GrassBillboardScreen.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassBillboardScreen.SetVector("CamUp", Ctx.Camera.transform.up);
			Ctx.GrassBillboardScreen.SetFloat("specular", Ctx.Settings.Specular);
			Ctx.GrassBillboardScreen.SetFloat("gloss", Ctx.Settings.Gloss);
			Ctx.GrassBillboardScreen.SetVector("viewDir", Ctx.Camera.transform.forward);
			Ctx.GrassBillboardScreen.SetVector("lightDir", Ctx.Light.transform.forward);
			Ctx.GrassBillboardScreen.SetVector("lightColor", Ctx.Light.color);
			
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", Time.deltaTime);
			Ctx.GrassSimulationComputeShader.SetFloat("Time", Time.time);
			Ctx.GrassSimulationComputeShader.SetMatrix("ViewProjMatrix",
				Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("CamPos", Ctx.Camera.transform.position.x,
				Ctx.Camera.transform.position.y, Ctx.Camera.transform.position.z);
			
			Ctx.GrassSimulationComputeShader.SetFloat("WindAmplitude", Ctx.Settings.WindAmplitude);
			Ctx.GrassSimulationComputeShader.SetVector("GravityVec", Ctx.Settings.Gravity);
		}
	}
}