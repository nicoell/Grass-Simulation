using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable AssignNullToNotNullAttribute

namespace InfiniteMeadow.Utils
{
	public class MaterialPropertyBlockLinker : ShaderLinker, ShaderLinker.IMaterialPropertyBlock
	{
		protected readonly MaterialPropertyBlock MaterialPropertyBlock;
		protected readonly Action<int, ComputeBuffer> SetPropBuffer;
		protected readonly Action<int, Color> SetPropColor;
		protected readonly Action<int, float> SetPropFloat;
		protected readonly Action<int, float[]> SetPropFloatArray;
		protected readonly Action<int, List<float>> SetPropFloatArrayList;
		protected readonly Action<int, Matrix4x4> SetPropMatrix;
		protected readonly Action<int, Matrix4x4[]> SetPropMatrixArray;
		protected readonly Action<int, List<Matrix4x4>> SetPropMatrixArrayList;
		protected readonly Action<int, Texture> SetPropTexture;
		protected readonly Action<int, Vector4> SetPropVector;
		protected readonly Action<int, Vector4[]> SetPropVectorArray;
		protected readonly Action<int, List<Vector4>> SetPropVectorArrayList;

		public MaterialPropertyBlockLinker(MaterialPropertyBlock materialPropertyBlock)
		{
			MaterialPropertyBlock = materialPropertyBlock;

			SetPropBuffer = (Action<int, ComputeBuffer>) Delegate.CreateDelegate(typeof(Action<int, ComputeBuffer>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetBuffer", new[] {typeof(int), typeof(ComputeBuffer)}));
			SetPropColor = (Action<int, Color>) Delegate.CreateDelegate(typeof(Action<int, Color>), MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetColor", new[] {typeof(int), typeof(Color)}));
			SetPropFloat = (Action<int, float>) Delegate.CreateDelegate(typeof(Action<int, float>), MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetFloat", new[] {typeof(int), typeof(float)}));
			SetPropFloatArray = (Action<int, float[]>) Delegate.CreateDelegate(typeof(Action<int, float[]>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetFloatArray", new[] {typeof(int), typeof(float[])}));
			SetPropFloatArrayList = (Action<int, List<float>>) Delegate.CreateDelegate(typeof(Action<int, List<float>>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetFloatArray", new[] {typeof(int), typeof(List<float>)}));
			SetPropMatrix = (Action<int, Matrix4x4>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetMatrix", new[] {typeof(int), typeof(Matrix4x4)}));
			SetPropMatrixArray = (Action<int, Matrix4x4[]>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4[]>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SeMatrixArray", new[] {typeof(int), typeof(Matrix4x4[])}));
			SetPropMatrixArrayList = (Action<int, List<Matrix4x4>>) Delegate.CreateDelegate(typeof(Action<int, List<Matrix4x4>>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetMatrixArray", new[] {typeof(int), typeof(List<Matrix4x4>)}));
			SetPropTexture = (Action<int, Texture>) Delegate.CreateDelegate(typeof(Action<int, Texture>), MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetTexture", new[] {typeof(int), typeof(Texture)}));
			SetPropVector = (Action<int, Vector4>) Delegate.CreateDelegate(typeof(Action<int, Vector4>), MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetVector", new[] {typeof(int), typeof(Vector4)}));
			SetPropVectorArray = (Action<int, Vector4[]>) Delegate.CreateDelegate(typeof(Action<int, Vector4[]>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetVectorArray", new[] {typeof(int), typeof(Vector4[])}));
			SetPropVectorArrayList = (Action<int, List<Vector4>>) Delegate.CreateDelegate(typeof(Action<int, List<Vector4>>),
				MaterialPropertyBlock,
				typeof(MaterialPropertyBlock).GetMethod("SetVectorArray", new[] {typeof(int), typeof(List<Vector4>)}));
		}

		public void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<ComputeBuffer>(shaderName, getter, SetPropBuffer));
		}

		public void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Color>(shaderName, getter, SetPropColor));
		}

		public void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float>(shaderName, getter, SetPropFloat));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float[]>(shaderName, getter, SetPropFloatArray));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<float>>(shaderName, getter, SetPropFloatArrayList));
		}

		public void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4>(shaderName, getter, SetPropMatrix));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4[]>(shaderName, getter, SetPropMatrixArray));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<Matrix4x4>>(shaderName, getter, SetPropMatrixArrayList));
		}

		public void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Texture>(shaderName, getter, SetPropTexture));
		}

		public void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4>(shaderName, getter, SetPropVector));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4[]>(shaderName, getter, SetPropVectorArray));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<Vector4>>(shaderName, getter, SetPropVectorArrayList));
		}
	}
}