void MainLight_half(out half3 Direction)
{
#ifdef SHADERGRAPH_PREVIEW
	Direction = half3(0, 1, 0);
#else
	Light light = GetMainLight();
	Direction = light.direction;
#endif
}