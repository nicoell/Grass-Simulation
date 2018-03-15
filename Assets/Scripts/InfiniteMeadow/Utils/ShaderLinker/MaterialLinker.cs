using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable AssignNullToNotNullAttribute

namespace InfiniteMeadow.Utils
{
	public class MaterialLinker : ShaderLinker, ShaderLinker.IMaterial
	{
		protected readonly Material Material;
		protected readonly Action<int, ComputeBuffer> SetMatBuffer;
		protected readonly Action<int, Color> SetMatColor;
		protected readonly Action<int, Color[]> SetMatColorArray;
		protected readonly Action<int, List<Color>> SetMatColorArrayList;
		protected readonly Action<int, float> SetMatFloat;
		protected readonly Action<int, float[]> SetMatFloatArray;
		protected readonly Action<int, List<float>> SetMatFloatArrayList;
		protected readonly Action<int, int> SetMatInt;
		protected readonly Action<int, Matrix4x4> SetMatMatrix;
		protected readonly Action<int, Matrix4x4[]> SetMatMatrixArray;
		protected readonly Action<int, List<Matrix4x4>> SetMatMatrixArrayList;
		protected readonly Action<int, Texture> SetMatTexture;
		protected readonly Action<int, Vector2> SetMatTextureOffset;
		protected readonly Action<int, Vector2> SetMatTextureScale;
		protected readonly Action<int, Vector4> SetMatVector;
		protected readonly Action<int, Vector4[]> SetMatVectorArray;
		protected readonly Action<int, List<Vector4>> SetMatVectorArrayList;

		public MaterialLinker(Material material)
		{
			Material = material;

			SetMatBuffer = (Action<int, ComputeBuffer>) Delegate.CreateDelegate(typeof(Action<int, ComputeBuffer>), Material,
				typeof(Material).GetMethod("SetBuffer", new[] {typeof(int), typeof(ComputeBuffer)}));
			SetMatColor = (Action<int, Color>) Delegate.CreateDelegate(typeof(Action<int, Color>), Material,
				typeof(Material).GetMethod("SetColor", new[] {typeof(int), typeof(Color)}));
			SetMatColorArray = (Action<int, Color[]>) Delegate.CreateDelegate(typeof(Action<int, Color[]>), Material,
				typeof(Material).GetMethod("SetColor", new[] {typeof(int), typeof(Color[])}));
			SetMatColorArrayList = (Action<int, List<Color>>) Delegate.CreateDelegate(typeof(Action<int, List<Color>>), Material,
				typeof(Material).GetMethod("SetColor", new[] {typeof(int), typeof(List<Color>)}));
			SetMatFloat = (Action<int, float>) Delegate.CreateDelegate(typeof(Action<int, float>), Material,
				typeof(Material).GetMethod("SetFloat", new[] {typeof(int), typeof(float)}));
			SetMatFloatArray = (Action<int, float[]>) Delegate.CreateDelegate(typeof(Action<int, float[]>), Material,
				typeof(Material).GetMethod("SetFloatArray", new[] {typeof(int), typeof(float[])}));
			SetMatFloatArrayList = (Action<int, List<float>>) Delegate.CreateDelegate(typeof(Action<int, List<float>>), Material,
				typeof(Material).GetMethod("SetFloatArray", new[] {typeof(int), typeof(List<float>)}));
			SetMatInt = (Action<int, int>) Delegate.CreateDelegate(typeof(Action<int, int>), Material,
				typeof(Material).GetMethod("SetInt", new[] {typeof(int), typeof(int)}));
			SetMatMatrix = (Action<int, Matrix4x4>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4>), Material,
				typeof(Material).GetMethod("SetMatrix", new[] {typeof(int), typeof(Matrix4x4)}));
			SetMatMatrixArray = (Action<int, Matrix4x4[]>) Delegate.CreateDelegate(typeof(Action<int, Matrix4x4[]>), Material,
				typeof(Material).GetMethod("SetMatrixArray", new[] {typeof(int), typeof(Matrix4x4[])}));
			SetMatMatrixArrayList = (Action<int, List<Matrix4x4>>) Delegate.CreateDelegate(
				typeof(Action<int, List<Matrix4x4>>), Material,
				typeof(Material).GetMethod("SetMatrixArray", new[] {typeof(int), typeof(List<Matrix4x4>)}));
			SetMatTexture = (Action<int, Texture>) Delegate.CreateDelegate(typeof(Action<int, Texture>), Material,
				typeof(Material).GetMethod("SetTexture", new[] {typeof(int), typeof(Texture)}));
			SetMatTextureOffset = (Action<int, Vector2>) Delegate.CreateDelegate(typeof(Action<int, Vector2>), Material,
				typeof(Material).GetMethod("SetTextureOffset", new[] {typeof(int), typeof(Vector2)}));
			SetMatTextureScale = (Action<int, Vector2>) Delegate.CreateDelegate(typeof(Action<int, Vector2>), Material,
				typeof(Material).GetMethod("SetTextureScale", new[] {typeof(int), typeof(Vector2)}));
			SetMatVector = (Action<int, Vector4>) Delegate.CreateDelegate(typeof(Action<int, Vector4>), Material,
				typeof(Material).GetMethod("SetVector", new[] {typeof(int), typeof(Vector4)}));
			SetMatVectorArray = (Action<int, Vector4[]>) Delegate.CreateDelegate(typeof(Action<int, Vector4[]>), Material,
				typeof(Material).GetMethod("SetVectorArray", new[] {typeof(int), typeof(Vector4[])}));
			SetMatVectorArrayList = (Action<int, List<Vector4>>) Delegate.CreateDelegate(typeof(Action<int, List<Vector4>>),
				Material,
				typeof(Material).GetMethod("SetVectorArray", new[] {typeof(int), typeof(List<Vector4>)}));
		}

		public void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<ComputeBuffer>(shaderName, getter, SetMatBuffer));
		}

		public void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Color>(shaderName, getter, SetMatColor));
		}

		public void LinkColorArray(UpdateRate updateRate, string shaderName, Func<Color[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Color[]>(shaderName, getter, SetMatColorArray));
		}

		public void LinkColorArray(UpdateRate updateRate, string shaderName, Func<List<Color>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<Color>>(shaderName, getter, SetMatColorArrayList));
		}

		public void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float>(shaderName, getter, SetMatFloat));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<float[]>(shaderName, getter, SetMatFloatArray));
		}

		public void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<float>>(shaderName, getter, SetMatFloatArrayList));
		}

		public void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<int>(shaderName, getter, SetMatInt));
		}

		public void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4>(shaderName, getter, SetMatMatrix));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Matrix4x4[]>(shaderName, getter, SetMatMatrixArray));
		}

		public void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<Matrix4x4>>(shaderName, getter, SetMatMatrixArrayList));
		}

		public void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Texture>(shaderName, getter, SetMatTexture));
		}

		public void LinkTextureOffset(UpdateRate updateRate, string shaderName, Func<Vector2> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector2>(shaderName, getter, SetMatTextureOffset));
		}

		public void LinkTextureScale(UpdateRate updateRate, string shaderName, Func<Vector2> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector2>(shaderName, getter, SetMatTextureScale));
		}

		public void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4>(shaderName, getter, SetMatVector));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<Vector4[]>(shaderName, getter, SetMatVectorArray));
		}

		public void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter)
		{
			LinkedDictionary[updateRate].Add(new ShaderLink<List<Vector4>>(shaderName, getter, SetMatVectorArrayList));
		}
	}
}