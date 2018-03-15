using System;
using UnityEngine;

// ReSharper disable AssignNullToNotNullAttribute

namespace InfiniteMeadow.Utils
{
	public class ComputeShaderLinker : ShaderLinker, ShaderLinker.IComputeShader
	{
		protected readonly ComputeShader ComputeShader;
		protected readonly Action<int, bool> SetCompBool;
		protected readonly Action<int, int, ComputeBuffer> SetCompBuffer;
		protected readonly Action<int, float> SetCompFloat;
		protected readonly Action<int, float[]> SetCompFloatArray;
		protected readonly Action<int, int> SetCompInt;
		protected readonly Action<int, int[]> SetCompIntArray;
		protected readonly Action<int, Matrix4x4> SetCompMatrix;
		protected readonly Action<int, Matrix4x4[]> SetCompMatrixArray;
		protected readonly Action<int, int, Texture> SetCompTexture;
		protected readonly Action<int, int, int> SetCompTextureFromGlobalInt;
		protected readonly Action<int, int, string> SetCompTextureFromGlobalString;
		protected readonly Action<int, Vector4> SetCompVector;
		protected readonly Action<int, Vector4[]> SetCompVectorArray;

		public ComputeShaderLinker(ComputeShader computeShader)
		{
			ComputeShader = computeShader;
			SetCompBool = (Action<int, bool>) Delegate.CreateDelegate(typeof(Action<int, bool>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetBool", new[] {typeof(bool), typeof(float)}));
			SetCompBuffer = (Action<int, int, ComputeBuffer>) Delegate.CreateDelegate(typeof(Action<int, int, ComputeBuffer>),
				ComputeShader,
				typeof(ComputeShader).GetMethod("SetBuffer", new[] {typeof(int), typeof(int), typeof(ComputeBuffer)}));
			SetCompFloat = (Action<int, float>) Delegate.CreateDelegate(typeof(Action<int, float>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetFloat", new[] {typeof(int), typeof(float)}));
			SetCompFloatArray = (Action<int, float[]>) Delegate.CreateDelegate(typeof(Action<int, float[]>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetFloats", new[] {typeof(int), typeof(float[])}));
			SetCompInt = (Action<int, int>) Delegate.CreateDelegate(typeof(Action<int, int>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetInt", new[] {typeof(int), typeof(int)}));
			SetCompIntArray = (Action<int, int[]>) Delegate.CreateDelegate(typeof(Action<int, int[]>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetInts", new[] {typeof(int[]), typeof(int)}));
			SetCompMatrix = (Action<int, Matrix4x4>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetMatrix", new[] {typeof(int), typeof(Matrix4x4)}));
			SetCompMatrixArray = (Action<int, Matrix4x4[]>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4[]>),
				ComputeShader,
				typeof(ComputeShader).GetMethod("SetMatrixArray", new[] {typeof(int), typeof(Matrix4x4[])}));
			SetCompTexture = (Action<int, int, Texture>) Delegate.CreateDelegate(typeof(Action<int, int, Texture>),
				ComputeShader,
				typeof(ComputeShader).GetMethod("SetTexture", new[] {typeof(int), typeof(int), typeof(Texture)}));
			SetCompTextureFromGlobalInt = (Action<int, int, int>) Delegate.CreateDelegate(typeof(Action<int, int, int>),
				ComputeShader,
				typeof(ComputeShader).GetMethod("SetTextureFromGlobal", new[] {typeof(int), typeof(int), typeof(int)}));
			SetCompTextureFromGlobalString = (Action<int, int, string>) Delegate.CreateDelegate(typeof(Action<int, int, string>),
				ComputeShader,
				typeof(ComputeShader).GetMethod("SetTextureFromGlobal", new[] {typeof(int), typeof(int), typeof(string)}));
			SetCompVector = (Action<int, Vector4>) Delegate.CreateDelegate(typeof(Action<int, Vector4>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetVector", new[] {typeof(int), typeof(Vector4)}));
			SetCompVectorArray = (Action<int, Vector4[]>) Delegate.CreateDelegate(typeof(Action<int, Vector4[]>), ComputeShader,
				typeof(ComputeShader).GetMethod("SetVectorArray", new[] {typeof(int), typeof(Vector4[])}));
		}

		public void LinkBool(UpdateRate updateRate, string shaderName, Func<bool> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<bool>(shaderName, getter, SetCompBool));
		}

		public void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float>(shaderName, getter, SetCompFloat));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float[]>(shaderName, getter, SetCompFloatArray));
		}

		public void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<int>(shaderName, getter, SetCompInt));
		}

		public void LinkIntArray(UpdateRate updateRate, string shaderName, Func<int[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<int[]>(shaderName, getter, SetCompIntArray));
		}

		public void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4>(shaderName, getter, SetCompMatrix));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4[]>(shaderName, getter, SetCompMatrixArray));
		}

		public void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4>(shaderName, getter, SetCompVector));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4[]>(shaderName, getter, SetCompVectorArray));
		}

		public void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter, int kernel)
		{
			LinkedDictionary[updateRate].Add(new KernelLink<ComputeBuffer>(shaderName, getter, SetCompBuffer, kernel));
		}

		public void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter, int[] kernels)
		{
			foreach (var kernel in kernels) LinkBuffer(updateRate, shaderName, getter, kernel);
		}

		public void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter, int kernel)
		{
			LinkedDictionary[updateRate].Add(new KernelLink<Texture>(shaderName, getter, SetCompTexture, kernel));
		}

		public void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter, int[] kernels)
		{
			foreach (var kernel in kernels) LinkTexture(updateRate, shaderName, getter, kernel);
		}

		public void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<string> getter, int kernel)
		{
			LinkedDictionary[updateRate].Add(new KernelLink<string>(shaderName, getter, SetCompTextureFromGlobalString, kernel));
		}

		public void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<string> getter, int[] kernels)
		{
			foreach (var kernel in kernels) LinkTextureFromGlobal(updateRate, shaderName, getter, kernel);
		}

		public void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<int> getter, int kernel)
		{
			LinkedDictionary[updateRate].Add(new KernelLink<int>(shaderName, getter, SetCompTextureFromGlobalInt, kernel));
		}

		public void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<int> getter, int[] kernels)
		{
			foreach (var kernel in kernels) LinkTextureFromGlobal(updateRate, shaderName, getter, kernel);
		}
	}
}