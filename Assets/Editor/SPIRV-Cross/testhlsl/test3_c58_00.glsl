#version 430 core
layout(local_size_x = 8, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 0) buffer data_i32 { int _data_i32_[];}; 

const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void test3_c58_00()
{ // range for
  // range known at compile time
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (8); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = 0 + _sid;
      int C = _itv;
      int Bi = int(7);
      int Bj = C & Bi;
      int Bw = int(1);
      int Bp = Bj + Bw;
      int B7 = 0;
      int Bx = int(0);
      int B9 = B7 + 96 * Bx; // S0
      int Ba = B9 + 0; // S1
      int Bd = Ba + 4 * Bj; // S1
      int Be = Bd + 0; // S2
      _data_i32_[Be >> 2] = Bp;
  }
}

void main()
{
  test3_c58_00();
}
