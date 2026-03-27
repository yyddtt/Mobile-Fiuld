#version 310 es
layout(local_size_x = 128, local_size_y = 1, local_size_z = 1) in;
precision highp float;
layout(std430, binding = 1) buffer gtmp_i32 { int _gtmp_i32_[];}; 
layout(std430, binding = 1) buffer gtmp_f32 { float _gtmp_f32_[];}; 
layout(std430, binding = 2) buffer args_i32 { int _args_i32_[];}; 
layout(std430, binding = 2) buffer args_f32 { float _args_f32_[];}; 
layout(std430, binding = 9) buffer arr1_i32 { int _arr1_i32_[];}; 
layout(std430, binding = 9) buffer arr1_f32 { float _arr1_f32_[];}; 
layout(std430, binding = 8) buffer arr3_i32 { int _arr3_i32_[];}; 
layout(std430, binding = 8) buffer arr3_f32 { float _arr3_f32_[];}; 
layout(std430, binding = 7) buffer arr2_i32 { int _arr2_i32_[];}; 
layout(std430, binding = 7) buffer arr2_f32 { float _arr2_f32_[];}; 
layout(std430, binding = 6) buffer arr0_i32 { int _arr0_i32_[];}; 
layout(std430, binding = 6) buffer arr0_f32 { float _arr0_f32_[];}; 
layout(std430, binding = 5) buffer arr5_i32 { int _arr5_i32_[];}; 
layout(std430, binding = 5) buffer arr5_f32 { float _arr5_f32_[];}; 
layout(std430, binding = 4) buffer arr4_i32 { int _arr4_i32_[];}; 
layout(std430, binding = 4) buffer arr4_f32 { float _arr4_f32_[];}; 
float atomicAdd_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_gtmp_f32(int addr, float rhs) { int old, new, ret; do { old = _gtmp_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_gtmp_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr0_f32(int addr, float rhs) { int old, new, ret; do { old = _arr0_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr0_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr1_f32(int addr, float rhs) { int old, new, ret; do { old = _arr1_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr1_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr2_f32(int addr, float rhs) { int old, new, ret; do { old = _arr2_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr2_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr3_f32(int addr, float rhs) { int old, new, ret; do { old = _arr3_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr3_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr4_f32(int addr, float rhs) { int old, new, ret; do { old = _arr4_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr4_i32_[addr], old, new)); return intBitsToFloat(old); }float atomicAdd_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) + rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicSub_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt((intBitsToFloat(old) - rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMax_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt(max(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); } float atomicMin_arr5_f32(int addr, float rhs) { int old, new, ret; do { old = _arr5_i32_[addr]; new = floatBitsToInt(min(intBitsToFloat(old), rhs)); } while (old != atomicCompSwap(_arr5_i32_[addr], old, new)); return intBitsToFloat(old); }
const float inf = 1.0f / 0.0f;
const float nan = 0.0f / 0.0f;
void substep_c54_05()
{ // range for
  // range known at runtime
  int _beg = 0, _end = _gtmp_i32_[16 >> 2];
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (_end - _beg); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = _beg + _sid;
      float F1 = float(0.0);
      int F2 = int(1);
      int F3 = int(125);
      int F4 = int(3);
      float F5 = float(-0.00196);
      int F6 = int(0);
      float F7 = float(1.0);
      int F8 = _itv;
      int F9 = 20;
      int Fa = _gtmp_i32_[F9 >> 2];
      int Fb = F8 - Fa * int(F8 / Fa);
      int Fc = F8 / Fa;
      int Fd = 24;
      int Fe = _gtmp_i32_[Fd >> 2];
      int Ff = Fc - Fe * int(Fc / Fe);
      int _li_Fh = 0;
      { // linear seek
        int _s0_Fh = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Fh = _args_i32_[16 + 5 * 8 + 1];
        _li_Fh *= _s0_Fh;
        _li_Fh += Ff;
        _li_Fh *= _s1_Fh;
        _li_Fh += Fb;
      }
      int Fh = _li_Fh << 2;
      float Fi = _arr5_f32_[Fh >> 2];
      int Fj = -int(Fi > F1);
      int Fk = Fj & F2;
      if (Fk != 0) {
        float Fm = _arr5_f32_[Fh >> 2];
        float Fn = F7 / Fm;
        int _li_Fp = 0;
        { // linear seek
          int _s0_Fp = _args_i32_[16 + 4 * 8 + 0];
          int _s1_Fp = _args_i32_[16 + 4 * 8 + 1];
          int _s2_Fp = _args_i32_[16 + 4 * 8 + 2];
          _li_Fp *= _s0_Fp;
          _li_Fp += Ff;
          _li_Fp *= _s1_Fp;
          _li_Fp += Fb;
          _li_Fp *= _s2_Fp;
          _li_Fp += F6;
        }
        int Fp = _li_Fp << 2;
        float Fq = _arr4_f32_[Fp >> 2];
        float Fr = Fn * Fq;
        int _li_Fs = 0;
        { // linear seek
          int _s0_Fs = _args_i32_[16 + 4 * 8 + 0];
          int _s1_Fs = _args_i32_[16 + 4 * 8 + 1];
          int _s2_Fs = _args_i32_[16 + 4 * 8 + 2];
          _li_Fs *= _s0_Fs;
          _li_Fs += Ff;
          _li_Fs *= _s1_Fs;
          _li_Fs += Fb;
          _li_Fs *= _s2_Fs;
          _li_Fs += F2;
        }
        int Fs = _li_Fs << 2;
        float Ft = _arr4_f32_[Fs >> 2];
        float Fu = Fn * Ft;
        _arr4_f32_[Fp >> 2] = Fr;
        _arr4_f32_[Fs >> 2] = Fu;
        float Fx;
        { // Begin Atomic Op
        Fx = atomicAdd_arr4_f32(Fs >> 2, F5);
        } // End Atomic Op
        int Fy = -int(Ff < F4);
        int Fz = Fy & F2;
        float FA = _arr4_f32_[Fp >> 2];
        int FB = -int(FA < F1);
        int FC = FB & F2;
        int FD = Fz & FC;
        if (FD != 0) {
          _arr4_f32_[Fp >> 2] = F1;
        }
        int FG = -int(Ff > F3);
        int FH = FG & F2;
        float FI = _arr4_f32_[Fp >> 2];
        int FJ = -int(FI > F1);
        int FK = FJ & F2;
        int FL = FH & FK;
        if (FL != 0) {
          _arr4_f32_[Fp >> 2] = F1;
        }
        int FO = -int(Fb < F4);
        int FP = FO & F2;
        float FQ = _arr4_f32_[Fs >> 2];
        int FR = -int(FQ < F1);
        int FS = FR & F2;
        int FT = FP & FS;
        if (FT != 0) {
          _arr4_f32_[Fs >> 2] = F1;
        }
        int FW = -int(Fb > F3);
        int FX = FW & F2;
        float FY = _arr4_f32_[Fs >> 2];
        int FZ = -int(FY > F1);
        int G0 = FZ & F2;
        int G1 = FX & G0;
        if (G1 != 0) {
          _arr4_f32_[Fs >> 2] = F1;
        }
      }
  }
}

void main()
{
  substep_c54_05();
}
