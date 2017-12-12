using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Core
{
	[Flags]
	public enum InputTarget
	{
		Geometry = 1,
		BillboardCrossed = 2,
		BillboardScreen = 4,
		Simulation = 8
	}
	
	public class ShaderInputManager : ContextRequirement
	{

		private readonly Dictionary<string, IKernelConnector> _kernelOneTime;
		private readonly Dictionary<string, IKernelConnector> _kernelPerFrame;
		private readonly Dictionary<string, IKernelConnector> _kernelPerPatch;

		private readonly Dictionary<string, IShaderConnector> _shaderOneTime;
		private readonly Dictionary<string, IShaderConnector> _shaderPerFrame;
		private readonly Dictionary<string, IShaderConnector> _shaderPerPatch;

		public ShaderInputManager(SimulationContext ctx) : base(ctx)
		{
			_shaderOneTime = new Dictionary<string, IShaderConnector>();
			_kernelOneTime = new Dictionary<string, IKernelConnector>();
			_shaderPerFrame = new Dictionary<string, IShaderConnector>();
			_kernelPerFrame = new Dictionary<string, IKernelConnector>();
			_shaderPerPatch = new Dictionary<string, IShaderConnector>();
			_kernelPerPatch = new Dictionary<string, IKernelConnector>();
		}

		public void RegisterOneTimeInput<T>(InputTarget target, string name, T value, string dictSuffix = "onetime")
		{
			RegisterInput(target, dictSuffix, name, value, _shaderOneTime, _kernelOneTime);
		}

		public void RegisterPerFrameInput<T>(InputTarget target, string name, T value, string dictSuffix = "perframe")
		{
			RegisterInput(target, dictSuffix, name, value, _shaderPerFrame, _kernelPerFrame);
		}

		public void RegisterPerPatchInput<T>(InputTarget target, string name, T value, string dictSuffix)
		{
			RegisterInput(target, dictSuffix, name, value, _shaderPerPatch, _kernelPerPatch);
		}

		public void UpdateOneTimeInputs(string dictSuffix = "onetime", int kernelId = -1)
		{
			foreach (var connector in _shaderOneTime)
				if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update();
			if (kernelId != -1)
				foreach (var connector in _kernelOneTime)
					if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update(kernelId);
		}
		
		public void UpdatePerFrameInputs(string dictSuffix = "perframe", int kernelId = -1)
		{
			foreach (var connector in _shaderPerFrame)
				if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update();
			if (kernelId != -1)
				foreach (var connector in _kernelPerFrame)
					if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update(kernelId);
		}
		
		public void UpdatePerPatchInputs(string dictSuffix, int kernelId = -1)
		{
			foreach (var connector in _shaderPerPatch)
				if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update();
			if (kernelId != -1)
				foreach (var connector in _kernelPerPatch)
					if (connector.Key.StartsWith(dictSuffix)) connector.Value.Update(kernelId);
		}

		public void UpdateOneTimeInput(string name, string dictSuffix = "onetime", int kernelId = -1)
		{
			_shaderOneTime[dictSuffix + name].Update();
			if (kernelId != -1) _kernelOneTime[dictSuffix + name].Update(kernelId);
		}

		public void UpdatePerFrameInput(string name, string dictSuffix = "perframe", int kernelId = -1)
		{
			_shaderPerFrame[dictSuffix + name].Update();
			if (kernelId != -1) _kernelPerFrame[dictSuffix + name].Update(kernelId);
		}

		public void UpdatePerPatchInput(string name, string dictSuffix, int kernelId = -1)
		{
			_shaderPerPatch[dictSuffix + name].Update();
			if (kernelId != -1) _kernelPerPatch[dictSuffix + name].Update(kernelId);
		}

		private void RegisterInput<T>(InputTarget target, string dictSuffix, string name, T value,
			IDictionary<string, IShaderConnector> shaderDict, IDictionary<string, IKernelConnector> kernelDict)
		{
			if ((target & InputTarget.Geometry) == InputTarget.Geometry)
				shaderDict.Add(dictSuffix + name,
					new ShaderConnector<T>(name, () => value, GetAction<T, Material>(Ctx.GrassGeometry)));
			if ((target & InputTarget.BillboardCrossed) == InputTarget.BillboardCrossed)
				shaderDict.Add(dictSuffix + name,
					new ShaderConnector<T>(name, () => value, GetAction<T, Material>(Ctx.GrassBillboardCrossed)));
			if ((target & InputTarget.BillboardScreen) == InputTarget.BillboardScreen)
				shaderDict.Add(dictSuffix + name,
					new ShaderConnector<T>(name, () => value, GetAction<T, Material>(Ctx.GrassBillboardScreen)));
			if ((target & InputTarget.Simulation) == InputTarget.Simulation)
			{
				var action = GetAction<T, ComputeShader>(Ctx.GrassSimulationComputeShader);
				if (action != null)
					shaderDict.Add(dictSuffix + name,
						new ShaderConnector<T>(name, () => value, action));
				else
					kernelDict.Add(dictSuffix + name,
						new KernelConnector<T>(name, () => value, GetKernelAction<T>(Ctx.GrassSimulationComputeShader)));
			}
		}

		private static Action<int, string, T> GetKernelAction<T>(ComputeShader target)
		{
			if (typeof(T) == typeof(ComputeBuffer))
				return (Action<int, string, T>) Delegate.CreateDelegate(typeof(Action<int, string, ComputeBuffer>), target,
					typeof(ComputeShader).GetMethod("SetBuffer"));
			if (typeof(T) == typeof(Texture))
				return (Action<int, string, T>) Delegate.CreateDelegate(typeof(Action<int, string, Texture>), target,
					typeof(ComputeShader).GetMethod("SetTexture"));

			return null;
		}

		private static Action<string, T> GetAction<T, TT>(TT target)
		{
			if (typeof(T) == typeof(bool))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, bool>), target,
					typeof(TT).GetMethod("SetBool"));
			if (typeof(T) == typeof(ComputeBuffer))
				return typeof(TT) == typeof(ComputeShader)
					? null
					: (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, ComputeBuffer>), target,
						typeof(TT).GetMethod("SetBuffer"));
			if (typeof(T) == typeof(Color))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Color>), target,
					typeof(TT).GetMethod("SetColor"));
			if (typeof(T) == typeof(Color[]))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Color[]>), target,
					typeof(TT).GetMethod("SetColorArray"));
			if (typeof(T) == typeof(float))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, float>), target,
					typeof(TT).GetMethod("SetFloat"));
			if (typeof(T) == typeof(float[]))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, float[]>), target,
					typeof(TT) == typeof(ComputeShader) ? typeof(TT).GetMethod("SetFloats") : typeof(TT).GetMethod("SetFloatArray"));
			if (typeof(T) == typeof(int))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, int>), target,
					typeof(TT).GetMethod("SetInt"));
			if (typeof(T) == typeof(int[]))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, int[]>), target,
					typeof(TT).GetMethod("SetInts"));
			if (typeof(T) == typeof(Matrix4x4))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Matrix4x4>), target,
					typeof(TT).GetMethod("SetMatrix"));
			if (typeof(T) == typeof(Matrix4x4[]))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Matrix4x4[]>), target,
					typeof(TT).GetMethod("SetMatrixArray"));
			if (typeof(T) == typeof(Texture))
				return typeof(TT) == typeof(ComputeShader)
					? null
					: (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Texture>), target,
						typeof(TT).GetMethod("SetTexture"));
			if (typeof(T) == typeof(Vector4))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Vector4>), target,
					typeof(TT).GetMethod("SetVector"));
			if (typeof(T) == typeof(Vector4[]))
				return (Action<string, T>) Delegate.CreateDelegate(typeof(Action<string, Vector4[]>), target,
					typeof(TT).GetMethod("SetVectorArray"));

			return null;
		}

		private interface IShaderConnector
		{
			void Update();
		}

		private interface IKernelConnector
		{
			void Update(int kernel);
		}

		private class ShaderConnector<T> : IShaderConnector
		{
			public ShaderConnector(string name, Func<T> getter, Action<string, T> action = null)
			{
				Name = name;
				GetValue = getter;
				Connect = action;
			}

			protected string Name { get; private set; }
			protected Func<T> GetValue { get; private set; }
			private Action<string, T> Connect { get; set; }

			public void Update()
			{
				Connect(Name, GetValue());
			}
		}

		private class KernelConnector<T> : ShaderConnector<T>, IKernelConnector
		{
			public KernelConnector(string name, Func<T> getter, Action<int, string, T> action) : base(name, getter)
			{
				Connect = action;
			}

			private Action<int, string, T> Connect { get; set; }

			public void Update(int kernel)
			{
				if (kernel >= 0) Connect(kernel, Name, GetValue());
			}
		}
	}
}