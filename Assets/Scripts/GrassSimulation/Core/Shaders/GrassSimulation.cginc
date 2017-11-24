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


#endif //GRASS_SIMULATION_CG_INCLUDED