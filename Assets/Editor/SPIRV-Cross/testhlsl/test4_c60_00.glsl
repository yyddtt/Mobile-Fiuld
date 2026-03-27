#version 430 core
layout(local_size_x = 8, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 0) buffer data_i32 { int _data_i32_[];}; 

const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void test4_c60_00()
{ // range for
  // range known at compile time
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (8); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = 0 + _sid;
      int C = _itv;
      int Ca = int(7);
      int Cb = C & Ca;
      int Ci = int(2);
      int Ch = Cb + Ci;
      int BZ = 0;
      int Cj = int(0);
      int C1 = BZ + 96 * Cj; // S0
      int C2 = C1 + 0; // S1
      int C5 = C2 + 4 * Cb; // S1
      int C6 = C5 + 0; // S2
      _data_i32_[C6 >> 2] = Ch;
  }
}

void main()
{
  test4_c60_00();
}
