#version 430 core
layout(local_size_x = 8, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 0) buffer data_i32 { int _data_i32_[];}; 

const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void test1_c54_00()
{ // range for
  // range known at compile time
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (8); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = 0 + _sid;
      int C = _itv;
      int J = int(1);
      int Bj = 0;
      int BA = int(0);
      int Bl = Bj + 96 * BA; // S0
      int Bm = Bl + 0; // S1
      int BH = int(7);
      int BG = C & BH;
      int Bp = Bm + 4 * BG; // S1
      int Bq = Bp + 0; // S2
      _data_i32_[Bq >> 2] = J;
  }
}

void main()
{
  test1_c54_00();
}
