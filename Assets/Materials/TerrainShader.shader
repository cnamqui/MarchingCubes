 
Shader "Custom/Terrain" {

	
	Properties{
		   
		_Rock ("Rock", Color) = (1,1,1,1)
		_Rock2 ("Rock2", Color) = (1,1,1,1)
		_Top ("Top", Color) = (1,1,1,1)
		_Top2 ("Top2", Color) = (1,1,1,1)
		_Ground ("Ground", Color) = (1,1,1,1)
		_Ground2 ("Ground2", Color) = (1,1,1,1)

		 
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5 
		_Metallic ("Metallic", Range(0,1)) = 0.5 
		_TopNormalThreshold ("_TopNormalThreshold", Range(0,1)) = 0.25  

		
		_tScale ("_tScale", Range(0.01,50)) = 1  
		_gScale ("_gScale", Range(0.01,50)) = 1  
		_rScale ("_rScale", Range(0.01,50)) = 1  

		
		_RockLevel ("Rock Level", Float) = 10.0

	}

	
	
	SubShader{

		Tags { "RenderType" = "Opaque" } 
		LOD 200 

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows 
		#pragma target 3.0 

		
		sampler2D _MainTex; 
		
		half _Glossiness;
		half _Metallic;
		fixed4 _Color; 
		float4 _Rock;
		float4 _Rock2;
		float4 _Top;
		float4 _Top2;
		float4 _Ground;
		float4 _Ground2;
		float _RockLevel;
		float _TopNormalThreshold; 
		float _tScale;
		float _gScale;
		float _rScale;

		
		struct Input {

			float3 worldPos;
			float3 worldNormal; 
		};
 
		float4 triplanarOffset(float3 vertPos, float3 normal, float3 scale, sampler2D tex, float2 offset) {
			float3 scaledPos = vertPos / scale;
			float4 colX = tex2D (tex, scaledPos.zy + offset);
			float4 colY = tex2D(tex, scaledPos.xz + offset);
			float4 colZ = tex2D (tex,scaledPos.xy + offset);
			
			// Square normal to make all values positive + increase blend sharpness
			float3 blendWeight = normal * normal;
			// Divide blend weight by the sum of its components. This will make x + y + z = 1
			blendWeight /= dot(blendWeight, 1);
			return colX * blendWeight.x + colY * blendWeight.y + colZ * blendWeight.z;
		}
		void surf(Input IN, inout SurfaceOutputStandard o) {

			float3 scaledWorldPos = IN.worldPos; 
			 

			
			float4 noise = triplanarOffset(IN.worldPos, IN.worldNormal, 256 * _tScale, _MainTex, 0);
			float4 noise2 = triplanarOffset(IN.worldPos, IN.worldNormal, 256 * _gScale, _MainTex, 0);
			float4 noise3 = triplanarOffset(IN.worldPos, IN.worldNormal, 256 * _rScale, _MainTex, 0);

			if(IN.worldNormal.y > _TopNormalThreshold && IN.worldPos.y > _RockLevel)
			{
				float4 top =  lerp(_Top,_Top2, noise.r);
				o.Albedo = top;
			}else if(IN.worldNormal.y < _TopNormalThreshold && IN.worldPos.y > _RockLevel)
			{ 
				float4 g =  lerp(_Ground,_Ground2, noise2.r);
				o.Albedo = g;
			}else{
				
				float4 r =  lerp(_Rock,_Rock2, noise3.r);
				o.Albedo = r;
			} 
			
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}