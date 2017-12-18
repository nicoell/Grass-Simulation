using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Utils
{
	public static class RenderTexture
	{
		public static Texture2D GetRenderTextureVolumeElementAsTexture2D(ComputeShader renderTextureVolumeToSlice,
			UnityEngine.RenderTexture source, int element, TextureFormat outpuTextureFormat = TextureFormat.ARGB32,
			bool mipmap = true, bool linear = false)
		{
			var target = new UnityEngine.RenderTexture(source.width, source.height, 0, source.format)
			{
				dimension = TextureDimension.Tex2D,
				enableRandomWrite = true,
				wrapMode = source.wrapMode,
				wrapModeU = source.wrapModeU,
				wrapModeV = source.wrapModeV,
				wrapModeW = source.wrapModeW
			};
			target.Create();

			int kernel;
			switch (source.dimension)
			{
				case TextureDimension.Tex2DArray:
					kernel = renderTextureVolumeToSlice.FindKernel("Tex2DArray");
					renderTextureVolumeToSlice.SetTexture(kernel, "Source2DArray", source);
					break;
				case TextureDimension.Tex3D:
					kernel = renderTextureVolumeToSlice.FindKernel("Tex3D");
					renderTextureVolumeToSlice.SetTexture(kernel, "Source3D", source);
					break;
				default:
					throw new NotSupportedException("Source RenderTexture dimension '" + source.dimension + "' not supported.");
			}
			renderTextureVolumeToSlice.SetTexture(kernel, "Target", target);
			renderTextureVolumeToSlice.SetInt("element", element);

			uint threadGroupX, threadGroupY, threadGroupZ;
			renderTextureVolumeToSlice.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY,
				out threadGroupZ);

			if (source.width % threadGroupX != 0 || source.height % threadGroupY != 0)
				throw new Exception("Cannot cover all pixels, please consider changing KernelThreadGroupSizes in compute shader.");

			renderTextureVolumeToSlice.Dispatch(kernel, (int) (source.width / threadGroupX),
				(int) (source.height / threadGroupY), 1);

			var output = new Texture2D(source.width, source.height, outpuTextureFormat, mipmap, linear);
			var restoreRt = UnityEngine.RenderTexture.active;
			UnityEngine.RenderTexture.active = target;
			output.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, mipmap);
			output.Apply();
			UnityEngine.RenderTexture.active = restoreRt;
			return output;
		}

		public static Texture2D GetRenderTextureAsTexture2D(UnityEngine.RenderTexture source,
			TextureFormat outpuTextureFormat = TextureFormat.ARGB32,
			bool mipmap = true, bool linear = false)
		{
			var output = new Texture2D(source.width, source.height, outpuTextureFormat, mipmap, linear);
			var restoreRt = UnityEngine.RenderTexture.active;
			UnityEngine.RenderTexture.active = source;
			output.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, mipmap);
			output.Apply();
			UnityEngine.RenderTexture.active = restoreRt;
			return output;
		}
	}
}