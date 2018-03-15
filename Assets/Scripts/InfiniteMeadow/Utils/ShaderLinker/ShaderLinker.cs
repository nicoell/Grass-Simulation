using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteMeadow.Utils
{
	[Flags]
	public enum UpdateRate
	{
		Once = 1,
		PerFrame = 2
	}

	public abstract class ShaderLinker
	{
		protected readonly Dictionary<UpdateRate, List<IShaderLink>> LinkedDictionary;

		protected ShaderLinker()
		{
			LinkedDictionary = new Dictionary<UpdateRate, List<IShaderLink>>();

			foreach (UpdateRate rate in Enum.GetValues(typeof(UpdateRate))) LinkedDictionary.Add(rate, new List<IShaderLink>());
		}

		public void UpdateLinks(UpdateRate updateRate)
		{
			foreach (var link in LinkedDictionary[updateRate]) link.Link();
		}

		protected internal interface IShader
		{
			void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter);
			void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter);
			void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter);
			void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter);
			void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter);
			void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter);
			void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter);
		}

		protected internal interface IMaterial
		{
			void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter);
			void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter);
			void LinkColorArray(UpdateRate updateRate, string shaderName, Func<Color[]> getter);
			void LinkColorArray(UpdateRate updateRate, string shaderName, Func<List<Color>> getter);
			void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter);
			void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter);
			void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter);
			void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter);
			void LinkTextureOffset(UpdateRate updateRate, string shaderName, Func<Vector2> getter);
			void LinkTextureScale(UpdateRate updateRate, string shaderName, Func<Vector2> getter);
			void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter);
		}

		protected internal interface IMaterialPropertyBlock
		{
			void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter);
			void LinkColor(UpdateRate updateRate, string shaderName, Func<Color> getter);
			void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<List<float>> getter);
			void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<List<Matrix4x4>> getter);
			void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter);
			void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter);
			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<List<Vector4>> getter);
		}

		protected internal interface IComputeShader
		{
			void LinkBool(UpdateRate updateRate, string shaderName, Func<bool> getter);
			void LinkFloat(UpdateRate updateRate, string shaderName, Func<float> getter);
			void LinkFloatArray(UpdateRate updateRate, string shaderName, Func<float[]> getter);
			void LinkInt(UpdateRate updateRate, string shaderName, Func<int> getter);
			void LinkIntArray(UpdateRate updateRate, string shaderName, Func<int[]> getter);
			void LinkMatrix(UpdateRate updateRate, string shaderName, Func<Matrix4x4> getter);
			void LinkMatrixArray(UpdateRate updateRate, string shaderName, Func<Matrix4x4[]> getter);
			void LinkVector(UpdateRate updateRate, string shaderName, Func<Vector4> getter);

			void LinkVectorArray(UpdateRate updateRate, string shaderName, Func<Vector4[]> getter);

			//With Kernel:
			void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter, int kernel);
			void LinkBuffer(UpdateRate updateRate, string shaderName, Func<ComputeBuffer> getter, int[] kernels);
			void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter, int kernel);
			void LinkTexture(UpdateRate updateRate, string shaderName, Func<Texture> getter, int[] kernels);
			void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<string> getter, int kernel);
			void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<string> getter, int[] kernels);
			void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<int> getter, int kernel);
			void LinkTextureFromGlobal(UpdateRate updateRate, string shaderName, Func<int> getter, int[] kernels);
		}
	}
}