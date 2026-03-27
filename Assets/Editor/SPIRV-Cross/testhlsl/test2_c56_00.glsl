#version 430 core
layout(local_size_x = 8, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 0) buffer data_i32 { int _data_i32_[];}; 

const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void test2_c56_00()
{ // range for
  // range known at compile time
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (8); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = 0 + _sid;
      int C = _itv;
      int Cb = int(7);
      int Cc = C & Cb;
      int C0 = 0;
      int Ch = int(0);
      int C2 = C0 + 96 * Ch; // S0
      int C3 = C2 + 0; // S1
      int C6 = C3 + 4 * Cc; // S1
      int C7 = C6 + 0; // S2
      _data_i32_[C7 >> 2] = Cc;
  }
}

void main()
{
  test2_c56_00();
}
