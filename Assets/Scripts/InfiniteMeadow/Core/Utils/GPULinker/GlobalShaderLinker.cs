using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable AssignNullToNotNullAttribute

namespace InfiniteMeadow.Core.Utils.GPULinker
{
	public class GlobalShaderLinker : GPULinker, GPULinker.IShader
	{
		protected static readonly Action<int, ComputeBuffer> SetGlobalBuffer;
		protected static readonly Action<int, Color> SetGlobalColor;
		protected static readonly Action<int, float> SetGlobalFloat;
		protected static readonly Action<int, float[]> SetGlobalFloatArray;
		protected static readonly Action<int, List<float>> SetGlobalFloatArrayList;
		protected static readonly Action<int, int> SetGlobalInt;
		protected static readonly Action<int, Matrix4x4> SetGlobalMatrix;
		protected static readonly Action<int, Matrix4x4[]> SetGlobalMatrixArray;
		protected static readonly Action<int, List<Matrix4x4>> SetGlobalMatrixArrayList;
		protected static readonly Action<int, Texture> SetGlobalTexture;
		protected static readonly Action<int, Vector4> SetGlobalVector;
		protected static readonly Action<int, Vector4[]> SetGlobalVectorArray;
		protected static readonly Action<int, List<Vector4>> SetGlobalVectorArrayList;

		static GlobalShaderLinker()
		{
			SetGlobalBuffer = (Action<int, ComputeBuffer>) Delegate.CreateDelegate(typeof(Action<int, ComputeBuffer>),
				typeof(Shader).GetMethod("SetGlobalBuffer", new[] {typeof(int), typeof(ComputeBuffer)}));
			SetGlobalColor = (Action<int, Color>) Delegate.CreateDelegate(typeof(Action<int, Color>),
				typeof(Shader).GetMethod("SetGlobalColor", new[] {typeof(int), typeof(Color)}));
			SetGlobalFloat = (Action<int, float>) Delegate.CreateDelegate(typeof(Action<int, float>),
				typeof(Shader).GetMethod("SetGlobalFloat", new[] {typeof(int), typeof(float)}));
			SetGlobalFloatArray = (Action<int, float[]>) Delegate.CreateDelegate(typeof(Action<int, float[]>),
				typeof(Shader).GetMethod("SetGlobalFloatArray", new[] {typeof(int), typeof(float[])}));
			SetGlobalFloatArrayList = (Action<int, List<float>>) Delegate.CreateDelegate(typeof(Action<int, List<float>>),
				typeof(Shader).GetMethod("SetGlobalFloatArray", new[] {typeof(int), typeof(List<float>)}));
			SetGlobalInt = (Action<int, int>) Delegate.CreateDelegate(typeof(Action<int, int>),
				typeof(Shader).GetMethod("SetGlobalInt", new[] {typeof(int), typeof(int)}));
			SetGlobalMatrix = (Action<int, Matrix4x4>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4>),
				typeof(Shader).GetMethod("SetGlobalMatrix", new[] {typeof(int), typeof(Matrix4x4)}));
			SetGlobalMatrixArray = (Action<int, Matrix4x4[]>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4[]>),
				typeof(Shader).GetMethod("SetGlobalMatrixArray", new[] {typeof(int), typeof(Matrix4x4[])}));
			SetGlobalMatrixArrayList = (Action<int, List<Matrix4x4>>) Delegate.CreateDelegate(
				typeof(Action<int, List<Matrix4x4>>),
				typeof(Shader).GetMethod("SetGlobalMatrixArray", new[] {typeof(int), typeof(List<Matrix4x4>)}));
			SetGlobalTexture = (Action<int, Texture>) Delegate.CreateDelegate(typeof(Action<int, Texture>),
				typeof(Shader).GetMethod("SetGlobalTexture", new[] {typeof(int), typeof(Texture)}));
			SetGlobalVector = (Action<int, Vector4>) Delegate.CreateDelegate(typeof(Action<int, Vector4>),
				typeof(Shader).GetMethod("SetGlobalVector", new[] {typeof(int), typeof(Vector4)}));
			SetGlobalVectorArray = (Action<int, Vector4[]>) Delegate.CreateDelegate(typeof(Action<int, Vector4[]>),
				typeof(Shader).GetMethod("SetGlobalVectorArray", new[] {typeof(int), typeof(Vector4[])}));
			SetGlobalVectorArrayList = (Action<int, List<Vector4>>) Delegate.CreateDelegate(typeof(Action<int, List<Vector4>>),
				typeof(Shader).GetMethod("SetGlobalVectorArray", new[] {typeof(int), typeof(List<Vector4>)}));
		}

		public void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<ComputeBuffer>(shaderName, getter, SetGlobalBuffer));
		}

		public void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Color>(shaderName, getter, SetGlobalColor));
		}

		public void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<float>(shaderName, getter, SetGlobalFloat));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<float[]>(shaderName, getter, SetGlobalFloatArray));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<List<float>>(shaderName, getter, SetGlobalFloatArrayList));
		}

		public void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<int>(shaderName, getter, SetGlobalInt));
		}

		public void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Matrix4x4>(shaderName, getter, SetGlobalMatrix));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Matrix4x4[]>(shaderName, getter, SetGlobalMatrixArray));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<List<Matrix4x4>>(shaderName, getter, SetGlobalMatrixArrayList));
		}

		public void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Vector4>(shaderName, getter, SetGlobalVector));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Vector4[]>(shaderName, getter, SetGlobalVectorArray));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<List<Vector4>>(shaderName, getter, SetGlobalVectorArrayList));
		}

		public void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter)
		{
			LinkedDictionary[updateRate].Add(new CommonLink<Texture>(shaderName, getter, SetGlobalTexture));
		}
	}
}