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
			Ctx.GrassGeometry.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassGeometry.SetVector("viewDir", Ctx.Camera.transform.forward);
			Ctx.GrassGeometry.SetMatrix("ViewProjMatrix", Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassGeometry.SetVector("LightDirection", Ctx.SunLight.transform.forward);
			Ctx.GrassGeometry.SetFloat("LightIntensity", Ctx.SunLight.intensity);
			Ctx.GrassGeometry.SetFloat("AmbientLightFactor", Ctx.Settings.AmbientLightFactor);

			if (Ctx.GrassBlossom)
			{
				Ctx.GrassBlossom.SetVector("CamPos", Ctx.Camera.transform.position);
				Ctx.GrassBlossom.SetVector("viewDir", Ctx.Camera.transform.forward);
				Ctx.GrassBlossom.SetMatrix("ViewProjMatrix", Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
				Ctx.GrassBlossom.SetVector("LightDirection", Ctx.SunLight.transform.forward);
				Ctx.GrassBlossom.SetFloat("LightIntensity", Ctx.SunLight.intensity);
				Ctx.GrassBlossom.SetFloat("AmbientLightFactor", Ctx.Settings.AmbientLightFactor);
			}
			Ctx.GrassBillboardCrossed.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassBillboardCrossed.SetVector("LightDirection", Ctx.SunLight.transform.forward);
			Ctx.GrassBillboardCrossed.SetFloat("LightIntensity", Ctx.SunLight.intensity);
			Ctx.GrassBillboardCrossed.SetFloat("AmbientLightFactor", Ctx.Settings.AmbientLightFactor);
			
			Ctx.GrassBillboardScreen.SetVector("CamPos", Ctx.Camera.transform.position);
			Ctx.GrassBillboardScreen.SetVector("CamUp", Ctx.Camera.transform.up);
			Ctx.GrassBillboardScreen.SetVector("LightDirection", Ctx.SunLight.transform.forward);
			Ctx.GrassBillboardScreen.SetFloat("LightIntensity", Ctx.SunLight.intensity);
			Ctx.GrassBillboardScreen.SetFloat("AmbientLightFactor", Ctx.Settings.AmbientLightFactor);
			
			Ctx.GrassSimulationComputeShader.SetBool("BillboardGeneration", false);
			Ctx.GrassSimulationComputeShader.SetFloat("DeltaTime", Time.deltaTime);
			Ctx.GrassSimulationComputeShader.SetFloat("Time", Time.time);
			Ctx.GrassSimulationComputeShader.SetMatrix("ViewProjMatrix",
				Ctx.Camera.projectionMatrix * Ctx.Camera.worldToCameraMatrix);
			Ctx.GrassSimulationComputeShader.SetFloats("CamPos", Ctx.Camera.transform.position.x,
				Ctx.Camera.transform.position.y, Ctx.Camera.transform.position.z);
			Ctx.GrassSimulationComputeShader.SetVector("SunLight", new Vector4(-Ctx.SunLight.transform.forward.x, -Ctx.SunLight.transform.forward.y, -Ctx.SunLight.transform.forward.z, Ctx.SunLight.intensity));
			Ctx.GrassSimulationComputeShader.SetVector("GravityVec", Ctx.Settings.Gravity);
		}
	}
}