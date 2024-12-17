using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SurfaceTextureLayer
    {
        public Source<Texture2D> albedo, normal = new SourceConstant<Texture2D>(Texture2D.normalTexture), metal = new SourceConstant<Texture2D>(Texture2D.blackTexture);

        public float height_weight = 1, weight_softness,
            height_params_1, height_params_2, height_params_3, height_params_4,
            slope_params_1, slope_params_2, slope_params_3, slope_params_4,
            grad_map_weights_1 = 1, grad_map_weights_2 = 1, grad_map_weights_3 = 1, grad_map_weights_4 = 1,
            peak_cav_params_1, peak_cav_params_2, peak_cav_params_3, peak_cav_params_4,
            curv_map_weights_1 = 1, curv_map_weights_2 = 1, curv_map_weights_3 = 1, curv_map_weights_4 = 1,
            uv_scale = 1, uv_offset = 1,
            tint_1 = 1, tint_2 = 1, tint_3 = 1, tint_4 = 1,
            brightness, contrast = 1, saturation = 1, normal_strength = 1, gloss_strength = 1, metallic_strength, emission_strength,
            emission_color_1 = 1, emission_color_2 = 1, emission_color_3 = 1, emission_color_4 = 1,
            ao_strength = 1, distance_resample_max = 4;

        internal (Source<Texture2D>, Source<Texture2D>, Source<Texture2D>) GetTextureTuple() => (albedo, normal, metal);
    }
}
