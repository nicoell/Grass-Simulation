#ifndef GRASS_SIMULATION_CG_INCLUDED
#define GRASS_SIMULATION_CG_INCLUDED

float SingleLerpMinMax(float min, float max, float cur, float start, float end){
    float t1 = clamp((cur - start) / (end - start), 0, 1);
    return lerp(max, min, t1);
}

float SingleLerp(float value, float cur, float start, float end)
{
    float t1 = clamp((cur - start) / (end - start), 0, 1);
    return lerp(value, 0, t1);
}

float DoubleLerp(float value, float cur, float start, float peak, float end)
{
    float t0 = clamp((cur - start) / (peak - start), 0, 1);
    float t1 = clamp((cur - peak) / (end - peak), 0, 1);
    return value - (lerp(value, 0, t0) + lerp(0, value, t1));
}

//YUV->RGB Colorspace conversion
//from https://www.fourcc.org/fccyvrgb.php

float3 RGBtoYUV(float3 rgb)
{
    float y =  0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b;
    float u = (rgb.b - y) * 0.585;
    float v = (rgb.r - y) * 0.713;
    return float3(y,u,v);
}

float3 YUVtoRGB(float3 yuv)
{
    float r = yuv.x + 1.403 * yuv.z;
    float g = yuv.x - 0.344 * yuv.y - 1.304 * yuv.z;
    float b = yuv.x + 1.770 * yuv.y;
    return float3(r,g,b);
}

#endif //GRASS_SIMULATION_CG_INCLUDED