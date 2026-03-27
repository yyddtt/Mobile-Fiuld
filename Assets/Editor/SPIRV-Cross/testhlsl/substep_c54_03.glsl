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
void substep_c54_03()
{ // range for
  // range known at runtime
  int _beg = 0, _end = _gtmp_i32_[12 >> 2];
  int _sid0 = int(gl_GlobalInvocationID.x);
  for (int _sid = _sid0; _sid < (_end - _beg); _sid += int(gl_WorkGroupSize.x * gl_NumWorkGroups.x)) {
    int _itv = _beg + _sid;
      int Az = _itv;
      int AA = 12;
      int AB = _gtmp_i32_[AA >> 2];
      int AC = Az - AB * int(Az / AB);
      int AE = int(0);
      int _li_AF = 0;
      { // linear seek
        int _s0_AF = _args_i32_[16 + 0 * 8 + 0];
        int _s1_AF = _args_i32_[16 + 0 * 8 + 1];
        _li_AF *= _s0_AF;
        _li_AF += AC;
        _li_AF *= _s1_AF;
        _li_AF += AE;
      }
      int AF = _li_AF << 2;
      float AG = _arr0_f32_[AF >> 2];
      float AH = float(128.0);
      float AI = AG * AH;
      int AJ = int(1);
      int _li_AK = 0;
      { // linear seek
        int _s0_AK = _args_i32_[16 + 0 * 8 + 0];
        int _s1_AK = _args_i32_[16 + 0 * 8 + 1];
        _li_AK *= _s0_AK;
        _li_AK += AC;
        _li_AK *= _s1_AK;
        _li_AK += AJ;
      }
      int AK = _li_AK << 2;
      float AL = _arr0_f32_[AK >> 2];
      float AM = AL * AH;
      float AN = float(0.5);
      float AO = AI - AN;
      float AP = AM - AN;
      int AQ = int(AO);
      int AR = int(AP);
      float AS = float(AQ);
      float AT = float(AR);
      float AU = AI - AS;
      float AV = AM - AT;
      float AW = float(1.5);
      float AX = AW - AU;
      float AY = AW - AV;
      float AZ = AX * AX;
      float B0 = AY * AY;
      float B1 = AZ * AN;
      float B2 = B0 * AN;
      float B3 = float(1.0);
      float B4 = AU - B3;
      float B5 = AV - B3;
      float B6 = B4 * B4;
      float B7 = B5 * B5;
      float B8 = float(0.75);
      float B9 = B8 - B6;
      float Ba = B8 - B7;
      float Bb = AU - AN;
      float Bc = AV - AN;
      float Bd = Bb * Bb;
      float Be = Bc * Bc;
      float Bf = Bd * AN;
      float Bg = Be * AN;
      int _li_Bi = 0;
      { // linear seek
        int _s0_Bi = _args_i32_[16 + 2 * 8 + 0];
        _li_Bi *= _s0_Bi;
        _li_Bi += AC;
      }
      int Bi = _li_Bi << 2;
      float Bj = _arr2_f32_[Bi >> 2];
      float Bk = Bj - B3;
      float Bl = float(-0.08);
      float Bm = Bk * Bl;
      int _li_Bo = 0;
      { // linear seek
        int _s0_Bo = _args_i32_[16 + 3 * 8 + 0];
        int _s1_Bo = _args_i32_[16 + 3 * 8 + 1];
        int _s2_Bo = _args_i32_[16 + 3 * 8 + 2];
        _li_Bo *= _s0_Bo;
        _li_Bo += AC;
        _li_Bo *= _s1_Bo;
        _li_Bo += AE;
        _li_Bo *= _s2_Bo;
        _li_Bo += AE;
      }
      int Bo = _li_Bo << 2;
      float Bp = _arr3_f32_[Bo >> 2];
      float Bq = float(1.5258789e-05);
      float Br = Bp * Bq;
      int _li_Bs = 0;
      { // linear seek
        int _s0_Bs = _args_i32_[16 + 3 * 8 + 0];
        int _s1_Bs = _args_i32_[16 + 3 * 8 + 1];
        int _s2_Bs = _args_i32_[16 + 3 * 8 + 2];
        _li_Bs *= _s0_Bs;
        _li_Bs += AC;
        _li_Bs *= _s1_Bs;
        _li_Bs += AE;
        _li_Bs *= _s2_Bs;
        _li_Bs += AJ;
      }
      int Bs = _li_Bs << 2;
      float Bt = _arr3_f32_[Bs >> 2];
      float Bu = Bt * Bq;
      int _li_Bv = 0;
      { // linear seek
        int _s0_Bv = _args_i32_[16 + 3 * 8 + 0];
        int _s1_Bv = _args_i32_[16 + 3 * 8 + 1];
        int _s2_Bv = _args_i32_[16 + 3 * 8 + 2];
        _li_Bv *= _s0_Bv;
        _li_Bv += AC;
        _li_Bv *= _s1_Bv;
        _li_Bv += AJ;
        _li_Bv *= _s2_Bv;
        _li_Bv += AE;
      }
      int Bv = _li_Bv << 2;
      float Bw = _arr3_f32_[Bv >> 2];
      float Bx = Bw * Bq;
      int _li_By = 0;
      { // linear seek
        int _s0_By = _args_i32_[16 + 3 * 8 + 0];
        int _s1_By = _args_i32_[16 + 3 * 8 + 1];
        int _s2_By = _args_i32_[16 + 3 * 8 + 2];
        _li_By *= _s0_By;
        _li_By += AC;
        _li_By *= _s1_By;
        _li_By += AJ;
        _li_By *= _s2_By;
        _li_By += AJ;
      }
      int By = _li_By << 2;
      float Bz = _arr3_f32_[By >> 2];
      float BA = Bz * Bq;
      float BB = Bm + Br;
      float BC = Bm + BA;
      float BD = float(0.0);
      float BE = BD - AU;
      float BF = BD - AV;
      float BG = float(0.0078125);
      float BH = BE * BG;
      float BI = BF * BG;
      float BJ = B1 * B2;
      int _li_BL = 0;
      { // linear seek
        int _s0_BL = _args_i32_[16 + 1 * 8 + 0];
        int _s1_BL = _args_i32_[16 + 1 * 8 + 1];
        _li_BL *= _s0_BL;
        _li_BL += AC;
        _li_BL *= _s1_BL;
        _li_BL += AE;
      }
      int BL = _li_BL << 2;
      float BM = _arr1_f32_[BL >> 2];
      float BN = BM * Bq;
      int _li_BO = 0;
      { // linear seek
        int _s0_BO = _args_i32_[16 + 1 * 8 + 0];
        int _s1_BO = _args_i32_[16 + 1 * 8 + 1];
        _li_BO *= _s0_BO;
        _li_BO += AC;
        _li_BO *= _s1_BO;
        _li_BO += AJ;
      }
      int BO = _li_BO << 2;
      float BP = _arr1_f32_[BO >> 2];
      float BQ = BP * Bq;
      float BR = BB * BH;
      float BS = Bu * BI;
      float BT = BR + BS;
      float BU = Bx * BH;
      float BV = BC * BI;
      float BW = BU + BV;
      float BX = BN + BT;
      float BY = BQ + BW;
      float BZ = BJ * BX;
      float C0 = BJ * BY;
      int _li_C2 = 0;
      { // linear seek
        int _s0_C2 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_C2 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_C2 = _args_i32_[16 + 4 * 8 + 2];
        _li_C2 *= _s0_C2;
        _li_C2 += AQ;
        _li_C2 *= _s1_C2;
        _li_C2 += AR;
        _li_C2 *= _s2_C2;
        _li_C2 += AE;
      }
      int C2 = _li_C2 << 2;
      float C3;
      { // Begin Atomic Op
      C3 = atomicAdd_arr4_f32(C2 >> 2, BZ);
      } // End Atomic Op
      int _li_C4 = 0;
      { // linear seek
        int _s0_C4 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_C4 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_C4 = _args_i32_[16 + 4 * 8 + 2];
        _li_C4 *= _s0_C4;
        _li_C4 += AQ;
        _li_C4 *= _s1_C4;
        _li_C4 += AR;
        _li_C4 *= _s2_C4;
        _li_C4 += AJ;
      }
      int C4 = _li_C4 << 2;
      float C5;
      { // Begin Atomic Op
      C5 = atomicAdd_arr4_f32(C4 >> 2, C0);
      } // End Atomic Op
      float C6 = BJ * Bq;
      int _li_C8 = 0;
      { // linear seek
        int _s0_C8 = _args_i32_[16 + 5 * 8 + 0];
        int _s1_C8 = _args_i32_[16 + 5 * 8 + 1];
        _li_C8 *= _s0_C8;
        _li_C8 += AQ;
        _li_C8 *= _s1_C8;
        _li_C8 += AR;
      }
      int C8 = _li_C8 << 2;
      float C9;
      { // Begin Atomic Op
      C9 = atomicAdd_arr5_f32(C8 >> 2, C6);
      } // End Atomic Op
      float Ca = B3 - AV;
      float Cb = Ca * BG;
      float Cc = B1 * Ba;
      int Cd = AR + AJ;
      float Ce = _arr1_f32_[BL >> 2];
      float Cf = Ce * Bq;
      float Cg = _arr1_f32_[BO >> 2];
      float Ch = Cg * Bq;
      float Ci = Bu * Cb;
      float Cj = BR + Ci;
      float Ck = BC * Cb;
      float Cl = BU + Ck;
      float Cm = Cf + Cj;
      float Cn = Ch + Cl;
      float Co = Cc * Cm;
      float Cp = Cc * Cn;
      int _li_Cq = 0;
      { // linear seek
        int _s0_Cq = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Cq = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Cq = _args_i32_[16 + 4 * 8 + 2];
        _li_Cq *= _s0_Cq;
        _li_Cq += AQ;
        _li_Cq *= _s1_Cq;
        _li_Cq += Cd;
        _li_Cq *= _s2_Cq;
        _li_Cq += AE;
      }
      int Cq = _li_Cq << 2;
      float Cr;
      { // Begin Atomic Op
      Cr = atomicAdd_arr4_f32(Cq >> 2, Co);
      } // End Atomic Op
      int _li_Cs = 0;
      { // linear seek
        int _s0_Cs = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Cs = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Cs = _args_i32_[16 + 4 * 8 + 2];
        _li_Cs *= _s0_Cs;
        _li_Cs += AQ;
        _li_Cs *= _s1_Cs;
        _li_Cs += Cd;
        _li_Cs *= _s2_Cs;
        _li_Cs += AJ;
      }
      int Cs = _li_Cs << 2;
      float Ct;
      { // Begin Atomic Op
      Ct = atomicAdd_arr4_f32(Cs >> 2, Cp);
      } // End Atomic Op
      float Cu = Cc * Bq;
      int _li_Cv = 0;
      { // linear seek
        int _s0_Cv = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Cv = _args_i32_[16 + 5 * 8 + 1];
        _li_Cv *= _s0_Cv;
        _li_Cv += AQ;
        _li_Cv *= _s1_Cv;
        _li_Cv += Cd;
      }
      int Cv = _li_Cv << 2;
      float Cw;
      { // Begin Atomic Op
      Cw = atomicAdd_arr5_f32(Cv >> 2, Cu);
      } // End Atomic Op
      float Cx = float(2.0);
      float Cy = Cx - AV;
      float Cz = Cy * BG;
      float CA = B1 * Bg;
      int CB = int(2);
      int CC = AR + CB;
      float CD = _arr1_f32_[BL >> 2];
      float CE = CD * Bq;
      float CF = _arr1_f32_[BO >> 2];
      float CG = CF * Bq;
      float CH = Bu * Cz;
      float CI = BR + CH;
      float CJ = BC * Cz;
      float CK = BU + CJ;
      float CL = CE + CI;
      float CM = CG + CK;
      float CN = CA * CL;
      float CO = CA * CM;
      int _li_CP = 0;
      { // linear seek
        int _s0_CP = _args_i32_[16 + 4 * 8 + 0];
        int _s1_CP = _args_i32_[16 + 4 * 8 + 1];
        int _s2_CP = _args_i32_[16 + 4 * 8 + 2];
        _li_CP *= _s0_CP;
        _li_CP += AQ;
        _li_CP *= _s1_CP;
        _li_CP += CC;
        _li_CP *= _s2_CP;
        _li_CP += AE;
      }
      int CP = _li_CP << 2;
      float CQ;
      { // Begin Atomic Op
      CQ = atomicAdd_arr4_f32(CP >> 2, CN);
      } // End Atomic Op
      int _li_CR = 0;
      { // linear seek
        int _s0_CR = _args_i32_[16 + 4 * 8 + 0];
        int _s1_CR = _args_i32_[16 + 4 * 8 + 1];
        int _s2_CR = _args_i32_[16 + 4 * 8 + 2];
        _li_CR *= _s0_CR;
        _li_CR += AQ;
        _li_CR *= _s1_CR;
        _li_CR += CC;
        _li_CR *= _s2_CR;
        _li_CR += AJ;
      }
      int CR = _li_CR << 2;
      float CS;
      { // Begin Atomic Op
      CS = atomicAdd_arr4_f32(CR >> 2, CO);
      } // End Atomic Op
      float CT = CA * Bq;
      int _li_CU = 0;
      { // linear seek
        int _s0_CU = _args_i32_[16 + 5 * 8 + 0];
        int _s1_CU = _args_i32_[16 + 5 * 8 + 1];
        _li_CU *= _s0_CU;
        _li_CU += AQ;
        _li_CU *= _s1_CU;
        _li_CU += CC;
      }
      int CU = _li_CU << 2;
      float CV;
      { // Begin Atomic Op
      CV = atomicAdd_arr5_f32(CU >> 2, CT);
      } // End Atomic Op
      float CW = B3 - AU;
      float CX = CW * BG;
      float CY = B9 * B2;
      int CZ = AQ + AJ;
      float D0 = _arr1_f32_[BL >> 2];
      float D1 = D0 * Bq;
      float D2 = _arr1_f32_[BO >> 2];
      float D3 = D2 * Bq;
      float D4 = BB * CX;
      float D5 = D4 + BS;
      float D6 = Bx * CX;
      float D7 = D6 + BV;
      float D8 = D1 + D5;
      float D9 = D3 + D7;
      float Da = CY * D8;
      float Db = CY * D9;
      int _li_Dc = 0;
      { // linear seek
        int _s0_Dc = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Dc = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Dc = _args_i32_[16 + 4 * 8 + 2];
        _li_Dc *= _s0_Dc;
        _li_Dc += CZ;
        _li_Dc *= _s1_Dc;
        _li_Dc += AR;
        _li_Dc *= _s2_Dc;
        _li_Dc += AE;
      }
      int Dc = _li_Dc << 2;
      float Dd;
      { // Begin Atomic Op
      Dd = atomicAdd_arr4_f32(Dc >> 2, Da);
      } // End Atomic Op
      int _li_De = 0;
      { // linear seek
        int _s0_De = _args_i32_[16 + 4 * 8 + 0];
        int _s1_De = _args_i32_[16 + 4 * 8 + 1];
        int _s2_De = _args_i32_[16 + 4 * 8 + 2];
        _li_De *= _s0_De;
        _li_De += CZ;
        _li_De *= _s1_De;
        _li_De += AR;
        _li_De *= _s2_De;
        _li_De += AJ;
      }
      int De = _li_De << 2;
      float Df;
      { // Begin Atomic Op
      Df = atomicAdd_arr4_f32(De >> 2, Db);
      } // End Atomic Op
      float Dg = CY * Bq;
      int _li_Dh = 0;
      { // linear seek
        int _s0_Dh = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Dh = _args_i32_[16 + 5 * 8 + 1];
        _li_Dh *= _s0_Dh;
        _li_Dh += CZ;
        _li_Dh *= _s1_Dh;
        _li_Dh += AR;
      }
      int Dh = _li_Dh << 2;
      float Di;
      { // Begin Atomic Op
      Di = atomicAdd_arr5_f32(Dh >> 2, Dg);
      } // End Atomic Op
      float Dj = B9 * Ba;
      float Dk = _arr1_f32_[BL >> 2];
      float Dl = Dk * Bq;
      float Dm = _arr1_f32_[BO >> 2];
      float Dn = Dm * Bq;
      float Do = D4 + Ci;
      float Dp = D6 + Ck;
      float Dq = Dl + Do;
      float Dr = Dn + Dp;
      float Ds = Dj * Dq;
      float Dt = Dj * Dr;
      int _li_Du = 0;
      { // linear seek
        int _s0_Du = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Du = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Du = _args_i32_[16 + 4 * 8 + 2];
        _li_Du *= _s0_Du;
        _li_Du += CZ;
        _li_Du *= _s1_Du;
        _li_Du += Cd;
        _li_Du *= _s2_Du;
        _li_Du += AE;
      }
      int Du = _li_Du << 2;
      float Dv;
      { // Begin Atomic Op
      Dv = atomicAdd_arr4_f32(Du >> 2, Ds);
      } // End Atomic Op
      int _li_Dw = 0;
      { // linear seek
        int _s0_Dw = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Dw = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Dw = _args_i32_[16 + 4 * 8 + 2];
        _li_Dw *= _s0_Dw;
        _li_Dw += CZ;
        _li_Dw *= _s1_Dw;
        _li_Dw += Cd;
        _li_Dw *= _s2_Dw;
        _li_Dw += AJ;
      }
      int Dw = _li_Dw << 2;
      float Dx;
      { // Begin Atomic Op
      Dx = atomicAdd_arr4_f32(Dw >> 2, Dt);
      } // End Atomic Op
      float Dy = Dj * Bq;
      int _li_Dz = 0;
      { // linear seek
        int _s0_Dz = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Dz = _args_i32_[16 + 5 * 8 + 1];
        _li_Dz *= _s0_Dz;
        _li_Dz += CZ;
        _li_Dz *= _s1_Dz;
        _li_Dz += Cd;
      }
      int Dz = _li_Dz << 2;
      float DA;
      { // Begin Atomic Op
      DA = atomicAdd_arr5_f32(Dz >> 2, Dy);
      } // End Atomic Op
      float DB = B9 * Bg;
      float DC = _arr1_f32_[BL >> 2];
      float DD = DC * Bq;
      float DE = _arr1_f32_[BO >> 2];
      float DF = DE * Bq;
      float DG = D4 + CH;
      float DH = D6 + CJ;
      float DI = DD + DG;
      float DJ = DF + DH;
      float DK = DB * DI;
      float DL = DB * DJ;
      int _li_DM = 0;
      { // linear seek
        int _s0_DM = _args_i32_[16 + 4 * 8 + 0];
        int _s1_DM = _args_i32_[16 + 4 * 8 + 1];
        int _s2_DM = _args_i32_[16 + 4 * 8 + 2];
        _li_DM *= _s0_DM;
        _li_DM += CZ;
        _li_DM *= _s1_DM;
        _li_DM += CC;
        _li_DM *= _s2_DM;
        _li_DM += AE;
      }
      int DM = _li_DM << 2;
      float DN;
      { // Begin Atomic Op
      DN = atomicAdd_arr4_f32(DM >> 2, DK);
      } // End Atomic Op
      int _li_DO = 0;
      { // linear seek
        int _s0_DO = _args_i32_[16 + 4 * 8 + 0];
        int _s1_DO = _args_i32_[16 + 4 * 8 + 1];
        int _s2_DO = _args_i32_[16 + 4 * 8 + 2];
        _li_DO *= _s0_DO;
        _li_DO += CZ;
        _li_DO *= _s1_DO;
        _li_DO += CC;
        _li_DO *= _s2_DO;
        _li_DO += AJ;
      }
      int DO = _li_DO << 2;
      float DP;
      { // Begin Atomic Op
      DP = atomicAdd_arr4_f32(DO >> 2, DL);
      } // End Atomic Op
      float DQ = DB * Bq;
      int _li_DR = 0;
      { // linear seek
        int _s0_DR = _args_i32_[16 + 5 * 8 + 0];
        int _s1_DR = _args_i32_[16 + 5 * 8 + 1];
        _li_DR *= _s0_DR;
        _li_DR += CZ;
        _li_DR *= _s1_DR;
        _li_DR += CC;
      }
      int DR = _li_DR << 2;
      float DS;
      { // Begin Atomic Op
      DS = atomicAdd_arr5_f32(DR >> 2, DQ);
      } // End Atomic Op
      float DT = Cx - AU;
      float DU = DT * BG;
      float DV = Bf * B2;
      int DW = AQ + CB;
      float DX = _arr1_f32_[BL >> 2];
      float DY = DX * Bq;
      float DZ = _arr1_f32_[BO >> 2];
      float E0 = DZ * Bq;
      float E1 = BB * DU;
      float E2 = E1 + BS;
      float E3 = Bx * DU;
      float E4 = E3 + BV;
      float E5 = DY + E2;
      float E6 = E0 + E4;
      float E7 = DV * E5;
      float E8 = DV * E6;
      int _li_E9 = 0;
      { // linear seek
        int _s0_E9 = _args_i32_[16 + 4 * 8 + 0];
        int _s1_E9 = _args_i32_[16 + 4 * 8 + 1];
        int _s2_E9 = _args_i32_[16 + 4 * 8 + 2];
        _li_E9 *= _s0_E9;
        _li_E9 += DW;
        _li_E9 *= _s1_E9;
        _li_E9 += AR;
        _li_E9 *= _s2_E9;
        _li_E9 += AE;
      }
      int E9 = _li_E9 << 2;
      float Ea;
      { // Begin Atomic Op
      Ea = atomicAdd_arr4_f32(E9 >> 2, E7);
      } // End Atomic Op
      int _li_Eb = 0;
      { // linear seek
        int _s0_Eb = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Eb = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Eb = _args_i32_[16 + 4 * 8 + 2];
        _li_Eb *= _s0_Eb;
        _li_Eb += DW;
        _li_Eb *= _s1_Eb;
        _li_Eb += AR;
        _li_Eb *= _s2_Eb;
        _li_Eb += AJ;
      }
      int Eb = _li_Eb << 2;
      float Ec;
      { // Begin Atomic Op
      Ec = atomicAdd_arr4_f32(Eb >> 2, E8);
      } // End Atomic Op
      float Ed = DV * Bq;
      int _li_Ee = 0;
      { // linear seek
        int _s0_Ee = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Ee = _args_i32_[16 + 5 * 8 + 1];
        _li_Ee *= _s0_Ee;
        _li_Ee += DW;
        _li_Ee *= _s1_Ee;
        _li_Ee += AR;
      }
      int Ee = _li_Ee << 2;
      float Ef;
      { // Begin Atomic Op
      Ef = atomicAdd_arr5_f32(Ee >> 2, Ed);
      } // End Atomic Op
      float Eg = Bf * Ba;
      float Eh = _arr1_f32_[BL >> 2];
      float Ei = Eh * Bq;
      float Ej = _arr1_f32_[BO >> 2];
      float Ek = Ej * Bq;
      float El = E1 + Ci;
      float Em = E3 + Ck;
      float En = Ei + El;
      float Eo = Ek + Em;
      float Ep = Eg * En;
      float Eq = Eg * Eo;
      int _li_Er = 0;
      { // linear seek
        int _s0_Er = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Er = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Er = _args_i32_[16 + 4 * 8 + 2];
        _li_Er *= _s0_Er;
        _li_Er += DW;
        _li_Er *= _s1_Er;
        _li_Er += Cd;
        _li_Er *= _s2_Er;
        _li_Er += AE;
      }
      int Er = _li_Er << 2;
      float Es;
      { // Begin Atomic Op
      Es = atomicAdd_arr4_f32(Er >> 2, Ep);
      } // End Atomic Op
      int _li_Et = 0;
      { // linear seek
        int _s0_Et = _args_i32_[16 + 4 * 8 + 0];
        int _s1_Et = _args_i32_[16 + 4 * 8 + 1];
        int _s2_Et = _args_i32_[16 + 4 * 8 + 2];
        _li_Et *= _s0_Et;
        _li_Et += DW;
        _li_Et *= _s1_Et;
        _li_Et += Cd;
        _li_Et *= _s2_Et;
        _li_Et += AJ;
      }
      int Et = _li_Et << 2;
      float Eu;
      { // Begin Atomic Op
      Eu = atomicAdd_arr4_f32(Et >> 2, Eq);
      } // End Atomic Op
      float Ev = Eg * Bq;
      int _li_Ew = 0;
      { // linear seek
        int _s0_Ew = _args_i32_[16 + 5 * 8 + 0];
        int _s1_Ew = _args_i32_[16 + 5 * 8 + 1];
        _li_Ew *= _s0_Ew;
        _li_Ew += DW;
        _li_Ew *= _s1_Ew;
        _li_Ew += Cd;
      }
      int Ew = _li_Ew << 2;
      float Ex;
      { // Begin Atomic Op
      Ex = atomicAdd_arr5_f32(Ew >> 2, Ev);
      } // End Atomic Op
      float Ey = Bf * Bg;
      float Ez = _arr1_f32_[BL >> 2];
      float EA = Ez * Bq;
      float EB = _arr1_f32_[BO >> 2];
      float EC = EB * Bq;
      float ED = E1 + CH;
      float EE = E3 + CJ;
      float EF = EA + ED;
      float EG = EC + EE;
      float EH = Ey * EF;
      float EI = Ey * EG;
      int _li_EJ = 0;
      { // linear seek
        int _s0_EJ = _args_i32_[16 + 4 * 8 + 0];
        int _s1_EJ = _args_i32_[16 + 4 * 8 + 1];
        int _s2_EJ = _args_i32_[16 + 4 * 8 + 2];
        _li_EJ *= _s0_EJ;
        _li_EJ += DW;
        _li_EJ *= _s1_EJ;
        _li_EJ += CC;
        _li_EJ *= _s2_EJ;
        _li_EJ += AE;
      }
      int EJ = _li_EJ << 2;
      float EK;
      { // Begin Atomic Op
      EK = atomicAdd_arr4_f32(EJ >> 2, EH);
      } // End Atomic Op
      int _li_EL = 0;
      { // linear seek
        int _s0_EL = _args_i32_[16 + 4 * 8 + 0];
        int _s1_EL = _args_i32_[16 + 4 * 8 + 1];
        int _s2_EL = _args_i32_[16 + 4 * 8 + 2];
        _li_EL *= _s0_EL;
        _li_EL += DW;
        _li_EL *= _s1_EL;
        _li_EL += CC;
        _li_EL *= _s2_EL;
        _li_EL += AJ;
      }
      int EL = _li_EL << 2;
      float EM;
      { // Begin Atomic Op
      EM = atomicAdd_arr4_f32(EL >> 2, EI);
      } // End Atomic Op
      float EN = Ey * Bq;
      int _li_EO = 0;
      { // linear seek
        int _s0_EO = _args_i32_[16 + 5 * 8 + 0];
        int _s1_EO = _args_i32_[16 + 5 * 8 + 1];
        _li_EO *= _s0_EO;
        _li_EO += DW;
        _li_EO *= _s1_EO;
        _li_EO += CC;
      }
      int EO = _li_EO << 2;
      float EP;
      { // Begin Atomic Op
      EP = atomicAdd_arr5_f32(EO >> 2, EN);
      } // End Atomic Op
  }
}

void main()
{
  substep_c54_03();
}
