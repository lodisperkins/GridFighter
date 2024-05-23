void MainLight_half(half3 WorldPos, out half3 Direction, out half3 Color,
	out half DistanceAttenuation, out half ShadowAttenuation)
{
#ifdef SHADERGRAPH_PREVIEW
	Direction = normalize(half3(0.5f, 0.5f, 0.25f));
	Color = half3(1.0f, 1.0f, 1.0f);
	DistanceAttenuation = 1.0f;
	ShadowAttenuation = 1.0f;
#else
	half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
	Light mainLight = GetMainLight(shadowCoord);

	Direction = mainLight.direction;
	Color = mainLight.color;
	DistanceAttenuation = mainLight.distanceAttenuation;
	ShadowAttenuation = mainLight.shadowAttenuation;
#endif
}

void AdditionalLight_half(half3 WorldPos, int Index, out half3 Direction,
	out half3 Color, out half DistanceAttenuation, out half ShadowAttenuation)
{
	Direction = normalize(half3(0.5f, 0.5f, 0.25f));
	Color = half3(0.0f, 0.0f, 0.0f);
	DistanceAttenuation = 0.0f;
	ShadowAttenuation = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
	int pixelLightCount = GetAdditionalLightsCount();
	if (Index < pixelLightCount)
	{
		Light light = GetAdditionalLight(Index, WorldPos);

		Direction = light.direction;
		Color = light.color;
		DistanceAttenuation = light.distanceAttenuation;
		ShadowAttenuation = light.shadowAttenuation;
	}
#endif
}