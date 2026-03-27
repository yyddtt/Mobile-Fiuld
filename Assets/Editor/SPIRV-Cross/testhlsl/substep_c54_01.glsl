#version 310 es
layout(local_size_x = 128, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 1) buffer gtmp_i32 { int _gtmp_i32_[];}; 
layout(std430, binding = 1) buffer gtmp_f32 { float _gtmp_f32_[];}; 
layout(std430, binding = 2) buffer args_i32 { int _args_i32_[];}; 
layout(std430, binding = 2) buffer args_f32 { float _args_f32_[];}; 
layout(std430, binding = 5) buffer arr5_i32 { int _arr5_i32_[];}; 
layout(std430, binding = 5) buffer arr5_f32 { float _arr5_f32_[];}; 
layout(std430, binding = 4) buffer arr4_i32 { int _arr4_i32_[];}; 
layout(std430, binding = 4) buffer arr4_f32 { float _arr4_f32_[];}; 

const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void substep_c54_01()
{ // range for
  // range known at runtime
  int _beg = 0, _end = _gtmp_i32_[0 >> 2];
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (_end - _beg); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = _beg + _sid;
      int L = _itv;
      int M = 4;
      int N = _gtmp_i32_[M >> 2];
      int O = L - N * int(L / N);
      int P = L / N;
      int Q = 8;
      int R = _gtmp_i32_[Q >> 2];
      int S = P - R * int(P / R);
      int U = int(0);
      int _li_V = 0;
      { // linear seek
        int _s0_V = _args_i32_[16 + 4 * 8 + 0];
        int _s1_V = _args_i32_[16 + 4 * 8 + 1];
        int _s2_V = _args_i32_[16 + 4 * 8 + 2];
        _li_V *= _s0_V;
        _li_V += S;
        _li_V *= _s1_V;
        _li_V += O;
        _li_V *= _s2_V;
        _li_V += U;
      }
      int V = _li_V << 2;
      float W = float(0.0);
      _arr4_f32_[V >> 2] = W;
      int Y = int(1);
      int _li_Z = 0;
      { // linear seek
        int _s0_Z = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Z = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Z = _args_i32_[16 + 4 * 8 + 2];
        _li_Z *= _s0_Z;
        _li_Z += S;
        _li_Z *= _s1_Z;
        _li_Z += O;
        _li_Z *= _s2_Z;
        _li_Z += Y;
      }
      int Z = _li_Z << 2;
      _arr4_f32_[Z >> 2] = W;
      int _li_As = 0;
      { // linear seek
        int _s0_As = _args_i32_[16 + 5 * 8 + 0];
        int _s1_As = _args_i32_[16 + 5 * 8 + 1];
        _li_As *= _s0_As;
        _li_As += S;
        _li_As *= _s1_As;
        _li_As += O;
      }
      int As = _li_As << 2;
      _arr5_f32_[As >> 2] = W;
  }
}

void main()
{
  substep_c54_01();
}
